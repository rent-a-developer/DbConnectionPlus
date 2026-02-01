// ReSharper disable ReturnValueOfPureMethodIsNotUsed

#pragma warning disable NS1000, NS1004, CA1806

namespace RentADeveloper.DbConnectionPlus.UnitTests;

public class DbConnectionExtensions_QueryOfTTests : StatementMethodTestsBase
{
    public DbConnectionExtensions_QueryOfTTests() : base(
        (
                connection,
                sql,
                transaction,
                timeout,
                commandType,
                cancellationToken
            ) =>
            connection.QueryAsync<Entity>(sql, transaction, timeout, commandType, cancellationToken)
                .ToListAsync(TestContext.Current.CancellationToken).AsTask(),
        (
                connection,
                sql,
                transaction,
                timeout,
                commandType,
                cancellationToken
            ) =>
            connection.Query<Entity>(sql, transaction, timeout, commandType, cancellationToken).ToList()
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
            this.MockDbConnection.Query<Entity>("SELECT * FROM Entity")
        );

        ArgumentNullGuardVerifier.Verify(() =>
            this.MockDbConnection.QueryAsync<Entity>("SELECT * FROM Entity")
        );
    }
}
