namespace ReadSide;

/// <summary>
/// Describes how one entity class maps to the database. Write one small class per entity.
/// </summary>
/// <typeparam name="T">The entity this mapping describes.</typeparam>
public interface IEntityMapping<T>
{
    /// <summary>
    /// Sets the mapping for the entity.
    /// </summary>
    /// <param name="entity">The builder to set the table, schema and columns on.</param>
    void Configure(EntityMappingBuilder<T> entity);
}
