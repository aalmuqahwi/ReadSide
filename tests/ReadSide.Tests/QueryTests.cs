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

    [Fact]
    public void Where_adds_where_clause_with_parameters()
    {
        var result = Query.From<Book>()
            .Where(book => book.Id == 5)
            .Build();

        Assert.Equal("SELECT * FROM Books WHERE (Id = @p0)", result.Sql);
        Assert.Equal(5, result.Parameters["p0"]);
    }

    [Fact]
    public void Where_twice_joins_conditions_with_and()
    {
        var result = Query.From<Book>()
            .Where(book => book.Id > 1)
            .Where(book => book.Title != null)
            .Build();

        Assert.Equal("SELECT * FROM Books WHERE (Id > @p0) AND (Title IS NOT NULL)", result.Sql);
        Assert.Equal(1, result.Parameters["p0"]);
    }

    [Fact]
    public void Where_does_not_change_the_original_query()
    {
        var original = Query.From<Book>();
        var filtered = original.Where(book => book.Id > 1);

        Assert.Equal("SELECT * FROM Books", original.Build().Sql);
        Assert.Equal("SELECT * FROM Books WHERE (Id > @p0)", filtered.Build().Sql);
    }
}
