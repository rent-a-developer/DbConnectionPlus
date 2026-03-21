using Microsoft.Data.Sqlite;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.Sqlite;

#pragma warning disable IDE0130
namespace RentADeveloper.DbConnectionPlus.Configuration;
#pragma warning restore IDE0130

/// <summary>
/// Extension methods for registering the SQLite database adapter.
/// </summary>
public static class SqliteConfigurationExtensions
{
    /// <summary>
    /// Registers the SQLite database adapter for use with <see cref="SqliteConnection" />.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The configuration instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configuration"/> is <see langword="null"/>.
    /// </exception>
    public static DbConnectionPlusConfiguration UseSqlite(this DbConnectionPlusConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        configuration.RegisterDatabaseAdapter<SqliteConnection>(new SqliteDatabaseAdapter());

        return configuration;
    }
}
