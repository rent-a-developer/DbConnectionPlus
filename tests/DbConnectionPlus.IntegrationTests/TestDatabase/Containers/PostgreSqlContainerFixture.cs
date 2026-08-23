// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using System.Data.Common;
using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.Xunit;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.TestDatabase.Containers;

/// <summary>
/// Runs the PostgreSQL server the PostgreSQL integration tests use in a Docker container.
/// </summary>
internal sealed class PostgreSqlContainerFixture()
    : DbContainerFixture<PostgreSqlBuilder, PostgreSqlContainer>(TestDatabaseDiagnosticMessageSink.Instance),
        ITestDatabaseContainerFixture
{
    private const string Image = "postgres:latest";

    /// <inheritdoc />
    public override string ConnectionString =>
        new NpgsqlConnectionStringBuilder
        {
            Host = this.Container.Hostname,
            Port = this.Container.GetMappedPublicPort(PostgreSqlBuilder.PostgreSqlPort),
            Username = PostgreSqlBuilder.DefaultUsername,
            Password = TestDatabaseContainers.Password,
        }.ConnectionString;

    /// <inheritdoc />
    public override DbProviderFactory DbProviderFactory => NpgsqlFactory.Instance;

    /// <inheritdoc />
    protected override PostgreSqlBuilder Configure() =>
        new PostgreSqlBuilder(Image).WithPassword(TestDatabaseContainers.Password);
}
