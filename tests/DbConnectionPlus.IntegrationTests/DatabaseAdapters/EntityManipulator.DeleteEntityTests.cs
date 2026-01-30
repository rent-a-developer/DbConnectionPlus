using System.Data.Common;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.DatabaseAdapters;

public sealed class
    EntityManipulator_DeleteEntityTests_MySql :
    EntityManipulator_DeleteEntityTests<MySqlTestDatabaseProvider>;

public sealed class
    EntityManipulator_DeleteEntityTests_Oracle :
    EntityManipulator_DeleteEntityTests<OracleTestDatabaseProvider>;

public sealed class
    EntityManipulator_DeleteEntityTests_PostgreSql :
    EntityManipulator_DeleteEntityTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    EntityManipulator_DeleteEntityTests_Sqlite :
    EntityManipulator_DeleteEntityTests<SqliteTestDatabaseProvider>;

public sealed class
    EntityManipulator_DeleteEntityTests_SqlServer :
    EntityManipulator_DeleteEntityTests<SqlServerTestDatabaseProvider>;

public abstract class EntityManipulator_DeleteEntityTests
    <TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    /// <inheritdoc />
    protected EntityManipulator_DeleteEntityTests() =>
        this.manipulator = this.DatabaseAdapter.EntityManipulator;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteEntityAsync_CancellationToken_ShouldCancelOperationIfCancellationIsRequested(
        Boolean useAsyncApi
    )
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var entityToDelete = this.CreateEntityInDb<Entity>();

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        await Invoking(() => this.CallApi(
                    useAsyncApi,
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteEntityAsync_EntityWithoutTableAttribute_ShouldUseEntityTypeNameAsTableName(
        Boolean useAsyncApi
    )
    {
        var entityToDelete = this.CreateEntityInDb<Entity>();

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            entityToDelete,
            null,
            TestContext.Current.CancellationToken
        );

        this.ExistsEntityInDb(entityToDelete)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteEntityAsync_EntityWithTableAttribute_ShouldUseTableNameFromAttribute(Boolean useAsyncApi)
    {
        var entity = this.CreateEntityInDb<EntityWithTableAttribute>();

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            entity,
            null,
            TestContext.Current.CancellationToken
        );

        this.ExistsEntityInDb(entity)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task DeleteEntityAsync_MissingKeyProperty_ShouldThrow(Boolean useAsyncApi)
    {
        var entityWithoutKeyProperty = new EntityWithoutKeyProperty();

        return Invoking(() => this.CallApi(
                    useAsyncApi,
                    this.Connection,
                    entityWithoutKeyProperty,
                    null,
                    TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage(
                $"Could not get the key property / properties of the type {typeof(EntityWithoutKeyProperty)}. " +
                "Make sure that at least one instance property of that type is denoted with a " +
                $"{typeof(KeyAttribute)}."
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteEntityAsync_ShouldHandleEntityWithCompositeKey(Boolean useAsyncApi)
    {
        var entity = this.CreateEntityInDb<EntityWithCompositeKey>();

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            entity,
            null,
            TestContext.Current.CancellationToken
        );

        this.ExistsEntityInDb(entity)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteEntityAsync_ShouldReturnNumberOfAffectedRows(Boolean useAsyncApi)
    {
        var entityToDelete = this.CreateEntityInDb<Entity>();

        (await this.CallApi(
                useAsyncApi,
                this.Connection,
                entityToDelete,
                null,
                TestContext.Current.CancellationToken
            ))
            .Should().Be(1);

        (await this.CallApi(
                useAsyncApi,
                this.Connection,
                entityToDelete,
                null,
                TestContext.Current.CancellationToken
            ))
            .Should().Be(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteEntityAsync_ShouldUseConfiguredColumnNames(Boolean useAsyncApi)
    {
        var entity = this.CreateEntityInDb<Entity>();
        var entityWithColumnAttributes = Generate.MapTo<EntityWithColumnAttributes>(entity);

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            entityWithColumnAttributes,
            null,
            TestContext.Current.CancellationToken
        );

        this.ExistsEntityInDb(entity)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteEntityAsync_Transaction_ShouldUseTransaction(Boolean useAsyncApi)
    {
        var entityToDelete = this.CreateEntityInDb<Entity>();

        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            await this.CallApi(
                useAsyncApi,
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

    private Task<Int32> CallApi<TEntity>(
        Boolean useAsyncApi,
        DbConnection connection,
        TEntity entity,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default
    )
        where TEntity : class
    {
        if (useAsyncApi)
        {
            return this.manipulator.DeleteEntityAsync(connection, entity, transaction, cancellationToken);
        }

        try
        {
            return Task.FromResult(
                this.manipulator.DeleteEntity(connection, entity, transaction, cancellationToken)
            );
        }
        catch (Exception ex)
        {
            return Task.FromException<Int32>(ex);
        }
    }

    private readonly IEntityManipulator manipulator;
}
