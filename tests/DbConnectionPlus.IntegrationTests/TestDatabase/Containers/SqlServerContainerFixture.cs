// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using System.Data.Common;
using Testcontainers.MsSql;
using Testcontainers.Xunit;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.TestDatabase.Containers;

/// <summary>
/// Runs the SQL Server server the SQL Server integration tests use in a Docker container.
/// </summary>
internal sealed class SqlServerContainerFixture()
    : DbContainerFixture<MsSqlBuilder, MsSqlContainer>(TestDatabaseDiagnosticMessageSink.Instance),
        ITestDatabaseContainerFixture
{
    private const string Image = "mcr.microsoft.com/mssql/server:2022-latest";

    /// <inheritdoc />
    public override string ConnectionString =>
        new SqlConnectionStringBuilder
        {
            DataSource = $"{this.Container.Hostname},{this.Container.GetMappedPublicPort(MsSqlBuilder.MsSqlPort)}",
            UserID = MsSqlBuilder.DefaultUsername,
            Password = TestDatabaseContainers.Password,

            // The certificate the container serves is self-signed and issued for a host name that is not the one
            // we connect to, so an encrypted connection would fail certificate validation.
            Encrypt = false,

            // Several tests execute a command while a data reader is still open.
            MultipleActiveResultSets = true,
        }.ConnectionString;

    /// <inheritdoc />
    public override DbProviderFactory DbProviderFactory => SqlClientFactory.Instance;

    /// <inheritdoc />
    protected override MsSqlBuilder Configure() =>
        new MsSqlBuilder(Image).WithPassword(TestDatabaseContainers.Password);
}
