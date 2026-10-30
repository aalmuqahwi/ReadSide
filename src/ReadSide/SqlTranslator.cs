using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

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
            if (IsNull(binary.Right))
            {
                var column = ToColumnOrValue(binary.Left, parameters);

                if (binary.NodeType == ExpressionType.Equal)
                    return $"({column} IS NULL)";

                if (binary.NodeType == ExpressionType.NotEqual)
                    return $"({column} IS NOT NULL)";
            }


            var isLogical = binary.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse;
            var left = isLogical ? ToSql(binary.Left, parameters) : ToColumnOrValue(binary.Left, parameters);
            var right = isLogical ? ToSql(binary.Right, parameters) : ToColumnOrValue(binary.Right, parameters);
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

        throw new NotSupportedException($"Cannot translate '{node}' to SQL (node type: {node.NodeType}).");
    }

    public string ToOrderBy(IEnumerable<(Expression Column, bool Descending)> clauses)
    {
        var columns = clauses.Select(c => $"{ToColumnOrValue(c.Column, [])} {(c.Descending ? "DESC" : "ASC")}");
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

    internal static string Quote(string name) => $"[{name.Replace("]", "]]")}]";

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
        _ => throw new NotSupportedException($"Cannot translate '{binary}' to SQL (operator: {binary.NodeType}).")
    };

    private static object? GetValue(Expression expression)
    {
        if (expression is ConstantExpression constant)
        {
            return constant.Value;
        }

        return Expression.Lambda(expression).Compile().DynamicInvoke();
    }

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

    private string ColumnName(MemberExpression column)
    {
        var entity = _options.Find(column.Expression!.Type);
        return entity?.Columns.GetValueOrDefault(column.Member.Name) ?? column.Member.Name;
    }

    private string ToSelectItem(Expression expression, string? alias, Dictionary<string, object?> parameters)
    {
        var column = ToColumnOrValue(expression, parameters);

        var isPlainColumn = IsColumn(expression, out var access)
            && ColumnName(access) == alias;

        return alias is null || isPlainColumn
            ? column
            : $"{column} AS {Quote(alias)}";
    }

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

    internal string ToColumnOrValue(Expression node, Dictionary<string, object?> parameters)
    {
        return IsColumn(node, out var column)
            ? Quote(ColumnName(column))
            : ToSql(node, parameters);
    }

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

    private static string EscapeLike(string value) => value
        .Replace("\\", "\\\\")
        .Replace("%", "\\%")
        .Replace("_", "\\_")
        .Replace("[", "\\[");
}
