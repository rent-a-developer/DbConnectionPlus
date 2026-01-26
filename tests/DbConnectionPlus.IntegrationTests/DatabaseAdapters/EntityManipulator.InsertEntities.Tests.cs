using RentADeveloper.DbConnectionPlus.DatabaseAdapters;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.DatabaseAdapters;

public sealed class
    EntityManipulator_InsertEntities_Tests_MySql :
    EntityManipulator_InsertEntities_Tests<MySqlTestDatabaseProvider>;

public sealed class
    EntityManipulator_InsertEntities_Tests_Oracle :
    EntityManipulator_InsertEntities_Tests<OracleTestDatabaseProvider>;

public sealed class
    EntityManipulator_InsertEntities_Tests_PostgreSql :
    EntityManipulator_InsertEntities_Tests<PostgreSqlTestDatabaseProvider>;

public sealed class
    EntityManipulator_InsertEntities_Tests_Sqlite :
    EntityManipulator_InsertEntities_Tests<SqliteTestDatabaseProvider>;

public sealed class
    EntityManipulator_InsertEntities_Tests_SqlServer :
    EntityManipulator_InsertEntities_Tests<SqlServerTestDatabaseProvider>;

public abstract class EntityManipulator_InsertEntities_Tests
    <TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    /// <inheritdoc />
    protected EntityManipulator_InsertEntities_Tests() =>
        this.manipulator = this.DatabaseAdapter.EntityManipulator;

    [Fact]
    public void InsertEntities_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var entities = Generate.Multiple<Entity>();

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        Invoking(() => this.manipulator.InsertEntities(this.Connection, entities, null, cancellationToken))
            .Should().Throw<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);

        // Since the operation was cancelled, the entities should not have been inserted.
        foreach (var entity in entities)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeFalse();
        }
    }

    [Fact]
    public void InsertEntities_EntitiesWithoutTableAttribute_ShouldUseEntityTypeNameAsTableName()
    {
        var entities = Generate.Multiple<Entity>();

        this.manipulator.InsertEntities(this.Connection, entities, null, TestContext.Current.CancellationToken);

        foreach (var entity in entities)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeTrue();
        }
    }

    [Fact]
    public void InsertEntities_EntitiesWithTableAttribute_ShouldUseTableNameFromAttribute()
    {
        var entities = Generate.Multiple<EntityWithTableAttribute>();

        this.manipulator.InsertEntities(
            this.Connection,
            entities,
            null,
            TestContext.Current.CancellationToken
        );

        foreach (var entity in entities)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeTrue();
        }
    }

    [Fact]
    public void InsertEntities_EnumSerializationModeIsIntegers_ShouldStoreEnumValuesAsIntegers()
    {
        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Integers;

        var entities = Generate.Multiple<EntityWithEnumStoredAsInteger>();

        this.manipulator.InsertEntities(this.Connection, entities, null, TestContext.Current.CancellationToken);

        this.Connection.Query<Int32>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsInteger")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entities.Select(a => (Int32)a.Enum));
    }

    [Fact]
    public void InsertEntities_EnumSerializationModeIsStrings_ShouldStoreEnumValuesAsStrings()
    {
        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Strings;

        var entities = Generate.Multiple<EntityWithEnumStoredAsString>();

        this.manipulator.InsertEntities(this.Connection, entities, null, TestContext.Current.CancellationToken);

        this.Connection.Query<String>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsString")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entities.Select(a => a.Enum.ToString()));
    }

    [Fact]
    public void InsertEntities_ShouldHandleComputedColumns()
    {
        var entities = Generate.Multiple<EntityWithComputedProperties>();

        this.manipulator.InsertEntities(
            this.Connection,
            entities,
            null,
            TestContext.Current.CancellationToken
        );

        entities
            .Should().BeEquivalentTo(
                this.Connection.Query<EntityWithComputedProperties>(
                    $"SELECT * FROM {Q("EntityWithComputedProperties")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            );
    }

    [Fact]
    public void InsertEntities_ShouldHandleIdentityAndComputedColumns()
    {
        var entities = Generate.Multiple<EntityWithIdentityAndComputedProperties>();

        this.manipulator.InsertEntities(
            this.Connection,
            entities,
            null,
            TestContext.Current.CancellationToken
        );

        entities
            .Should().BeEquivalentTo(
                this.Connection.Query<EntityWithIdentityAndComputedProperties>(
                    $"SELECT * FROM {Q("EntityWithIdentityAndComputedProperties")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            );
    }

    [Fact]
    public void InsertEntities_ShouldIgnorePropertiesDenotedWithNotMappedAttribute()
    {
        var entities = Generate.Multiple<EntityWithNotMappedProperty>();
        entities.ForEach(a => a.NotMappedValue = "ShouldNotBePersisted");

        this.manipulator.InsertEntities(
            this.Connection,
            entities,
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
    public void InsertEntities_ShouldInsertEntities()
    {
        var entities = Generate.Multiple<Entity>();

        this.manipulator.InsertEntities(this.Connection, entities, null, TestContext.Current.CancellationToken);

        this.Connection.Query<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entities);
    }

    [Fact]
    public void InsertEntities_ShouldReturnNumberOfAffectedRows()
    {
        var entities = Generate.Multiple<Entity>();

        this.manipulator.InsertEntities(this.Connection, entities, null, TestContext.Current.CancellationToken)
            .Should().Be(entities.Count);

        this.manipulator.InsertEntities(
                this.Connection,
                Array.Empty<Entity>(),
                null,
                TestContext.Current.CancellationToken
            )
            .Should().Be(0);
    }

    [Fact]
    public void InsertEntities_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entities = Generate.Multiple<EntityWithDateTimeOffset>();

        this.manipulator.InsertEntities(this.Connection, entities, null, TestContext.Current.CancellationToken);

        this.Connection.Query<EntityWithDateTimeOffset>(
                $"SELECT * FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entities);
    }

    [Fact]
    public void InsertEntities_Transaction_ShouldUseTransaction()
    {
        var entities = Generate.Multiple<Entity>();

        using (var transaction = this.Connection.BeginTransaction())
        {
            this.manipulator.InsertEntities(
                    this.Connection,
                    entities,
                    transaction,
                    TestContext.Current.CancellationToken
                )
                .Should().Be(entities.Count);

            foreach (var entity in entities)
            {
                this.ExistsEntityInDb(entity, transaction)
                    .Should().BeTrue();
            }

            transaction.Rollback();
        }

        foreach (var entity in entities)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeFalse();
        }
    }

    [Fact]
    public async Task InsertEntitiesAsync_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var entities = Generate.Multiple<Entity>();

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        await Invoking(() =>
                this.manipulator.InsertEntitiesAsync(this.Connection, entities, null, cancellationToken)
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

    [Fact]
    public async Task InsertEntitiesAsync_EntitiesWithoutTableAttribute_ShouldUseEntityTypeNameAsTableName()
    {
        var entities = Generate.Multiple<Entity>();

        await this.manipulator.InsertEntitiesAsync(
            this.Connection,
            entities,
            null,
            TestContext.Current.CancellationToken
        );

        foreach (var entity in entities)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeTrue();
        }
    }

    [Fact]
    public async Task InsertEntitiesAsync_EntitiesWithTableAttribute_ShouldUseTableNameFromAttribute()
    {
        var entities = Generate.Multiple<EntityWithTableAttribute>();

        await this.manipulator.InsertEntitiesAsync(
            this.Connection,
            entities,
            null,
            TestContext.Current.CancellationToken
        );

        foreach (var entity in entities)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeTrue();
        }
    }

    [Fact]
    public async Task InsertEntitiesAsync_EnumSerializationModeIsIntegers_ShouldStoreEnumValuesAsIntegers()
    {
        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Integers;

        var entities = Generate.Multiple<EntityWithEnumStoredAsInteger>();

        await this.manipulator.InsertEntitiesAsync(
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

    [Fact]
    public async Task InsertEntitiesAsync_EnumSerializationModeIsStrings_ShouldStoreEnumValuesAsStrings()
    {
        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Strings;

        var entities = Generate.Multiple<EntityWithEnumStoredAsString>();

        await this.manipulator.InsertEntitiesAsync(
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

    [Fact]
    public async Task InsertEntitiesAsync_ShouldHandleComputedColumns()
    {
        var entities = Generate.Multiple<EntityWithComputedProperties>();

        await this.manipulator.InsertEntitiesAsync(
            this.Connection,
            entities,
            null,
            TestContext.Current.CancellationToken
        );

        entities
            .Should().BeEquivalentTo(
                await this.Connection.QueryAsync<EntityWithComputedProperties>(
                    $"SELECT * FROM {Q("EntityWithComputedProperties")}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToListAsync(TestContext.Current.CancellationToken)
            );
    }

    [Fact]
    public async Task InsertEntitiesAsync_ShouldHandleIdentityAndComputedColumns()
    {
        var entities = Generate.Multiple<EntityWithIdentityAndComputedProperties>();

        await this.manipulator.InsertEntitiesAsync(
            this.Connection,
            entities,
            null,
            TestContext.Current.CancellationToken
        );

        entities
            .Should().BeEquivalentTo(
                await this.Connection.QueryAsync<EntityWithIdentityAndComputedProperties>(
                    $"SELECT * FROM {Q("EntityWithIdentityAndComputedProperties")}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToListAsync(TestContext.Current.CancellationToken)
            );
    }

    [Fact]
    public async Task InsertEntitiesAsync_ShouldIgnorePropertiesDenotedWithNotMappedAttribute()
    {
        var entities = Generate.Multiple<EntityWithNotMappedProperty>();
        entities.ForEach(a => a.NotMappedValue = "ShouldNotBePersisted");

        await this.manipulator.InsertEntitiesAsync(
            this.Connection,
            entities,
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

    [Fact]
    public async Task InsertEntitiesAsync_ShouldInsertEntities()
    {
        var entities = Generate.Multiple<Entity>();

        (await this.manipulator.InsertEntitiesAsync(
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

    [Fact]
    public async Task InsertEntitiesAsync_ShouldReturnNumberOfAffectedRows()
    {
        var entities = Generate.Multiple<Entity>();

        (await this.manipulator.InsertEntitiesAsync(
                this.Connection,
                entities,
                null,
                TestContext.Current.CancellationToken
            ))
            .Should().Be(entities.Count);

        (await this.manipulator.InsertEntitiesAsync(
                this.Connection,
                Array.Empty<Entity>(),
                null,
                TestContext.Current.CancellationToken
            ))
            .Should().Be(0);
    }

    [Fact]
    public async Task InsertEntitiesAsync_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entities = Generate.Multiple<EntityWithDateTimeOffset>();

        await this.manipulator.InsertEntitiesAsync(
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

    [Fact]
    public async Task InsertEntitiesAsync_Transaction_ShouldUseTransaction()
    {
        var entities = Generate.Multiple<Entity>();

        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            (await this.manipulator.InsertEntitiesAsync(
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

    private readonly IEntityManipulator manipulator;
}
