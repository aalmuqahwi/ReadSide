using System.Linq.Expressions;

namespace ReadSide;

/// <summary>A query that has been grouped. The next step is <c>Select</c>.</summary>
/// <typeparam name="T">The entity being grouped.</typeparam>
/// <typeparam name="TKey">The type of the grouping key.</typeparam>
public sealed class GroupByQuery<T, TKey>
{
    private readonly ReadSideOptions _options;
    private readonly IReadOnlyList<Expression> _conditions;
    private readonly Expression _key;
    private readonly IReadOnlyList<LambdaExpression> _having;

    internal GroupByQuery(
        ReadSideOptions options,
        IReadOnlyList<Expression> conditions,
        Expression key,
        IReadOnlyList<LambdaExpression>? having = null)
    {
        _options = options;
        _conditions = conditions;
        _key = key;
        _having = having ?? [];
    }

    /// <summary>Chooses what each group returns.</summary>
    /// <param name="selector">A lambda with the key and the group, such as <c>(key, group) => new { key.Title, Total = group.Count() }</c>.</param>
    /// <typeparam name="TResult">The shape of one result row.</typeparam>
    /// <returns>A query that can be built.</returns>
    public GroupedQuery<T> Select<TResult>(Expression<Func<TKey, Group<T>, TResult>> selector)
        => new(_options, _conditions, _key, selector.Body, selector.Parameters[0], _having);

    /// <summary>Keeps only the groups that match the condition. Write it with the key and the group.</summary>
    public GroupByQuery<T, TKey> Having(Expression<Func<TKey, Group<T>, bool>> condition)
        => new(_options, _conditions, _key, [.. _having, condition]);
}
