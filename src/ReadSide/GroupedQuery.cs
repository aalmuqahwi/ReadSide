using System.Linq.Expressions;

namespace ReadSide;

/// <summary>A grouped query with its select list. It can only be built.</summary>
/// <typeparam name="T">The entity being grouped.</typeparam>
public sealed class GroupedQuery<T>
{
    private readonly ReadSideOptions _options;
    private readonly IReadOnlyList<Expression> _conditions;
    private readonly Expression _key;
    private readonly Expression _columns;
    private readonly ParameterExpression _keyParameter;
    private readonly IReadOnlyList<LambdaExpression> _having;

    internal GroupedQuery(
        ReadSideOptions options,
        IReadOnlyList<Expression> conditions,
        Expression key,
        Expression columns,
        ParameterExpression keyParameter,
        IReadOnlyList<LambdaExpression> having)
    {
        _options = options;
        _conditions = conditions;
        _key = key;
        _columns = columns;
        _keyParameter = keyParameter;
        _having = having;
    }

    /// <summary>Builds the SQL Server statement and its parameters.</summary>
    /// <returns>The SQL text and the parameter values.</returns>
    public SqlResult Build()
    {
        var parameters = new Dictionary<string, object?>();
        var translator = new SqlTranslator(_options);

        // In the order they appear in the SQL, so parameters are numbered the same way.
        var rewritten = new KeyReplacer(_keyParameter, _key).Visit(_columns)!;
        var columns = translator.ToColumnOrValue(rewritten, parameters);
        var fromClause = translator.ToFrom(typeof(T));
        var whereClause = translator.ToWhere(_conditions, parameters);
        var groupByClause = " GROUP BY " + translator.ToColumnOrValue(_key, parameters);

        var havingConditions = _having
            .Select(h => new KeyReplacer(h.Parameters[0], _key).Visit(h.Body)!)
            .ToList();
            
        var havingClause = translator.ToHaving(havingConditions, parameters);

        var sql = $"SELECT {columns} FROM {fromClause}{whereClause}{groupByClause}{havingClause}";

        return new SqlResult(sql, parameters);
    }
}