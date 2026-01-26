// ReSharper disable AccessToDisposedClosure

using Npgsql;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.PostgreSql;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.DatabaseAdapters.PostgreSql;

public class PostgreSqlDatabaseAdapterTests : IntegrationTestsBase<PostgreSqlTestDatabaseProvider>
{
    [Fact]
    public void SupportsTemporaryTables_ShouldReturnTrue() =>
        this.adapter.SupportsTemporaryTables(this.Connection)
            .Should().BeTrue();

    [Fact]
    public void WasSqlStatementCancelledByCancellationToken_StatementWasCancelled_ShouldReturnTrue()
    {
        using var command = this.Connection.CreateCommand();
        command.CommandText = this.TestDatabaseProvider.DelayTwoSecondsStatement;

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        using var registration = DbCommandHelper.RegisterDbCommandCancellation(command, cancellationToken);

        var exception = Invoking(() => command.ExecuteNonQuery())
            .Should().Throw<OperationCanceledException>().Subject.First();

        this.adapter.WasSqlStatementCancelledByCancellationToken(exception, cancellationToken)
            .Should().BeTrue();
    }

    [Fact]
    public void WasSqlStatementCancelledByCancellationToken_StatementWasNotCancelled_ShouldReturnFalse()
    {
        using var command = this.Connection.CreateCommand();
        command.CommandText = "InvalidStatement";

        var exception = Invoking(() => command.ExecuteNonQuery())
            .Should().Throw<PostgresException>().Subject.First();

        this.adapter.WasSqlStatementCancelledByCancellationToken(exception, CancellationToken.None)
            .Should().BeFalse();
    }

    private readonly PostgreSqlDatabaseAdapter adapter = new();
}
