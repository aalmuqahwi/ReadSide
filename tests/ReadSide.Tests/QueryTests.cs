using ReadSide.Tests.Models;

namespace ReadSide.Tests;

public class QueryTests
{
    [Fact]
    public void From_builds_select_all_from_table()
    {
        var result = Query.From<Book>().Build();

        Assert.Equal("SELECT * FROM Books", result.Sql);
        Assert.Empty(result.Parameters);
    }
}
