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

        Assert.Equal("([Id] = @p0)", sql);
        Assert.Equal(3, parameters["p0"]);
    }

    [Fact]
    public void Not_equal_becomes_not_equal_operator()
    {
        var parameters = new Dictionary<string, object?>();
        Expression<Func<Book, bool>> expression = book => book.Id != 3;

        var sql = SqlTranslator.ToSql(expression.Body, parameters);

        Assert.Equal("([Id] <> @p0)", sql);
        Assert.Equal(3, parameters["p0"]);
    }

    [Fact]
    public void GreaterThan_becomes_greater_than_operator()
    {
        var parameters = new Dictionary<string, object?>();
        Expression<Func<Book, bool>> expression = book => book.Id > 3;

        var sql = SqlTranslator.ToSql(expression.Body, parameters);

        Assert.Equal("([Id] > @p0)", sql);
        Assert.Equal(3, parameters["p0"]);
    }

    [Fact]
    public void GreaterThanEqual_becomes_greater_than_equal_operator()
    {
        var parameters = new Dictionary<string, object?>();
        Expression<Func<Book, bool>> expression = book => book.Id >= 3;

        var sql = SqlTranslator.ToSql(expression.Body, parameters);

        Assert.Equal("([Id] >= @p0)", sql);
        Assert.Equal(3, parameters["p0"]);
    }

    [Fact]
    public void LessThan_becomes_less_than_operator()
    {
        var parameters = new Dictionary<string, object?>();
        Expression<Func<Book, bool>> expression = book => book.Id < 3;

        var sql = SqlTranslator.ToSql(expression.Body, parameters);

        Assert.Equal("([Id] < @p0)", sql);
        Assert.Equal(3, parameters["p0"]);
    }

    [Fact]
    public void LessThanEqual_becomes_less_than_equal_operator()
    {
        var parameters = new Dictionary<string, object?>();
        Expression<Func<Book, bool>> expression = book => book.Id <= 3;

        var sql = SqlTranslator.ToSql(expression.Body, parameters);

        Assert.Equal("([Id] <= @p0)", sql);
        Assert.Equal(3, parameters["p0"]);
    }

    [Fact]
    public void AndAlso_becomes_and_operator()
    {
        var parameters = new Dictionary<string, object?>();
        Expression<Func<Book, bool>> expression = book => book.Id == 3 && book.Title == "test";

        var sql = SqlTranslator.ToSql(expression.Body, parameters);

        Assert.Equal("(([Id] = @p0) AND ([Title] = @p1))", sql);
        Assert.Equal(3, parameters["p0"]);
        Assert.Equal("test", parameters["p1"]);
    }

    [Fact]
    public void Not_becomes_not_operator()
    {
        var parameters = new Dictionary<string, object?>();
        Expression<Func<Book, bool>> expression = book => !(book.Id == 3);

        var sql = SqlTranslator.ToSql(expression.Body, parameters);

        Assert.Equal("NOT ([Id] = @p0)", sql);
        Assert.Equal(3, parameters["p0"]);
    }

    [Fact]
    public void Null_becomes_is_null_operator()
    {
        var parameters = new Dictionary<string, object?>();
        Expression<Func<Book, bool>> expression = book => book.Title == null;

        var sql = SqlTranslator.ToSql(expression.Body, parameters);

        Assert.Equal("([Title] IS NULL)", sql);
        Assert.Empty(parameters);
    }

    [Fact]
    public void Comparing_to_a_captured_null_becomes_is_null()
    {
        string? title = null;
        Expression<Func<Book, bool>> predicate = b => b.Title == title;
        var parameters = new Dictionary<string, object?>();

        var sql = SqlTranslator.ToSql(predicate.Body, parameters);

        Assert.Equal("([Title] IS NULL)", sql);
        Assert.Empty(parameters);
    }

    [Fact]
    public void Comparing_to_a_captured_null_with_not_equal_becomes_is_not_null()
    {
        string? title = null;
        Expression<Func<Book, bool>> predicate = b => b.Title != title;
        var parameters = new Dictionary<string, object?>();

        var sql = SqlTranslator.ToSql(predicate.Body, parameters);

        Assert.Equal("([Title] IS NOT NULL)", sql);
        Assert.Empty(parameters);
    }

    [Fact]
    public void NotNull_becomes_is_not_null_operator()
    {
        var parameters = new Dictionary<string, object?>();
        Expression<Func<Book, bool>> expression = book => book.Title != null;

        var sql = SqlTranslator.ToSql(expression.Body, parameters);

        Assert.Equal("([Title] IS NOT NULL)", sql);
        Assert.Empty(parameters);
    }

    [Fact]
    public void OrElse_inside_AndAlso_keeps_grouping()
    {
        var parameters = new Dictionary<string, object?>();
        Expression<Func<Book, bool>> expression = book => (book.Id == 1 || book.Id == 2) && book.Id > 3;

        var sql = SqlTranslator.ToSql(expression.Body, parameters);

        Assert.Equal("((([Id] = @p0) OR ([Id] = @p1)) AND ([Id] > @p2))", sql);
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

        Assert.Equal("([Title] = @p0)", sql);
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
        Expression<Func<Book, bool>> expression = book => book.Title!.Trim() == "x";

        var exception = Assert.Throws<NotSupportedException>(
            () => SqlTranslator.ToSql(expression.Body, parameters));

        Assert.Contains("Trim", exception.Message);
    }

    [Fact]
    public void Convert_is_ignored_and_the_operand_is_translated()
    {
        var parameters = new Dictionary<string, object?>();
        long big = 5;
        Expression<Func<Book, bool>> expression = book => book.Id == big;

        var sql = SqlTranslator.ToSql(expression.Body, parameters);

        Assert.Equal("([Id] = @p0)", sql);
        Assert.Equal(5L, parameters["p0"]);
    }

    [Fact]
    public void Contains_becomes_like_with_whildcards_in_the_parameter()
    {
        Expression<Func<Book, bool>> predicate = b => b.Title!.Contains("dune");
        var parameters = new Dictionary<string, object?>();

        var sql = SqlTranslator.ToSql(predicate.Body, parameters);

        Assert.Equal("([Title] LIKE @p0 ESCAPE '\\')", sql);
        Assert.Equal("%dune%", parameters["p0"]);
    }

    [Fact]
    public void Contains_with_a_captured_variable_uses_its_value()
    {
        var term = "dune";
        Expression<Func<Book, bool>> predicate = b => b.Title!.Contains(term);
        var parameters = new Dictionary<string, object?>();

        var sql = SqlTranslator.ToSql(predicate.Body, parameters);

        Assert.Equal("([Title] LIKE @p0 ESCAPE '\\')", sql);
        Assert.Equal("%dune%", parameters["p0"]);
    }

    [Fact]
    public void StartsWith_becomes_like_with_trailing_wildcard()
    {
        Expression<Func<Book, bool>> predicate = b => b.Title!.StartsWith("du");
        var parameters = new Dictionary<string, object?>();

        var sql = SqlTranslator.ToSql(predicate.Body, parameters);

        Assert.Equal("([Title] LIKE @p0 ESCAPE '\\')", sql);
        Assert.Equal("du%", parameters["p0"]);
    }

    [Fact]
    public void EndsWith_becomes_like_with_leading_wildcard()
    {
        Expression<Func<Book, bool>> predicate = b => b.Title!.EndsWith("ne");
        var parameters = new Dictionary<string, object?>();

        var sql = SqlTranslator.ToSql(predicate.Body, parameters);

        Assert.Equal("([Title] LIKE @p0 ESCAPE '\\')", sql);
        Assert.Equal("%ne", parameters["p0"]);
    }

    [Fact]
    public void Like_escapes_wildcard_characters_in_the_value()
    {
        Expression<Func<Book, bool>> predicate = b => b.Title!.Contains("100%_[x]");
        var parameters = new Dictionary<string, object?>();

        var sql = SqlTranslator.ToSql(predicate.Body, parameters);

        Assert.Equal("([Title] LIKE @p0 ESCAPE '\\')", sql);
        Assert.Equal("%100\\%\\_\\[x]%", parameters["p0"]);
    }

    [Fact]
    public void List_Contains_becomes_in_with_one_parameter_per_value()
    {
        var ids = new List<int> { 1, 2 };
        Expression<Func<Book, bool>> expression = book => ids.Contains(book.Id);
        var parameters = new Dictionary<string, object?>();

        var sql = SqlTranslator.ToSql(expression.Body, parameters);

        Assert.Equal("([Id] IN (@p0, @p1))", sql);
        Assert.Equal(1, parameters["p0"]);
        Assert.Equal(2, parameters["p1"]);
    }

    [Fact]
    public void Array_Contains_becomes_in_with_one_parameter_per_value()
    {
        var ids = new[] { 1, 2 };
        Expression<Func<Book, bool>> predicate = b => ids.Contains(b.Id);
        var parameters = new Dictionary<string, object?>();

        var sql = SqlTranslator.ToSql(predicate.Body, parameters);

        Assert.Equal("([Id] IN (@p0, @p1))", sql);
        Assert.Equal(1, parameters["p0"]);
        Assert.Equal(2, parameters["p1"]);
    }

    [Fact]
    public void Empty_collection_becomes_a_condition_that_matches_nothing()
    {
        var ids = new List<int>();
        Expression<Func<Book, bool>> predicate = b => ids.Contains(b.Id);
        var parameters = new Dictionary<string, object?>();

        var sql = SqlTranslator.ToSql(predicate.Body, parameters);

        // always false condition
        Assert.Equal("(1 = 0)", sql);
        Assert.Empty(parameters);
    }

    [Fact]
    public void Negated_Contains_becomes_not_in()
    {
        var ids = new List<int> { 1, 2 };
        Expression<Func<Book, bool>> predicate = b => !ids.Contains(b.Id);
        var parameters = new Dictionary<string, object?>();

        var sql = SqlTranslator.ToSql(predicate.Body, parameters);

        Assert.Equal("NOT ([Id] IN (@p0, @p1))", sql);
    }

    [Fact]
    public void In_continues_parameter_numbering_after_other_conditions()
    {
        var ids = new List<int> { 5, 6 };
        Expression<Func<Book, bool>> predicate = b => b.Id > 0 && ids.Contains(b.Id);
        var parameters = new Dictionary<string, object?>();

        var sql = SqlTranslator.ToSql(predicate.Body, parameters);

        Assert.Equal("(([Id] > @p0) AND ([Id] IN (@p1, @p2)))", sql);
        Assert.Equal(0, parameters["p0"]);
        Assert.Equal(5, parameters["p1"]);
        Assert.Equal(6, parameters["p2"]);
    }

    [Fact]
    public void Bare_boolean_column_becomes_equals_one()
    {
        Expression<Func<Book, bool>> predicate = b => b.IsAvailable;
        var parameters = new Dictionary<string, object?>();

        var sql = SqlTranslator.ToSql(predicate.Body, parameters);

        Assert.Equal("([IsAvailable] = 1)", sql);
        Assert.Empty(parameters);
    }

    [Fact]
    public void Boolean_column_compared_to_a_value_stays_a_normal_comparison()
    {
        Expression<Func<Book, bool>> predicate = b => b.IsAvailable == true;
        var parameters = new Dictionary<string, object?>();

        var sql = SqlTranslator.ToSql(predicate.Body, parameters);

        Assert.Equal("([IsAvailable] = @p0)", sql);
        Assert.Equal(true, parameters["p0"]);
    }
}
