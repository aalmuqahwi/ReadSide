using System;
using System.Linq.Expressions;

namespace ReadSide;

public static class SqlTranslator
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
                return member.Member.Name;
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
            var op = GetOperator(binary.NodeType);

            return $"({left} {op} {right})";
        }

        if (node is UnaryExpression unary && unary.NodeType == ExpressionType.Not)
        {
            return $"NOT {ToSql(unary.Operand, parameters)}";
        }

        if(node is NewExpression created)
        {
            var columns = created.Arguments.Select(argument => ToSql(argument, parameters));
            return string.Join(", ", columns);
        }

        throw new NotSupportedException($"Unsupported node: {node.NodeType}");
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

    public static string GetOperator(ExpressionType type) => type switch
    {
        ExpressionType.Equal => "=",
        ExpressionType.NotEqual => "<>",
        ExpressionType.GreaterThan => ">",
        ExpressionType.GreaterThanOrEqual => ">=",
        ExpressionType.LessThan => "<",
        ExpressionType.LessThanOrEqual => "<=",
        ExpressionType.AndAlso => "AND",
        ExpressionType.OrElse => "OR",
        _ => throw new NotSupportedException($"Unsupported operator: {type}")
    };
}
