// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using System.Data.Common;
using MySqlConnector;
using Testcontainers.MySql;
using Testcontainers.Xunit;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.TestDatabase.Containers;

/// <summary>
/// Runs the MySQL server the MySQL integration tests use in a Docker container.
/// </summary>
internal sealed class MySqlContainerFixture()
    : DbContainerFixture<MySqlBuilder, MySqlContainer>(TestDatabaseDiagnosticMessageSink.Instance),
        ITestDatabaseContainerFixture
{
    /// <inheritdoc />
    public override string ConnectionString =>
        new MySqlConnectionStringBuilder
        {
            Server = this.Container.Hostname,
            Port = this.Container.GetMappedPublicPort(MySqlBuilder.MySqlPort),
            UserID = RootUsername,
            Password = TestDatabaseContainers.Password,

            // MySqlTemporaryTableBuilder fills temporary tables with MySqlBulkCopy, which is LOAD DATA LOCAL
            // INFILE underneath and refuses to run unless the client allows it.
            AllowLoadLocalInfile = true,
        }.ConnectionString;

    /// <inheritdoc />
    public override DbProviderFactory DbProviderFactory => MySqlConnectorFactory.Instance;

    /// <inheritdoc />
    protected override MySqlBuilder Configure() =>
        // The tests drop and create their own database, so they connect as root rather than as the unprivileged
        // user the module creates by default.
        new MySqlBuilder(Image)
            .WithUsername(RootUsername)
            .WithPassword(TestDatabaseContainers.Password);

    private const string Image = "mysql:latest";

    private const string RootUsername = "root";
}
