using System.Linq.Expressions;

using ReadSide.Tests.Models;

namespace ReadSide.Tests;

public class ArithmeticTests
{
    [Fact]
    public void Addition_becomes_plus()
    {
        Expression<Func<Book, bool>> predicate = b => b.Id + 1 > 5;
        var parameters = new Dictionary<string, object?>();

        var sql = TestSql.ToSql(predicate.Body, parameters);

        Assert.Equal("(([Id] + @p0) > @p1)", sql);
        Assert.Equal(1, parameters["p0"]);
        Assert.Equal(5, parameters["p1"]);
    }

    [Fact]
    public void Subtraction_becomes_minus()
    {
        Expression<Func<Book, bool>> predicate = b => b.Id - 1 > 5;
        var parameters = new Dictionary<string, object?>();

        var sql = TestSql.ToSql(predicate.Body, parameters);

        Assert.Equal("(([Id] - @p0) > @p1)", sql);
    }

    [Fact]
    public void Multiplication_becomes_times()
    {
        Expression<Func<Book, bool>> predicate = b => b.Id * 2 > 5;
        var parameters = new Dictionary<string, object?>();

        var sql = TestSql.ToSql(predicate.Body, parameters);

        Assert.Equal("(([Id] * @p0) > @p1)", sql);
    }

    [Fact]
    public void Division_becomes_slash()
    {
        Expression<Func<Book, bool>> predicate = b => b.Id / 2 > 5;
        var parameters = new Dictionary<string, object?>();

        var sql = TestSql.ToSql(predicate.Body, parameters);

        Assert.Equal("(([Id] / @p0) > @p1)", sql);
    }

    [Fact]
    public void Modulo_becomes_percent()
    {
        Expression<Func<Book, bool>> predicate = b => b.Id % 2 == 0;
        var parameters = new Dictionary<string, object?>();

        var sql = TestSql.ToSql(predicate.Body, parameters);

        Assert.Equal("(([Id] % @p0) = @p1)", sql);
    }

    [Fact]
    public void Arithmetic_in_select_keeps_the_alias()
    {
        var result = Query.From<Book>()
            .Select(book => new { Double = book.Id * 2 })
            .Build();

        Assert.Equal("SELECT ([Id] * @p0) AS [Double] FROM [Books]", result.Sql);
        Assert.Equal(2, result.Parameters["p0"]);
    }
}
