// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.MySql;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.Oracle;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.PostgreSql;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.Sqlite;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.SqlServer;

namespace RentADeveloper.DbConnectionPlus.DatabaseAdapters;

// TODO: Move to DbConnectionPlusConfiguration.
/// <summary>
/// A registry for database adapters that adopt DbConnectionPlus to specific database systems.
/// </summary>
public static class DatabaseAdapterRegistry
{
    /// <summary>
    /// Initializes the <see cref="DatabaseAdapterRegistry" /> class by registering the default database adapters.
    /// </summary>
    static DatabaseAdapterRegistry()
    {
        adapters.TryAdd(typeof(MySqlConnection), new MySqlDatabaseAdapter());
        adapters.TryAdd(typeof(OracleConnection), new OracleDatabaseAdapter());
        adapters.TryAdd(typeof(NpgsqlConnection), new PostgreSqlDatabaseAdapter());
        adapters.TryAdd(typeof(SqliteConnection), new SqliteDatabaseAdapter());
        adapters.TryAdd(typeof(SqlConnection), new SqlServerDatabaseAdapter());
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
    public static void RegisterAdapter<TConnection>(IDatabaseAdapter adapter)
        where TConnection : DbConnection
    {
        ArgumentNullException.ThrowIfNull(adapter);

        adapters.AddOrUpdate(typeof(TConnection), adapter, (_, _) => adapter);
    }

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
    internal static IDatabaseAdapter GetAdapter(Type connectionType)
    {
        ArgumentNullException.ThrowIfNull(connectionType);

        return adapters.TryGetValue(connectionType, out var adapter)
            ? adapter
            : throw new InvalidOperationException(
                $"No database adapter is registered for the database connection of the type {connectionType}. " +
                $"Please call {nameof(DatabaseAdapterRegistry)}.{nameof(RegisterAdapter)} to register an adapter " +
                "for that connection type."
            );
    }

    private static readonly ConcurrentDictionary<Type, IDatabaseAdapter> adapters = [];
}
