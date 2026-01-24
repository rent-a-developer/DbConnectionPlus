namespace RentADeveloper.DbConnectionPlus.UnitTests;

public class DbConnectionExtensions_ExistsTests() : StatementMethodTestsBase(
    (
            connection,
            sql,
            transaction,
            timeout,
            commandType,
            cancellationToken
        ) =>
        connection.ExistsAsync(sql, transaction, timeout, commandType, cancellationToken),
    (
            connection,
            sql,
            transaction,
            timeout,
            commandType,
            cancellationToken
        ) =>
        connection.Exists(sql, transaction, timeout, commandType, cancellationToken)
)
{
    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        ArgumentNullGuardVerifier.Verify(() =>
            this.MockDbConnection.Exists("SELECT 1")
        );

        ArgumentNullGuardVerifier.Verify(() =>
            this.MockDbConnection.ExistsAsync("SELECT 1")
        );
    }
}
