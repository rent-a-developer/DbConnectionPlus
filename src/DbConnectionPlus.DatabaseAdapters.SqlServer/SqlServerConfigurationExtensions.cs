using RentADeveloper.DbConnectionPlus.DatabaseAdapters.SqlServer;

#pragma warning disable IDE0130
namespace RentADeveloper.DbConnectionPlus.Configuration;

#pragma warning restore IDE0130

/// <summary>
/// Extension methods for registering the SQL Server database adapter.
/// </summary>
public static class SqlServerConfigurationExtensions
{
    /// <summary>
    /// Registers the SQL Server database adapter for use with <see cref="SqlConnection" />.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The configuration instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configuration"/> is <see langword="null"/>.
    /// </exception>
    public static DbConnectionPlusConfiguration UseSqlServer(this DbConnectionPlusConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        configuration.RegisterDatabaseAdapter<SqlConnection>(new SqlServerDatabaseAdapter());

        return configuration;
    }
}
