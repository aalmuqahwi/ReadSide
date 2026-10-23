using System.Linq.Expressions;

namespace ReadSide;

public sealed class Query<T>
{
    private readonly IReadOnlyList<Expression> _conditions;

    internal Query() : this([])
    {
    }

    private Query(IReadOnlyList<Expression> conditions)
    {
        _conditions = conditions;
    }

    public Query<T> Where(Expression<Func<T, bool>> predicate)
        => new([.. _conditions, predicate.Body]);

    public SqlResult Build()
    {
        var parameters = new Dictionary<string, object?>();
        var table = typeof(T).Name + "s";
        var sql = $"SELECT * FROM {table}";

        if (_conditions.Count > 0)
        {
            var conditions = _conditions.Select(c => SqlTranslator.ToSql(c, parameters));
            sql += " WHERE " + string.Join(" AND ", conditions);
        }

        return new SqlResult(sql, parameters);
    }
}
