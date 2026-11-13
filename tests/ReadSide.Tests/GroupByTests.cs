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
}
