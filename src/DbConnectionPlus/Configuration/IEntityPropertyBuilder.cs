namespace RentADeveloper.DbConnectionPlus.Configuration;

/// <summary>
/// Represents a builder for configuring an entity property.
/// </summary>
internal interface IEntityPropertyBuilder : IFreezable
{
    /// <summary>
    /// The name of the column the property is mapped to.
    /// </summary>
    internal String? ColumnName { get; }

    /// <summary>
    /// Determines whether the property is mapped to a computed database column.
    /// </summary>
    internal Boolean IsComputed { get; }

    /// <summary>
    /// Determines whether the property is mapped to an identity database column.
    /// </summary>
    internal Boolean IsIdentity { get; }

    /// <summary>
    /// Determines whether the property is not mapped to a database column.
    /// </summary>
    internal Boolean IsIgnored { get; }

    /// <summary>
    /// Determines whether the property is mapped to a key database column.
    /// </summary>
    internal Boolean IsKey { get; }

    /// <summary>
    /// The name of the property being configured.
    /// </summary>
    internal String PropertyName { get; }
}
