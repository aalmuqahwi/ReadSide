using System.Linq.Expressions;

namespace ReadSide.Tests;

internal static class TestSql
{
    public static string ToSql(Expression node, Dictionary<string, object?> parameters)
        => SqlTranslator.ToSql(node, parameters);

    public static string ToOrderBy(IEnumerable<(Expression Column, bool Descending)> clauses)
        => SqlTranslator.ToOrderBy(clauses);
}