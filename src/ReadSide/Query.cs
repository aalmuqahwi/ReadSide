namespace ReadSide;

public static class Query
{
    public static Query<T> From<T>() => new();
}
