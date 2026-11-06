using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace ReadSide;

internal sealed class SqlTranslator
{
    private readonly ReadSideOptions _options;

    public SqlTranslator(ReadSideOptions options)
    {
        _options = options;
    }

    // Branches are checked in order and the first match wins.
    // Anything that matches none of them reaches the exception at the end.
    public string ToSql(Expression node, Dictionary<string, object?> parameters)
    {
        // A literal value like 5 or "dune": becomes a parameter.
        if (node is ConstantExpression constant)
        {
            var name = $"p{parameters.Count}";
            parameters[name] = constant.Value;
            return $"@{name}";
        }

        // A column of the lambda parameter, like b.Title. A bool column on its own is a condition: (Name = 1).
        if (IsColumn(node, out var columnMember))
        {
            return columnMember.Type == typeof(bool)
                ? $"({Quote(ColumnName(columnMember))} = 1)"
                : Quote(ColumnName(columnMember));
        }

        // .HasValue on a nullable column becomes IS NOT NULL
        if (node is MemberExpression { Member.Name: "HasValue", Expression: { } owner }
            && Nullable.GetUnderlyingType(owner.Type) is not null
            && IsColumn(owner, out var nullableColumn))
        {
            return $"({Quote(ColumnName(nullableColumn))} IS NOT NULL)";
        }

        // .Value on a nullable column is just the column: SQL has no separate "value".
        if (node is MemberExpression { Member.Name: "Value", Expression: { } valueOwner }
            && Nullable.GetUnderlyingType(valueOwner.Type) is not null
            && IsColumn(valueOwner, out var valueColumn))
        {
            return Quote(ColumnName(valueColumn));
        }

        // DateTime.Now / DateTime.UtcNow: the database clock, not a client value
        if (node is MemberExpression { Expression: null } clock
            && clock.Member.DeclaringType == typeof(DateTime)
            && clock.Member.Name is "Now" or "UtcNow")
        {
            return clock.Member.Name == "Now"
                ? "GETDATE()"
                : "GETUTCDATE()";
        }

        // A captured variable, like minId: evaluate it and make it a parameter.
        if (node is MemberExpression member)
        {
            var value = Expression.Lambda(member).Compile().DynamicInvoke();

            var paramName = $"p{parameters.Count}";
            parameters[paramName] = value;

            return $"@{paramName}";
        }

        // Comparisons and AND/OR. Comparing to null becomes IS NULL / IS NOT NULL.
        if (node is BinaryExpression binary)
        {
            string left;
            string right;

            if(binary.NodeType == ExpressionType.Coalesce)
            {
                left = ToColumnOrValue(binary.Left, parameters);
                right = ToColumnOrValue(binary.Right, parameters);
                return $"COALESCE({left}, {right})";
            }

            if (IsNull(binary.Right))
            {
                var column = ToColumnOrValue(binary.Left, parameters);

                if (binary.NodeType == ExpressionType.Equal)
                    return $"({column} IS NULL)";

                if (binary.NodeType == ExpressionType.NotEqual)
                    return $"({column} IS NOT NULL)";
            }

            var isLogical = binary.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse;
            left = isLogical ? ToSql(binary.Left, parameters) : ToColumnOrValue(binary.Left, parameters);
            right = isLogical ? ToSql(binary.Right, parameters) : ToColumnOrValue(binary.Right, parameters);
            var op = GetOperator(binary);

            return $"({left} {op} {right})";
        }

        // !condition becomes NOT (...).
        if (node is UnaryExpression unary && unary.NodeType == ExpressionType.Not)
        {
            return $"NOT {ToSql(unary.Operand, parameters)}";
        }

        // A type conversion the compiler adds on its own. For example, with `int? Rating`
        // and `int minRating`, the lambda b.Rating == minRating really contains
        // Convert(minRating) to int?. SQL doesn't need it, so skip it and translate what is inside.
        if (node is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } conversion)
        {
            return ToSql(conversion.Operand, parameters);
        }

        // new { b.Title, b.Id } in Select: a column list, with AS when the name differs.
        if (node is NewExpression created)
        {
            var columns = created.Arguments.Select((argument, index) =>
                ToSelectItem(argument, created.Members?[index].Name, parameters));

            return string.Join(", ", columns);
        }

