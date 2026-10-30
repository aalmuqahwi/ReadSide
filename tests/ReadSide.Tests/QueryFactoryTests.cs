using ReadSide.Tests.Models;

namespace ReadSide.Tests;

public class QueryFactoryTests
{
    private class BookMapping : IEntityMapping<Book>
    {
        public void Configure(EntityMappingBuilder<Book> entity)
        {
            entity.Table("BookListtbl")
                .Schema("dbo"); 
        }
    }

    [Fact]
    public void Query_factory_without_mappings_uses_the_convention()
    {
        var queries = new QueryFactory(new ReadSideOptions());

        var result = queries.From<Book>().Build();

        Assert.Equal("SELECT * FROM [Books]", result.Sql);
    }

    [Fact]
    public void Mapped_table_and_schema_are_used_in_from()
    {
        var options = new ReadSideOptions().Add(new BookMapping());
        var queries = new QueryFactory(options);

        var result = queries.From<Book>().Build();

        Assert.Equal("SELECT * FROM [dbo].[BookListtbl]", result.Sql);
    }
}