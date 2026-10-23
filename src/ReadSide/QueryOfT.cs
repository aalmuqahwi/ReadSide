namespace ReadSide;

public sealed class Query<T>
{
    public SqlResult Build()
    {
        var table = typeof(T).Name + "s";
        var sql = $"SELECT * FROM {table}";

        return new SqlResult(sql, new Dictionary<string, object?>());
    }
}
