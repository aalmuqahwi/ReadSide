namespace ReadSide;

/// <summary>The SQL text and parameters produced by <see cref="Query{T}.Build"/>.</summary>
/// <param name="Sql">The complete SQL statement, with <c>@p0</c>-style placeholders.</param>
/// <param name="Parameters">The values for the placeholders, keyed by name without the <c>@</c>.</param>
public sealed record SqlResult(string Sql, IReadOnlyDictionary<string, object?> Parameters);