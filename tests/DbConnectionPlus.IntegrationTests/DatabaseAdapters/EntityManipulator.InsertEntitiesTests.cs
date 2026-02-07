using System.Data.Common;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.DatabaseAdapters;

public sealed class
    EntityManipulator_InsertEntitiesTests_MySql :
    EntityManipulator_InsertEntitiesTests<MySqlTestDatabaseProvider>;

public sealed class
    EntityManipulator_InsertEntitiesTests_Oracle :
    EntityManipulator_InsertEntitiesTests<OracleTestDatabaseProvider>;

public sealed class
    EntityManipulator_InsertEntitiesTests_PostgreSql :
    EntityManipulator_InsertEntitiesTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    EntityManipulator_InsertEntitiesTests_Sqlite :
    EntityManipulator_InsertEntitiesTests<SqliteTestDatabaseProvider>;

public sealed class
    EntityManipulator_InsertEntitiesTests_SqlServer :
    EntityManipulator_InsertEntitiesTests<SqlServerTestDatabaseProvider>;

public abstract class EntityManipulator_InsertEntitiesTests
    <TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    /// <inheritdoc />
    protected EntityManipulator_InsertEntitiesTests() =>
        this.manipulator = this.DatabaseAdapter.EntityManipulator;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsertEntities_CancellationToken_ShouldCancelOperationIfCancellationIsRequested(
        Boolean useAsyncApi
    )
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var entities = Generate.Multiple<Entity>();

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DelayNextDbCommand = true;

        await Invoking(() =>
                this.CallApi(useAsyncApi, this.Connection, entities, null, cancellationToken)
            )
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);

        // Since the operation was cancelled, the entities should not have been inserted.
        foreach (var entityToInsert in entities)
        {
            this.ExistsEntityInDb(entityToInsert)
                .Should().BeFalse();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsertEntities_EnumSerializationModeIsIntegers_ShouldStoreEnumValuesAsIntegers(
        Boolean useAsyncApi
    )
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Integers;

        var entities = Generate.Multiple<EntityWithEnumStoredAsInteger>();

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            entities,
            null,
            TestContext.Current.CancellationToken
        );

        (await this.Connection.QueryAsync<Int32>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsInteger")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(entities.Select(a => (Int32)a.Enum));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsertEntities_EnumSerializationModeIsStrings_ShouldStoreEnumValuesAsStrings(Boolean useAsyncApi)
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        var entities = Generate.Multiple<EntityWithEnumStoredAsString>();

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            entities,
            null,
            TestContext.Current.CancellationToken
        );

        (await this.Connection.QueryAsync<String>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsString")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(entities.Select(a => a.Enum.ToString()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsertEntities_Mapping_Attributes_ShouldUseAttributesMapping(Boolean useAsyncApi)
    {
        var entities = Generate.Multiple<MappingTestEntityAttributes>();
        entities.ForEach(a =>
            {
                a.Computed_ = 0;
                a.Identity_ = 0;
                a.NotMapped = "ShouldNotBePersisted";
            }
        );

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            entities,
            null,
            TestContext.Current.CancellationToken
        );

        this.Connection.Query<MappingTestEntityAttributes>($"SELECT * FROM {Q("MappingTestEntity")}")
            .Should().BeEquivalentTo(
                entities,
                options => options.Using<String>(context => context.Subject.Should().BeNull())
                    .When(info => info.Path.EndsWith("NotMapped"))
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsertEntities_Mapping_FluentApi_ShouldUseFluentApiMapping(Boolean useAsyncApi)
    {
        MappingTestEntityFluentApi.Configure();

        var entities = Generate.Multiple<MappingTestEntityFluentApi>();
        entities.ForEach(a =>
            {
                a.Computed_ = 0;
                a.Identity_ = 0;
                a.NotMapped = "ShouldNotBePersisted";
            }
        );

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            entities,
            null,
            TestContext.Current.CancellationToken
        );

        this.Connection.Query<MappingTestEntityFluentApi>($"SELECT * FROM {Q("MappingTestEntity")}")
            .Should().BeEquivalentTo(
                entities,
                options => options.Using<String>(context => context.Subject.Should().BeNull())
                    .When(info => info.Path.EndsWith("NotMapped"))
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsertEntities_Mapping_NoMapping_ShouldUseEntityTypeNameAndPropertyNames(Boolean useAsyncApi)
    {
        var entities = Generate.Multiple<MappingTestEntity>();

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            entities,
            null,
            TestContext.Current.CancellationToken
        );

        this.Connection.Query<MappingTestEntity>($"SELECT * FROM {Q("MappingTestEntity")}")
            .Should().BeEquivalentTo(entities);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsertEntities_ShouldInsertEntities(Boolean useAsyncApi)
    {
        var entities = Generate.Multiple<Entity>();

        (await this.CallApi(
                useAsyncApi,
                this.Connection,
                entities,
                null,
                TestContext.Current.CancellationToken
            ))
            .Should().Be(entities.Count);

        (await this.Connection.QueryAsync<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(entities);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsertEntities_ShouldReturnNumberOfAffectedRows(Boolean useAsyncApi)
    {
        var entities = Generate.Multiple<Entity>();

        (await this.CallApi(
                useAsyncApi,
                this.Connection,
                entities,
                null,
                TestContext.Current.CancellationToken
            ))
            .Should().Be(entities.Count);

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
    public async Task InsertEntities_ShouldSupportDateTimeOffsetValues(Boolean useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entities = Generate.Multiple<EntityWithDateTimeOffset>();

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            entities,
            null,
            TestContext.Current.CancellationToken
        );

        (await this.Connection.QueryAsync<EntityWithDateTimeOffset>(
                $"SELECT * FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(entities);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsertEntities_Transaction_ShouldUseTransaction(Boolean useAsyncApi)
    {
        var entities = Generate.Multiple<Entity>();

        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            (await this.CallApi(
                    useAsyncApi,
                    this.Connection,
                    entities,
                    transaction,
                    TestContext.Current.CancellationToken
                ))
                .Should().Be(entities.Count);

            foreach (var entity in entities)
            {
                this.ExistsEntityInDb(entity, transaction)
                    .Should().BeTrue();
            }

            await transaction.RollbackAsync();
        }

        foreach (var entity in entities)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeFalse();
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
            return this.manipulator.InsertEntitiesAsync(connection, entities, transaction, cancellationToken);
        }

        try
        {
            return Task.FromResult(
                this.manipulator.InsertEntities(connection, entities, transaction, cancellationToken)
            );
        }
        catch (Exception ex)
        {
            return Task.FromException<Int32>(ex);
        }
    }

    private readonly IEntityManipulator manipulator;
}
