using MySqlConnector;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.MySql;

#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace RentADeveloper.DbConnectionPlus.Configuration;

#pragma warning restore IDE0130

/// <summary>
/// Extension methods for registering the MySQL database adapter.
/// </summary>
public static class MySqlConfigurationExtensions
{
    /// <summary>
    /// Registers the MySQL database adapter for use with <see cref="MySqlConnection" />.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The configuration instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configuration"/> is <see langword="null"/>.
    /// </exception>
    public static DbConnectionPlusConfiguration UseMySql(this DbConnectionPlusConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        configuration.RegisterDatabaseAdapter<MySqlConnection>(new MySqlDatabaseAdapter());

        return configuration;
    }
}
