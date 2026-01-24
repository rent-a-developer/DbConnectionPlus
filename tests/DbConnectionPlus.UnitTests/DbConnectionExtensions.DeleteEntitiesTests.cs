namespace RentADeveloper.DbConnectionPlus.UnitTests;

public class DbConnectionExtensions_DeleteEntitiesTests : UnitTestsBase
{
    [Fact]
    public void DeleteEntities_ShouldCallEntityManipulator()
    {
        var entities = Generate.Multiple<Entity>();
        using var transaction = this.MockDbConnection.BeginTransaction();
        var cancellationToken = TestContext.Current.CancellationToken;
        var numberOfAffectedRows = Generate.SmallNumber();

        this.MockEntityManipulator.DeleteEntities(
            this.MockDbConnection,
            entities,
            transaction,
            cancellationToken
        ).Returns(numberOfAffectedRows);

        this.MockDbConnection.DeleteEntities(entities, transaction, cancellationToken)
            .Should().Be(numberOfAffectedRows);

        this.MockEntityManipulator.Received().DeleteEntities(
            this.MockDbConnection,
            entities,
            transaction,
            cancellationToken
        );
    }

    [Fact]
    public async Task DeleteEntitiesAsync_ShouldCallEntityManipulator()
    {
        var entities = Generate.Multiple<Entity>();
        using var transaction = await this.MockDbConnection.BeginTransactionAsync();
        var cancellationToken = TestContext.Current.CancellationToken;
        var numberOfAffectedRows = Generate.SmallNumber();

        this.MockEntityManipulator.DeleteEntitiesAsync(
            this.MockDbConnection,
            entities,
            transaction,
            cancellationToken
        ).Returns(numberOfAffectedRows);

        (await this.MockDbConnection.DeleteEntitiesAsync(entities, transaction, cancellationToken))
            .Should().Be(numberOfAffectedRows);

        await this.MockEntityManipulator.Received().DeleteEntitiesAsync(
            this.MockDbConnection,
            entities,
            transaction,
            cancellationToken
        );
    }

    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        var entities = Generate.Multiple<Entity>();

        ArgumentNullGuardVerifier.Verify(() =>
            this.MockDbConnection.DeleteEntities(entities)
        );

        ArgumentNullGuardVerifier.Verify(() =>
            this.MockDbConnection.DeleteEntitiesAsync(entities)
        );
    }
}
