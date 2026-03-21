using Oracle.ManagedDataAccess.Client;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.Oracle;

#pragma warning disable IDE0130
namespace RentADeveloper.DbConnectionPlus.Configuration;
#pragma warning restore IDE0130

/// <summary>
/// Extension methods for registering the Oracle database adapter.
/// </summary>
public static class OracleConfigurationExtensions
{
    /// <summary>
    /// Registers the Oracle database adapter for use with <see cref="OracleConnection" />.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The configuration instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configuration"/> is <see langword="null"/>.
    /// </exception>
    public static DbConnectionPlusConfiguration UseOracle(this DbConnectionPlusConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        configuration.RegisterDatabaseAdapter<OracleConnection>(new OracleDatabaseAdapter());

        return configuration;
    }
}
