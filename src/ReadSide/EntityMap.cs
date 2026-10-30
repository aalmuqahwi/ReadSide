namespace ReadSide;

internal record EntityMap(
    string? Table, 
    string? Schema,
    IReadOnlyDictionary<string, string> Columns);
