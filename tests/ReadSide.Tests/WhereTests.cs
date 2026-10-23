using System;
using System.Linq.Expressions;

using ReadSide.Tests.Models;

namespace ReadSide.Tests;

public class WhereTests
{
    [Fact]
    public void Equal_with_constant_becomes_parameter()
    {
        var parameters = new Dictionary<string, object?>();
        Expression<Func<Book, bool>> expression = book => book.Id == 3;

        var sql = SqlTranslator.ToSql(expression.Body, parameters);

        Assert.Equal("(Id = @p0)", sql);
        Assert.Equal(3, parameters["p0"]);
    }

    [Fact]
    public void Not_equal_becomes_not_equal_operator()
    {
        var parameters = new Dictionary<string, object?>();
        Expression<Func<Book, bool>> expression = book => book.Id != 3;

        var sql = SqlTranslator.ToSql(expression.Body, parameters);

        Assert.Equal("(Id <> @p0)", sql);
        Assert.Equal(3, parameters["p0"]);
    }

    [Fact]
    public void GreaterThan_becomes_greater_than_operator()
    {
        var parameters = new Dictionary<string, object?>();
        Expression<Func<Book, bool>> expression = book => book.Id > 3;

        var sql = SqlTranslator.ToSql(expression.Body, parameters);

        Assert.Equal("(Id > @p0)", sql);
        Assert.Equal(3, parameters["p0"]);
    }

    [Fact]
    public void GreaterThanEqual_becomes_greater_than_equal_operator()
    {
        var parameters = new Dictionary<string, object?>();
        Expression<Func<Book, bool>> expression = book => book.Id >= 3;

        var sql = SqlTranslator.ToSql(expression.Body, parameters);

        Assert.Equal("(Id >= @p0)", sql);
        Assert.Equal(3, parameters["p0"]);
    }

    [Fact]
    public void LessThan_becomes_less_than_operator()
    {
        var parameters = new Dictionary<string, object?>();
        Expression<Func<Book, bool>> expression = book => book.Id < 3;

        var sql = SqlTranslator.ToSql(expression.Body, parameters);

        Assert.Equal("(Id < @p0)", sql);
        Assert.Equal(3, parameters["p0"]);
    }

    [Fact]
    public void LessThanEqual_becomes_less_than_equal_operator()
    {
        var parameters = new Dictionary<string, object?>();
        Expression<Func<Book, bool>> expression = book => book.Id <= 3;

        var sql = SqlTranslator.ToSql(expression.Body, parameters);

        Assert.Equal("(Id <= @p0)", sql);
        Assert.Equal(3, parameters["p0"]);
    }

    [Fact]
    public void AndAlso_becomes_and_operator()
    {
        var parameters = new Dictionary<string, object?>();
        Expression<Func<Book, bool>> expression = book => book.Id == 3 && book.Title == "test";

        var sql = SqlTranslator.ToSql(expression.Body, parameters);

        Assert.Equal("((Id = @p0) AND (Title = @p1))", sql);
        Assert.Equal(3, parameters["p0"]);
        Assert.Equal("test", parameters["p1"]);
    }

    [Fact]
    public void Not_becomes_not_operator()
    {
        var parameters = new Dictionary<string, object?>();
        Expression<Func<Book, bool>> expression = book => !(book.Id == 3);

        var sql = SqlTranslator.ToSql(expression.Body, parameters);

        Assert.Equal("NOT (Id = @p0)", sql);
        Assert.Equal(3, parameters["p0"]);
    }

    [Fact]
    public void Null_becomes_is_null_operator()
    {
        var parameters = new Dictionary<string, object?>();
        Expression<Func<Book, bool>> expression = book => book.Title == null;

        var sql = SqlTranslator.ToSql(expression.Body, parameters);

        Assert.Equal("(Title IS NULL)", sql);
        Assert.Empty(parameters);
    }

    [Fact]
    public void NotNull_becomes_is_not_null_operator()
    {
        var parameters = new Dictionary<string, object?>();
        Expression<Func<Book, bool>> expression = book => book.Title != null;

        var sql = SqlTranslator.ToSql(expression.Body, parameters);

        Assert.Equal("(Title IS NOT NULL)", sql);
        Assert.Empty(parameters);
    }

    [Fact]
    public void OrElse_inside_AndAlso_keeps_grouping()
    {
        var parameters = new Dictionary<string, object?>();
        Expression<Func<Book, bool>> expression = book => (book.Id == 1 || book.Id == 2) && book.Id > 3;

        var sql = SqlTranslator.ToSql(expression.Body, parameters);

        Assert.Equal("(((Id = @p0) OR (Id = @p1)) AND (Id > @p2))", sql);
        Assert.Equal(1, parameters["p0"]);
        Assert.Equal(2, parameters["p1"]);
        Assert.Equal(3, parameters["p2"]);
    }

    [Fact]
    public void Captured_variable_becomes_parameter()
    {
        var parameters = new Dictionary<string, object?>();
        string title = "C# Expressions";
        Expression<Func<Book, bool>> expression = book => book.Title == title;

        var sql = SqlTranslator.ToSql(expression.Body, parameters);

        Assert.Equal("(Title = @p0)", sql);
        Assert.Equal("C# Expressions", parameters["p0"]);
    }

    [Fact]
    public void Unsupported_operator_throws_not_supported()
    {
        var parameters = new Dictionary<string, object?>();
        Expression<Func<Book, bool>> expression = book => book.Id + 1 == 3;

        Assert.Throws<NotSupportedException>(() => SqlTranslator.ToSql(expression.Body, parameters));
    }

    [Fact]
    public void Unsupported_node_message_includes_the_expression_text()
    {
        var parameters = new Dictionary<string, object?>();
        Expression<Func<Book, bool>> expression = book => book.Title!.Contains("x");

        var exception = Assert.Throws<NotSupportedException>(
            () => SqlTranslator.ToSql(expression.Body, parameters));

        Assert.Contains("Contains", exception.Message);
    }
}
