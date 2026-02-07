using NSubstitute.DbConnection;
using RentADeveloper.DbConnectionPlus.SqlStatements;

namespace RentADeveloper.DbConnectionPlus.UnitTests;

/// <summary>
/// Base class for unit tests of methods that execute SQL statements.
/// </summary>
public abstract class StatementMethodTestsBase : UnitTestsBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StatementMethodTestsBase" /> class.
    /// </summary>
    /// <param name="asyncTestMethod">The asynchronous version of the statement method to test.</param>
    /// <param name="syncTestMethod">The synchronous version of the statement method to test.</param>
    protected StatementMethodTestsBase(
        Func<DbConnection, InterpolatedSqlStatement, DbTransaction?, TimeSpan?, CommandType, CancellationToken, Task>
            asyncTestMethod,
        Action<DbConnection, InterpolatedSqlStatement, DbTransaction?, TimeSpan?, CommandType, CancellationToken>
            syncTestMethod
    )
    {
        this.asyncTestMethod = asyncTestMethod;
        this.syncTestMethod = syncTestMethod;
    }

    [Fact]
    public async Task AsyncMethod_ShouldUseCommandTimeout()
    {
        var timeout = Generate.Single<TimeSpan>();

        await this.asyncTestMethod(
            this.MockDbConnection,
            "",
            null,
            timeout,
            CommandType.Text,
            TestContext.Current.CancellationToken
        );

        this.MockInterceptDbCommand.Received().Invoke(
            Arg.Is<DbCommand>(cmd => cmd.CommandTimeout == (Int32) timeout.TotalSeconds),
            Arg.Any<IReadOnlyList<InterpolatedTemporaryTable>>()
        );
    }

    [Fact]
    public async Task AsyncMethod_ShouldUseCommandType()
    {
        await this.asyncTestMethod(
            this.MockDbConnection,
            "",
            null,
            null,
            CommandType.StoredProcedure,
            TestContext.Current.CancellationToken
        );

        this.MockInterceptDbCommand.Received().Invoke(
            Arg.Is<DbCommand>(cmd => cmd.CommandType == CommandType.StoredProcedure),
            Arg.Any<IReadOnlyList<InterpolatedTemporaryTable>>()
        );
    }

    [Fact]
    public async Task AsyncMethod_ShouldUseTransaction()
    {
        await using var transaction = await this.MockDbConnection.BeginTransactionAsync();

        await this.asyncTestMethod(
            this.MockDbConnection,
            "",
            transaction,
            null,
            CommandType.Text,
            TestContext.Current.CancellationToken
        );

        this.MockInterceptDbCommand.Received().Invoke(
            Arg.Is<DbCommand>(cmd => cmd.Transaction == transaction),
            Arg.Any<IReadOnlyList<InterpolatedTemporaryTable>>()
        );
    }

    [Fact]
    public void SyncMethod_ShouldUseCommandTimeout()
    {
        var timeout = Generate.Single<TimeSpan>();

        this.syncTestMethod(
            this.MockDbConnection,
            "",
            null,
            timeout,
            CommandType.Text,
            TestContext.Current.CancellationToken
        );

        this.MockInterceptDbCommand.Received().Invoke(
            Arg.Is<DbCommand>(cmd => cmd.CommandTimeout == (Int32) timeout.TotalSeconds),
            Arg.Any<IReadOnlyList<InterpolatedTemporaryTable>>()
        );
    }

    [Fact]
    public void SyncMethod_ShouldUseCommandType()
    {
        this.syncTestMethod(
            this.MockDbConnection,
            "",
            null,
            null,
            CommandType.StoredProcedure,
            TestContext.Current.CancellationToken
        );

        this.MockInterceptDbCommand.Received().Invoke(
            Arg.Is<DbCommand>(cmd => cmd.CommandType == CommandType.StoredProcedure),
            Arg.Any<IReadOnlyList<InterpolatedTemporaryTable>>()
        );
    }

    [Fact]
    public void SyncMethod_ShouldUseTransaction()
    {
        using var transaction = this.MockDbConnection.BeginTransaction();

        this.syncTestMethod(
            this.MockDbConnection,
            "",
            transaction,
            null,
            CommandType.Text,
            TestContext.Current.CancellationToken
        );

        this.MockInterceptDbCommand.Received().Invoke(
            Arg.Is<DbCommand>(cmd => cmd.Transaction == transaction),
            Arg.Any<IReadOnlyList<InterpolatedTemporaryTable>>()
        );
    }

    private readonly
        Func<DbConnection, InterpolatedSqlStatement, DbTransaction?, TimeSpan?, CommandType, CancellationToken, Task>
        asyncTestMethod;

    private readonly
        Action<DbConnection, InterpolatedSqlStatement, DbTransaction?, TimeSpan?, CommandType, CancellationToken>
        syncTestMethod;
}
