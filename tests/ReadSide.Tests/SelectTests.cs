using System.Linq.Expressions;

using ReadSide.Tests.Models;

namespace ReadSide.Tests;

public class SelectTests
{
    private static string Translate<T>(Expression<Func<Book, T>> expression, Dictionary<string, object?> parameters)
        => SqlTranslator.ToSql(expression.Body, parameters);

    [Fact]
    public void Single_column_becomes_column_name()
    {
        var parameters = new Dictionary<string, object?>();
        Expression<Func<Book, string?>> expression = book => book.Title;

        var sql = SqlTranslator.ToSql(expression.Body, parameters);

        Assert.Equal("Title", sql);
        Assert.Empty(parameters);
    }

    [Fact]
    public void Anonymous_type_becomes_column_list()
    {
        var parameters = new Dictionary<string, object?>();

        var sql = Translate(book => new { book.Title, book.Id }, parameters);

        Assert.Equal("Title, Id", sql);
        Assert.Empty(parameters);
    }
}
