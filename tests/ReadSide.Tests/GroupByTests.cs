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
}
