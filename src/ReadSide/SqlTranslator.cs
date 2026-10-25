using System;
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
            if (binary.Right is ConstantExpression { Value: null })
            {
                var column = ToSql(binary.Left, parameters);

                if (binary.NodeType == ExpressionType.Equal)
                    return $"({column} IS NULL)";

                if (binary.NodeType == ExpressionType.NotEqual)
                    return $"({column} IS NOT NULL)";
            }


            var left = ToSql(binary.Left, parameters);
            var right = ToSql(binary.Right, parameters);
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
                var column = ToSql(argument, parameters);
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
            && call.Method.Name == "Contains")
        {
            var column = ToSql(call.Object!, parameters);
            var value = GetValue(call.Arguments[0]);

            var name = $"p{parameters.Count}";
            parameters[name] = $"%{value}%";

            return $"({column} LIKE @{name})";
        }

        throw new NotSupportedException($"Cannot translate '{node}' to SQL (node type: {node.NodeType}).");
    }

    public static string ToOrderBy(IEnumerable<(Expression Column, bool Descending)> clauses)
    {
        var columns = clauses.Select(c => $"{ToSql(c.Column, [])} {(c.Descending ? "DESC" : "ASC")}");
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
        if(expression is ConstantExpression constant)
        {
            return constant.Value;
        }

        return Expression.Lambda(expression).Compile().DynamicInvoke();
    }
}
