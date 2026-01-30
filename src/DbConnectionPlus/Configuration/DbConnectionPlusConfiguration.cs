namespace RentADeveloper.DbConnectionPlus.Configuration;

/// <summary>
/// The configuration for DbConnectionPlus.
/// </summary>
public sealed class DbConnectionPlusConfiguration : IFreezable
{
    /// <summary>
    /// <para>
    /// Controls how <see cref="Enum" /> values are serialized when they are sent to a database using one of the
    /// following methods:
    /// </para>
    /// <para>
    /// 1. When an entity containing an enum property is inserted via
    /// <see cref="DbConnectionExtensions.InsertEntities{TEntity}" />,
    /// <see cref="DbConnectionExtensions.InsertEntitiesAsync{TEntity}" />,
    /// <see cref="DbConnectionExtensions.InsertEntity{TEntity}" /> or
    /// <see cref="DbConnectionExtensions.InsertEntityAsync{TEntity}" />.
    /// </para>
    /// <para>
    /// 2. When an entity containing an enum property is updated via
    /// <see cref="DbConnectionExtensions.UpdateEntities{TEntity}" />,
    /// <see cref="DbConnectionExtensions.UpdateEntitiesAsync{TEntity}" />,
    /// <see cref="DbConnectionExtensions.UpdateEntity{TEntity}" /> or
    /// <see cref="DbConnectionExtensions.UpdateEntityAsync{TEntity}" />.
    /// </para>
    /// <para>
    /// 3. When an enum value is passed as a parameter to an SQL statement via
    /// <see cref="DbConnectionExtensions.Parameter" />.
    /// </para>
    /// <para>
    /// 4. When a sequence of enum values is passed as a temporary table to an SQL statement via
    /// <see cref="DbConnectionExtensions.TemporaryTable{T}" />.
    /// </para>
    /// <para>
    /// 5. When objects containing an enum property are passed as a temporary table to an SQL statement via
    /// <see cref="DbConnectionExtensions.TemporaryTable{T}" />.
    /// </para>
    /// <para>The default is <see cref="DbConnectionPlus.EnumSerializationMode.Strings" />.</para>
    /// </summary>
    public EnumSerializationMode EnumSerializationMode
    {
        get;
        set
        {
            this.EnsureNotFrozen();
            field = value;
        }
    } = EnumSerializationMode.Strings;

    /// <summary>
    /// A function that can be used to intercept database commands executed via DbConnectionPlus.
    /// Can be used for logging or modifying commands before execution.
    /// </summary>
    public InterceptDbCommand? InterceptDbCommand
    {
        get;
        set
        {
            this.EnsureNotFrozen();
            field = value;
        }
    }

    /// <summary>
    /// Gets a builder for configuring the entity type <typeparamref name="TEntity" />.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity to configure.</typeparam>
    /// <returns>A builder to configure the entity type.</returns>
    public EntityTypeBuilder<TEntity> Entity<TEntity>()
    {
        this.EnsureNotFrozen();

        return (EntityTypeBuilder<TEntity>)this.entityTypeBuilders.GetOrAdd(
            typeof(TEntity),
            _ => new EntityTypeBuilder<TEntity>()
        );
    }

    /// <inheritdoc />
    void IFreezable.Freeze()
    {
        this.isFrozen = true;

        foreach (var entityTypeBuilder in this.entityTypeBuilders.Values)
        {
            entityTypeBuilder.Freeze();
        }
    }

    /// <summary>
    /// The singleton instance of <see cref="DbConnectionPlusConfiguration" />.
    /// </summary>
    public static DbConnectionPlusConfiguration Instance { get; internal set; } = new();

    /// <summary>
    /// Gets the configured entity type builders.
    /// </summary>
    /// <returns>The configured entity type builders.</returns>
    internal IReadOnlyDictionary<Type, IEntityTypeBuilder> GetEntityTypeBuilders() => this.entityTypeBuilders;

    private void EnsureNotFrozen()
    {
        if (this.isFrozen)
        {
            ThrowHelper.ThrowConfigurationIsFrozenException();
        }
    }

    private readonly ConcurrentDictionary<Type, IEntityTypeBuilder> entityTypeBuilders = new();
    private Boolean isFrozen;
}
