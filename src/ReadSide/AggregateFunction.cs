namespace ReadSide;

// The aggregate functions ReadSide can write. The SQL name is chosen in AggregateQuery.
internal sealed class AggregateFunction
{
    public static readonly AggregateFunction Count = new("COUNT");

    public string Value { get; }

    private AggregateFunction(string value)
    {
        Value = value;
    }
}
