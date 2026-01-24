namespace RentADeveloper.DbConnectionPlus.UnitTests;

public class DbConnectionExtensions_ExecuteReaderTests() : StatementMethodTestsBase(
    (
            connection,
            sql,
            transaction,
            timeout,
            commandType,
            cancellationToken
        ) =>
        connection.ExecuteReaderAsync(
            sql,
            transaction,
            timeout,
            CommandBehavior.Default,
            commandType,
            cancellationToken
        ),
    (
            connection,
            sql,
            transaction,
            timeout,
            commandType,
            cancellationToken
        ) =>
        connection.ExecuteReader(sql, transaction, timeout, CommandBehavior.Default, commandType, cancellationToken)
)
{
    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        ArgumentNullGuardVerifier.Verify(() =>
            this.MockDbConnection.ExecuteReader("SELECT * FROM Entity")
        );

        ArgumentNullGuardVerifier.Verify(() =>
            this.MockDbConnection.ExecuteReaderAsync("SELECT * FROM Entity")
        );
    }
}
