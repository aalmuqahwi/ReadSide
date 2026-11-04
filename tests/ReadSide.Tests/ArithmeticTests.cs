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
}
