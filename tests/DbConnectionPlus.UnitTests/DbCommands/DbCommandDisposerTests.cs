#pragma warning disable NS1001

using RentADeveloper.DbConnectionPlus.DatabaseAdapters;
using RentADeveloper.DbConnectionPlus.DbCommands;

namespace RentADeveloper.DbConnectionPlus.UnitTests.DbCommands;

public class DbCommandDisposerTests : UnitTestsBase
{
    [Fact]
    public void Dispose_AlreadyDisposed_ShouldNotDisposeCommandResourcesAgain()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var cancellationTokenRegistration =
            DbCommandHelper.RegisterDbCommandCancellation(this.MockDbCommand, cancellationToken);

        var dropTableFunction1 = Substitute.For<Action>();
        var dropTableAsyncFunction1 = Substitute.For<Func<ValueTask>>();

        var dropTableFunction2 = Substitute.For<Action>();
        var dropTableAsyncFunction2 = Substitute.For<Func<ValueTask>>();

        var temporaryTableDisposers = new[]
        {
            new TemporaryTableDisposer(dropTableFunction1, dropTableAsyncFunction1),
            new TemporaryTableDisposer(dropTableFunction2, dropTableAsyncFunction2)
        };

        var disposer = new DbCommandDisposer(
            this.MockDbCommand,
            temporaryTableDisposers,
            cancellationTokenRegistration
        );

        disposer.Dispose();
        disposer.Dispose();
        disposer.Dispose();

        this.MockDbCommand.Received(1).Dispose();
        dropTableFunction1.Received(1).Invoke();
        dropTableFunction2.Received(1).Invoke();
    }

    [Fact]
    public void Dispose_ShouldDisposeCommandResources()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var cancellationTokenRegistration =
            DbCommandHelper.RegisterDbCommandCancellation(this.MockDbCommand, cancellationToken);

        var dropTableFunction1 = Substitute.For<Action>();
        var dropTableAsyncFunction1 = Substitute.For<Func<ValueTask>>();

        var dropTableFunction2 = Substitute.For<Action>();
        var dropTableAsyncFunction2 = Substitute.For<Func<ValueTask>>();

        var temporaryTableDisposers = new[]
        {
            new TemporaryTableDisposer(dropTableFunction1, dropTableAsyncFunction1),
            new TemporaryTableDisposer(dropTableFunction2, dropTableAsyncFunction2)
        };

        var disposer = new DbCommandDisposer(
            this.MockDbCommand,
            temporaryTableDisposers,
            cancellationTokenRegistration
        );

        disposer.Dispose();

        this.MockDbCommand.Received(1).Dispose();
        dropTableFunction1.Received(1).Invoke();
        dropTableFunction2.Received(1).Invoke();

        // Verify that cancellation token registration is disposed.
        cancellationTokenSource.Cancel();
        this.MockDbCommand.DidNotReceive().Cancel();
    }

    [Fact]
    public async Task DisposeAsync_AlreadyDisposed_ShouldNotDisposeCommandResourcesAsyncAgain()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var cancellationTokenRegistration =
            DbCommandHelper.RegisterDbCommandCancellation(this.MockDbCommand, cancellationToken);

        var dropTableFunction1 = Substitute.For<Action>();
        var dropTableAsyncFunction1 = Substitute.For<Func<ValueTask>>();

        var dropTableFunction2 = Substitute.For<Action>();
        var dropTableAsyncFunction2 = Substitute.For<Func<ValueTask>>();

        var temporaryTableDisposers = new[]
        {
            new TemporaryTableDisposer(dropTableFunction1, dropTableAsyncFunction1),
            new TemporaryTableDisposer(dropTableFunction2, dropTableAsyncFunction2)
        };

        var disposer = new DbCommandDisposer(
            this.MockDbCommand,
            temporaryTableDisposers,
            cancellationTokenRegistration
        );

        await disposer.DisposeAsync();
        await disposer.DisposeAsync();
        await disposer.DisposeAsync();

        await this.MockDbCommand.Received(1).DisposeAsync();
        await dropTableAsyncFunction1.Received(1).Invoke();
        await dropTableAsyncFunction2.Received(1).Invoke();
    }

    [Fact]
    public async Task DisposeAsync_ShouldDisposeCommandResourcesAsync()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var cancellationTokenRegistration =
            DbCommandHelper.RegisterDbCommandCancellation(this.MockDbCommand, cancellationToken);

        var dropTableFunction1 = Substitute.For<Action>();
        var dropTableAsyncFunction1 = Substitute.For<Func<ValueTask>>();

        var dropTableFunction2 = Substitute.For<Action>();
        var dropTableAsyncFunction2 = Substitute.For<Func<ValueTask>>();

        var temporaryTableDisposers = new[]
        {
            new TemporaryTableDisposer(dropTableFunction1, dropTableAsyncFunction1),
            new TemporaryTableDisposer(dropTableFunction2, dropTableAsyncFunction2)
        };

        var disposer = new DbCommandDisposer(
            this.MockDbCommand,
            temporaryTableDisposers,
            cancellationTokenRegistration
        );

        await disposer.DisposeAsync();

        await this.MockDbCommand.Received(1).DisposeAsync();
        await dropTableAsyncFunction1.Received(1).Invoke();
        await dropTableAsyncFunction2.Received(1).Invoke();

        // Verify that cancellation token registration is disposed.
        await cancellationTokenSource.CancelAsync();
        this.MockDbCommand.DidNotReceive().Cancel();
    }

    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        TemporaryTableDisposer[] temporaryTableDisposers = [];

        ArgumentNullGuardVerifier.Verify(() => new DbCommandDisposer(
                this.MockDbCommand,
                temporaryTableDisposers,
                default
            )
        );
    }
}
