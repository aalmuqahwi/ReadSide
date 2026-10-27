using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace ReadSide;

internal static class SqlTranslator
{
    public static string ToSql(Expression node, Dictionary<string, object?> parameters)
    {
        if (node is ConstantExpression constant)
        {
            var name = $"p{parameters.Count}";
            parameters[name] = constant.Value;
            return $"@{name}";
        }

        if (IsColumn(node, out var boolColumn)&& boolColumn.Type == typeof(bool))
        {
            return $"({Quote(boolColumn.Member.Name)} = 1)";
        }

        if (node is MemberExpression member)
        {
            if (member.Expression is ParameterExpression)
            {
                return Quote(member.Member.Name);
            }

            var value = Expression.Lambda(member).Compile().DynamicInvoke();

            var paramName = $"p{parameters.Count}";
            parameters[paramName] = value;

            return $"@{paramName}";
        }

        if (node is BinaryExpression binary)
        {
            if (IsNull(binary.Right))
            {
                var column = ToOperand(binary.Left, parameters);

                if (binary.NodeType == ExpressionType.Equal)
                    return $"({column} IS NULL)";

                if (binary.NodeType == ExpressionType.NotEqual)
                    return $"({column} IS NOT NULL)";
            }


            var isLogical = binary.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse;
            var left = isLogical ? ToSql(binary.Left, parameters) : ToOperand(binary.Left, parameters);
            var right = isLogical ? ToSql(binary.Right, parameters) : ToOperand(binary.Right, parameters);
            var op = GetOperator(binary);

            return $"({left} {op} {right})";
        }

        if (node is UnaryExpression unary && unary.NodeType == ExpressionType.Not)
        {
            return $"NOT {ToSql(unary.Operand, parameters)}";
        }

        if (node is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } conversion)
        {
            return ToSql(conversion.Operand, parameters);
        }

        if (node is NewExpression created)
        {
            var columns = created.Arguments.Select((argument, index) =>
            {
                var column = ToOperand(argument, parameters);
                var alias = created.Members?[index].Name;

                var isPlainColumn = argument is MemberExpression { Expression: ParameterExpression } access
                                    && access.Member.Name == alias;

                return alias is null || isPlainColumn
                    ? column
                    : $"{column} AS {Quote(alias)}";
            });

            return string.Join(", ", columns);
        }

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

        throw new NotSupportedException($"Cannot translate '{node}' to SQL (node type: {node.NodeType}).");
    }

    public static string ToOrderBy(IEnumerable<(Expression Column, bool Descending)> clauses)
    {
        var columns = clauses.Select(c => $"{ToOperand(c.Column, [])} {(c.Descending ? "DESC" : "ASC")}");
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

    internal static string ToOperand(Expression node, Dictionary<string, object?> parameters)
    {
        return IsColumn(node, out var column) 
            ? Quote(column.Member.Name) 
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
