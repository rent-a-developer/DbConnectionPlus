namespace RentADeveloper.DbConnectionPlus.UnitTests;

public class DbConnectionExtensions_ExecuteNonQueryTests() : StatementMethodTestsBase(
    (
            connection,
            sql,
            transaction,
            timeout,
            commandType,
            cancellationToken
        ) =>
        connection.ExecuteNonQueryAsync(sql, transaction, timeout, commandType, cancellationToken),
    (
            connection,
            sql,
            transaction,
            timeout,
            commandType,
            cancellationToken
        ) =>
        connection.ExecuteNonQuery(sql, transaction, timeout, commandType, cancellationToken)
)
{
    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        ArgumentNullGuardVerifier.Verify(() =>
            this.MockDbConnection.ExecuteNonQuery("DELETE FROM Entity")
        );

        ArgumentNullGuardVerifier.Verify(() =>
            this.MockDbConnection.ExecuteNonQueryAsync("DELETE FROM Entity")
        );
    }
}
