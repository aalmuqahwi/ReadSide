using ReadSide.Tests.Models;

namespace ReadSide.Tests;

public class QueryFactoryTests
{
    private class BookMapping : IEntityMapping<Book>
    {
        public void Configure(EntityMappingBuilder<Book> entity)
        {
            entity.Table("BookListtbl")
                .Schema("dbo")
                .Column(b => b.Title, "Title_Txt");
        }
    }

    private class BookTableOnlyMapping : IEntityMapping<Book>
    {
        public void Configure(EntityMappingBuilder<Book> entity)
        {
            entity.Table("BookListtbl").Schema("dbo");
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
    public void Mapped_column_name_is_used_in_where()
    {
        var options = new ReadSideOptions().Add(new BookMapping());
        var queries = new QueryFactory(options);

        var result = queries.From<Book>()
            .Where(b => b.Title == "Dune")
            .Select(b => b.Id)
            .Build();

        Assert.Equal("SELECT [Id] FROM [dbo].[BookListtbl] WHERE ([Title_Txt] = @p0)", result.Sql);
    }

    [Fact]
    public void Mapped_column_in_select_keeps_the_property_name_as_alias()
    {
        var options = new ReadSideOptions().Add(new BookMapping());
        var queries = new QueryFactory(options);

        var result = queries.From<Book>().Select(b => new { b.Title, b.Id }).Build();

        Assert.Equal("SELECT [Title_Txt] AS [Title], [Id] FROM [dbo].[BookListtbl]", result.Sql);
    }

    [Fact]
    public void Without_select_a_mapped_column_keeps_the_property_name()
    {
        var options = new ReadSideOptions().Add(new BookMapping());
        var queries = new QueryFactory(options);

        var result = queries.From<Book>().Build();

        Assert.Equal(
            "SELECT [Id], [Title_Txt] AS [Title], [PublishedAt], [IsAvailable], [ReturnedAt] FROM [dbo].[BookListtbl]",
            result.Sql);
    }

    [Fact]
    public void Without_select_a_table_only_mapping_keeps_star()
    {
        var options = new ReadSideOptions().Add(new BookTableOnlyMapping());
        var queries = new QueryFactory(options);

        var result = queries.From<Book>().Build();

        Assert.Equal("SELECT * FROM [dbo].[BookListtbl]", result.Sql);
    }
}