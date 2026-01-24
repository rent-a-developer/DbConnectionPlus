using RentADeveloper.DbConnectionPlus.DatabaseAdapters;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.DatabaseAdapters;

public sealed class
    EntityManipulator_DeleteEntities_Tests_MySql :
    EntityManipulator_DeleteEntities_Tests<MySqlTestDatabaseProvider>;

public sealed class
    EntityManipulator_DeleteEntities_Tests_Oracle :
    EntityManipulator_DeleteEntities_Tests<OracleTestDatabaseProvider>;

public sealed class
    EntityManipulator_DeleteEntities_Tests_PostgreSql :
    EntityManipulator_DeleteEntities_Tests<PostgreSqlTestDatabaseProvider>;

public sealed class
    EntityManipulator_DeleteEntities_Tests_Sqlite :
    EntityManipulator_DeleteEntities_Tests<SqliteTestDatabaseProvider>;

public sealed class
    EntityManipulator_DeleteEntities_Tests_SqlServer :
    EntityManipulator_DeleteEntities_Tests<SqlServerTestDatabaseProvider>;

public abstract class EntityManipulator_DeleteEntities_Tests
    <TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    /// <inheritdoc />
    protected EntityManipulator_DeleteEntities_Tests() =>
        this.manipulator = this.DatabaseAdapter.EntityManipulator;

    [Fact]
    public void DeleteEntities_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var entitiesToDelete = this.CreateEntitiesInDb<Entity>();

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        Invoking(() => this.manipulator.DeleteEntities(
                    this.Connection,
                    entitiesToDelete,
                    null,
                    cancellationToken
                )
            )
            .Should().Throw<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);

        foreach (var entity in entitiesToDelete)
        {
            // Since the operation was cancelled, the entities should still exist.
            this.ExistsEntityInDb(entity)
                .Should().BeTrue();
        }
    }

    [Fact]
    public void DeleteEntities_EntitiesHaveNoKeyProperty_ShouldThrow()
    {
        var entityWithoutKeyProperty = new EntityWithoutKeyProperty();

        Invoking(() => this.manipulator.DeleteEntities(
                    this.Connection,
                    [entityWithoutKeyProperty],
                    null,
                    TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<ArgumentException>()
            .WithMessage(
                $"Could not get the key property / properties of the type {typeof(EntityWithoutKeyProperty)}. " +
                $"Make sure that at least one instance property of that type is denoted with a {typeof(KeyAttribute)}."
            );
    }

    [Fact]
    public void DeleteEntities_EntitiesWithoutTableAttribute_ShouldUseEntityTypeNameAsTableName()
    {
        var entitiesToDelete = this.CreateEntitiesInDb<Entity>();

        this.manipulator.DeleteEntities(
            this.Connection,
            entitiesToDelete,
            null,
            TestContext.Current.CancellationToken
        );

        foreach (var entity in entitiesToDelete)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeFalse();
        }
    }

    [Fact]
    public void DeleteEntities_EntitiesWithTableAttribute_ShouldUseTableNameFromAttribute()
    {
        var entities = this.CreateEntitiesInDb<EntityWithTableAttribute>();

        this.manipulator.DeleteEntities(
            this.Connection,
            entities,
            null,
            TestContext.Current.CancellationToken
        );

        foreach (var entity in entities)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeFalse();
        }
    }

    [Fact]
    public void DeleteEntities_MoreThan10Entities_ShouldBatchDeleteIfPossible()
    {
        // Some database adapters (like the SQL Server one) use batch deletion for more than 10 entities, so we need
        // to test that as well.

        var entitiesToDelete = this.CreateEntitiesInDb<Entity>(20);

        this.manipulator.DeleteEntities(
            this.Connection,
            entitiesToDelete,
            null,
            TestContext.Current.CancellationToken
        );

        foreach (var entity in entitiesToDelete)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeFalse();
        }
    }

    [Fact]
    public void DeleteEntities_MoreThan10EntitiesWithCompositeKey_ShouldBatchDeleteIfPossible()
    {
        // Some database adapters (like the SQL Server one) use batch deletion for more than 10 entities, so we need
        // to test that as well.

        var entitiesToDelete = this.CreateEntitiesInDb<EntityWithCompositeKey>(20);

        this.manipulator.DeleteEntities(
            this.Connection,
            entitiesToDelete,
            null,
            TestContext.Current.CancellationToken
        );

        foreach (var entity in entitiesToDelete)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeFalse();
        }
    }

    [Fact]
    public void DeleteEntities_ShouldHandleEntityWithCompositeKey()
    {
        var entities = this.CreateEntitiesInDb<EntityWithCompositeKey>();

        this.manipulator.DeleteEntities(
            this.Connection,
            entities,
            null,
            TestContext.Current.CancellationToken
        );

        foreach (var entity in entities)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeFalse();
        }
    }

    [Fact]
    public void DeleteEntities_ShouldReturnNumberOfAffectedRows()
    {
        var entitiesToDelete = this.CreateEntitiesInDb<Entity>();

        this.manipulator.DeleteEntities(
                this.Connection,
                entitiesToDelete,
                null,
                TestContext.Current.CancellationToken
            )
            .Should().Be(entitiesToDelete.Count);

        this.manipulator.DeleteEntities(
                this.Connection,
                entitiesToDelete,
                null,
                TestContext.Current.CancellationToken
            )
            .Should().Be(0);
    }

    [Fact]
    public void DeleteEntities_Transaction_ShouldUseTransaction()
    {
        var entitiesToDelete = this.CreateEntitiesInDb<Entity>();

        using (var transaction = this.Connection.BeginTransaction())
        {
            this.manipulator.DeleteEntities(
                this.Connection,
                entitiesToDelete,
                transaction,
                TestContext.Current.CancellationToken
            );

            foreach (var entity in entitiesToDelete)
            {
                this.ExistsEntityInDb(entity, transaction)
                    .Should().BeFalse();
            }

            transaction.Rollback();
        }

        foreach (var entity in entitiesToDelete)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeTrue();
        }
    }

    [Fact]
    public async Task DeleteEntitiesAsync_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var entitiesToDelete = this.CreateEntitiesInDb<Entity>();

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        await Invoking(() => this.manipulator.DeleteEntitiesAsync(
                    this.Connection,
                    entitiesToDelete,
                    null,
                    cancellationToken
                )
            )
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);

        foreach (var entity in entitiesToDelete)
        {
            // Since the operation was cancelled, the entities should still exist.
            this.ExistsEntityInDb(entity)
                .Should().BeTrue();
        }
    }

    [Fact]
    public Task DeleteEntitiesAsync_EntitiesHaveNoKeyProperty_ShouldThrow()
    {
        var entityWithoutKeyProperty = new EntityWithoutKeyProperty();

        return Invoking(() => this.manipulator.DeleteEntitiesAsync(
                    this.Connection,
                    [entityWithoutKeyProperty],
                    null,
                    TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage(
                $"Could not get the key property / properties of the type {typeof(EntityWithoutKeyProperty)}. " +
                $"Make sure that at least one instance property of that type is denoted with a {typeof(KeyAttribute)}."
            );
    }

    [Fact]
    public async Task DeleteEntitiesAsync_EntitiesWithoutTableAttribute_ShouldUseEntityTypeNameAsTableName()
    {
        var entitiesToDelete = this.CreateEntitiesInDb<Entity>();

        await this.manipulator.DeleteEntitiesAsync(
            this.Connection,
            entitiesToDelete,
            null,
            TestContext.Current.CancellationToken
        );

        foreach (var entity in entitiesToDelete)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeFalse();
        }
    }

    [Fact]
    public async Task DeleteEntitiesAsync_EntitiesWithTableAttribute_ShouldUseTableNameFromAttribute()
    {
        var entities = this.CreateEntitiesInDb<EntityWithTableAttribute>();

        await this.manipulator.DeleteEntitiesAsync(
            this.Connection,
            entities,
            null,
            TestContext.Current.CancellationToken
        );

        foreach (var entity in entities)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeFalse();
        }
    }

    [Fact]
    public async Task DeleteEntitiesAsync_MoreThan10Entities_ShouldBatchDeleteIfPossible()
    {
        // Some database adapters (like the SQL Server one) use batch deletion for more than 10 entities, so we need
        // to test that as well.

        var entitiesToDelete = this.CreateEntitiesInDb<Entity>(20);

        await this.manipulator.DeleteEntitiesAsync(
            this.Connection,
            entitiesToDelete,
            null,
            TestContext.Current.CancellationToken
        );

        foreach (var entity in entitiesToDelete)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeFalse();
        }
    }

    [Fact]
    public async Task DeleteEntitiesAsync_ShouldHandleEntityWithCompositeKey()
    {
        var entities = this.CreateEntitiesInDb<EntityWithCompositeKey>();

        await this.manipulator.DeleteEntitiesAsync(
            this.Connection,
            entities,
            null,
            TestContext.Current.CancellationToken
        );

        foreach (var entity in entities)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeFalse();
        }
    }

    [Fact]
    public async Task DeleteEntitiesAsync_ShouldReturnNumberOfAffectedRows()
    {
        var entitiesToDelete = this.CreateEntitiesInDb<Entity>();

        (await this.manipulator.DeleteEntitiesAsync(
                this.Connection,
                entitiesToDelete,
                null,
                TestContext.Current.CancellationToken
            ))
            .Should().Be(entitiesToDelete.Count);

        (await this.manipulator.DeleteEntitiesAsync(
                this.Connection,
                entitiesToDelete,
                null,
                TestContext.Current.CancellationToken
            ))
            .Should().Be(0);
    }

    [Fact]
    public async Task DeleteEntitiesAsync_Transaction_ShouldUseTransaction()
    {
        var entitiesToDelete = this.CreateEntitiesInDb<Entity>();

        using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            await this.manipulator.DeleteEntitiesAsync(
                this.Connection,
                entitiesToDelete,
                transaction,
                TestContext.Current.CancellationToken
            );

            foreach (var entity in entitiesToDelete)
            {
                this.ExistsEntityInDb(entity, transaction)
                    .Should().BeFalse();
            }

            await transaction.RollbackAsync();
        }

        foreach (var entity in entitiesToDelete)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeTrue();
        }
    }

    private readonly IEntityManipulator manipulator;
}
