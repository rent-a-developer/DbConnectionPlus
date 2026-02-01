namespace RentADeveloper.DbConnectionPlus.Configuration;

/// <summary>
/// A builder for configuring an entity property.
/// </summary>
public sealed class EntityPropertyBuilder : IEntityPropertyBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityPropertyBuilder" /> class.
    /// </summary>
    /// <param name="entityTypeBuilder">The entity type builder this property builder belongs to.</param>
    /// <param name="propertyName">The name of the property being configured.</param>
    internal EntityPropertyBuilder(IEntityTypeBuilder entityTypeBuilder, String propertyName)
    {
        this.entityTypeBuilder = entityTypeBuilder;
        this.propertyName = propertyName;
    }

    /// <summary>
    /// Sets the name of the column to map the property to.
    /// </summary>
    /// <param name="columnName">The name of the column to map the property to.</param>
    /// <returns>This builder instance for further configuration.</returns>
    /// <exception cref="InvalidOperationException">
    /// The configuration of DbConnectionPlus is already frozen and can no longer be modified.
    /// </exception>
    // ReSharper disable once ParameterHidesMember
    public EntityPropertyBuilder HasColumnName(String columnName)
    {
        this.EnsureNotFrozen();

        this.columnName = columnName;
        return this;
    }

    /// <summary>
    /// Marks the property as mapped to a computed database column.
    /// Such properties will be ignored during insert and update operations.
    /// Their values will be read back from the database after an insert or update and populated on the entity.
    /// </summary>
    /// <returns>This builder instance for further configuration.</returns>
    /// <exception cref="InvalidOperationException">
    /// The configuration of DbConnectionPlus is already frozen and can no longer be modified.
    /// </exception>
    public EntityPropertyBuilder IsComputed()
    {
        this.EnsureNotFrozen();

        this.isComputed = true;
        return this;
    }

    /// <summary>
    /// Marks the property as mapped to an identity database column.
    /// Such properties will be ignored during insert and update operations.
    /// Their values will be read back from the database after an insert or update and populated on the entity.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Another property is already marked as an identity property for the entity type.
    /// </exception>
    /// <returns>This builder instance for further configuration.</returns>
    /// <exception cref="InvalidOperationException">
    /// The configuration of DbConnectionPlus is already frozen and can no longer be modified.
    /// </exception>
    public EntityPropertyBuilder IsIdentity()
    {
        this.EnsureNotFrozen();

        var otherIdentityProperty =
            this.entityTypeBuilder.PropertyBuilders.Values.FirstOrDefault(a =>
                a.PropertyName != this.propertyName && a.IsIdentity
            );

        if (otherIdentityProperty is not null)
        {
            throw new InvalidOperationException(
                $"There is already the property '{otherIdentityProperty.PropertyName}' marked as an identity " +
                $"property for the entity type {this.entityTypeBuilder.EntityType}. Only one property can be marked " +
                "as identity property per entity type."
            );
        }

        this.isIdentity = true;
        return this;
    }

    /// <summary>
    /// Marks the property to not be mapped to a database column.
    /// </summary>
    /// <returns>This builder instance for further configuration.</returns>
    /// <exception cref="InvalidOperationException">
    /// The configuration of DbConnectionPlus is already frozen and can no longer be modified.
    /// </exception>
    public EntityPropertyBuilder IsIgnored()
    {
        this.EnsureNotFrozen();

        this.isIgnored = true;
        return this;
    }

    /// <summary>
    /// Marks the property as a key property.
    /// </summary>
    /// <returns>This builder instance for further configuration.</returns>
    /// <exception cref="InvalidOperationException">
    /// The configuration of DbConnectionPlus is already frozen and can no longer be modified.
    /// </exception>
    public EntityPropertyBuilder IsKey()
    {
        this.EnsureNotFrozen();

        this.isKey = true;
        return this;
    }

    /// <inheritdoc />
    String? IEntityPropertyBuilder.ColumnName => this.columnName;

    /// <inheritdoc />
    void IFreezable.Freeze() => this.isFrozen = true;

    /// <inheritdoc />
    Boolean IEntityPropertyBuilder.IsComputed => this.isComputed;

    /// <inheritdoc />
    Boolean IEntityPropertyBuilder.IsIdentity => this.isIdentity;

    /// <inheritdoc />
    Boolean IEntityPropertyBuilder.IsIgnored => this.isIgnored;

    /// <inheritdoc />
    Boolean IEntityPropertyBuilder.IsKey => this.isKey;

    /// <inheritdoc />
    String IEntityPropertyBuilder.PropertyName => this.propertyName;

    /// <summary>
    /// Ensures this instance is not frozen.
    /// </summary>
    /// <exception cref="InvalidOperationException">This object is already frozen.</exception>
    private void EnsureNotFrozen()
    {
        if (this.isFrozen)
        {
            ThrowHelper.ThrowConfigurationIsFrozenException();
        }
    }

    private readonly IEntityTypeBuilder entityTypeBuilder;
    private readonly String propertyName;

    private String? columnName;
    private Boolean isComputed;
    private Boolean isFrozen;
    private Boolean isIdentity;
    private Boolean isIgnored;
    private Boolean isKey;
}
