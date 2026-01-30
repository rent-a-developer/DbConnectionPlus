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

    [Fact]
    public void UpdateEntities_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var entities = this.CreateEntitiesInDb<Entity>();
        var updatedEntities = Generate.UpdatesFor(entities);

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        Invoking(() => this.manipulator.UpdateEntities(this.Connection, updatedEntities, null, cancellationToken))
            .Should().Throw<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);

        // Since the operation was cancelled, the entities should not have been updated.
        this.Connection.Query<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entities);
    }

    [Fact]
    public void UpdateEntities_MissingKeyProperty_ShouldThrow() =>
        Invoking(() =>
                this.manipulator.UpdateEntities(
                    this.Connection,
                    [new EntityWithoutKeyProperty()],
                    null,
                    TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<ArgumentException>()
            .WithMessage(
                $"Could not get the key property / properties of the type {typeof(EntityWithoutKeyProperty)}. Make " +
                $"sure that at least one instance property of that type is denoted with a {typeof(KeyAttribute)}."
            );

    [Fact]
    public void UpdateEntities_EntitiesWithoutTableAttribute_ShouldUseEntityTypeNameAsTableName()
    {
        var entities = this.CreateEntitiesInDb<Entity>();
        var updatedEntities = Generate.UpdatesFor(entities);

        this.manipulator.UpdateEntities(this.Connection, updatedEntities, null, TestContext.Current.CancellationToken);

        this.Connection.Query<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(updatedEntities);
    }

    [Fact]
    public void UpdateEntities_EntitiesWithTableAttribute_ShouldUseTableNameFromAttribute()
    {
        var entities = this.CreateEntitiesInDb<EntityWithTableAttribute>();
        var updatedEntities = Generate.UpdatesFor(entities);

        this.manipulator.UpdateEntities(
            this.Connection,
            updatedEntities,
            null,
            TestContext.Current.CancellationToken
        );

        this.Connection.Query<EntityWithTableAttribute>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(updatedEntities);
    }

    [Fact]
    public void UpdateEntities_EnumSerializationModeIsIntegers_ShouldStoreEnumValuesAsIntegers()
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Integers;

        var entities = Generate.Multiple<EntityWithEnumStoredAsInteger>();

        this.manipulator.InsertEntities(this.Connection, entities, null, TestContext.Current.CancellationToken);

        // Make sure the enums are stored as integers:
        this.Connection.Query<Int32>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsInteger")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entities.Select(a => (Int32)a.Enum));

        var updatedEntities = Generate.UpdatesFor(entities);

        this.manipulator.UpdateEntities(
            this.Connection,
            updatedEntities,
            null,
            TestContext.Current.CancellationToken
        );

        // Make sure the enums are stored as integers:
        this.Connection.Query<Int32>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsInteger")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(updatedEntities.Select(a => (Int32)a.Enum));
    }

    [Fact]
    public void UpdateEntities_EnumSerializationModeIsStrings_ShouldStoreEnumValuesAsStrings()
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        var entities = Generate.Multiple<EntityWithEnumStoredAsString>();

        this.manipulator.InsertEntities(this.Connection, entities, null, TestContext.Current.CancellationToken);

        // Make sure the enums are stored as strings:
        this.Connection.Query<String>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsString")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entities.Select(a => a.Enum.ToString()));

        var updatedEntities = Generate.UpdatesFor(entities);

        this.manipulator.UpdateEntities(
            this.Connection,
            updatedEntities,
            null,
            TestContext.Current.CancellationToken
        );

        // Make sure the enums are stored as strings:
        this.Connection.Query<String>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsString")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(updatedEntities.Select(a => a.Enum.ToString()));
    }

    [Fact]
    public void UpdateEntities_ShouldHandleEntityWithCompositeKey()
    {
        var entities = this.CreateEntitiesInDb<EntityWithCompositeKey>();
        var updatedEntities = Generate.UpdatesFor(entities);

        this.manipulator.UpdateEntities(
            this.Connection,
            updatedEntities,
            null,
            TestContext.Current.CancellationToken
        );

        this.Connection.Query<EntityWithCompositeKey>(
                $"SELECT * FROM {Q("EntityWithCompositeKey")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(updatedEntities);
    }

    [Fact]
    public void UpdateEntities_ShouldHandleIdentityAndComputedColumns()
    {
        var entities = this.CreateEntitiesInDb<EntityWithIdentityAndComputedProperties>();
        var updatedEntities = Generate.UpdatesFor(entities);

        this.manipulator.UpdateEntities(
            this.Connection,
            updatedEntities,
            null,
            TestContext.Current.CancellationToken
        );

        updatedEntities
            .Should().BeEquivalentTo(
                this.Connection.Query<EntityWithIdentityAndComputedProperties>(
                    $"SELECT * FROM {Q("EntityWithIdentityAndComputedProperties")}"
                )
            );
    }

    [Fact]
    public void UpdateEntities_ShouldIgnorePropertiesDenotedWithNotMappedAttribute()
    {
        var entities = this.CreateEntitiesInDb<EntityWithNotMappedProperty>();

        var updatedEntities = Generate.UpdatesFor(entities);
        updatedEntities.ForEach(a => a.NotMappedValue = "ShouldNotBePersisted");

        this.manipulator.UpdateEntities(
            this.Connection,
            updatedEntities,
            null,
            TestContext.Current.CancellationToken
        );

        using var reader = this.Connection.ExecuteReader(
            $"SELECT {Q("Id")}, {Q("NotMappedValue")} FROM {Q("EntityWithNotMappedProperty")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        while (reader.Read())
        {
            reader.IsDBNull(reader.GetOrdinal("NotMappedValue"))
                .Should().BeTrue();
        }
    }

    [Fact]
    public void UpdateEntities_ShouldReturnNumberOfAffectedRows()
    {
        var entities = this.CreateEntitiesInDb<Entity>();
        var updatedEntities = Generate.UpdatesFor(entities);

        this.manipulator.UpdateEntities(this.Connection, updatedEntities, null, TestContext.Current.CancellationToken)
            .Should().Be(entities.Count);

        var nonExistentEntities = Generate.Multiple<Entity>();

        this.manipulator.UpdateEntities(
                this.Connection,
                nonExistentEntities,
                null,
                TestContext.Current.CancellationToken
            )
            .Should().Be(0);
    }

    [Fact]
    public void UpdateEntities_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entities = this.CreateEntitiesInDb<EntityWithDateTimeOffset>();
        var updatedEntities = Generate.UpdatesFor(entities);

        this.manipulator.UpdateEntities(
            this.Connection,
            updatedEntities,
            null,
            TestContext.Current.CancellationToken
        );

        this.Connection.Query<EntityWithDateTimeOffset>(
                $"SELECT * FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(updatedEntities);
    }

    [Fact]
    public void UpdateEntities_ShouldUpdateEntities()
    {
        var entities = this.CreateEntitiesInDb<Entity>();
        var updatedEntities = Generate.UpdatesFor(entities);

        this.manipulator.UpdateEntities(this.Connection, updatedEntities, null, TestContext.Current.CancellationToken);

        this.Connection.Query<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(updatedEntities);
    }

    [Fact]
    public void UpdateEntities_ShouldUseConfiguredColumnNames()
    {
        var entities = this.CreateEntitiesInDb<EntityWithColumnAttributes>();
        var updatedEntities = Generate.UpdatesFor(entities);

        this.manipulator.UpdateEntities(this.Connection, updatedEntities, null, TestContext.Current.CancellationToken);

        this.Connection.Query<EntityWithColumnAttributes>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(updatedEntities);
    }

    [Fact]
    public void UpdateEntities_Transaction_ShouldUseTransaction()
    {
        var entities = this.CreateEntitiesInDb<Entity>();

        using (var transaction = this.Connection.BeginTransaction())
        {
            var updatedEntities = Generate.UpdatesFor(entities);

            this.manipulator.UpdateEntities(
                    this.Connection,
                    updatedEntities,
                    transaction,
                    TestContext.Current.CancellationToken
                )
                .Should().Be(entities.Count);

            this.Connection.Query<Entity>($"SELECT * FROM {Q("Entity")}", transaction)
                .Should().BeEquivalentTo(updatedEntities);

            transaction.Rollback();
        }

        this.Connection.Query<Entity>($"SELECT * FROM {Q("Entity")}")
            .Should().BeEquivalentTo(entities);
    }

    [Fact]
    public async Task UpdateEntitiesAsync_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var entities = this.CreateEntitiesInDb<Entity>();
        var updatedEntities = Generate.UpdatesFor(entities);

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        await Invoking(() =>
                this.manipulator.UpdateEntitiesAsync(this.Connection, updatedEntities, null, cancellationToken)
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

    [Fact]
    public Task UpdateEntitiesAsync_MissingKeyProperty_ShouldThrow() =>
        Invoking(() =>
                this.manipulator.UpdateEntitiesAsync(
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

    [Fact]
    public async Task UpdateEntitiesAsync_EntitiesWithoutTableAttribute_ShouldUseEntityTypeNameAsTableName()
    {
        var entities = this.CreateEntitiesInDb<Entity>();
        var updatedEntities = Generate.UpdatesFor(entities);

        await this.manipulator.UpdateEntitiesAsync(
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

    [Fact]
    public async Task UpdateEntitiesAsync_EntitiesWithTableAttribute_ShouldUseTableNameFromAttribute()
    {
        var entities = this.CreateEntitiesInDb<EntityWithTableAttribute>();
        var updatedEntities = Generate.UpdatesFor(entities);

        await this.manipulator.UpdateEntitiesAsync(
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

    [Fact]
    public async Task UpdateEntitiesAsync_EnumSerializationModeIsIntegers_ShouldStoreEnumValuesAsIntegers()
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

        await this.manipulator.UpdateEntitiesAsync(
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

    [Fact]
    public async Task UpdateEntitiesAsync_EnumSerializationModeIsStrings_ShouldStoreEnumValuesAsStrings()
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

        await this.manipulator.UpdateEntitiesAsync(
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

    [Fact]
    public async Task UpdateEntitiesAsync_ShouldHandleEntityWithCompositeKey()
    {
        var entities = this.CreateEntitiesInDb<EntityWithCompositeKey>();
        var updatedEntities = Generate.UpdatesFor(entities);

        await this.manipulator.UpdateEntitiesAsync(
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

    [Fact]
    public async Task UpdateEntitiesAsync_ShouldHandleIdentityAndComputedColumns()
    {
        var entities = this.CreateEntitiesInDb<EntityWithIdentityAndComputedProperties>();
        var updatedEntities = Generate.UpdatesFor(entities);

        await this.manipulator.UpdateEntitiesAsync(
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

    [Fact]
    public async Task UpdateEntitiesAsync_ShouldIgnorePropertiesDenotedWithNotMappedAttribute()
    {
        var entities = this.CreateEntitiesInDb<EntityWithNotMappedProperty>();

        var updatedEntities = Generate.UpdatesFor(entities);
        updatedEntities.ForEach(a => a.NotMappedValue = "ShouldNotBePersisted");

        await this.manipulator.UpdateEntitiesAsync(
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

    [Fact]
    public async Task UpdateEntitiesAsync_ShouldReturnNumberOfAffectedRows()
    {
        var entities = this.CreateEntitiesInDb<Entity>();
        var updatedEntities = Generate.UpdatesFor(entities);

        (await this.manipulator.UpdateEntitiesAsync(
                this.Connection,
                updatedEntities,
                null,
                TestContext.Current.CancellationToken
            ))
            .Should().Be(entities.Count);

        var nonExistentEntities = Generate.Multiple<Entity>();

        (await this.manipulator.UpdateEntitiesAsync(
                this.Connection,
                nonExistentEntities,
                null,
                TestContext.Current.CancellationToken
            ))
            .Should().Be(0);
    }

    [Fact]
    public async Task UpdateEntitiesAsync_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entities = this.CreateEntitiesInDb<EntityWithDateTimeOffset>();
        var updatedEntities = Generate.UpdatesFor(entities);

        await this.manipulator.UpdateEntitiesAsync(
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

    [Fact]
    public async Task UpdateEntitiesAsync_ShouldUpdateEntities()
    {
        var entities = this.CreateEntitiesInDb<Entity>();
        var updatedEntities = Generate.UpdatesFor(entities);

        (await this.manipulator.UpdateEntitiesAsync(
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

    [Fact]
    public async Task UpdateEntitiesAsync_ShouldUseConfiguredColumnNames()
    {
        var entities = this.CreateEntitiesInDb<EntityWithColumnAttributes>();
        var updatedEntities = Generate.UpdatesFor(entities);

        await this.manipulator.UpdateEntitiesAsync(
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

    [Fact]
    public async Task UpdateEntitiesAsync_Transaction_ShouldUseTransaction()
    {
        var entities = this.CreateEntitiesInDb<Entity>();

        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            var updatedEntities = Generate.UpdatesFor(entities);

            (await this.manipulator.UpdateEntitiesAsync(
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

    private readonly IEntityManipulator manipulator;
}
