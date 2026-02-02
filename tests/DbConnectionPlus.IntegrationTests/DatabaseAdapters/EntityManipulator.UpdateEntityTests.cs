using System.Data.Common;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.DatabaseAdapters;

public sealed class
    EntityManipulator_UpdateEntityTests_MySql :
    EntityManipulator_UpdateEntityTests<MySqlTestDatabaseProvider>;

public sealed class
    EntityManipulator_UpdateEntityTests_Oracle :
    EntityManipulator_UpdateEntityTests<OracleTestDatabaseProvider>;

public sealed class
    EntityManipulator_UpdateEntityTests_PostgreSql :
    EntityManipulator_UpdateEntityTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    EntityManipulator_UpdateEntityTests_Sqlite :
    EntityManipulator_UpdateEntityTests<SqliteTestDatabaseProvider>;

public sealed class
    EntityManipulator_UpdateEntityTests_SqlServer :
    EntityManipulator_UpdateEntityTests<SqlServerTestDatabaseProvider>;

public abstract class EntityManipulator_UpdateEntityTests
    <TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    /// <inheritdoc />
    protected EntityManipulator_UpdateEntityTests() =>
        this.manipulator = this.DatabaseAdapter.EntityManipulator;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateEntity_CancellationToken_ShouldCancelOperationIfCancellationIsRequested(Boolean useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var entity = this.CreateEntityInDb<Entity>();
        var updatedEntity = Generate.UpdateFor(entity);

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        await Invoking(() =>
                this.CallApi(useAsyncApi, this.Connection, updatedEntity, null, cancellationToken)
            )
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);

        // Since the operation was cancelled, the entity should not have been updated.
        (await this.Connection.QuerySingleAsync<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateEntity_EnumSerializationModeIsIntegers_ShouldStoreEnumValuesAsIntegers(Boolean useAsyncApi)
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Integers;

        var entity = Generate.Single<EntityWithEnumStoredAsInteger>();

        await this.manipulator.InsertEntityAsync(this.Connection, entity, null, TestContext.Current.CancellationToken);

        // Make sure the enum is stored as integer:
        (await this.Connection.QuerySingleAsync<Int32>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsInteger")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be((Int32)entity.Enum);

        var updatedEntity = Generate.UpdateFor(entity);

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            updatedEntity,
            null,
            TestContext.Current.CancellationToken
        );

        // Make sure the enum is stored as integer:
        (await this.Connection.QuerySingleAsync<Int32>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsInteger")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be((Int32)updatedEntity.Enum);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateEntity_EnumSerializationModeIsStrings_ShouldStoreEnumValuesAsStrings(Boolean useAsyncApi)
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        var entity = Generate.Single<EntityWithEnumStoredAsString>();

        await this.manipulator.InsertEntityAsync(this.Connection, entity, null, TestContext.Current.CancellationToken);

        // Make sure the enum is stored as string:
        (await this.Connection.QuerySingleAsync<String>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsString")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entity.Enum.ToString());

        var updatedEntity = Generate.UpdateFor(entity);

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            updatedEntity,
            null,
            TestContext.Current.CancellationToken
        );

        // Make sure the enum is stored as string:
        (await this.Connection.QuerySingleAsync<String>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsString")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(updatedEntity.Enum.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateEntity_Mapping_Attributes_ShouldUseAttributesMapping(Boolean useAsyncApi)
    {
        var entity = this.CreateEntityInDb<MappingTestEntityAttributes>();

        var updatedEntity = Generate.UpdateFor(entity);
        updatedEntity.Computed_ = 0;
        updatedEntity.Identity_ = 0;
        updatedEntity.NotMapped = "ShouldNotBePersisted";

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            updatedEntity,
            null,
            TestContext.Current.CancellationToken
        );

        this.Connection.QueryFirst<MappingTestEntityAttributes>($"SELECT * FROM {Q("MappingTestEntity")}")
            .Should().BeEquivalentTo(
                updatedEntity,
                options => options.Using<String>(context => context.Subject.Should().BeNull())
                    .When(info => info.Path.EndsWith("NotMapped"))
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateEntity_Mapping_FluentApi_ShouldUseFluentApiMapping(Boolean useAsyncApi)
    {
        MappingTestEntityFluentApi.Configure();

        var entity = this.CreateEntityInDb<MappingTestEntityFluentApi>();

        var updatedEntity = Generate.UpdateFor(entity);
        updatedEntity.Computed_ = 0;
        updatedEntity.Identity_ = 0;
        updatedEntity.NotMapped = "ShouldNotBePersisted";

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            updatedEntity,
            null,
            TestContext.Current.CancellationToken
        );

        this.Connection.QueryFirst<MappingTestEntityFluentApi>($"SELECT * FROM {Q("MappingTestEntity")}")
            .Should().BeEquivalentTo(
                updatedEntity,
                options => options.Using<String>(context => context.Subject.Should().BeNull())
                    .When(info => info.Path.EndsWith("NotMapped"))
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task UpdateEntity_Mapping_MissingKeyProperty_ShouldThrow(Boolean useAsyncApi)
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
                $"No property of the type {typeof(EntityWithoutKeyProperty)} is configured as a key property. Make " +
                "sure that at least one instance property of that type is configured as key property."
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateEntity_Mapping_NoMapping_ShouldUseEntityTypeNameAndPropertyNames(Boolean useAsyncApi)
    {
        var entity = this.CreateEntityInDb<MappingTestEntity>();
        var updatedEntity = Generate.UpdateFor(entity);

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            updatedEntity,
            null,
            TestContext.Current.CancellationToken
        );

        this.Connection.QueryFirst<MappingTestEntity>($"SELECT * FROM {Q("MappingTestEntity")}")
            .Should().BeEquivalentTo(updatedEntity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateEntity_ShouldReturnNumberOfAffectedRows(Boolean useAsyncApi)
    {
        var entity = this.CreateEntityInDb<Entity>();
        var updatedEntity = Generate.UpdateFor(entity);

        (await this.CallApi(
                useAsyncApi,
                this.Connection,
                updatedEntity,
                null,
                TestContext.Current.CancellationToken
            ))
            .Should().Be(1);

        var nonExistentEntity = Generate.Single<Entity>();

        (await this.CallApi(
                useAsyncApi,
                this.Connection,
                nonExistentEntity,
                null,
                TestContext.Current.CancellationToken
            ))
            .Should().Be(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateEntity_ShouldSupportDateTimeOffsetValues(Boolean useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entity = this.CreateEntityInDb<EntityWithDateTimeOffset>();
        var updatedEntity = Generate.UpdateFor(entity);

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            updatedEntity,
            null,
            TestContext.Current.CancellationToken
        );

        (await this.Connection.QuerySingleAsync<EntityWithDateTimeOffset>(
                $"SELECT * FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(updatedEntity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateEntity_ShouldUpdateEntity(Boolean useAsyncApi)
    {
        var entity = this.CreateEntityInDb<Entity>();
        var updatedEntity = Generate.UpdateFor(entity);

        (await this.CallApi(
                useAsyncApi,
                this.Connection,
                updatedEntity,
                null,
                TestContext.Current.CancellationToken
            ))
            .Should().Be(1);

        (await this.Connection.QuerySingleAsync<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(updatedEntity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateEntity_Transaction_ShouldUseTransaction(Boolean useAsyncApi)
    {
        var entity = this.CreateEntityInDb<Entity>();

        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            var updatedEntity = Generate.UpdateFor(entity);

            (await this.CallApi(
                    useAsyncApi,
                    this.Connection,
                    updatedEntity,
                    transaction,
                    TestContext.Current.CancellationToken
                ))
                .Should().Be(1);

            (await this.Connection.QuerySingleAsync<Entity>($"SELECT * FROM {Q("Entity")}", transaction))
                .Should().BeEquivalentTo(updatedEntity);

            await transaction.RollbackAsync();
        }

        (await this.Connection.QuerySingleAsync<Entity>($"SELECT * FROM {Q("Entity")}"))
            .Should().BeEquivalentTo(entity);
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
            return this.manipulator.UpdateEntityAsync(connection, entity, transaction, cancellationToken);
        }

        try
        {
            return Task.FromResult(
                this.manipulator.UpdateEntity(connection, entity, transaction, cancellationToken)
            );
        }
        catch (Exception ex)
        {
            return Task.FromException<Int32>(ex);
        }
    }

    private readonly IEntityManipulator manipulator;
}
