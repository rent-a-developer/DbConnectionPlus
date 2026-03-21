using Npgsql;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.PostgreSql;

#pragma warning disable IDE0130
namespace RentADeveloper.DbConnectionPlus.Configuration;
#pragma warning restore IDE0130

/// <summary>
/// Extension methods for registering the PostgreSQL database adapter.
/// </summary>
public static class PostgreSqlConfigurationExtensions
{
    /// <summary>
    /// Registers the PostgreSQL database adapter for use with <see cref="NpgsqlConnection" />.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The configuration instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configuration"/> is <see langword="null"/>.
    /// </exception>
    public static DbConnectionPlusConfiguration UsePostgreSql(this DbConnectionPlusConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        configuration.RegisterDatabaseAdapter<NpgsqlConnection>(new PostgreSqlDatabaseAdapter());

        return configuration;
    }
}
