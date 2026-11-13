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
}
