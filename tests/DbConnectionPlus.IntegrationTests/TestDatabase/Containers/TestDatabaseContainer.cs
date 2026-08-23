// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using System.Diagnostics;
using System.Globalization;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.TestDatabase.Containers;

/// <summary>
/// Starts the container of a single database system on first use and keeps it running until the test run ends.
/// </summary>
/// <typeparam name="TFixture">The type of the fixture that runs the database server in a container.</typeparam>
/// <param name="databaseSystemName">The name of the database system, used in the diagnostic output.</param>
/// <remarks>
/// Starting is lazy so that a run only pays for the database systems it actually tests: a run filtered to SQLite
/// and SQL Server never starts the MySQL, Oracle or PostgreSQL containers. Sharing is what keeps it to one
/// container per database system - every test class asks for the same instance.
/// </remarks>
internal sealed class TestDatabaseContainer<TFixture>(string databaseSystemName)
    where TFixture : class, ITestDatabaseContainerFixture, new()
{
    /// <summary>
    /// The fixture that runs the database server.
    /// </summary>
    /// <exception cref="InvalidOperationException">The container has not been started.</exception>
    public TFixture Fixture =>
        this.fixture.IsValueCreated
            ? this.fixture.Value.GetAwaiter().GetResult()
            : throw new InvalidOperationException(
                $"The {databaseSystemName} container has not been started. Tests reach a database through "
                    + $"{nameof(IntegrationTestsBase<>)}, which starts the container it needs before the first test "
                    + "of a test class runs."
            );

    /// <summary>
    /// Stops and removes the container, if it was started.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!this.fixture.IsValueCreated)
        {
            return;
        }

        // A container that never started has already failed every test that needed it. Awaiting the task
        // unconditionally would rethrow that here and add a second, less useful failure at the end of the run.
        if (this.fixture.Value.IsCompletedSuccessfully)
        {
            await this.fixture.Value.Result.DisposeAsync();
        }
    }

    /// <summary>
    /// Starts the container and waits until the database server inside it accepts connections. Does nothing if the
    /// container is already starting or started.
    /// </summary>
    public ValueTask StartAsync() => new(this.fixture.Value);

    private static async Task<TFixture> CreateAndStartAsync(string databaseSystemName)
    {
        TestContext.Current.SendDiagnosticMessage($"Starting the {databaseSystemName} container ...");

        var stopwatch = Stopwatch.StartNew();

        var fixture = new TFixture();

        await fixture.InitializeAsync();

        // Reading the connection string is also what surfaces a failed start: the fixture captures the exception
        // and rethrows it on the first access to the container.
        var connectionString = fixture.ConnectionString;

        var elapsedSeconds = stopwatch.Elapsed.TotalSeconds.ToString("N1", CultureInfo.InvariantCulture);

        TestContext.Current.SendDiagnosticMessage(
            $"The {databaseSystemName} container is ready after {elapsedSeconds} seconds and is using the "
                + $"following connection string: {connectionString}"
        );

        return fixture;
    }

    /// <summary>
    /// The started - or currently starting - fixture.
    /// </summary>
    /// <remarks>
    /// <see cref="Lazy{T}" /> defaults to <see cref="LazyThreadSafetyMode.ExecutionAndPublication" />, so the
    /// task - and with it the container - is created once, no matter how many test classes ask for it.
    /// </remarks>
    private readonly Lazy<Task<TFixture>> fixture = new(() => CreateAndStartAsync(databaseSystemName));
}
