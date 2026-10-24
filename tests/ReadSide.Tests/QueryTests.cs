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

        Assert.Equal("SELECT * FROM Books WHERE ([Id] = @p0)", result.Sql);
        Assert.Equal(5, result.Parameters["p0"]);
    }

    [Fact]
    public void Where_twice_joins_conditions_with_and()
    {
        var result = Query.From<Book>()
            .Where(book => book.Id > 1)
            .Where(book => book.Title != null)
            .Build();

        Assert.Equal("SELECT * FROM Books WHERE ([Id] > @p0) AND ([Title] IS NOT NULL)", result.Sql);
        Assert.Equal(1, result.Parameters["p0"]);
    }

    [Fact]
    public void Where_does_not_change_the_original_query()
    {
        var original = Query.From<Book>();
        var filtered = original.Where(book => book.Id > 1);

        Assert.Equal("SELECT * FROM Books", original.Build().Sql);
        Assert.Equal("SELECT * FROM Books WHERE ([Id] > @p0)", filtered.Build().Sql);
    }

    [Fact]
    public void Select_replaces_star_with_columns()
    {
        var result = Query.From<Book>()
            .Select(book => new { book.Title, book.Id })
            .Build();

        Assert.Equal("SELECT [Title], [Id] FROM Books", result.Sql);
    }

    [Fact]
    public void OrderBy_and_ThenByDescending_add_order_by_clause()
    {
        var result = Query.From<Book>()
            .OrderBy(book => book.Title)
            .ThenByDescending(book => book.Id)
            .Build();

        Assert.Equal("SELECT * FROM Books ORDER BY [Title] ASC, [Id] DESC", result.Sql);
    }

    [Fact]
    public void Where_comes_before_order_by_whatever_the_call_order()
    {
        var result = Query.From<Book>()
            .OrderBy(book => book.Title)
            .Where(book => book.Id > 1)
            .Build();

        Assert.Equal("SELECT * FROM Books WHERE ([Id] > @p0) ORDER BY [Title] ASC", result.Sql);
        Assert.Equal(1, result.Parameters["p0"]);
    }

    [Fact]
    public void OrderByDescending_and_ThenBy_add_order_by_clause()
    {
        var result = Query.From<Book>()
            .OrderByDescending(book => book.Id)
            .ThenBy(book => book.Title)
            .Build();

        Assert.Equal("SELECT * FROM Books ORDER BY [Id] DESC, [Title] ASC", result.Sql);
    }

    [Fact]
    public void OrderBy_called_twice_starts_a_new_ordering()
    {
        var result = Query.From<Book>()
            .OrderBy(book => book.Title)
            .OrderBy(book => book.Id)
            .Build();

        Assert.Equal("SELECT * FROM Books ORDER BY [Id] ASC", result.Sql);
    }

    [Fact]
    public void Skip_and_take_add_offset_fetch_after_order_by()
    {
        var result = Query.From<Book>()
            .OrderBy(book => book.Id)
            .Skip(10)
            .Take(5)
            .Build();

        Assert.Equal(
            "SELECT * FROM Books ORDER BY [Id] ASC OFFSET @p0 ROWS FETCH NEXT @p1 ROWS ONLY",
            result.Sql
        );

        Assert.Equal(10, result.Parameters["p0"]);
        Assert.Equal(5, result.Parameters["p1"]);
    }

    [Fact]
    public void Take_alone_starts_at_offset_zero()
    {
        var result = Query.From<Book>()
            .OrderBy(book => book.Id)
            .Take(5)
            .Build();

        Assert.Equal(
            "SELECT * FROM Books ORDER BY [Id] ASC OFFSET @p0 ROWS FETCH NEXT @p1 ROWS ONLY",
            result.Sql
        );

        Assert.Equal(0, result.Parameters["p0"]);
        Assert.Equal(5, result.Parameters["p1"]);
    }

    [Fact]
    public void Paging_without_order_by_throws()
    {
        var query = Query.From<Book>()
            .Skip(10)
            .Take(5);

        var exception = Assert.Throws<InvalidOperationException>(() => query.Build());

        Assert.Contains("Paging requires an OrderBy.", exception.Message);
    }

    [Fact]
    public void Paging_continues_numbering_after_where()
    {
        var result = Query.From<Book>()
            .Where(book => book.Id > 1)
            .OrderBy(book => book.Id)
            .Take(5)
            .Build();

        Assert.Equal(
            "SELECT * FROM Books WHERE ([Id] > @p0) ORDER BY [Id] ASC OFFSET @p1 ROWS FETCH NEXT @p2 ROWS ONLY",
            result.Sql);
        Assert.Equal(1, result.Parameters["p0"]);
        Assert.Equal(0, result.Parameters["p1"]);
        Assert.Equal(5, result.Parameters["p2"]);
    }
}
