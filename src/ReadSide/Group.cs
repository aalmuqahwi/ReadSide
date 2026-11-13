namespace ReadSide;

/// <summary>The rows of one group. It only exists inside a <c>Select</c> lambda: its methods are read by ReadSide, never run.</summary>
/// <typeparam name="T">The entity being grouped.</typeparam>
public sealed class Group<T>
{
    private Group()
    {
    }

    /// <summary>Counts the rows in the group, as <c>COUNT(*)</c>.</summary>
    /// <returns>Never returns. Use it inside a ReadSide <c>Select</c> only.</returns>
    public int Count() => throw new InvalidOperationException("Group methods can only be used inside a ReadSide Select.");

    /// <summary>Sums a value over the rows of the group. Only valid inside a ReadSide Select.</summary>
    public TValue Sum<TValue>(Func<T, TValue> selector)
        => throw new InvalidOperationException("Group methods can only be used inside a ReadSide Select.");

    /// <summary>Finds the smallest value over the rows of the group. Only valid inside a ReadSide Select.</summary>
    public TValue Min<TValue>(Func<T, TValue> selector)
        => throw new InvalidOperationException("Group methods can only be used inside a ReadSide Select.");

    /// <summary>Finds the largest value over the rows of the group. Only valid inside a ReadSide Select.</summary>
    public TValue Max<TValue>(Func<T, TValue> selector)
        => throw new InvalidOperationException("Group methods can only be used inside a ReadSide Select.");
}
