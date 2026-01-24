namespace RentADeveloper.DbConnectionPlus.UnitTests;

public class DbConnectionExtensions_InsertEntityTests : UnitTestsBase
{
    [Fact]
    public void InsertEntity_ShouldCallEntityManipulator()
    {
        var entity = Generate.Single<Entity>();
        using var transaction = this.MockDbConnection.BeginTransaction();
        var cancellationToken = TestContext.Current.CancellationToken;
        var numberOfAffectedRows = Generate.SmallNumber();

        this.MockEntityManipulator.InsertEntity(
            this.MockDbConnection,
            entity,
            transaction,
            cancellationToken
        ).Returns(numberOfAffectedRows);

        this.MockDbConnection.InsertEntity(entity, transaction, cancellationToken)
            .Should().Be(numberOfAffectedRows);

        this.MockEntityManipulator.Received().InsertEntity(
            this.MockDbConnection,
            entity,
            transaction,
            cancellationToken
        );
    }

    [Fact]
    public async Task InsertEntityAsync_ShouldCallEntityManipulator()
    {
        var entity = Generate.Single<Entity>();
        using var transaction = await this.MockDbConnection.BeginTransactionAsync();
        var cancellationToken = TestContext.Current.CancellationToken;
        var numberOfAffectedRows = Generate.SmallNumber();

        this.MockEntityManipulator.InsertEntityAsync(
            this.MockDbConnection,
            entity,
            transaction,
            cancellationToken
        ).Returns(numberOfAffectedRows);

        (await this.MockDbConnection.InsertEntityAsync(entity, transaction, cancellationToken))
            .Should().Be(numberOfAffectedRows);

        await this.MockEntityManipulator.Received().InsertEntityAsync(
            this.MockDbConnection,
            entity,
            transaction,
            cancellationToken
        );
    }

    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        var entity = Generate.Single<Entity>();

        ArgumentNullGuardVerifier.Verify(() =>
            this.MockDbConnection.InsertEntity(entity)
        );

        ArgumentNullGuardVerifier.Verify(() =>
            this.MockDbConnection.InsertEntityAsync(entity)
        );
    }
}
