using System.Linq.Expressions;

namespace ReadSide;

// Replaces the key parameter of a grouped Select with the expression the rows were grouped by.
//   group by b.IsAvailable, select Available = key   →   Available = b.IsAvailable
internal sealed class KeyReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _keyParameter;
    private readonly Expression _key;

    public KeyReplacer(ParameterExpression keyParameter, Expression key)
    {
        _keyParameter = keyParameter;
        _key = key;
    }

    protected override Expression VisitParameter(ParameterExpression node)
        => node == _keyParameter ? _key : node;
}