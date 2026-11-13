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

    // key.Title with a key of new { Title = b.Title, ... }   →   b.Title
    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression == _keyParameter
            && _key is NewExpression { Members: { } members } created)
        {
            for (var i = 0; i < members.Count; i++)
            {
                if (members[i].Name == node.Member.Name)
                {
                    return created.Arguments[i];
                }
            }
        }

        return base.VisitMember(node);
    }
}