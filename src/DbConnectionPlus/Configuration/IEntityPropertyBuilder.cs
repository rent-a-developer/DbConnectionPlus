namespace RentADeveloper.DbConnectionPlus.Configuration;

/// <summary>
/// Represents a builder for configuring an entity property.
/// </summary>
internal interface IEntityPropertyBuilder : IFreezable
{
    /// <summary>
    /// The name of the column the property is mapped to.
    /// </summary>
    internal string? ColumnName { get; }

    /// <summary>
    /// Determines whether the property is mapped to a computed database column.
    /// </summary>
    internal bool IsComputed { get; }

    /// <summary>
    /// Determines whether the property participates in optimistic concurrency checks.
    /// </summary>
    internal bool IsConcurrencyToken { get; }

    /// <summary>
    /// Determines whether the property is mapped to an identity database column.
    /// </summary>
    internal bool IsIdentity { get; }

    /// <summary>
    /// Determines whether the property is not mapped to a database column.
    /// </summary>
    internal bool IsIgnored { get; }

    /// <summary>
    /// Determines whether the property is mapped to a key database column.
    /// </summary>
    internal bool IsKey { get; }

    /// <summary>
    /// Determines whether the property is a row version used for concurrency control.
    /// </summary>
    internal bool IsRowVersion { get; }

    /// <summary>
    /// The name of the property being configured.
    /// </summary>
    internal string PropertyName { get; }
}
