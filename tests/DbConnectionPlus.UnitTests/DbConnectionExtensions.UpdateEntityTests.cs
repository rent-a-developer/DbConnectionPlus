namespace RentADeveloper.DbConnectionPlus.UnitTests;

public class DbConnectionExtensions_UpdateEntityTests : UnitTestsBase
{
    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        var entity = Generate.Single<Entity>();

        ArgumentNullGuardVerifier.Verify(() =>
            this.MockDbConnection.UpdateEntity(entity)
        );

        ArgumentNullGuardVerifier.Verify(() =>
            this.MockDbConnection.UpdateEntityAsync(entity)
        );
    }

    [Fact]
    public void UpdateEntity_ShouldCallEntityManipulator()
    {
        var entity = Generate.Single<Entity>();
        using var transaction = this.MockDbConnection.BeginTransaction();
        var cancellationToken = TestContext.Current.CancellationToken;
        var numberOfAffectedRows = Generate.SmallNumber();

        this.MockEntityManipulator.UpdateEntity(
            this.MockDbConnection,
            entity,
            transaction,
            cancellationToken
        ).Returns(numberOfAffectedRows);

        this.MockDbConnection.UpdateEntity(entity, transaction, cancellationToken)
            .Should().Be(numberOfAffectedRows);

        this.MockEntityManipulator.Received().UpdateEntity(
            this.MockDbConnection,
            entity,
            transaction,
            cancellationToken
        );
    }

    [Fact]
    public async Task UpdateEntityAsync_ShouldCallEntityManipulator()
    {
        var entity = Generate.Single<Entity>();
        using var transaction = await this.MockDbConnection.BeginTransactionAsync();
        var cancellationToken = TestContext.Current.CancellationToken;
        var numberOfAffectedRows = Generate.SmallNumber();

        this.MockEntityManipulator.UpdateEntityAsync(
            this.MockDbConnection,
            entity,
            transaction,
            cancellationToken
        ).Returns(numberOfAffectedRows);

        (await this.MockDbConnection.UpdateEntityAsync(entity, transaction, cancellationToken))
            .Should().Be(numberOfAffectedRows);

        await this.MockEntityManipulator.Received().UpdateEntityAsync(
            this.MockDbConnection,
            entity,
            transaction,
            cancellationToken
        );
    }
}
