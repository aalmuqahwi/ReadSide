# ReadSide
[![CI](https://github.com/aalmuqahwi/ReadSide/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/aalmuqahwi/ReadSide/actions/workflows/ci.yml)

Typed, refactor-safe read side for CQRS. Work in progress.

## Status

🚧 A learning project, built to explore expression trees and SQL generation in C#. Pre-1.0: the API may change.

- Targets SQL Server. Not published to NuGet yet.
- The generated SQL is verified by tests that compare strings, and has been checked by hand against SQL Server 2022 (in Docker). There are no automated database tests yet.

## Example

```csharp
int minId = 5;

var result = Query.From<Book>()
    .Select(b => new { b.Title, b.Id })
    .Where(b => b.Id > minId && b.Title != null)
    .OrderBy(b => b.Title)
    .Skip(10).Take(5)
    .Build();

// result.Sql:
// SELECT [Title], [Id] FROM [Books]
// WHERE (([Id] > @p0) AND ([Title] IS NOT NULL))
// ORDER BY [Title] ASC
// OFFSET @p1 ROWS FETCH NEXT @p2 ROWS ONLY
//
// result.Parameters: { p0: 5, p1: 10, p2: 5 }
```

`Book` is any class with public properties. By default the table name is the class name plus `s`; see [Mapping](#mapping) to change it.

## Mapping

By default the class `Book` reads from the table `[Books]`, and every property is a column with the same name. If your database uses other names, describe them once in a small class:

```csharp
public class BookMapping : IEntityMapping<Book>
{
    public void Configure(EntityMappingBuilder<Book> entity)
    {
        entity.Table("BookListtbl")
            .Schema("dbo")
            .Column(b => b.Title, "Title_Txt");
    }
}

var options = new ReadSideOptions().Add(new BookMapping());
var queries = new QueryFactory(options);

var result = queries.From<Book>()
    .Where(b => b.Title == "Dune")
    .Select(b => b.Id)
    .Build();
// SELECT [Id] FROM [dbo].[BookListtbl] WHERE ([Title_Txt] = @p0)
```

The results keep the property names: `Title` is returned as `[Title_Txt] AS [Title]`. Without a `Select`, an entity with mapped columns lists its public properties instead of using `*`, so the names still match.

A property that is computed in C# and has no column can be left out with `Ignore`:

```csharp
entity.Ignore(b => b.FullTitle);
```

It is not listed when columns are selected for you, and using it in `Where`, `Select` or `OrderBy` throws a clear error.

With several entities, chain the calls:

```csharp
var options = new ReadSideOptions()
    .Add(new BookMapping())
    .Add(new AuthorMapping())
    .Add(new PublisherMapping());

var queries = new QueryFactory(options);
```

Create the options once, when your application starts, and reuse the same `QueryFactory`. An entity without a mapping keeps the default names.

Planned: registering all mappings of an assembly at once, and a dependency-injection package that sets this up for you.

## Features

- [x] `Select`: one column or an anonymous type, with aliases (`new { BookTitle = b.Title }`)
- [x] `Where` with `==`, `!=`, `<`, `>`, `<=`, `>=`; several calls are joined with `AND`
- [x] `&&`, `||`, `!` with correct grouping
- [x] `IS NULL` / `IS NOT NULL`, including a captured variable that is `null`
- [x] Boolean columns: `Where(b => b.IsAvailable)` and `Where(b => !b.IsAvailable)`
- [x] `Contains`, `StartsWith`, `EndsWith` on strings, translated to `LIKE` (special characters are escaped)
- [x] `Contains` on a list or array, translated to `IN`
- [x] `string.IsNullOrEmpty`, translated to `IS NULL OR = ''`
- [x] Captured variables become parameters
- [x] Every value is a parameter, never pasted into the SQL
- [x] `OrderBy`, `OrderByDescending`, `ThenBy`, `ThenByDescending`
- [x] `Skip` / `Take` (`OFFSET ... FETCH`)
- [x] Identifier quoting (`[Order]`)
- [x] Immutable queries: each call returns a new one
- [x] Clear errors for unsupported expressions
- [x] Select into classes (`new Dto { ... }`)
- [x] Table, schema and column mapping (`IEntityMapping<T>`)
- [x] Ignore a property in a mapping (`entity.Ignore(b => b.Prop)`)
- [ ] Pluggable dialects (PostgreSQL, MySQL, ...)
- [ ] Aggregates and `GroupBy`
- [ ] Joins
- [ ] Raw SQL fragments
- [ ] Dapper and dependency-injection packages
- [ ] NuGet package

## Limitations

- SQL Server syntax only
- Mapping covers table, schema, column names and ignored properties. Views, query filters and value converters are not supported yet
- No nested members (`b.Author.Name`), aggregates, or joins
- Only these method calls are translated: `Contains`, `StartsWith` and `EndsWith` on strings, and `Contains` on a list or array. Other methods throw a clear error.
- `IN` lists are limited by SQL Server's maximum of about 2,100 parameters per statement
- Window functions are not planned

## Design notes

- The core only produces SQL and parameters; it never touches a connection, so it works with Dapper or anything else.
- Queries are immutable. Each method returns a new query, and the SQL is assembled once, in `Build()`.
- Expressions it cannot translate throw a `NotSupportedException` that quotes the expression, instead of producing wrong SQL.
- Settings live in a plain `ReadSideOptions` object that you create once. There is no global state, so tests and applications can each have their own.

## Try it

```bash
git clone https://github.com/aalmuqahwi/ReadSide.git
cd ReadSide
dotnet test
```

## Learning resources

- **Expression trees**, Microsoft Learn (C# docs): how a lambda becomes a tree I can read
- **Translating expression trees**, Microsoft Learn (C# docs): the idea of walking a tree recursively to produce something else, which is what `ToSql` does
- **ORDER BY clause, OFFSET and FETCH**, Microsoft Learn (Transact-SQL reference): the paging syntax behind `Skip` / `Take`
- **SQL Injection Prevention Cheat Sheet**, OWASP: why every value is a parameter and never part of the SQL text
- **Pattern matching**, Microsoft Learn (C# docs): the `is` and property patterns used to inspect nodes
- **`with` expression**, Microsoft Learn (C# docs): copying a record with one change, behind the immutable `Query<T>`
- **Collection expressions**, Microsoft Learn (C# docs): the `[.. list, item]` syntax used to extend lists without mutating them
- **Conventional Commits**: the commit message style used in this repository
- **[SqlKata](https://github.com/sqlkata/querybuilder)**, on GitHub: where I first saw how a fluent query builder is put together
- **LIKE (Transact-SQL)**, on Microsoft Learn (the section on the ESCAPE clause): which characters are special in a LIKE pattern, and how ESCAPE makes them match literally.
- **Creating and configuring a model**, EF Core docs on Microsoft Learn: the idea of one small configuration class per entity, which `IEntityMapping<T>` follows
- **Builder pattern**: building an object step by step with chained calls, as `EntityMappingBuilder<T>` does
- **Factory pattern**: letting one object create the others, as `QueryFactory` does
- **Options pattern**: keeping settings in a plain class that is created once and passed around, as `ReadSideOptions` does