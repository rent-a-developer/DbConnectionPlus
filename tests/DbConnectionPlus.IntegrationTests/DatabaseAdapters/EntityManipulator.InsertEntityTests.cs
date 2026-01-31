using System.Data.Common;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.DatabaseAdapters;

public sealed class
    EntityManipulator_InsertEntityTests_MySql :
    EntityManipulator_InsertEntityTests<MySqlTestDatabaseProvider>;

public sealed class
    EntityManipulator_InsertEntityTests_Oracle :
    EntityManipulator_InsertEntityTests<OracleTestDatabaseProvider>;

public sealed class
    EntityManipulator_InsertEntityTests_PostgreSql :
    EntityManipulator_InsertEntityTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    EntityManipulator_InsertEntityTests_Sqlite :
    EntityManipulator_InsertEntityTests<SqliteTestDatabaseProvider>;

public sealed class
    EntityManipulator_InsertEntityTests_SqlServer :
    EntityManipulator_InsertEntityTests<SqlServerTestDatabaseProvider>;

public abstract class EntityManipulator_InsertEntityTests
    <TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    /// <inheritdoc />
    protected EntityManipulator_InsertEntityTests() =>
        this.manipulator = this.DatabaseAdapter.EntityManipulator;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsertEntity_CancellationToken_ShouldCancelOperationIfCancellationIsRequested(Boolean useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var entity = Generate.Single<Entity>();

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        await Invoking(() =>
                this.CallApi(useAsyncApi, this.Connection, entity, null, cancellationToken)
            )
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);

        // Since the operation was cancelled, the entity should not have been inserted.
        this.ExistsEntityInDb(entity)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsertEntity_EnumSerializationModeIsIntegers_ShouldStoreEnumValuesAsIntegers(Boolean useAsyncApi)
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Integers;

        var entity = Generate.Single<EntityWithEnumStoredAsInteger>();

        await this.CallApi(useAsyncApi, this.Connection, entity, null, TestContext.Current.CancellationToken);

        (await this.Connection.QuerySingleAsync<Int32>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsInteger")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be((Int32)entity.Enum);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsertEntity_EnumSerializationModeIsStrings_ShouldStoreEnumValuesAsStrings(Boolean useAsyncApi)
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        var entity = Generate.Single<EntityWithEnumStoredAsString>();

        await this.CallApi(useAsyncApi, this.Connection, entity, null, TestContext.Current.CancellationToken);

        (await this.Connection.QuerySingleAsync<String>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsString")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entity.Enum.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsertEntity_Mapping_Attributes_ShouldUseAttributesMapping(Boolean useAsyncApi)
    {
        var entity = Generate.Single<MappingTestEntityAttributes>();
        entity.ComputedColumn_ = 0;
        entity.IdentityColumn_ = 0;
        entity.NotMappedColumn = "ShouldNotBePersisted";

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            entity,
            null,
            TestContext.Current.CancellationToken
        );

        this.Connection.QueryFirst<MappingTestEntityAttributes>($"SELECT * FROM {Q("MappingTestEntity")}")
            .Should().BeEquivalentTo(
                entity,
                options => options.Using<String>(context => context.Subject.Should().BeNull())
                    .When(info => info.Path.EndsWith("NotMappedColumn"))
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsertEntity_Mapping_FluentApi_ShouldUseFluentApiMapping(Boolean useAsyncApi)
    {
        MappingTestEntityFluentApi.Configure();

        var entity = Generate.Single<MappingTestEntityFluentApi>();
        entity.ComputedColumn_ = 0;
        entity.IdentityColumn_ = 0;
        entity.NotMappedColumn = "ShouldNotBePersisted";

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            entity,
            null,
            TestContext.Current.CancellationToken
        );

        this.Connection.QueryFirst<MappingTestEntityFluentApi>($"SELECT * FROM {Q("MappingTestEntity")}")
            .Should().BeEquivalentTo(
                entity,
                options => options.Using<String>(context => context.Subject.Should().BeNull())
                    .When(info => info.Path.EndsWith("NotMappedColumn"))
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsertEntity_Mapping_NoMapping_ShouldUseEntityTypeNameAndPropertyNames(Boolean useAsyncApi)
    {
        var entity = Generate.Single<MappingTestEntity>();

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            entity,
            null,
            TestContext.Current.CancellationToken
        );

        this.Connection.QueryFirst<MappingTestEntity>($"SELECT * FROM {Q("MappingTestEntity")}")
            .Should().BeEquivalentTo(entity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsertEntity_ShouldInsertEntity(Boolean useAsyncApi)
    {
        var entity = Generate.Single<Entity>();

        (await this.CallApi(useAsyncApi, this.Connection, entity, null, TestContext.Current.CancellationToken))
            .Should().Be(1);

        (await this.Connection.QuerySingleAsync<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsertEntity_ShouldReturnNumberOfAffectedRows(Boolean useAsyncApi)
    {
        var entity = Generate.Single<Entity>();

        (await this.CallApi(useAsyncApi, this.Connection, entity, null, TestContext.Current.CancellationToken))
            .Should().Be(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsertEntity_ShouldSupportDateTimeOffsetValues(Boolean useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entity = Generate.Single<EntityWithDateTimeOffset>();

        await this.CallApi(useAsyncApi, this.Connection, entity, null, TestContext.Current.CancellationToken);

        (await this.Connection.QuerySingleAsync<EntityWithDateTimeOffset>(
                $"SELECT * FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsertEntity_Transaction_ShouldUseTransaction(Boolean useAsyncApi)
    {
        var entity = Generate.Single<Entity>();

        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            (await this.CallApi(
                    useAsyncApi,
                    this.Connection,
                    entity,
                    transaction,
                    TestContext.Current.CancellationToken
                ))
                .Should().Be(1);

            this.ExistsEntityInDb(entity, transaction)
                .Should().BeTrue();

            await transaction.RollbackAsync();
        }

        this.ExistsEntityInDb(entity)
            .Should().BeFalse();
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
            return this.manipulator.InsertEntityAsync(connection, entity, transaction, cancellationToken);
        }

        try
        {
            return Task.FromResult(
                this.manipulator.InsertEntity(connection, entity, transaction, cancellationToken)
            );
        }
        catch (Exception ex)
        {
            return Task.FromException<Int32>(ex);
        }
    }

    private readonly IEntityManipulator manipulator;
}
