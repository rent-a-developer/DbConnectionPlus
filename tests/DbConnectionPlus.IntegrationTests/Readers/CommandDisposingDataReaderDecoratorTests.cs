using RentADeveloper.DbConnectionPlus.Readers;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.Readers;

public sealed class
    CommandDisposingDataReaderDecoratorTests_MySql :
    CommandDisposingDataReaderDecoratorTests<MySqlTestDatabaseProvider>;

public sealed class
    CommandDisposingDataReaderDecoratorTests_Oracle :
    CommandDisposingDataReaderDecoratorTests<OracleTestDatabaseProvider>;

public sealed class
    CommandDisposingDataReaderDecoratorTests_PostgreSql :
    CommandDisposingDataReaderDecoratorTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    CommandDisposingDataReaderDecoratorTests_Sqlite :
    CommandDisposingDataReaderDecoratorTests<SqliteTestDatabaseProvider>;

public sealed class
    CommandDisposingDataReaderDecoratorTests_SqlServer :
    CommandDisposingDataReaderDecoratorTests<SqlServerTestDatabaseProvider>;

public abstract class
    CommandDisposingDataReaderDecoratorTests<TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    [Fact]
    public void Read_OperationCancelledViaCancellationToken_ShouldThrowOperationCanceledException()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        // PostgreSQL doesn't support streaming partial results with delays inside a single query.
        // So SELECT 1; pg_sleep(2); SELECT 1; will not return the first row until after the delay,
        // meaning the cancellation won't be observed until after the delay.
        Assert.SkipWhen(this.TestDatabaseProvider is PostgreSqlTestDatabaseProvider, "");

        using var command = this.Connection.CreateCommand();
        command.CommandText = "SELECT 1; " + this.TestDatabaseProvider.DelayTwoSecondsStatement + " SELECT 1;";

        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.Sleep(100);
                cancellationTokenSource.Cancel();
                // ReSharper disable once AccessToDisposedClosure
                command.Cancel();
            }
        );

        var commandDisposer = new DbCommandDisposer(command, [], default);

        using var decoratedReader = command.ExecuteReader();

        using var decorator = new CommandDisposingDataReaderDecorator(
            decoratedReader,
            this.DatabaseAdapter,
            commandDisposer,
            cancellationToken
        );

        // Read the value from before the delay:
        decorator.Read()
            .Should().BeTrue();

        // The next read should be cancelled:
        // ReSharper disable once AccessToDisposedClosure
        Invoking(() => decorator.Read())
            .Should().Throw<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Fact]
    public async Task ReadAsync_OperationCancelledViaCancellationToken_ShouldThrowOperationCanceledException()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        // PostgreSQL doesn't support streaming partial results with delays inside a single query.
        // So SELECT 1; pg_sleep(2); SELECT 1; will not return the first row until after the delay,
        // meaning the cancellation won't be observed until after the delay.
        Assert.SkipWhen(this.TestDatabaseProvider is PostgreSqlTestDatabaseProvider, "");

        await using var command = this.Connection.CreateCommand();
        command.CommandText = "SELECT 1; " + this.TestDatabaseProvider.DelayTwoSecondsStatement + " SELECT 1;";

        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.Sleep(100);
                cancellationTokenSource.Cancel();
                // ReSharper disable once AccessToDisposedClosure
                command.Cancel();
            }
        );

        var commandDisposer = new DbCommandDisposer(command, [], default);

        await using var decoratedReader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);

        await using var decorator = new CommandDisposingDataReaderDecorator(
            decoratedReader,
            this.DatabaseAdapter,
            commandDisposer,
            cancellationToken
        );

        // Read the value from before the delay:
        (await decorator.ReadAsync(TestContext.Current.CancellationToken))
            .Should().BeTrue();

        // The next read should be cancelled:
        // ReSharper disable once AccessToDisposedClosure
        await Invoking(() => decorator.ReadAsync(cancellationToken))
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }
}
