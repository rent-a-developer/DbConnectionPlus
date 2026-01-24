namespace RentADeveloper.DbConnectionPlus.UnitTests;

#pragma warning disable NS1000
#pragma warning disable NS1004

public class DbConnectionExtensions_QuerySingleOrDefaultTests : StatementMethodTestsBase
{
    public DbConnectionExtensions_QuerySingleOrDefaultTests() : base(
        (
                connection,
                sql,
                transaction,
                timeout,
                commandType,
                cancellationToken
            ) =>
            connection.QuerySingleOrDefaultAsync(sql, transaction, timeout, commandType, cancellationToken),
        (
                connection,
                sql,
                transaction,
                timeout,
                commandType,
                cancellationToken
            ) =>
            connection.QuerySingleOrDefault(sql, transaction, timeout, commandType, cancellationToken)
    )
    {
        var mockDbDataReader = Substitute.For<DbDataReader>();

        mockDbDataReader.FieldCount.Returns(1);
        mockDbDataReader.GetName(0).Returns("Id");
        mockDbDataReader.GetFieldType(0).Returns(typeof(Int64));

        mockDbDataReader.Read().Returns(true, false);
        mockDbDataReader.ReadAsync(TestContext.Current.CancellationToken).Returns(true, false);

        this.MockDbCommand.ExecuteReader(Arg.Any<CommandBehavior>())
            .Returns(mockDbDataReader);

        this.MockDbCommand.ExecuteReaderAsync(Arg.Any<CommandBehavior>(), Arg.Any<CancellationToken>())
            .Returns(mockDbDataReader);
    }

    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        ArgumentNullGuardVerifier.Verify(() =>
            this.MockDbConnection.QuerySingleOrDefault("SELECT * FROM Entity")
        );

        ArgumentNullGuardVerifier.Verify(() =>
            this.MockDbConnection.QuerySingleOrDefaultAsync("SELECT * FROM Entity")
        );
    }
}
