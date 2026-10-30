namespace ReadSide;

/// <summary>
/// Creates queries that use the given <see cref="ReadSideOptions"/>.
/// </summary>
public class QueryFactory
{
    private readonly ReadSideOptions _options;

    /// <summary>
    /// Creates a factory that uses the given options.
    /// </summary>
    /// <param name="options">The settings shared by every query this factory creates.</param>
    public QueryFactory(ReadSideOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Starts a query on the given entity type.
    /// </summary>
    /// <typeparam name="T">The entity the query reads from.</typeparam>
    /// <returns>An empty query on <typeparamref name="T"/>.</returns>
    public Query<T> From<T>() => new(_options);
}
