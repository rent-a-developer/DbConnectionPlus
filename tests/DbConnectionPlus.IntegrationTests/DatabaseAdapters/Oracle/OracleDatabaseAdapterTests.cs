// ReSharper disable AccessToDisposedClosure

using Oracle.ManagedDataAccess.Client;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.Oracle;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.DatabaseAdapters.Oracle;

public class OracleDatabaseAdapterTests : IntegrationTestsBase<OracleTestDatabaseProvider>
{
    [Fact]
    public void QuoteTemporaryTableName_ShouldQuoteTableName()
    {
        var prefix = this.Connection.ExecuteScalar<String>(
            "SELECT VALUE FROM v$parameter WHERE NAME = 'private_temp_table_prefix'"
        );

        this.adapter.QuoteTemporaryTableName("TempTable", this.Connection)
            .Should().Be($"\"{prefix}TempTable\"");
    }

    [Fact]
    public void WasSqlStatementCancelledByCancellationToken_StatementWasCancelled_ShouldReturnTrue()
    {
        using var command = this.Connection.CreateCommand();
        command.CommandText = this.TestDatabaseProvider.DelayTwoSecondsStatement;

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        using var cancellationTokenRegistration =
            DbCommandHelper.RegisterDbCommandCancellation(command, cancellationToken);

        var exception = Invoking(() => command.ExecuteNonQuery())
            .Should().Throw<OracleException>().Subject.First();

        this.adapter.WasSqlStatementCancelledByCancellationToken(exception, cancellationToken)
            .Should().BeTrue();
    }

    [Fact]
    public void WasSqlStatementCancelledByCancellationToken_StatementWasNotCancelled_ShouldReturnFalse()
    {
        using var command = this.Connection.CreateCommand();
        command.CommandText = "InvalidStatement";

        var exception = Invoking(() => command.ExecuteNonQuery())
            .Should().Throw<OracleException>().Subject.First();

        this.adapter.WasSqlStatementCancelledByCancellationToken(exception, CancellationToken.None)
            .Should().BeFalse();
    }

    private readonly OracleDatabaseAdapter adapter = new();
}
