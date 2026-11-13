namespace ReadSide;

// The aggregate functions ReadSide can write. The SQL name is chosen in AggregateQuery.
internal sealed class AggregateFunction
{
    public static readonly AggregateFunction Count = new("COUNT");
    public static readonly AggregateFunction Sum = new("SUM");
    public static readonly AggregateFunction Min = new("MIN");
    public static readonly AggregateFunction Max = new("MAX");
    public static readonly AggregateFunction Average = new("AVG");

    public string Value { get; }

    private AggregateFunction(string value)
    {
        Value = value;
    }
}
