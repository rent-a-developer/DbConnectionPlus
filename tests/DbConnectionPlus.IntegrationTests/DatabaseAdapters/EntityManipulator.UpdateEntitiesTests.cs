using System.Data.Common;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.DatabaseAdapters;

public sealed class
    EntityManipulator_UpdateEntitiesTests_MySql :
    EntityManipulator_UpdateEntitiesTests<MySqlTestDatabaseProvider>;

public sealed class
    EntityManipulator_UpdateEntitiesTests_Oracle :
    EntityManipulator_UpdateEntitiesTests<OracleTestDatabaseProvider>;

public sealed class
    EntityManipulator_UpdateEntitiesTests_PostgreSql :
    EntityManipulator_UpdateEntitiesTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    EntityManipulator_UpdateEntitiesTests_Sqlite :
    EntityManipulator_UpdateEntitiesTests<SqliteTestDatabaseProvider>;

public sealed class
    EntityManipulator_UpdateEntitiesTests_SqlServer :
    EntityManipulator_UpdateEntitiesTests<SqlServerTestDatabaseProvider>;

public abstract class EntityManipulator_UpdateEntitiesTests
    <TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    /// <inheritdoc />
    protected EntityManipulator_UpdateEntitiesTests() =>
        this.manipulator = this.DatabaseAdapter.EntityManipulator;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateEntitiesAsync_CancellationToken_ShouldCancelOperationIfCancellationIsRequested(
        Boolean useAsyncApi
    )
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var entities = this.CreateEntitiesInDb<Entity>();
        var updatedEntities = Generate.UpdatesFor(entities);

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        await Invoking(() =>
                this.CallApi(useAsyncApi, this.Connection, updatedEntities, null, cancellationToken)
            )
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);

        // Since the operation was cancelled, the entities should not have been updated.
        (await this.Connection.QueryAsync<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(entities);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateEntitiesAsync_EntitiesWithoutTableAttribute_ShouldUseEntityTypeNameAsTableName(
        Boolean useAsyncApi
    )
    {
        var entities = this.CreateEntitiesInDb<Entity>();
        var updatedEntities = Generate.UpdatesFor(entities);

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            updatedEntities,
            null,
            TestContext.Current.CancellationToken
        );

        (await this.Connection.QueryAsync<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(updatedEntities);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateEntitiesAsync_EntitiesWithTableAttribute_ShouldUseTableNameFromAttribute(
        Boolean useAsyncApi
    )
    {
        var entities = this.CreateEntitiesInDb<EntityWithTableAttribute>();
        var updatedEntities = Generate.UpdatesFor(entities);

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            updatedEntities,
            null,
            TestContext.Current.CancellationToken
        );

        (await this.Connection.QueryAsync<EntityWithTableAttribute>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(updatedEntities);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateEntitiesAsync_EnumSerializationModeIsIntegers_ShouldStoreEnumValuesAsIntegers(
        Boolean useAsyncApi
    )
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Integers;

        var entities = Generate.Multiple<EntityWithEnumStoredAsInteger>();

        await this.manipulator.InsertEntitiesAsync(
            this.Connection,
            entities,
            null,
            TestContext.Current.CancellationToken
        );

        // Make sure the enums are stored as integers:
        (await this.Connection.QueryAsync<Int32>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsInteger")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(entities.Select(a => (Int32)a.Enum));

        var updatedEntities = Generate.UpdatesFor(entities);

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            updatedEntities,
            null,
            TestContext.Current.CancellationToken
        );

        // Make sure the enums are stored as integers:
        (await this.Connection.QueryAsync<Int32>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsInteger")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(updatedEntities.Select(a => (Int32)a.Enum));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateEntitiesAsync_EnumSerializationModeIsStrings_ShouldStoreEnumValuesAsStrings(
        Boolean useAsyncApi
    )
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        var entities = Generate.Multiple<EntityWithEnumStoredAsString>();

        await this.manipulator.InsertEntitiesAsync(
            this.Connection,
            entities,
            null,
            TestContext.Current.CancellationToken
        );

        // Make sure the enums are stored as strings:
        (await this.Connection.QueryAsync<String>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsString")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(entities.Select(a => a.Enum.ToString()));

        var updatedEntities = Generate.UpdatesFor(entities);

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            updatedEntities,
            null,
            TestContext.Current.CancellationToken
        );

        // Make sure the enums are stored as strings:
        (await this.Connection.QueryAsync<String>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsString")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(updatedEntities.Select(a => a.Enum.ToString()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task UpdateEntitiesAsync_MissingKeyProperty_ShouldThrow(Boolean useAsyncApi) =>
        Invoking(() =>
                this.CallApi(
                    useAsyncApi,
                    this.Connection,
                    [new EntityWithoutKeyProperty()],
                    null,
                    TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage(
                $"Could not get the key property / properties of the type {typeof(EntityWithoutKeyProperty)}. Make " +
                $"sure that at least one instance property of that type is denoted with a {typeof(KeyAttribute)}."
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateEntitiesAsync_ShouldHandleEntityWithCompositeKey(Boolean useAsyncApi)
    {
        var entities = this.CreateEntitiesInDb<EntityWithCompositeKey>();
        var updatedEntities = Generate.UpdatesFor(entities);

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            updatedEntities,
            null,
            TestContext.Current.CancellationToken
        );

        (await this.Connection.QueryAsync<EntityWithCompositeKey>(
                $"SELECT * FROM {Q("EntityWithCompositeKey")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(updatedEntities);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateEntitiesAsync_ShouldHandleIdentityAndComputedColumns(Boolean useAsyncApi)
    {
        var entities = this.CreateEntitiesInDb<EntityWithIdentityAndComputedProperties>();
        var updatedEntities = Generate.UpdatesFor(entities);

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            updatedEntities,
            null,
            TestContext.Current.CancellationToken
        );

        updatedEntities
            .Should().BeEquivalentTo(
                await this.Connection.QueryAsync<EntityWithIdentityAndComputedProperties>(
                    $"SELECT * FROM {Q("EntityWithIdentityAndComputedProperties")}"
                ).ToListAsync(TestContext.Current.CancellationToken)
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateEntitiesAsync_ShouldIgnorePropertiesDenotedWithNotMappedAttribute(Boolean useAsyncApi)
    {
        var entities = this.CreateEntitiesInDb<EntityWithNotMappedProperty>();

        var updatedEntities = Generate.UpdatesFor(entities);
        updatedEntities.ForEach(a => a.NotMappedValue = "ShouldNotBePersisted");

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            updatedEntities,
            null,
            TestContext.Current.CancellationToken
        );

        await using var reader = await this.Connection.ExecuteReaderAsync(
            $"SELECT {Q("Id")}, {Q("NotMappedValue")} FROM {Q("EntityWithNotMappedProperty")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        while (await reader.ReadAsync())
        {
            reader.IsDBNull(reader.GetOrdinal("NotMappedValue"))
                .Should().BeTrue();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateEntitiesAsync_ShouldReturnNumberOfAffectedRows(Boolean useAsyncApi)
    {
        var entities = this.CreateEntitiesInDb<Entity>();
        var updatedEntities = Generate.UpdatesFor(entities);

        (await this.CallApi(
                useAsyncApi,
                this.Connection,
                updatedEntities,
                null,
                TestContext.Current.CancellationToken
            ))
            .Should().Be(entities.Count);

        var nonExistentEntities = Generate.Multiple<Entity>();

        (await this.CallApi(
                useAsyncApi,
                this.Connection,
                nonExistentEntities,
                null,
                TestContext.Current.CancellationToken
            ))
            .Should().Be(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateEntitiesAsync_ShouldSupportDateTimeOffsetValues(Boolean useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entities = this.CreateEntitiesInDb<EntityWithDateTimeOffset>();
        var updatedEntities = Generate.UpdatesFor(entities);

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            updatedEntities,
            null,
            TestContext.Current.CancellationToken
        );

        (await this.Connection.QueryAsync<EntityWithDateTimeOffset>(
                $"SELECT * FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(updatedEntities);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateEntitiesAsync_ShouldUpdateEntities(Boolean useAsyncApi)
    {
        var entities = this.CreateEntitiesInDb<Entity>();
        var updatedEntities = Generate.UpdatesFor(entities);

        (await this.CallApi(
                useAsyncApi,
                this.Connection,
                updatedEntities,
                null,
                TestContext.Current.CancellationToken
            ))
            .Should().Be(updatedEntities.Count);

        (await this.Connection.QueryAsync<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(updatedEntities);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateEntitiesAsync_ShouldUseConfiguredColumnNames(Boolean useAsyncApi)
    {
        var entities = this.CreateEntitiesInDb<EntityWithColumnAttributes>();
        var updatedEntities = Generate.UpdatesFor(entities);

        await this.CallApi(
            useAsyncApi,
            this.Connection,
            updatedEntities,
            null,
            TestContext.Current.CancellationToken
        );

        (await this.Connection.QueryAsync<EntityWithColumnAttributes>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync())
            .Should().BeEquivalentTo(updatedEntities);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateEntitiesAsync_Transaction_ShouldUseTransaction(Boolean useAsyncApi)
    {
        var entities = this.CreateEntitiesInDb<Entity>();

        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            var updatedEntities = Generate.UpdatesFor(entities);

            (await this.CallApi(
                    useAsyncApi,
                    this.Connection,
                    updatedEntities,
                    transaction,
                    TestContext.Current.CancellationToken
                ))
                .Should().Be(entities.Count);

            (await this.Connection.QueryAsync<Entity>($"SELECT * FROM {Q("Entity")}", transaction)
                    .ToListAsync(TestContext.Current.CancellationToken))
                .Should().BeEquivalentTo(updatedEntities);

            await transaction.RollbackAsync();
        }

        (await this.Connection.QueryAsync<Entity>($"SELECT * FROM {Q("Entity")}")
                .ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(entities);
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
            return this.manipulator.UpdateEntitiesAsync(connection, entities, transaction, cancellationToken);
        }

        try
        {
            return Task.FromResult(
                this.manipulator.UpdateEntities(connection, entities, transaction, cancellationToken)
            );
        }
        catch (Exception ex)
        {
            return Task.FromException<Int32>(ex);
        }
    }

    private readonly IEntityManipulator manipulator;
}
