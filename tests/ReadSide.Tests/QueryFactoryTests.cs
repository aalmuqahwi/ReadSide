using ReadSide.Tests.Models;

namespace ReadSide.Tests;

public class QueryFactoryTests
{
    [Fact]
    public void Query_factory_without_mappings_uses_the_convention()
    {
        var queries = new QueryFactory(new ReadSideOptions());

        var result = queries.From<Book>().Build();

        Assert.Equal("SELECT * FROM [Books]", result.Sql);
    }
}