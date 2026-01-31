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

// TODO: Table Name -> via Type Name
// TODO: Table Name -> via Attribute
// TODO: Table Name -> via Fluent API

// TODO: Column Name -> via Property Name
// TODO: Column Name -> via Attribute
// TODO: Column Name -> via Fluent API

// TODO: Key Property -> via Attribute
// TODO: Key Property -> via Fluent API

// TODO: Computed Property -> via Attribute
// TODO: Computed Property -> via Fluent API

// TODO: Identity Property -> via Attribute
// TODO: Identity Property -> via Fluent API

// TODO: Ignore Property -> via Attribute
// TODO: Ignore Property -> via Fluent API

public abstract class EntityManipulator_DeleteEntitiesTests
    <TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    /// <inheritdoc />
    protected EntityManipulator_DeleteEntitiesTests() =>
        this.manipulator = this.DatabaseAdapter.EntityManipulator;

    [Theory]
    [InlineData(false, 10)]
    [InlineData(true, 10)]
    // Some database adapters (like the SQL Server one) use batch deletion for more than 10 entities, so we need
    // to test that as well.
    [InlineData(false, 30)]
    [InlineData(true, 30)]
    public async Task DeleteEntities_Mapping_Attributes_ShouldUseAttributesMapping(Boolean useAsyncApi, Int32 numberOfEntities)
    {
        var entities = this.CreateEntitiesInDb<MappingTestEntityAttributes>(numberOfEntities);
        var entitiesToDelete = entities.Take(numberOfEntities/2).ToList();
        var entitiesToKeep = entities.Skip(numberOfEntities / 2).ToList();

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
    [InlineData(false, 10)]
    [InlineData(true, 10)]
    // Some database adapters (like the SQL Server one) use batch deletion for more than 10 entities, so we need
    // to test that as well.
    [InlineData(false, 30)]
    [InlineData(true, 30)]
    public async Task DeleteEntities_Mapping_FluentApi_ShouldUseFluentApiMapping(Boolean useAsyncApi, Int32 numberOfEntities)
    {
        Configure(config =>
            {
                config.Entity<MappingTestEntityFluentApi>()
                    .ToTable("MappingTestEntity");

                config.Entity<MappingTestEntityFluentApi>()
                    .Property(a => a.KeyColumn1_)
                    .HasColumnName("KeyColumn1")
                    .IsKey();

                config.Entity<MappingTestEntityFluentApi>()
                    .Property(a => a.KeyColumn2_)
                    .HasColumnName("KeyColumn2")
                    .IsKey();

                config.Entity<MappingTestEntityFluentApi>()
                    .Property(a => a.ValueColumn_)
                    .HasColumnName("ValueColumn");

                config.Entity<MappingTestEntityFluentApi>()
                    .Property(a => a.ComputedColumn_)
                    .HasColumnName("ComputedColumn")
                    .IsComputed();

                config.Entity<MappingTestEntityFluentApi>()
                    .Property(a => a.IdentityColumn_)
                    .HasColumnName("IdentityColumn")
                    .IsIdentity();

                config.Entity<MappingTestEntityFluentApi>()
                    .Property(a => a.NotMappedColumn)
                    .IsIgnored();
            }
        );

        var entities = this.CreateEntitiesInDb<MappingTestEntityFluentApi>(numberOfEntities);
        var entitiesToDelete = entities.Take(numberOfEntities / 2).ToList();
        var entitiesToKeep = entities.Skip(numberOfEntities / 2).ToList();

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
    [InlineData(false, 10)]
    [InlineData(true, 10)]
    // Some database adapters (like the SQL Server one) use batch deletion for more than 10 entities, so we need
    // to test that as well.
    [InlineData(false, 30)]
    [InlineData(true, 30)]
    public async Task DeleteEntities_Mapping_NoMapping_ShouldUseDefaults(Boolean useAsyncApi, Int32 numberOfEntities)
    {
        var entities = this.CreateEntitiesInDb<MappingTestEntity>(numberOfEntities);
        var entitiesToDelete = entities.Take(numberOfEntities / 2).ToList();
        var entitiesToKeep = entities.Skip(numberOfEntities / 2).ToList();

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
                $"Could not get the key property / properties of the type {typeof(EntityWithoutKeyProperty)}. " +
                $"Make sure that at least one instance property of that type is denoted with a {typeof(KeyAttribute)}."
            );
    }

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
                entitiesToDelete,
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
