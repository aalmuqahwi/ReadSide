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
}
