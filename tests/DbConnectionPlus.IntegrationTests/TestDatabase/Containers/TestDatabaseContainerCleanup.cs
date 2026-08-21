// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.TestDatabase.Containers;

/// <summary>
/// Removes the database containers the test run started when the run ends.
/// </summary>
/// <remarks>
/// Registered as an assembly fixture in <c>AssemblyAttributes.cs</c>, which is what gets it disposed after the
/// last test of the assembly - a class fixture would tear the containers down after every test class. Testcontainers'
/// resource reaper would eventually clean up too, but only after the process is gone.
/// </remarks>
public sealed class TestDatabaseContainerCleanup : IAsyncLifetime
{
    /// <inheritdoc />
    public ValueTask DisposeAsync() =>
        TestDatabaseContainers.DisposeAsync();

    /// <inheritdoc />
    public ValueTask InitializeAsync() =>
        default;
}
