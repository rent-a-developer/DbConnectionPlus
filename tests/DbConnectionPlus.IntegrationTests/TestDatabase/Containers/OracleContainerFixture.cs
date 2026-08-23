// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using System.Data.Common;
using Oracle.ManagedDataAccess.Client;
using Testcontainers.Oracle;
using Testcontainers.Xunit;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.TestDatabase.Containers;

/// <summary>
/// Runs the Oracle server the Oracle integration tests use in a Docker container.
/// </summary>
internal sealed class OracleContainerFixture()
    : DbContainerFixture<OracleBuilder, OracleContainer>(TestDatabaseDiagnosticMessageSink.Instance),
        ITestDatabaseContainerFixture
{
    /// <summary>
    /// The image the container runs.
    /// </summary>
    /// <remarks>
    /// The faststart variants carry an already created database and come up in well under a minute, where the
    /// plain image spends several minutes creating FREEPDB1 on first start. The tag has to name the major
    /// version: the module reads it to decide that this image serves FREEPDB1 rather than XEPDB1.
    /// </remarks>
    private const string Image = "gvenzl/oracle-free:23-slim-faststart";

    private const string ServiceName = "FREEPDB1";

    private const string SystemUsername = "SYSTEM";

    /// <inheritdoc />
    /// <remarks>
    /// The tests connect as <c>SYSTEM</c>, not as the unprivileged application user the module creates: the
    /// command-timeout tests call <c>DBMS_LOCK.SLEEP</c>, on which that user holds no EXECUTE grant.
    /// <see cref="OracleBuilder.WithPassword" /> sets the password of both accounts.
    /// </remarks>
    public override string ConnectionString =>
        new OracleConnectionStringBuilder
        {
            DataSource = $"{this.Container.Hostname}:{this.MappedPort}/{ServiceName}",
            UserID = SystemUsername,
            Password = TestDatabaseContainers.Password,
        }.ConnectionString;

    /// <inheritdoc />
    public override DbProviderFactory DbProviderFactory => OracleClientFactory.Instance;

    /// <summary>
    /// The host port the container's Oracle listener is published on.
    /// </summary>
    private ushort MappedPort => this.Container.GetMappedPublicPort(OracleBuilder.OraclePort);

    /// <inheritdoc />
    protected override OracleBuilder Configure() =>
        // WithDatabase is deliberately not called: for an Oracle 18+ image the module would only pass the name
        // on to ORACLE_DATABASE if it differed from the pluggable database the image already ships, and asking
        // this one to create a second FREEPDB1 fails.
        new OracleBuilder(Image).WithPassword(TestDatabaseContainers.Password);
}
