using ReadSide.Tests.Models;

namespace ReadSide.Tests;

public class GroupByTests
{
    [Fact]
    public void Group_by_one_column_with_a_count()
    {
        var result = Query.From<Book>()
            .GroupBy(b => b.IsAvailable)
            .Select((key, group) => new { Available = key, Total = group.Count() })
            .Build();

        Assert.Equal(
            "SELECT [IsAvailable] AS [Available], COUNT(*) AS [Total] FROM [Books] GROUP BY [IsAvailable]",
            result.Sql);
    }

    [Fact]
    public void Group_by_two_columns_with_a_count()
    {
        var result = Query.From<Book>()
            .GroupBy(b => new { b.Title, b.IsAvailable })
            .Select((key, group) => new { Name = key.Title, Available = key.IsAvailable, Total = group.Count() })
            .Build();

        Assert.Equal(
            "SELECT [Title] AS [Name], [IsAvailable] AS [Available], COUNT(*) AS [Total] FROM [Books] GROUP BY [Title], [IsAvailable]",
            result.Sql);
    }

    [Fact]
    public void Group_by_one_column_with_a_sum()
    {
        var result = Query.From<Book>()
            .GroupBy(b => b.IsAvailable)
            .Select((key, group) => new { Available = key, Total = group.Sum(b => b.Id) })
            .Build();

        Assert.Equal(
            "SELECT [IsAvailable] AS [Available], SUM([Id]) AS [Total] FROM [Books] GROUP BY [IsAvailable]",
            result.Sql);
    }

    [Fact]
    public void Group_by_one_column_with_a_min()
    {
        var result = Query.From<Book>()
            .GroupBy(b => b.IsAvailable)
            .Select((key, group) => new { Available = key, Lowest = group.Min(b => b.Id) })
            .Build();

        Assert.Equal(
            "SELECT [IsAvailable] AS [Available], MIN([Id]) AS [Lowest] FROM [Books] GROUP BY [IsAvailable]",
            result.Sql);
    }

    [Fact]
    public void Group_by_one_column_with_a_max()
    {
        var result = Query.From<Book>()
            .GroupBy(b => b.IsAvailable)
            .Select((key, group) => new { Available = key, Highest = group.Max(b => b.Id) })
            .Build();

        Assert.Equal(
            "SELECT [IsAvailable] AS [Available], MAX([Id]) AS [Highest] FROM [Books] GROUP BY [IsAvailable]",
            result.Sql);
    }

    [Fact]
    public void Group_by_one_column_with_an_average()
    {
        var result = Query.From<Book>()
            .GroupBy(b => b.IsAvailable)
            .Select((key, group) => new { Available = key, Mean = group.Average(b => b.Id) })
            .Build();

        Assert.Equal(
            "SELECT [IsAvailable] AS [Available], AVG([Id]) AS [Mean] FROM [Books] GROUP BY [IsAvailable]",
            result.Sql);
    }

    [Fact]
    public void Where_before_group_by_comes_before_the_group_by_clause()
    {
        var result = Query.From<Book>()
            .Where(b => b.Id > 10)
            .GroupBy(b => b.IsAvailable)
            .Select((key, group) => new { Available = key, Total = group.Count() })
            .Build();

        Assert.Equal(
            "SELECT [IsAvailable] AS [Available], COUNT(*) AS [Total] FROM [Books] WHERE ([Id] > @p0) GROUP BY [IsAvailable]",
            result.Sql);

    }

    [Fact]
    public void Select_before_group_by_is_rejected()
    {
        var query = Query.From<Book>().Select(b => new { b.Id });

        var exception = Assert.Throws<InvalidOperationException>(() => query.GroupBy(b => b.IsAvailable));

        Assert.Equal(
            "Select cannot come before GroupBy. Use GroupBy(...).Select((key, group) => ...) instead.",
            exception.Message);
    }

    [Fact]
    public void Having_filters_groups_by_a_count()
    {
        var result = Query.From<Book>()
            .GroupBy(b => b.IsAvailable)
            .Having((key, group) => group.Count() > 5)
            .Select((key, group) => new { Available = key, Total = group.Count() })
            .Build();

        Assert.Equal(
            "SELECT [IsAvailable] AS [Available], COUNT(*) AS [Total] FROM [Books] GROUP BY [IsAvailable] HAVING (COUNT(*) > @p0)",
            result.Sql);
    }
}
