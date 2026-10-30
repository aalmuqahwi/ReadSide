namespace ReadSide;

/// <summary>
/// Settings for ReadSide, created once and shared.
/// </summary>
public class ReadSideOptions
{
    private readonly Dictionary<Type, EntityMap> _entities = [];

    /// <summary>
    /// Adds the mapping for one entity.
    /// </summary>
    /// <typeparam name="T">The entity the mapping describes.</typeparam>
    /// <param name="mapping">The mapping to apply.</param>
    /// <returns>The same options, so calls can be chained.</returns>
    public ReadSideOptions Add<T>(IEntityMapping<T> mapping)
    {
        var builder = new EntityMappingBuilder<T>();
        mapping.Configure(builder);
        _entities[typeof(T)] = builder.ToEntityMap();
        return this;
    }

    internal EntityMap? Find(Type entity) => _entities.GetValueOrDefault(entity);
}
