namespace RentADeveloper.DbConnectionPlus.Configuration;

/// <summary>
/// Represents a builder for configuring an entity type.
/// </summary>
internal interface IEntityTypeBuilder : IFreezable
{
    /// <summary>
    /// The entity type being configured.
    /// </summary>
    internal Type EntityType { get; }

    /// <summary>
    /// The property builders associated with the entity type.
    /// </summary>
    internal IReadOnlyDictionary<String, IEntityPropertyBuilder> PropertyBuilders { get; }

    /// <summary>
    /// The name of the table the entity type is mapped to.
    /// </summary>
    internal String? TableName { get; }
}
