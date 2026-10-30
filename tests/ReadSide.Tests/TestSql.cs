using System.Linq.Expressions;

namespace ReadSide.Tests;

internal static class TestSql
{
    public static string ToSql(Expression node, Dictionary<string, object?> parameters)
        => new SqlTranslator(new ReadSideOptions()).ToSql(node, parameters);

    public static string ToOrderBy(IEnumerable<(Expression Column, bool Descending)> clauses)
        => new SqlTranslator(new ReadSideOptions()).ToOrderBy(clauses);
}