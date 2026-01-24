using RentADeveloper.DbConnectionPlus.DatabaseAdapters;

namespace RentADeveloper.DbConnectionPlus.UnitTests.DatabaseAdapters;

public class TemporaryTableDisposerTests : UnitTestsBase
{
    [Fact]
    public void Dispose_AlreadyDisposed_ShouldNotCallDropFunctionAgain()
    {
        var dropTableFunction = Substitute.For<Action>();
        var dropTableAsyncFunction = Substitute.For<Func<ValueTask>>();

        var disposer = new TemporaryTableDisposer(dropTableFunction, dropTableAsyncFunction);

        disposer.Dispose();
        disposer.Dispose();
        disposer.Dispose();

        dropTableFunction.Received(1).Invoke();
    }

    [Fact]
    public void Dispose_ShouldCallDropFunction()
    {
        var dropTableFunction = Substitute.For<Action>();
        var dropTableAsyncFunction = Substitute.For<Func<ValueTask>>();

        var disposer = new TemporaryTableDisposer(dropTableFunction, dropTableAsyncFunction);
        disposer.Dispose();

        dropTableFunction.Received(1).Invoke();
    }

    [Fact]
    public async Task DisposeAsync_AlreadyDisposed_ShouldNotCallAsyncDropFunctionAgain()
    {
        var dropTableFunction = Substitute.For<Action>();
        var dropTableAsyncFunction = Substitute.For<Func<ValueTask>>();

        var disposer = new TemporaryTableDisposer(dropTableFunction, dropTableAsyncFunction);

        await disposer.DisposeAsync();
        await disposer.DisposeAsync();
        await disposer.DisposeAsync();

        await dropTableAsyncFunction.Received(1).Invoke();
    }

    [Fact]
    public async Task DisposeAsync_ShouldCallAsyncDropFunction()
    {
        var dropTableFunction = Substitute.For<Action>();
        var dropTableAsyncFunction = Substitute.For<Func<ValueTask>>();

        var disposer = new TemporaryTableDisposer(dropTableFunction, dropTableAsyncFunction);

        await disposer.DisposeAsync();

        await dropTableAsyncFunction.Received(1).Invoke();
    }

    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        var dropTableFunction = () => { };
        var dropTableAsyncFunction = () => ValueTask.CompletedTask;

        ArgumentNullGuardVerifier.Verify(() =>
            new TemporaryTableDisposer(dropTableFunction, dropTableAsyncFunction)
        );
    }
}
