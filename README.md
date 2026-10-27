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

`Book` is any class with public properties. The table name is the class name plus `s`.

## Features

- [x] `Select`: one column or an anonymous type, with aliases (`new { BookTitle = b.Title }`)
- [x] `Where` with `==`, `!=`, `<`, `>`, `<=`, `>=`; several calls are joined with `AND`
- [x] `&&`, `||`, `!` with correct grouping
- [x] `IS NULL` / `IS NOT NULL`, including a captured variable that is `null`
- [x] Boolean columns: `Where(b => b.IsAvailable)` and `Where(b => !b.IsAvailable)`
- [x] `Contains`, `StartsWith`, `EndsWith` on strings, translated to `LIKE` (special characters are escaped)
- [x] `Contains` on a list or array, translated to `IN`
- [x] Captured variables become parameters
- [x] Every value is a parameter, never pasted into the SQL
- [x] `OrderBy`, `OrderByDescending`, `ThenBy`, `ThenByDescending`
- [x] `Skip` / `Take` (`OFFSET ... FETCH`)
- [x] Identifier quoting (`[Order]`)
- [x] Immutable queries: each call returns a new one
- [x] Clear errors for unsupported expressions
- [ ] Pluggable dialects (PostgreSQL, MySQL, ...)
- [ ] Select into classes (`new Dto { ... }`)
- [ ] Aggregates and `GroupBy`
- [ ] Joins
- [ ] Table and column mapping configuration
- [ ] Raw SQL fragments
- [ ] Dapper and dependency-injection packages
- [ ] NuGet package

## Limitations

- SQL Server syntax only
- The table name is the class name plus `s`, with no override yet
- No nested members (`b.Author.Name`), aggregates, or joins
- Only these method calls are translated: `Contains`, `StartsWith` and `EndsWith` on strings, and `Contains` on a list or array. Other methods throw a clear error.
- `IN` lists are limited by SQL Server's maximum of about 2,100 parameters per statement
- Window functions are not planned

## Design notes

- The core only produces SQL and parameters; it never touches a connection, so it works with Dapper or anything else.
- Queries are immutable. Each method returns a new query, and the SQL is assembled once, in `Build()`.
- Expressions it cannot translate throw a `NotSupportedException` that quotes the expression, instead of producing wrong SQL.

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
- **LIKE (Transact-SQL)**, on Microsoft Learn (the section on the ESCAPE clause): which characters are special in a LIKE pattern, and how ESCAPE makes them match literally. (clauses collected, SQL compiled at the end)