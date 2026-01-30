using System.Data.Common;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.DatabaseAdapters;

public sealed class
    EntityManipulator_DeleteEntitiesTests_MySql :
    EntityManipulator_DeleteEntitiesTests<MySqlTestDatabaseProvider>;

public sealed class
    EntityManipulator_DeleteEntitiesTests_Oracle :
    EntityManipulator_DeleteEntitiesTests<OracleTestDatabaseProvider>;

public sealed class
    EntityManipulator_DeleteEntitiesTests_PostgreSql :
    EntityManipulator_DeleteEntitiesTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    EntityManipulator_DeleteEntitiesTests_Sqlite :
    EntityManipulator_DeleteEntitiesTests<SqliteTestDatabaseProvider>;

public sealed class
    EntityManipulator_DeleteEntitiesTests_SqlServer :
    EntityManipulator_DeleteEntitiesTests<SqlServerTestDatabaseProvider>;

// TODO: Implement integration test (CRUD, Query, Temporary Tables) for fluent API config as well as attribute based
// config.

// TODO: Table mapping via Type Name
// TODO: Table Name mapping via Attribute
// TODO: Table Name mapping via Fluent API
// TODO: Column Name mapping via Attribute
// TODO: Column Name mapping via Fluent API
// TODO: Key Property mapping via Attribute
// TODO: Key Property mapping via Fluent API

public abstract class EntityManipulator_DeleteEntitiesTests
    <TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    /// <inheritdoc />
    protected EntityManipulator_DeleteEntitiesTests() =>
        this.manipulator = this.DatabaseAdapter.EntityManipulator;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteEntities_CancellationToken_ShouldCancelOperationIfCancellationIsRequested(
        Boolean useAsyncApi
    )
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var entitiesToDelete = this.CreateEntitiesInDb<Entity>();

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        await Invoking(() => this.CallApi(
                    useAsyncApi,
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteEntitiesAsync_EntitiesWithoutTableAttribute_ShouldUseEntityTypeNameAsTableName(
        Boolean useAsyncApi
    )
    {
        var entitiesToDelete = this.CreateEntitiesInDb<Entity>();

        await this.CallApi(
            useAsyncApi,
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteEntitiesAsync_EntitiesWithTableAttribute_ShouldUseTableNameFromAttribute(
        Boolean useAsyncApi
    )
    {
        var entities = this.CreateEntitiesInDb<EntityWithTableAttribute>();

        await this.CallApi(
            useAsyncApi,
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task DeleteEntitiesAsync_MissingKeyProperty_ShouldThrow(Boolean useAsyncApi)
    {
        var entityWithoutKeyProperty = new EntityWithoutKeyProperty();

        return Invoking(() => this.CallApi(
                    useAsyncApi,
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteEntitiesAsync_MoreThan10Entities_ShouldBatchDeleteIfPossible(Boolean useAsyncApi)
    {
        // Some database adapters (like the SQL Server one) use batch deletion for more than 10 entities, so we need
        // to test that as well.

        var entitiesToDelete = this.CreateEntitiesInDb<Entity>(20);

        await this.CallApi(
            useAsyncApi,
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteEntitiesAsync_MoreThan10Entities_ShouldUseConfiguredColumnNames(Boolean useAsyncApi)
    {
        // Some database adapters (like the SQL Server one) use batch deletion for more than 10 entities, so we need
        // to test that as well.

        var entities = this.CreateEntitiesInDb<Entity>(20);
        var entitiesWithColumnAttributes = Generate.MapTo<EntityWithColumnAttributes>(entities);

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            entitiesWithColumnAttributes,
            null,
            TestContext.Current.CancellationToken
        );

        foreach (var entity in entities)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeFalse();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteEntitiesAsync_ShouldHandleEntityWithCompositeKey(Boolean useAsyncApi)
    {
        var entities = this.CreateEntitiesInDb<EntityWithCompositeKey>();

        await this.CallApi(
            useAsyncApi,
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteEntitiesAsync_ShouldReturnNumberOfAffectedRows(Boolean useAsyncApi)
    {
        var entitiesToDelete = this.CreateEntitiesInDb<Entity>();

        (await this.CallApi(
                useAsyncApi,
                this.Connection,
                entitiesToDelete,
                null,
                TestContext.Current.CancellationToken
            ))
            .Should().Be(entitiesToDelete.Count);

        (await this.CallApi(
                useAsyncApi,
                this.Connection,
                entitiesToDelete,
                null,
                TestContext.Current.CancellationToken
            ))
            .Should().Be(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteEntitiesAsync_ShouldUseConfiguredColumnNames(Boolean useAsyncApi)
    {
        var entities = this.CreateEntitiesInDb<Entity>();
        var entitiesWithColumnAttributes = Generate.MapTo<EntityWithColumnAttributes>(entities);

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            entitiesWithColumnAttributes,
            null,
            TestContext.Current.CancellationToken
        );

        foreach (var entity in entities)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeFalse();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteEntitiesAsync_Transaction_ShouldUseTransaction(Boolean useAsyncApi)
    {
        var entitiesToDelete = this.CreateEntitiesInDb<Entity>();

        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            await this.CallApi(
                useAsyncApi,
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

    private Task<Int32> CallApi<TEntity>(
        Boolean useAsyncApi,
        DbConnection connection,
        IEnumerable<TEntity> entities,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default
    )
        where TEntity : class
    {
        if (useAsyncApi)
        {
            return this.manipulator.DeleteEntitiesAsync(connection, entities, transaction, cancellationToken);
        }

        try
        {
            return Task.FromResult(
                this.manipulator.DeleteEntities(connection, entities, transaction, cancellationToken)
            );
        }
        catch (Exception ex)
        {
            return Task.FromException<Int32>(ex);
        }
    }

    private readonly IEntityManipulator manipulator;
}
