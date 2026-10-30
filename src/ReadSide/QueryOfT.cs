using System.Linq.Expressions;

namespace ReadSide;

/// <summary>An immutable query. Every method returns a new query and leaves the original unchanged.</summary>
/// <typeparam name="T">The class that describes the table.</typeparam>
public sealed class Query<T>
{
    private sealed record State(
        ReadSideOptions Options,
        IReadOnlyList<Expression> Conditions,
        Expression? Columns,
        IReadOnlyList<(Expression Column, bool Descending)> Orders,
        int? Skip,
        int? Take)
    {
        public static State Empty(ReadSideOptions options) => new(
            Options: options,
            Conditions: [],
            Columns: null,
            Orders: [],
            Skip: null,
            Take: null);
    }

    private readonly State _state;

    internal Query() : this(new ReadSideOptions())
    {
    }

    internal Query(ReadSideOptions options) : this(State.Empty(options))
    {
        
    }

    private Query(State state)
    {
        _state = state;
    }

    /// <summary>Adds a condition. Several calls are combined with <c>AND</c>.</summary>
    /// <param name="predicate">A lambda such as <c>b => b.Id > 5</c>. Values, including captured variables, become parameters.</param>
    /// <returns>A new query.</returns>
    public Query<T> Where(Expression<Func<T, bool>> predicate)
        => new(_state with { Conditions = [.. _state.Conditions, predicate.Body] });

    /// <summary>Chooses the columns to return, replacing <c>*</c>.</summary>
    /// <param name="selector">A column (<c>b => b.Title</c>) or an anonymous type (<c>b => new { b.Title, b.Id }</c>). A renamed member, such as <c>BookTitle = b.Title</c>, becomes an alias.</param>
    /// <returns>A new query.</returns>
    public Query<T> Select<TResult>(Expression<Func<T, TResult>> selector)
        => new(_state with { Columns = selector.Body });

    /// <summary>Starts a new ordering, ascending. Replaces any earlier ordering.</summary>
    /// <param name="key">The column to order by, such as <c>b => b.Title</c>.</param>
    /// <returns>A new query.</returns>
    public Query<T> OrderBy<TKey>(Expression<Func<T, TKey>> key)
        => new(_state with { Orders = [(key.Body, false)] });

    /// <summary>Adds an ascending column to the current ordering.</summary>
    /// <param name="key">The column to order by, such as <c>b => b.Id</c>.</param>
    /// <returns>A new query.</returns>
    public Query<T> ThenBy<TKey>(Expression<Func<T, TKey>> key)
        => new(_state with { Orders = [.. _state.Orders, (key.Body, false)] });

    /// <summary>Starts a new ordering, descending. Replaces any earlier ordering.</summary>
    /// <param name="key">The column to order by, such as <c>b => b.PublishedAt</c>.</param>
    /// <returns>A new query.</returns>
    public Query<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> key)
        => new(_state with { Orders = [(key.Body, true)] });

    /// <summary>Adds a descending column to the current ordering.</summary>
    /// <param name="key">The column to order by, such as <c>b => b.Id</c>.</param>
    /// <returns>A new query.</returns>
    public Query<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> key)
        => new(_state with { Orders = [.. _state.Orders, (key.Body, true)] });

    /// <summary>Skips the first <paramref name="rows"/> rows. Requires <see cref="Take"/> and an ordering when built.</summary>
    /// <param name="rows">How many rows to skip. Must not be negative; this is checked in <see cref="Build"/>.</param>
    /// <returns>A new query.</returns>
    public Query<T> Skip(int rows)
        => new(_state with { Skip = rows });

    /// <summary>Returns at most <paramref name="rows"/> rows. Requires an ordering when built.</summary>
    /// <param name="rows">How many rows to return. Must be at least 1; this is checked in <see cref="Build"/>.</param>
    /// <returns>A new query.</returns>
    public Query<T> Take(int rows)
        => new(_state with { Take = rows });

    /// <summary>Builds the SQL Server statement and its parameters.</summary>
    /// <returns>The SQL text and the parameter values.</returns>
    /// <exception cref="NotSupportedException">A lambda contains an expression that can't be translated.</exception>
    /// <exception cref="InvalidOperationException">Paging is used without an <c>OrderBy</c>, or <c>Skip</c> without <c>Take</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Skip is negative, or Take is less than 1.</exception>
    public SqlResult Build()
    {
        var parameters = new Dictionary<string, object?>();
        var translator = new SqlTranslator(_state.Options);

        var columns = _state.Columns is null
            ? "*"
            : translator.ToColumnOrValue(_state.Columns, parameters);

        var entity = _state.Options.Find(typeof(T));
        var table = SqlTranslator.Quote(entity?.Table ?? typeof(T).Name + "s");

        var from = entity?.Schema is { } schema
            ? $"{SqlTranslator.Quote(schema)}.{table}"
            : table;

        var sql = $"SELECT {columns} FROM {from}";

        if (_state.Conditions.Count > 0)
        {
            var conditions = _state.Conditions.Select(c => translator.ToSql(c, parameters));
            sql += " WHERE " + string.Join(" AND ", conditions);
        }

        if (_state.Orders.Count > 0)
        {
            sql += " ORDER BY " + translator.ToOrderBy(_state.Orders);
        }

        if (_state.Take is not null)
        {
            if (_state.Orders.Count == 0)
            {
                throw new InvalidOperationException("Paging requires an OrderBy.");
            }

            sql += " " + SqlTranslator.ToPaging(_state.Skip ?? 0, _state.Take.Value, parameters);
        }
        else if (_state.Skip is not null)
        {
            throw new InvalidOperationException("Skip requires Take.");
        }

        return new SqlResult(sql, parameters);
    }
}
