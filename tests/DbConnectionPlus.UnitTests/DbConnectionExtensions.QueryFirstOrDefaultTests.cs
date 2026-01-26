#pragma warning disable NS1000, NS1004

namespace RentADeveloper.DbConnectionPlus.UnitTests;

public class DbConnectionExtensions_QueryFirstOrDefaultTests : StatementMethodTestsBase
{
    public DbConnectionExtensions_QueryFirstOrDefaultTests() : base(
        (
                connection,
                sql,
                transaction,
                timeout,
                commandType,
                cancellationToken
            ) =>
            connection.QueryFirstOrDefaultAsync(sql, transaction, timeout, commandType, cancellationToken),
        (
                connection,
                sql,
                transaction,
                timeout,
                commandType,
                cancellationToken
            ) =>
            connection.QueryFirstOrDefault(sql, transaction, timeout, commandType, cancellationToken)
    )
    {
        var mockDbDataReader = Substitute.For<DbDataReader>();

        mockDbDataReader.FieldCount.Returns(1);
        mockDbDataReader.GetName(0).Returns("Id");
        mockDbDataReader.GetFieldType(0).Returns(typeof(Int64));

        mockDbDataReader.Read().Returns(true);
        mockDbDataReader.ReadAsync(TestContext.Current.CancellationToken).Returns(true);

        this.MockDbCommand.ExecuteReader(Arg.Any<CommandBehavior>())
            .Returns(mockDbDataReader);

        this.MockDbCommand.ExecuteReaderAsync(Arg.Any<CommandBehavior>(), Arg.Any<CancellationToken>())
            .Returns(mockDbDataReader);
    }

    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        ArgumentNullGuardVerifier.Verify(() =>
            this.MockDbConnection.QueryFirstOrDefault("SELECT * FROM Entity")
        );

        ArgumentNullGuardVerifier.Verify(() =>
            this.MockDbConnection.QueryFirstOrDefaultAsync("SELECT * FROM Entity")
        );
    }
}
