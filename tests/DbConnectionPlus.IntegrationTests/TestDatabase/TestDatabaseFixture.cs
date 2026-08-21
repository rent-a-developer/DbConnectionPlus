// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.TestDatabase;

/// <summary>
/// Makes sure the database server that <typeparamref name="TTestDatabaseProvider" /> connects to is running and
/// accepting connections before the first test of a test class runs.
/// </summary>
/// <typeparam name="TTestDatabaseProvider">The type of the test database provider used by the test class.</typeparam>
/// <remarks>
/// <see cref="IntegrationTestsBase{TTestDatabaseProvider}" /> declares this as an
/// <see cref="IClassFixture{TFixture}" /> without taking it as a constructor argument - it exists for its
/// lifetime, not for its data. xUnit creates a class fixture immediately before the first test of a class and
/// awaits <see cref="InitializeAsync" />, which is what lets the constructor of the test class open a connection
/// straight away, and what keeps a run from starting containers for database systems it does not test.
/// <para>
/// The container itself outlives this fixture and is shared by every test class of that database system, so
/// <see cref="DisposeAsync" /> does nothing - see <see cref="Containers.TestDatabaseContainers" />.
/// </para>
/// </remarks>
public sealed class TestDatabaseFixture<TTestDatabaseProvider> : IAsyncLifetime
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    /// <inheritdoc />
    public ValueTask DisposeAsync() =>
        default;

    /// <inheritdoc />
    public ValueTask InitializeAsync() =>
        TTestDatabaseProvider.StartDatabaseAsync();
}
