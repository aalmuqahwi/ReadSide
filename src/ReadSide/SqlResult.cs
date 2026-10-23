namespace ReadSide;

public sealed record SqlResult(string Sql, IReadOnlyDictionary<string, object?> Parameters);