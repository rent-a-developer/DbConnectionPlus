namespace RentADeveloper.DbConnectionPlus.UnitTests;

public class DbConnectionExtensions_ExecuteScalarTests()
    : StatementMethodTestsBase(
        (connection, sql, transaction, timeout, commandType, cancellationToken) =>
            connection.ExecuteScalarAsync<int?>(sql, transaction, timeout, commandType, cancellationToken),
        (connection, sql, transaction, timeout, commandType, cancellationToken) =>
            connection.ExecuteScalar<int?>(sql, transaction, timeout, commandType, cancellationToken)
    )
{
    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        ArgumentNullGuardVerifier.Verify(() => this.MockDbConnection.ExecuteScalar<int>("SELECT 1"));

        ArgumentNullGuardVerifier.Verify(() => this.MockDbConnection.ExecuteScalarAsync<int>("SELECT 1"));
    }
}
