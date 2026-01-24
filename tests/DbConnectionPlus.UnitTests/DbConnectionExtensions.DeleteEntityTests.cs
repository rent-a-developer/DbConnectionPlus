namespace RentADeveloper.DbConnectionPlus.UnitTests;

public class DbConnectionExtensions_DeleteEntityTests : UnitTestsBase
{
    [Fact]
    public void DeleteEntity_ShouldCallEntityManipulator()
    {
        var entity = Generate.Single<Entity>();
        using var transaction = this.MockDbConnection.BeginTransaction();
        var cancellationToken = TestContext.Current.CancellationToken;
        var numberOfAffectedRows = Generate.SmallNumber();

        this.MockEntityManipulator.DeleteEntity(
            this.MockDbConnection,
            entity,
            transaction,
            cancellationToken
        ).Returns(numberOfAffectedRows);

        this.MockDbConnection.DeleteEntity(entity, transaction, cancellationToken)
            .Should().Be(numberOfAffectedRows);

        this.MockEntityManipulator.Received().DeleteEntity(
            this.MockDbConnection,
            entity,
            transaction,
            cancellationToken
        );
    }

    [Fact]
    public async Task DeleteEntityAsync_ShouldCallEntityManipulator()
    {
        var entity = Generate.Single<Entity>();
        using var transaction = await this.MockDbConnection.BeginTransactionAsync();
        var cancellationToken = TestContext.Current.CancellationToken;
        var numberOfAffectedRows = Generate.SmallNumber();

        this.MockEntityManipulator.DeleteEntityAsync(
            this.MockDbConnection,
            entity,
            transaction,
            cancellationToken
        ).Returns(numberOfAffectedRows);

        (await this.MockDbConnection.DeleteEntityAsync(entity, transaction, cancellationToken))
            .Should().Be(numberOfAffectedRows);

        await this.MockEntityManipulator.Received().DeleteEntityAsync(
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
            this.MockDbConnection.DeleteEntity(entity)
        );

        ArgumentNullGuardVerifier.Verify(() =>
            this.MockDbConnection.DeleteEntityAsync(entity)
        );
    }
}
