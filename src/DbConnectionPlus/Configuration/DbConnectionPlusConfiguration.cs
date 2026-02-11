using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.MySql;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.Oracle;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.PostgreSql;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.Sqlite;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.SqlServer;

namespace RentADeveloper.DbConnectionPlus.Configuration;

/// <summary>
/// The configuration for DbConnectionPlus.
/// </summary>
public sealed class DbConnectionPlusConfiguration : IFreezable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DbConnectionPlusConfiguration" /> class.
    /// </summary>
    internal DbConnectionPlusConfiguration()
    {
        this.databaseAdapters.Add(typeof(MySqlConnection), new MySqlDatabaseAdapter());
        this.databaseAdapters.Add(typeof(OracleConnection), new OracleDatabaseAdapter());
        this.databaseAdapters.Add(typeof(NpgsqlConnection), new PostgreSqlDatabaseAdapter());
        this.databaseAdapters.Add(typeof(SqliteConnection), new SqliteDatabaseAdapter());
        this.databaseAdapters.Add(typeof(SqlConnection), new SqlServerDatabaseAdapter());
    }

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
    /// <exception cref="InvalidOperationException">
    /// An attempt was made to modify this property and the configuration of DbConnectionPlus is already frozen and
    /// can no longer be modified.
    /// </exception>
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
    /// <exception cref="InvalidOperationException">
    /// An attempt was made to modify this property and the configuration of DbConnectionPlus is already frozen and
    /// can no longer be modified.
    /// </exception>
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
    /// <exception cref="InvalidOperationException">
    /// The configuration of DbConnectionPlus is already frozen and can no longer be modified.
    /// </exception>
    public EntityTypeBuilder<TEntity> Entity<TEntity>()
    {
        this.EnsureNotFrozen();

        if (!this.entityTypeBuilders.TryGetValue(typeof(TEntity), out var builder))
        {
            builder = new EntityTypeBuilder<TEntity>();

            this.entityTypeBuilders.Add(typeof(TEntity), builder);
        }

        return (EntityTypeBuilder<TEntity>)builder;
    }

    /// <summary>
    /// Registers a database adapter for the connection type <typeparamref name="TConnection" />.
    /// If an adapter is already registered for the connection type <typeparamref name="TConnection" />, the existing
    /// adapter is replaced.
    /// </summary>
    /// <typeparam name="TConnection">
    /// The type of database connection for which <paramref name="adapter" /> is being registered.
    /// </typeparam>
    /// <param name="adapter">
    /// The database adapter to associate with the connection type <typeparamref name="TConnection" />.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="adapter" /> is <see langword="null" />.</exception>
    public void RegisterDatabaseAdapter<TConnection>(IDatabaseAdapter adapter)
        where TConnection : DbConnection
    {
        ArgumentNullException.ThrowIfNull(adapter);

        this.databaseAdapters[typeof(TConnection)] = adapter;
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
    /// Retrieves the database adapter associated with the connection type <paramref name="connectionType" />.
    /// </summary>
    /// <param name="connectionType">
    /// The type of the database connection for which to retrieve the adapter.
    /// </param>
    /// <returns>
    /// An <see cref="IDatabaseAdapter" /> instance that supports database connections of the type
    /// <paramref name="connectionType" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="connectionType" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// No adapter is registered for the connection type <paramref name="connectionType" />.
    /// </exception>
    internal IDatabaseAdapter GetDatabaseAdapter(Type connectionType)
    {
        ArgumentNullException.ThrowIfNull(connectionType);

        return this.databaseAdapters.TryGetValue(connectionType, out var adapter)
            ? adapter
            : throw new InvalidOperationException(
                $"No database adapter is registered for the database connection of the type {connectionType}. " +
                $"Please call {nameof(DbConnectionExtensions)}.{nameof(DbConnectionExtensions.Configure)} to " +
                "register an adapter for that connection type."
            );
    }

    /// <summary>
    /// Gets the configured entity type builders.
    /// </summary>
    /// <returns>The configured entity type builders.</returns>
    internal IReadOnlyDictionary<Type, IEntityTypeBuilder> GetEntityTypeBuilders() => this.entityTypeBuilders;

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

    private readonly Dictionary<Type, IDatabaseAdapter> databaseAdapters = [];
    private readonly Dictionary<Type, IEntityTypeBuilder> entityTypeBuilders = new();
    private Boolean isFrozen;
}
