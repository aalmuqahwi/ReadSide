using System.Linq.Expressions;

namespace ReadSide;

public sealed class Query<T>
{
    private sealed record State(
        IReadOnlyList<Expression> Conditions,
        Expression? Columns,
        IReadOnlyList<(Expression Column, bool Descending)> Orders);

    private readonly State _state;

    internal Query() : this(new State([], null, []))
    {
    }

    private Query(State state)
    {
        _state = state;
    }

    public Query<T> Where(Expression<Func<T, bool>> predicate)
        => new(_state with { Conditions = [.. _state.Conditions, predicate.Body] });

    public Query<T> Select<TResult>(Expression<Func<T, TResult>> selector)
        => new(_state with { Columns = selector.Body });

    public Query<T> OrderBy<TKey>(Expression<Func<T, TKey>> key)
        => new(_state with { Orders = [(key.Body, false)] });

    public Query<T> ThenBy<TKey>(Expression<Func<T, TKey>> key)
        => new(_state with { Orders = [.. _state.Orders, (key.Body, false)] });

    public Query<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> key)
    => new(_state with { Orders = [(key.Body, true)] });

    public Query<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> key)
        => new(_state with { Orders = [.. _state.Orders, (key.Body, true)] });

    public SqlResult Build()
    {
        var parameters = new Dictionary<string, object?>();

        var columns = _state.Columns is null
            ? "*"
            : SqlTranslator.ToSql(_state.Columns, parameters);

        var sql = $"SELECT {columns} FROM {typeof(T).Name}s";

        if (_state.Conditions.Count > 0)
        {
            var conditions = _state.Conditions.Select(c => SqlTranslator.ToSql(c, parameters));
            sql += " WHERE " + string.Join(" AND ", conditions);
        }

        if (_state.Orders.Count > 0)
        {
            sql += " ORDER BY " + SqlTranslator.ToOrderBy(_state.Orders);
        }

        return new SqlResult(sql, parameters);
    }
}
