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
    public async Task InsertEntityAsync_CancellationToken_ShouldCancelOperationIfCancellationIsRequested(
        Boolean useAsyncApi
    )
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
    public async Task InsertEntityAsync_EntityWithoutTableAttribute_ShouldUseEntityTypeNameAsTableName(
        Boolean useAsyncApi
    )
    {
        var entity = Generate.Single<Entity>();

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            entity,
            null,
            TestContext.Current.CancellationToken
        );

        this.ExistsEntityInDb(entity)
            .Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsertEntityAsync_EntityWithTableAttribute_ShouldUseTableNameFromAttribute(Boolean useAsyncApi)
    {
        var entity = Generate.Single<EntityWithTableAttribute>();

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            entity,
            null,
            TestContext.Current.CancellationToken
        );

        this.ExistsEntityInDb(entity)
            .Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsertEntityAsync_EnumSerializationModeIsIntegers_ShouldStoreEnumValuesAsIntegers(
        Boolean useAsyncApi
    )
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
    public async Task InsertEntityAsync_EnumSerializationModeIsStrings_ShouldStoreEnumValuesAsStrings(
        Boolean useAsyncApi
    )
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
    public async Task InsertEntityAsync_ShouldHandleIdentityAndComputedColumns(Boolean useAsyncApi)
    {
        var entity = Generate.Single<EntityWithIdentityAndComputedProperties>();

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            entity,
            null,
            TestContext.Current.CancellationToken
        );

        entity
            .Should().BeEquivalentTo(
                await this.Connection.QuerySingleAsync<EntityWithIdentityAndComputedProperties>(
                    $"SELECT * FROM {Q("EntityWithIdentityAndComputedProperties")}"
                )
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsertEntityAsync_ShouldIgnorePropertiesDenotedWithNotMappedAttribute(Boolean useAsyncApi)
    {
        var entity = Generate.Single<EntityWithNotMappedProperty>();
        entity.NotMappedValue = "ShouldNotBePersisted";

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            entity,
            null,
            TestContext.Current.CancellationToken
        );

        await using var reader = await this.Connection.ExecuteReaderAsync(
            $"SELECT {Q("Id")}, {Q("NotMappedValue")} FROM {Q("EntityWithNotMappedProperty")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            reader.IsDBNull(reader.GetOrdinal("NotMappedValue"))
                .Should().BeTrue();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsertEntityAsync_ShouldInsertEntity(Boolean useAsyncApi)
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
    public async Task InsertEntityAsync_ShouldReturnNumberOfAffectedRows(Boolean useAsyncApi)
    {
        var entity = Generate.Single<Entity>();

        (await this.CallApi(useAsyncApi, this.Connection, entity, null, TestContext.Current.CancellationToken))
            .Should().Be(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsertEntityAsync_ShouldSupportDateTimeOffsetValues(Boolean useAsyncApi)
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
    public async Task InsertEntityAsync_ShouldUseConfiguredColumnNames(Boolean useAsyncApi)
    {
        var entity = Generate.Single<EntityWithColumnAttributes>();

        await this.CallApi(useAsyncApi, this.Connection, entity, null, TestContext.Current.CancellationToken);

        (await this.Connection.QuerySingleAsync<EntityWithColumnAttributes>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsertEntityAsync_Transaction_ShouldUseTransaction(Boolean useAsyncApi)
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
