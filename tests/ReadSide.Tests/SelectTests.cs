using System.Linq.Expressions;

using ReadSide.Tests.Models;

namespace ReadSide.Tests;

public class SelectTests
{
    [Fact]
    public void Single_column_becomes_column_name()
    {
        var parameters = new Dictionary<string, object?>();
        Expression<Func<Book, string?>> expression = book => book.Title;

        var sql = SqlTranslator.ToSql(expression.Body, parameters);

        Assert.Equal("Title", sql);
        Assert.Empty(parameters);
    }
}
