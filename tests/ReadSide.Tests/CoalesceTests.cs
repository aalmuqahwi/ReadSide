using System.Linq.Expressions;

using ReadSide.Tests.Models;

namespace ReadSide.Tests;

public class CoalesceTests
{
    [Fact]
    public void Null_coalescing_becoms_COALESCE()
    {
        Expression<Func<Book, bool>> predicate = b => (b.Title ?? "none") == "x";
        var parameters = new Dictionary<string, object?>();

        var sql = TestSql.ToSql(predicate.Body, parameters);

        Assert.Equal("(COALESCE([Title], @p0) = @p1)", sql);
        Assert.Equal("none", parameters["p0"]);
        Assert.Equal("x", parameters["p1"]);
    }

    [Fact]
    public void Coalesce_numbers_the_left_parameter_first()
    {
        string? fallback = "a";
        Expression<Func<Book, bool>> predicate = b => (fallback ?? "none") == b.Title;
        var parameters = new Dictionary<string, object?>();

        var sql = TestSql.ToSql(predicate.Body, parameters);

        Assert.Equal("(COALESCE(@p0, @p1) = [Title])", sql);
        Assert.Equal("a", parameters["p0"]);
        Assert.Equal("none", parameters["p1"]);
    }

    [Fact]
    public void Coalesce_in_select_keeps_the_alias()
    {
        var result = Query.From<Book>()
            .Select(book => new { Display = book.Title ?? "none" })
            .Build();

        Assert.Equal("SELECT COALESCE([Title], @p0) AS [Display] FROM [Books]", result.Sql);
        Assert.Equal("none", result.Parameters["p0"]);
    }

    [Fact]
    public void Coalesce_with_a_column_is_a_valid_order_by_key()
    {
        var result = Query.From<Book>()
            .OrderBy(book => book.ReturnedAt ?? book.PublishedAt)
            .Build();

        Assert.Equal("SELECT * FROM [Books] ORDER BY COALESCE([ReturnedAt], [PublishedAt]) ASC", result.Sql);
    }

    [Fact]
    public void Order_by_coalesce_without_a_column_is_rejected()
    {
        string? fallback = "a";

        var error = Assert.Throws<NotSupportedException>(() =>
            Query.From<Book>().OrderBy(book => fallback ?? "none").Build());

        Assert.Contains("Cannot order by", error.Message);
    }
}
