// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.TestDatabase.Containers;

/// <summary>
/// Represents a fixture that runs the server of a test database system in a Docker container.
/// </summary>
internal interface ITestDatabaseContainerFixture : IAsyncLifetime
{
    /// <summary>
    /// The connection string that connects to the database server running in the container.
    /// </summary>
    /// <remarks>
    /// The container publishes its port to a free port of the host, so this is only known once the container
    /// has been started.
    /// </remarks>
    public string ConnectionString { get; }
}
