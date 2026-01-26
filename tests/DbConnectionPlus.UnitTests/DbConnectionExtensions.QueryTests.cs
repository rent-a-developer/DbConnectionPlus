namespace RentADeveloper.DbConnectionPlus.UnitTests;

// ReSharper disable ReturnValueOfPureMethodIsNotUsed
#pragma warning disable NS1000
#pragma warning disable NS1004
#pragma warning disable CA1806

public class DbConnectionExtensions_QueryTests : StatementMethodTestsBase
{
    public DbConnectionExtensions_QueryTests() : base(
        (
                connection,
                sql,
                transaction,
                timeout,
                commandType,
                cancellationToken
            ) =>
            connection.QueryAsync(sql, transaction, timeout, commandType, cancellationToken)
                .ToListAsync(cancellationToken)
                .AsTask(),
        (
                connection,
                sql,
                transaction,
                timeout,
                commandType,
                cancellationToken
            ) =>
            connection.Query(sql, transaction, timeout, commandType, cancellationToken).ToList()
    )
    {
        var mockDbDataReader = Substitute.For<DbDataReader>();

        mockDbDataReader.FieldCount.Returns(1);
        mockDbDataReader.GetName(0).Returns("Id");
        mockDbDataReader.GetFieldType(0).Returns(typeof(Int64));

        this.MockDbCommand.ExecuteReader(Arg.Any<CommandBehavior>())
            .Returns(mockDbDataReader);

        this.MockDbCommand.ExecuteReaderAsync(Arg.Any<CommandBehavior>(), Arg.Any<CancellationToken>())
            .Returns(mockDbDataReader);
    }

    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        ArgumentNullGuardVerifier.Verify(() =>
            this.MockDbConnection.Query("SELECT * FROM Entity")
        );

        ArgumentNullGuardVerifier.Verify(() =>
            this.MockDbConnection.QueryAsync("SELECT * FROM Entity")
        );
    }
}
