
namespace ReadSide.Tests;

public class PagingTests
{
    [Fact]
    public void Skip_and_take_become_offset_and_fetch_parameters()
    {
        var parameters = new Dictionary<string, object?>();

        var sql = SqlTranslator.ToPaging(skip: 10, take: 5, parameters);

        Assert.Equal("OFFSET @p0 ROWS FETCH NEXT @p1 ROWS ONLY", sql);
        Assert.Equal(10, parameters["p0"]);
        Assert.Equal(5, parameters["p1"]);
    }

    [Fact]
    public void Negative_skip_throws()
    {
        var parameters = new Dictionary<string, object?>();

        Assert.Throws<ArgumentOutOfRangeException>(() => SqlTranslator.ToPaging(-1, 5, parameters));
    }

    [Fact]
    public void Skip_zero_is_allowed()
    {
        var parameters = new Dictionary<string, object?>();

        var sql = SqlTranslator.ToPaging(skip: 0, take: 5, parameters);

        Assert.Equal("OFFSET @p0 ROWS FETCH NEXT @p1 ROWS ONLY", sql);
        Assert.Equal(0, parameters["p0"]);
        Assert.Equal(5, parameters["p1"]);
    }

    [Fact]
    public void Paging_continues_parameter_numbering()
    {
        var parameters = new Dictionary<string, object?> { ["p0"] = "x" };

        var sql = SqlTranslator.ToPaging(skip: 10, take: 5, parameters);

        Assert.Equal("OFFSET @p1 ROWS FETCH NEXT @p2 ROWS ONLY", sql);
        Assert.Equal(10, parameters["p1"]);
        Assert.Equal(5, parameters["p2"]);
    }

    [Fact]
    public void Take_below_one_throws()
    {
        var parameters = new Dictionary<string, object?>();

        Assert.Throws<ArgumentOutOfRangeException>(() => SqlTranslator.ToPaging(skip: 0, take: 0, parameters));
    }
}
