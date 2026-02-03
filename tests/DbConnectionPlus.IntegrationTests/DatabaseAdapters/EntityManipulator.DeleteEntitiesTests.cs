using System.Data.Common;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters;
using RentADeveloper.DbConnectionPlus.Exceptions;

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

        var entities = this.CreateEntitiesInDb<Entity>(10);
        var entitiesToDelete = entities.Take(5).ToList();

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

        foreach (var entity in entities)
        {
            // Since the operation was cancelled, all entities should still exist.
            this.ExistsEntityInDb(entity)
                .Should().BeTrue();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteEntities_ConcurrencyTokenMismatch_ShouldThrow(Boolean useAsyncApi)
    {
        var entitiesToDelete = this.CreateEntitiesInDb<MappingTestEntityAttributes>(5);

        var failingEntity = entitiesToDelete[^1];
        failingEntity.ConcurrencyToken_ = Generate.Single<Byte[]>();

        (await Invoking(() => this.CallApi(
                        useAsyncApi,
                        this.Connection,
                        entitiesToDelete,
                        null,
                        TestContext.Current.CancellationToken
                    )
                )
                .Should().ThrowAsync<DbUpdateConcurrencyException>()
                .WithMessage(
                    "The database operation was expected to affect 1 row(s), but actually affected 0 row(s). " +
                    "Data in the database may have been modified or deleted since entities were loaded. See " +
                    $"{nameof(DbUpdateConcurrencyException)}.{nameof(DbUpdateConcurrencyException.Entity)} for " +
                    "the entity that was involved in the operation."
                ))
            .And.Entity.Should().Be(failingEntity);

        foreach (var entity in entitiesToDelete.Except([failingEntity]))
        {
            this.ExistsEntityInDb(entity)
                .Should().BeFalse();
        }

        this.ExistsEntityInDb(failingEntity)
            .Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteEntities_Mapping_Attributes_ShouldUseAttributesMapping(Boolean useAsyncApi)
    {
        var entities = this.CreateEntitiesInDb<MappingTestEntityAttributes>(10);
        var entitiesToDelete = entities.Take(5).ToList();
        var entitiesToKeep = entities.Skip(5).ToList();

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

        foreach (var entity in entitiesToKeep)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeTrue();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteEntities_Mapping_FluentApi_ShouldUseFluentApiMapping(Boolean useAsyncApi)
    {
        MappingTestEntityFluentApi.Configure();

        var entities = this.CreateEntitiesInDb<MappingTestEntityFluentApi>(10);
        var entitiesToDelete = entities.Take(5).ToList();
        var entitiesToKeep = entities.Skip(5).ToList();

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

        foreach (var entity in entitiesToKeep)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeTrue();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task DeleteEntities_Mapping_MissingKeyProperty_ShouldThrow(Boolean useAsyncApi)
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
                $"No property of the type {typeof(EntityWithoutKeyProperty)} is configured as a key property. Make " +
                "sure that at least one instance property of that type is configured as key property."
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteEntities_Mapping_NoMapping_ShouldUseEntityTypeNameAndPropertyNames(Boolean useAsyncApi)
    {
        var entities = this.CreateEntitiesInDb<MappingTestEntity>(10);
        var entitiesToDelete = entities.Take(5).ToList();
        var entitiesToKeep = entities.Skip(5).ToList();

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

        foreach (var entity in entitiesToKeep)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeTrue();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteEntities_RowVersionMismatch_ShouldThrow(Boolean useAsyncApi)
    {
        var entitiesToDelete = this.CreateEntitiesInDb<MappingTestEntityAttributes>(5);

        var failingEntity = entitiesToDelete[^1];
        failingEntity.RowVersion_ = Generate.Single<Byte[]>();

        (await Invoking(() => this.CallApi(
                        useAsyncApi,
                        this.Connection,
                        entitiesToDelete,
                        null,
                        TestContext.Current.CancellationToken
                    )
                )
                .Should().ThrowAsync<DbUpdateConcurrencyException>()
                .WithMessage(
                    "The database operation was expected to affect 1 row(s), but actually affected 0 row(s). " +
                    "Data in the database may have been modified or deleted since entities were loaded. See " +
                    $"{nameof(DbUpdateConcurrencyException)}.{nameof(DbUpdateConcurrencyException.Entity)} for " +
                    "the entity that was involved in the operation."
                ))
            .And.Entity.Should().Be(failingEntity);

        foreach (var entity in entitiesToDelete.Except([failingEntity]))
        {
            this.ExistsEntityInDb(entity)
                .Should().BeFalse();
        }

        this.ExistsEntityInDb(failingEntity)
            .Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteEntities_ShouldReturnNumberOfAffectedRows(Boolean useAsyncApi)
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
                Array.Empty<Entity>(),
                null,
                TestContext.Current.CancellationToken
            ))
            .Should().Be(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteEntities_Transaction_ShouldUseTransaction(Boolean useAsyncApi)
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