        // new Dto { Id = b.Id, Name = b.Title } in Select: the same column list, with the names taken from the bindings.
        if (node is MemberInitExpression init)
        {
            var columns = init.Bindings.Select(binding => binding is MemberAssignment assignment
                ? ToSelectItem(assignment.Expression, assignment.Member.Name, parameters)
                : throw new NotSupportedException($"Cannot translate '{binding}' to SQL (binding type: {binding.BindingType})."));

            return string.Join(", ", columns);
        }

        // string.Contains / StartsWith / EndsWith becomes LIKE, with %, _ and [ escaped.
        if (node is MethodCallExpression call
            && call.Method.DeclaringType == typeof(string)
            && call.Method.Name is "Contains" or "StartsWith" or "EndsWith")
        {
            var column = ToSql(call.Object!, parameters);
            var value = EscapeLike(GetValue(call.Arguments[0])?.ToString() ?? "");

            var pattern = call.Method.Name switch
            {
                "StartsWith" => $"{value}%",
                "EndsWith" => $"%{value}",
                _ => $"%{value}%"
            };

            var name = $"p{parameters.Count}";
            parameters[name] = pattern;

            return $"({column} LIKE @{name} ESCAPE '\\')";
        }

        // Contains on a list or an array becomes IN (...). An empty list becomes (1 = 0).
        if (node is MethodCallExpression inCall
            && inCall.Method.Name == "Contains"
            && inCall.Arguments.Count is 1 or 2)
        {
            // List: the collection is the Object, the item is Arguments[0].
            // Array: static call, the collection is Arguments[0], the item is Arguments[1].
            var (collection, item) = inCall.Object is not null
                ? (inCall.Object, inCall.Arguments[0])
                : (inCall.Arguments[0], inCall.Arguments[1]);

            var values = (IEnumerable)GetValue(StripConversions(collection))!;
            var column = ToSql(item, parameters);

            var names = new List<string>();
            foreach (var value in values)
            {
                var name = $"p{parameters.Count}";
                parameters[name] = value;
                names.Add($"@{name}");
            }

            if (names.Count == 0)
            {
                // always false condition
                return "(1 = 0)";
            }

            return $"({column} IN ({string.Join(", ", names)}))";
        }

        // string.IsNullOrEmpty(x) becomes (x IS NULL OR x = '').
        if (node is MethodCallExpression emptyCheck
            && emptyCheck.Method.DeclaringType == typeof(string)
            && emptyCheck.Method.Name == "IsNullOrEmpty")
        {
            var column = ToSql(emptyCheck.Arguments[0], parameters);
            return $"({column} IS NULL OR {column} = '')";
        }

        // .AddDays becomes DATEADD
        if (node is MethodCallExpression dateCall
            && dateCall.Method.DeclaringType == typeof(DateTime)
            && TryGetDatePart(dateCall.Method.Name, out var datePart))
        {
            var column = ToSql(dateCall.Object!, parameters);
            var value = GetValue(dateCall.Arguments[0]);

            var name = $"p{parameters.Count}";
            parameters[name] = value;

            return $"DATEADD({datePart}, @{name}, {column})";
        }

