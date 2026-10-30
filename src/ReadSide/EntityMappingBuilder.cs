namespace ReadSide;

/// <summary>
/// Collects the settings for one entity.
/// </summary>
/// <typeparam name="T">The entity being mapped.</typeparam>
public class EntityMappingBuilder<T>
{
    private string? _table;
    private string? _schema;

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

    internal EntityMap ToEntityMap() => new(_table, _schema);
}