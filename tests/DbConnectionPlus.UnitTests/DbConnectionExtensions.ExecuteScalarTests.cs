namespace RentADeveloper.DbConnectionPlus.UnitTests;

public class DbConnectionExtensions_ExecuteScalarTests() : StatementMethodTestsBase(
    (
            connection,
            sql,
            transaction,
            timeout,
            commandType,
            cancellationToken
        ) =>
        connection.ExecuteScalarAsync<Int32?>(sql, transaction, timeout, commandType, cancellationToken),
    (
            connection,
            sql,
            transaction,
            timeout,
            commandType,
            cancellationToken
        ) =>
        connection.ExecuteScalar<Int32?>(sql, transaction, timeout, commandType, cancellationToken)
)
{
    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        ArgumentNullGuardVerifier.Verify(() =>
            this.MockDbConnection.ExecuteScalar<Int32>("SELECT 1")
        );

        ArgumentNullGuardVerifier.Verify(() =>
            this.MockDbConnection.ExecuteScalarAsync<Int32>("SELECT 1")
        );
    }
}
