using ReadSide.Tests.Models;

namespace ReadSide.Tests;

public class AggregateTests
{
    [Fact]
    public void Count_counts_all_rows()
    {
        var result = Query.From<Book>().Count().Build();

        Assert.Equal("SELECT COUNT(*) FROM [Books]", result.Sql);
        Assert.Empty(result.Parameters);
    }

    [Fact]
    public void Count_keeps_the_conditions()
    {
        var result = Query.From<Book>().Where(b => b.IsAvailable).Count().Build();

        Assert.Equal("SELECT COUNT(*) FROM [Books] WHERE ([IsAvailable] = 1)", result.Sql);
    }
}
