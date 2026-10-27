# Contributing to ReadSide

Thanks for taking an interest. ReadSide is a side project I am building to learn how expression trees work, so please keep that in mind.

## Ways to help

- **Open an issue** for a bug, a question or an idea. This is the most useful thing you can do.
- **Small pull requests are welcome:** typos, documentation, and clear bug fixes that come with a test.
- **For a new feature, please open an issue first.** I may want to build it myself to learn from it, or it may not fit the design.

I may decline a pull request, and I cannot promise a fast reply. That is only because this is a side project.

## Building and testing

You need the .NET SDK version in `global.json`.

```bash
dotnet build
dotnet test
```

The tests do not need a database. The generated SQL is also checked by hand against a real SQL Server, so if you change what the SQL looks like, please say what you tried.

## Pull requests

- Add or update a test for every change in behaviour.
- Keep the build free of warnings.
- Use [conventional commit](https://www.conventionalcommits.org/) messages, such as `feat: ...`, `fix: ...` or `docs: ...`.