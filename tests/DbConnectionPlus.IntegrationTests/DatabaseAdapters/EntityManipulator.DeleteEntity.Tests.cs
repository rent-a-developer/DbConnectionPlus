using RentADeveloper.DbConnectionPlus.DatabaseAdapters;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.DatabaseAdapters;

public sealed class
    EntityManipulator_DeleteEntity_Tests_MySql :
    EntityManipulator_DeleteEntity_Tests<MySqlTestDatabaseProvider>;

public sealed class
    EntityManipulator_DeleteEntity_Tests_Oracle :
    EntityManipulator_DeleteEntity_Tests<OracleTestDatabaseProvider>;

public sealed class
    EntityManipulator_DeleteEntity_Tests_PostgreSql :
    EntityManipulator_DeleteEntity_Tests<PostgreSqlTestDatabaseProvider>;

public sealed class
    EntityManipulator_DeleteEntity_Tests_Sqlite :
    EntityManipulator_DeleteEntity_Tests<SqliteTestDatabaseProvider>;

public sealed class
    EntityManipulator_DeleteEntity_Tests_SqlServer :
    EntityManipulator_DeleteEntity_Tests<SqlServerTestDatabaseProvider>;

public abstract class EntityManipulator_DeleteEntity_Tests
    <TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    /// <inheritdoc />
    protected EntityManipulator_DeleteEntity_Tests() =>
        this.manipulator = this.DatabaseAdapter.EntityManipulator;

    [Fact]
    public void DeleteEntity_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var entityToDelete = this.CreateEntityInDb<Entity>();

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        Invoking(() => this.manipulator.DeleteEntity(
                    this.Connection,
                    entityToDelete,
                    null,
                    cancellationToken
                )
            )
            .Should().Throw<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);

        // Since the operation was cancelled, the entity should still exist.
        this.ExistsEntityInDb(entityToDelete)
            .Should().BeTrue();
    }

    [Fact]
    public void DeleteEntity_EntityHasNoKeyProperty_ShouldThrow()
    {
        var entityWithoutKeyProperty = new EntityWithoutKeyProperty();

        Invoking(() => this.manipulator.DeleteEntity(
                    this.Connection,
                    entityWithoutKeyProperty,
                    null,
                    TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<ArgumentException>()
            .WithMessage(
                $"Could not get the key property / properties of the type {typeof(EntityWithoutKeyProperty)}. " +
                $"Make sure that at least one instance property of that type is denoted with a " +
                $"{typeof(KeyAttribute)}."
            );
    }

    [Fact]
    public void DeleteEntity_EntityWithoutTableAttribute_ShouldUseEntityTypeNameAsTableName()
    {
        var entityToDelete = this.CreateEntityInDb<Entity>();

        this.manipulator.DeleteEntity(
            this.Connection,
            entityToDelete,
            null,
            TestContext.Current.CancellationToken
        );

        this.ExistsEntityInDb(entityToDelete)
            .Should().BeFalse();
    }

    [Fact]
    public void DeleteEntity_EntityWithTableAttribute_ShouldUseTableNameFromAttribute()
    {
        var entity = this.CreateEntityInDb<EntityWithTableAttribute>();

        this.manipulator.DeleteEntity(
            this.Connection,
            entity,
            null,
            TestContext.Current.CancellationToken
        );

        this.ExistsEntityInDb(entity)
            .Should().BeFalse();
    }

    [Fact]
    public void DeleteEntity_ShouldHandleEntityWithCompositeKey()
    {
        var entity = this.CreateEntityInDb<EntityWithCompositeKey>();

        this.manipulator.DeleteEntity(
            this.Connection,
            entity,
            null,
            TestContext.Current.CancellationToken
        );

        this.ExistsEntityInDb(entity)
            .Should().BeFalse();
    }

    [Fact]
    public void DeleteEntity_ShouldReturnNumberOfAffectedRows()
    {
        var entityToDelete = this.CreateEntityInDb<Entity>();

        this.manipulator.DeleteEntity(
                this.Connection,
                entityToDelete,
                null,
                TestContext.Current.CancellationToken
            )
            .Should().Be(1);

        this.manipulator.DeleteEntity(
                this.Connection,
                entityToDelete,
                null,
                TestContext.Current.CancellationToken
            )
            .Should().Be(0);
    }

    [Fact]
    public void DeleteEntity_Transaction_ShouldUseTransaction()
    {
        var entityToDelete = this.CreateEntityInDb<Entity>();

        using (var transaction = this.Connection.BeginTransaction())
        {
            this.manipulator.DeleteEntity(
                this.Connection,
                entityToDelete,
                transaction,
                TestContext.Current.CancellationToken
            );

            this.ExistsEntityInDb(entityToDelete, transaction)
                .Should().BeFalse();

            transaction.Rollback();
        }

        this.ExistsEntityInDb(entityToDelete)
            .Should().BeTrue();
    }

    [Fact]
    public async Task DeleteEntityAsync_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var entityToDelete = this.CreateEntityInDb<Entity>();

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        await Invoking(() => this.manipulator.DeleteEntityAsync(
                    this.Connection,
                    entityToDelete,
                    null,
                    cancellationToken
                )
            )
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);

        // Since the operation was cancelled, the entity should still exist.
        this.ExistsEntityInDb(entityToDelete)
            .Should().BeTrue();
    }

    [Fact]
    public Task DeleteEntityAsync_EntityHasNoKeyProperty_ShouldThrow()
    {
        var entityWithoutKeyProperty = new EntityWithoutKeyProperty();

        return Invoking(() => this.manipulator.DeleteEntityAsync(
                    this.Connection,
                    entityWithoutKeyProperty,
                    null,
                    TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage(
                $"Could not get the key property / properties of the type {typeof(EntityWithoutKeyProperty)}. " +
                $"Make sure that at least one instance property of that type is denoted with a " +
                $"{typeof(KeyAttribute)}."
            );
    }

    [Fact]
    public async Task DeleteEntityAsync_EntityWithoutTableAttribute_ShouldUseEntityTypeNameAsTableName()
    {
        var entityToDelete = this.CreateEntityInDb<Entity>();

        await this.manipulator.DeleteEntityAsync(
            this.Connection,
            entityToDelete,
            null,
            TestContext.Current.CancellationToken
        );

        this.ExistsEntityInDb(entityToDelete)
            .Should().BeFalse();
    }

    [Fact]
    public async Task DeleteEntityAsync_EntityWithTableAttribute_ShouldUseTableNameFromAttribute()
    {
        var entity = this.CreateEntityInDb<EntityWithTableAttribute>();

        await this.manipulator.DeleteEntityAsync(
            this.Connection,
            entity,
            null,
            TestContext.Current.CancellationToken
        );

        this.ExistsEntityInDb(entity)
            .Should().BeFalse();
    }

    [Fact]
    public async Task DeleteEntityAsync_ShouldHandleEntityWithCompositeKey()
    {
        var entity = this.CreateEntityInDb<EntityWithCompositeKey>();

        await this.manipulator.DeleteEntityAsync(
            this.Connection,
            entity,
            null,
            TestContext.Current.CancellationToken
        );

        this.ExistsEntityInDb(entity)
            .Should().BeFalse();
    }

    [Fact]
    public async Task DeleteEntityAsync_ShouldReturnNumberOfAffectedRows()
    {
        var entityToDelete = this.CreateEntityInDb<Entity>();

        (await this.manipulator.DeleteEntityAsync(
                this.Connection,
                entityToDelete,
                null,
                TestContext.Current.CancellationToken
            ))
            .Should().Be(1);

        (await this.manipulator.DeleteEntityAsync(
                this.Connection,
                entityToDelete,
                null,
                TestContext.Current.CancellationToken
            ))
            .Should().Be(0);
    }

    [Fact]
    public async Task DeleteEntityAsync_Transaction_ShouldUseTransaction()
    {
        var entityToDelete = this.CreateEntityInDb<Entity>();

        using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            await this.manipulator.DeleteEntityAsync(
                this.Connection,
                entityToDelete,
                transaction,
                TestContext.Current.CancellationToken
            );

            this.ExistsEntityInDb(entityToDelete, transaction)
                .Should().BeFalse();

            await transaction.RollbackAsync();
        }

        this.ExistsEntityInDb(entityToDelete)
            .Should().BeTrue();
    }

    private readonly IEntityManipulator manipulator;
}
