namespace ReadSide;

/// <summary>Entry point for building queries.</summary>
public static class Query
{
    /// <summary>Starts a query over the table that matches <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The class whose name (plus <c>s</c>) is the table name and whose properties are the columns.</typeparam>
    public static Query<T> From<T>() => new();
}