        throw new NotSupportedException($"Cannot translate '{node}' to SQL (node type: {node.NodeType}).");
    }

    public string ToOrderBy(IEnumerable<(Expression Column, bool Descending)> clauses, Dictionary<string, object?> parameters)
    {
        var columns = clauses.Select(c => $"{ToSortKey(c.Column, parameters)} {(c.Descending ? "DESC" : "ASC")}");
        return string.Join(", ", columns);
    }

    public static string ToPaging(int skip, int take, Dictionary<string, object?> parameters)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfLessThan(take, 1);

        var skipName = $"p{parameters.Count}";
        parameters[skipName] = skip;

        var takeName = $"p{parameters.Count}";
        parameters[takeName] = take;

        return $"OFFSET @{skipName} ROWS FETCH NEXT @{takeName} ROWS ONLY";
    }

    // Wraps a name in square brackets so SQL Server treats it as one name.
    // A closing bracket inside the name is doubled.
    //   "Title"    → [Title]
    //   "My]Col"   → [My]]Col]
    internal static string Quote(string name) => $"[{name.Replace("]", "]]")}]";

    // The SQL operator for a binary node. Anything not listed fails.
    //   Equal       → "="        GreaterThanOrEqual → ">="
    //   AndAlso     → "AND"      Add                → "+"
    //   Modulo      → "%"
    //   LeftShift   → throws "Cannot translate '...' to SQL (operator: LeftShift)."
    private static string GetOperator(BinaryExpression binary) => binary.NodeType switch
    {
        ExpressionType.Equal => "=",
        ExpressionType.NotEqual => "<>",
        ExpressionType.GreaterThan => ">",
        ExpressionType.GreaterThanOrEqual => ">=",
        ExpressionType.LessThan => "<",
        ExpressionType.LessThanOrEqual => "<=",
        ExpressionType.AndAlso => "AND",
        ExpressionType.OrElse => "OR",
        ExpressionType.Add => "+",
        ExpressionType.Subtract => "-",
        ExpressionType.Multiply => "*",
        ExpressionType.Divide => "/",
        ExpressionType.Modulo => "%",
        _ => throw new NotSupportedException($"Cannot translate '{binary}' to SQL (operator: {binary.NodeType}).")
    };

    // The actual value of an expression that does not involve the lambda parameter.
    // A constant is returned as it is. Anything else is compiled and run.
    //   5                      → 5
    //   minId (captured)       → the current value of minId
    //   DateTime.Now           → the current client time
    //   b.Title                → fails: it needs b, which does not exist here
    private static object? GetValue(Expression expression)
    {
        if (expression is ConstantExpression constant)
        {
            return constant.Value;
        }

        return Expression.Lambda(expression).Compile().DynamicInvoke();
    }

    // Is this node a column of the entity, like b.Title?
    // Yes only when it is a member access whose owner is the lambda parameter itself.
    //   b.Title                → true  (column = b.Title)
    //   b.ReturnedAt.Value     → false (the owner is b.ReturnedAt, not b)
    //   minId (captured)       → false
    //   5                      → false
    private static bool IsColumn(Expression node, [NotNullWhen(true)] out MemberExpression? column)
    {
        if (node is MemberExpression { Expression: ParameterExpression } member)
        {
            column = member;
            return true;
        }

        column = null;
        return false;
    }

    // Turns a DateTime method name into the unit DATEADD expects.
    //   "AddDays"    → true, datePart = "day"
    //   "AddMonths"  → true, datePart = "month"
    //   "ToString"   → false
    private static bool TryGetDatePart(string methodName, [NotNullWhen(true)] out string? datePart)
    {
        datePart = methodName switch
        {
            "AddDays" => "day",
            "AddHours" => "hour",
            "AddMinutes" => "minute",
            "AddSeconds" => "second",
            "AddMonths" => "month",
            "AddYears" => "year",
            _ => null
        };

        return datePart is not null;
    }

    //   Title  (mapped to Title_Txt)       → "Title_Txt"
    //   Id     (not mapped)                → "Id"
    //   Label  (marked with Ignore)        → throws "'Label' is ignored in the mapping of Article..."
    private string ColumnName(MemberExpression column)
    {
        var entityType = column.Expression!.Type;
        var entity = _options.Find(entityType);

        return entity is not null && entity.Ignored.Contains(column.Member.Name)
            ? throw new NotSupportedException(
                $"'{column.Member.Name}' is ignored in the mapping of {entityType.Name}, so it cannot be used in a query.")
            : entity?.Columns.GetValueOrDefault(column.Member.Name) ?? column.Member.Name;
    }

    // One item of a SELECT list: the value, plus AS when the name must change.
    //   (b.Title, "Title")                  → [Title]
    //   (b.Title, "Name")                   → [Title] AS [Name]
    //   (b.Title, "Title"), Title mapped    → [Title_Txt] AS [Title]
    //   (b.Id * 2, "Double")                → ([Id] * @p0) AS [Double]
    //   (b.Title, null)                     → [Title]
    private string ToSelectItem(Expression expression, string? alias, Dictionary<string, object?> parameters)
    {
        var column = ToColumnOrValue(expression, parameters);

        var isPlainColumn = IsColumn(expression, out var access)
            && ColumnName(access) == alias;

        return alias is null || isPlainColumn
            ? column
            : $"{column} AS {Quote(alias)}";
    }

    // Is this the value null, written as null or as a captured variable that holds null?
    //   null (constant)             → true
    //   string? x = null; x         → true
    //   5                           → false
    //   b.Title                     → false
    private static bool IsNull(Expression expression)
    {
        if (expression is ConstantExpression constant)
        {
            return constant.Value is null;
        }

        if (expression is MemberExpression { Expression: not ParameterExpression } captured)
        {
            return GetValue(captured) is null;
        }

        return false;
    }

    // A column becomes its quoted name; anything else is translated by ToSql.
    // A bool column stays [IsAvailable] here, without "= 1".
    //   b.Title        → [Title]
    //   5              → @p0
    //   b.Id * 2       → ([Id] * @p0)
    //   b.IsAvailable  → [IsAvailable]
    internal string ToColumnOrValue(Expression node, Dictionary<string, object?> parameters)
    {
        return IsColumn(node, out var column)
            ? Quote(ColumnName(column))
            : ToSql(node, parameters);
    }

    // The column list for a query with no Select.
    // No column renames in the mapping → "*". Otherwise every readable property, in declaration order,
    // skipping ignored ones, with AS when the column name differs from the property name.
    //   no mapping                       → *
    //   Title mapped to Title_Txt        → [Id], [Title_Txt] AS [Title], [PublishedAt], ...
    internal string ToDefaultColumns(Type entity)
    {
        var map = _options.Find(entity);

        if (map is null || map.Columns.Count == 0)
        {
            return "*";
        }

        var properties = entity
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead && !map.Ignored.Contains(property.Name))
            .OrderBy(property => property.MetadataToken);

        var columns = properties.Select(property =>
        {
            var column = map.Columns.GetValueOrDefault(property.Name) ?? property.Name;

            return column == property.Name
                ? Quote(column)
                : $"{Quote(column)} AS {Quote(property.Name)}";
        });

        return string.Join(", ", columns);
    }

    // Removes the wrappers the compiler adds: Convert (also checked) and implicit-conversion calls.
    // Repeats until nothing is left to remove.
    //   (object)b.IsAvailable          → b.IsAvailable
    //   Convert(Convert(x))            → x
    //   op_Implicit(x)                 → x
    //   b.Title                        → b.Title (unchanged)
    private static Expression StripConversions(Expression expression)
    {
        while (true)
        {
            if (expression is UnaryExpression
                { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
            {
                expression = unary.Operand;
            }
            else if (expression is MethodCallExpression
            { Method.Name: "op_Implicit", Arguments.Count: 1 } conversion)
            {
                expression = conversion.Arguments[0];
            }
            else
            {
                return expression;
            }
        }
    }

    // Makes the special characters of LIKE ordinary, so "50%" matches a literal 50%.
    // The backslash is the escape character (see ESCAPE '\' in the query).
    //   "50%"    → "50\%"
    //   "a_b"    → "a\_b"
    //   "[x]"    → "\[x]"
    //   "a\b"    → "a\\b"
    private static string EscapeLike(string value) => value
        .Replace("\\", "\\\\")
        .Replace("%", "\\%")
        .Replace("_", "\\_")
        .Replace("[", "\\[");

    // Checks that one ORDER BY key is something SQL Server can sort by, then translates it.
    //   b.Title                      → [Title]
    //   b.ReturnedAt.Value           → [ReturnedAt]
    //   b.PublishedAt.AddDays(1)     → DATEADD(day, @p0, [PublishedAt])
    //   new { b.Title, b.Id }        → throws "...Use ThenBy..."
    //   b.Title == "x"               → throws "Cannot order by..."
    private string ToSortKey(Expression node, Dictionary<string, object?> parameters)
    {
        // new { a, b } is a list of keys, and one ASC or DESC cannot cover a list.
        if (node is NewExpression or MemberInitExpression)
        {
            throw new NotSupportedException($"Cannot order by '{node}'. Use ThenBy to sort by more than one column.");
        }

        var key = StripConversions(node);

        return IsSortable(key)
            ? ToColumnOrValue(key, parameters)
            : throw new NotSupportedException($"Cannot order by '{node}'. Use a column, arithmetic on a column, or a DateTime Add method.");
    }

    // Can SQL Server sort by this? A column, .Value of a nullable column, a DateTime Add method,
    // or arithmetic where at least one side can be sorted by (so b.Id * 2 yes, minId + 1 no).
    private static bool IsSortable(Expression key)
    {
        key = StripConversions(key);

        return IsColumn(key, out _)
            || (key is MemberExpression { Member.Name: "Value", Expression: { } owner } && IsColumn(owner, out _))
            || (key is MethodCallExpression { Method: var method }
                && method.DeclaringType == typeof(DateTime)
                && TryGetDatePart(method.Name, out _))
            || (key is BinaryExpression
            {
                NodeType: ExpressionType.Add or ExpressionType.Subtract or ExpressionType.Multiply
                        or ExpressionType.Divide or ExpressionType.Modulo or ExpressionType.Coalesce
            } combined
                && (IsSortable(combined.Left) || IsSortable(combined.Right)));
    }
}
