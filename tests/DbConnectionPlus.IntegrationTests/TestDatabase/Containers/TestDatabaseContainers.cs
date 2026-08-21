// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.TestDatabase.Containers;

/// <summary>
/// The Docker containers the integration tests run their databases in.
/// </summary>
/// <remarks>
/// Testcontainers owns the whole lifecycle: each container is started by the first test class that needs it (see
/// <see cref="TestDatabaseFixture{TTestDatabaseProvider}" />), publishes its port to a free port of the host, and
/// is removed again by <see cref="TestDatabaseContainerCleanup" /> when the run ends. Nothing has to be started
/// by hand, and nothing has to agree on a port number.
/// </remarks>
internal static class TestDatabaseContainers
{
    /// <summary>
    /// The password of the administrative database user in every container.
    /// </summary>
    public const String Password = "TestTest123!";

    /// <summary>
    /// The container running the MySQL server.
    /// </summary>
    public static MySqlContainerFixture MySql =>
        mySql.Fixture;

    /// <summary>
    /// The container running the Oracle server.
    /// </summary>
    public static OracleContainerFixture Oracle =>
        oracle.Fixture;

    /// <summary>
    /// The container running the PostgreSQL server.
    /// </summary>
    public static PostgreSqlContainerFixture PostgreSql =>
        postgreSql.Fixture;

    /// <summary>
    /// The container running the SQL Server server.
    /// </summary>
    public static SqlServerContainerFixture SqlServer =>
        sqlServer.Fixture;

    /// <summary>
    /// Stops and removes every container that was started during the test run.
    /// </summary>
    public static async ValueTask DisposeAsync()
    {
        await mySql.DisposeAsync();
        await oracle.DisposeAsync();
        await postgreSql.DisposeAsync();
        await sqlServer.DisposeAsync();
    }

    /// <summary>
    /// Starts the MySQL container and waits until it accepts connections.
    /// </summary>
    public static ValueTask StartMySqlAsync() =>
        mySql.StartAsync();

    /// <summary>
    /// Starts the Oracle container and waits until it accepts connections.
    /// </summary>
    public static ValueTask StartOracleAsync() =>
        oracle.StartAsync();

    /// <summary>
    /// Starts the PostgreSQL container and waits until it accepts connections.
    /// </summary>
    public static ValueTask StartPostgreSqlAsync() =>
        postgreSql.StartAsync();

    /// <summary>
    /// Starts the SQL Server container and waits until it accepts connections.
    /// </summary>
    public static ValueTask StartSqlServerAsync() =>
        sqlServer.StartAsync();

    private static readonly TestDatabaseContainer<MySqlContainerFixture> mySql = new("MySQL");

    private static readonly TestDatabaseContainer<OracleContainerFixture> oracle = new("Oracle");

    private static readonly TestDatabaseContainer<PostgreSqlContainerFixture> postgreSql = new("PostgreSQL");

    private static readonly TestDatabaseContainer<SqlServerContainerFixture> sqlServer = new("SQL Server");
}
