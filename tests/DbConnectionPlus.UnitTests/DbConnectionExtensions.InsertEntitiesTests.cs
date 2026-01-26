namespace RentADeveloper.DbConnectionPlus.UnitTests;

public class DbConnectionExtensions_InsertEntitiesTests : UnitTestsBase
{
    [Fact]
    public void InsertEntities_ShouldCallEntityManipulator()
    {
        var entities = Generate.Multiple<Entity>();
        using var transaction = this.MockDbConnection.BeginTransaction();
        var cancellationToken = TestContext.Current.CancellationToken;
        var numberOfAffectedRows = Generate.SmallNumber();

        this.MockEntityManipulator.InsertEntities(
            this.MockDbConnection,
            entities,
            transaction,
            cancellationToken
        ).Returns(numberOfAffectedRows);

        this.MockDbConnection.InsertEntities(entities, transaction, cancellationToken)
            .Should().Be(numberOfAffectedRows);

        this.MockEntityManipulator.Received().InsertEntities(
            this.MockDbConnection,
            entities,
            transaction,
            cancellationToken
        );
    }

    [Fact]
    public async Task InsertEntitiesAsync_ShouldCallEntityManipulator()
    {
        var entities = Generate.Multiple<Entity>();
        await using var transaction = await this.MockDbConnection.BeginTransactionAsync();
        var cancellationToken = TestContext.Current.CancellationToken;
        var numberOfAffectedRows = Generate.SmallNumber();

        this.MockEntityManipulator.InsertEntitiesAsync(
            this.MockDbConnection,
            entities,
            transaction,
            cancellationToken
        ).Returns(numberOfAffectedRows);

        (await this.MockDbConnection.InsertEntitiesAsync(entities, transaction, cancellationToken))
            .Should().Be(numberOfAffectedRows);

        await this.MockEntityManipulator.Received().InsertEntitiesAsync(
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
            this.MockDbConnection.InsertEntities(entities)
        );

        ArgumentNullGuardVerifier.Verify(() =>
            this.MockDbConnection.InsertEntitiesAsync(entities)
        );
    }
}
