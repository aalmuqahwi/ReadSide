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

    [Fact]
    public void Sum_adds_up_a_column()
    {
        var result = Query.From<Book>().Sum(b => b.Id).Build();

        Assert.Equal("SELECT SUM([Id]) FROM [Books]", result.Sql);
    }

    [Fact]
    public void Sum_of_an_expression_numbers_its_parameter_before_the_where()
    {
        var result = Query.From<Book>()
            .Where(b => b.Id > 5)
            .Sum(b => b.Id * 2)
            .Build();

        Assert.Equal("SELECT SUM(([Id] * @p0)) FROM [Books] WHERE ([Id] > @p1)", result.Sql);
        Assert.Equal(2, result.Parameters["p0"]);
        Assert.Equal(5, result.Parameters["p1"]);
    }

    [Fact]
    public void Min_finds_the_smallest_value()
    {
        var result = Query.From<Book>().Min(b => b.Id).Build();

        Assert.Equal("SELECT MIN([Id]) FROM [Books]", result.Sql);
    }

    [Fact]
    public void Max_finds_the_largest_value()
    {
        var result = Query.From<Book>().Max(b => b.PublishedAt).Build();

        Assert.Equal("SELECT MAX([PublishedAt]) FROM [Books]", result.Sql);
    }

    [Fact]
    public void Average_becomes_AVG()
    {
        var result = Query.From<Book>().Average(b => b.Id).Build();

        Assert.Equal("SELECT AVG([Id]) FROM [Books]", result.Sql);
    }
}
