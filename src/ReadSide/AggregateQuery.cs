using System.Linq.Expressions;

namespace ReadSide;

/// <summary>A query that returns aggregate values, such as <c>SELECT COUNT(*)</c>. It can only be built.</summary>
/// <typeparam name="T">The entity the query reads from.</typeparam>
public sealed class AggregateQuery<T>
{
    private readonly ReadSideOptions _options;
    private readonly IReadOnlyList<Expression> _conditions;
    private readonly string _columns;

    internal AggregateQuery(
        ReadSideOptions options,
        IReadOnlyList<Expression> conditions,
        string columns)
    {
        _options = options;
        _conditions = conditions;
        _columns = columns;
    }

    /// <summary>Builds the SQL Server statement and its parameters.</summary>
    /// <returns>The SQL text and the parameter values.</returns>
    /// <exception cref="NotSupportedException">A condition contains an expression that can't be translated.</exception>
    public SqlResult Build()
    {
        var parameters = new Dictionary<string, object?>();
        var translator = new SqlTranslator(_options);
        var fromClause = translator.ToFrom(typeof(T));
        var whereClause = translator.ToWhere(_conditions, parameters);

        var sql = $"SELECT {_columns} FROM {fromClause}{whereClause}";

        return new SqlResult(sql, parameters);
    }
}
