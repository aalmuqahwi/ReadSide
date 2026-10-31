using System.Linq.Expressions;

namespace ReadSide;

/// <summary>
/// Collects the settings for one entity.
/// </summary>
/// <typeparam name="T">The entity being mapped.</typeparam>
public class EntityMappingBuilder<T>
{
    private string? _table;
    private string? _schema;
    private readonly Dictionary<string, string> _columns = [];
    private readonly HashSet<string> _ignored = [];

    /// <summary>
    /// Sets the table name.
    /// </summary>
    /// <param name="name">The table name in the database.</param>
    /// <returns>The same builder, so calls can be chained.</returns>
    public EntityMappingBuilder<T> Table(string name)
    {
        _table = name;
        return this;
    }

    /// <summary>
    /// Sets the schema name.
    /// </summary>
    /// <param name="name">The schema name in the database, such as <c>dbo</c>.</param>
    /// <returns>The same builder, so calls can be chained.</returns>
    public EntityMappingBuilder<T> Schema(string name)
    {
        _schema = name;
        return this;
    }

    /// <summary>
    /// Sets the database column name for one property.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <param name="property">A lambda that picks the property, such as <c>b => b.Title</c>.</param>
    /// <param name="name">The column name in the database.</param>
    /// <returns>The same builder, so calls can be chained.</returns>
    /// <exception cref="ArgumentException">The lambda is not a simple property of the entity.</exception>
    public EntityMappingBuilder<T> Column<TProperty>(Expression<Func<T, TProperty>> property, string name)
    {
        _columns[PropertyName(property)] = name;
        return this;
    }

    /// <summary>
    /// Marks a property as not being a column, such as a property computed in C#.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <param name="property">A lambda that picks the property, such as <c>b => b.Label</c>.</param>
    /// <returns>The same builder, so calls can be chained.</returns>
    /// <exception cref="ArgumentException">The lambda is not a simple property of the entity.</exception>
    public EntityMappingBuilder<T> Ignore<TProperty>(Expression<Func<T, TProperty>> property)
    {
        _ignored.Add(PropertyName(property));
        return this;
    }

    private static string PropertyName<TProperty>(Expression<Func<T, TProperty>> property)
    {
        return property.Body is not MemberExpression { Expression: ParameterExpression } member
            ? throw new ArgumentException("Pick a property of the entity, such as b => b.Title.", nameof(property))
            : member.Member.Name;
    }

    internal EntityMap ToEntityMap() => 
        new(_table, _schema, new Dictionary<string, string>(_columns), new HashSet<string>(_ignored));
}