using System.Linq.Expressions;

using ReadSide.Tests.Models;

namespace ReadSide.Tests;

public class OrderByTests
{
    private static Expression Body<T>(Expression<Func<Book, T>> expression) => expression.Body;

    [Fact]
    public void Clauses_become_ordered_column_list()
    {
        var clauses = new List<(Expression Column, bool Descending)>
        {
            (Body(book => book.Title), false),
            (Body(book => book.Id), true)
        };

        var sql = SqlTranslator.ToOrderBy(clauses);

        Assert.Equal("Title ASC, Id DESC", sql);
    }
}