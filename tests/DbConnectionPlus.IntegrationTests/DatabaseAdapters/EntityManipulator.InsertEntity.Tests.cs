using RentADeveloper.DbConnectionPlus.DatabaseAdapters;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.DatabaseAdapters;

public sealed class
    EntityManipulator_InsertEntity_Tests_MySql :
    EntityManipulator_InsertEntity_Tests<MySqlTestDatabaseProvider>;

public sealed class
    EntityManipulator_InsertEntity_Tests_Oracle :
    EntityManipulator_InsertEntity_Tests<OracleTestDatabaseProvider>;

public sealed class
    EntityManipulator_InsertEntity_Tests_PostgreSql :
    EntityManipulator_InsertEntity_Tests<PostgreSqlTestDatabaseProvider>;

public sealed class
    EntityManipulator_InsertEntity_Tests_Sqlite :
    EntityManipulator_InsertEntity_Tests<SqliteTestDatabaseProvider>;

public sealed class
    EntityManipulator_InsertEntity_Tests_SqlServer :
    EntityManipulator_InsertEntity_Tests<SqlServerTestDatabaseProvider>;

public abstract class EntityManipulator_InsertEntity_Tests
    <TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    /// <inheritdoc />
    protected EntityManipulator_InsertEntity_Tests() =>
        this.manipulator = this.DatabaseAdapter.EntityManipulator;

    [Fact]
    public void InsertEntity_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var entity = Generate.Single<Entity>();

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        Invoking(() => this.manipulator.InsertEntity(this.Connection, entity, null, cancellationToken))
            .Should().Throw<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);

        // Since the operation was cancelled, the entity should not have been inserted.
        this.ExistsEntityInDb(entity)
            .Should().BeFalse();
    }

    [Fact]
    public void InsertEntity_EntityWithoutTableAttribute_ShouldUseEntityTypeNameAsTableName()
    {
        var entity = Generate.Single<Entity>();

        this.manipulator.InsertEntity(this.Connection, entity, null, TestContext.Current.CancellationToken);

        this.ExistsEntityInDb(entity)
            .Should().BeTrue();
    }

    [Fact]
    public void InsertEntity_EntityWithTableAttribute_ShouldUseTableNameFromAttribute()
    {
        var entity = Generate.Single<EntityWithTableAttribute>();

        this.manipulator.InsertEntity(
            this.Connection,
            entity,
            null,
            TestContext.Current.CancellationToken
        );

        this.ExistsEntityInDb(entity)
            .Should().BeTrue();
    }

    [Fact]
    public void InsertEntity_EnumSerializationModeIsIntegers_ShouldStoreEnumValuesAsIntegers()
    {
        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Integers;

        var entity = Generate.Single<EntityWithEnumStoredAsInteger>();

        this.manipulator.InsertEntity(this.Connection, entity, null, TestContext.Current.CancellationToken);

        this.Connection.QueryFirst<Int32>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsInteger")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be((Int32)entity.Enum);
    }

    [Fact]
    public void InsertEntity_EnumSerializationModeIsStrings_ShouldStoreEnumValuesAsStrings()
    {
        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Strings;

        var entity = Generate.Single<EntityWithEnumStoredAsString>();

        this.manipulator.InsertEntity(this.Connection, entity, null, TestContext.Current.CancellationToken);

        this.Connection.Query<String>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsString")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entity.Enum.ToString());
    }

    [Fact]
    public void InsertEntity_ShouldHandleComputedColumns()
    {
        var entity = Generate.Single<EntityWithComputedProperties>();

        this.manipulator.InsertEntity(
            this.Connection,
            entity,
            null,
            TestContext.Current.CancellationToken
        );

        this.Connection.QueryFirst<EntityWithComputedProperties>(
                $"SELECT * FROM {Q("EntityWithComputedProperties")}"
            )
            .Should().BeEquivalentTo(entity);
    }

    [Fact]
    public void InsertEntity_ShouldHandleIdentityAndComputedColumns()
    {
        var entity = Generate.Single<EntityWithIdentityAndComputedProperties>();

        this.manipulator.InsertEntity(
            this.Connection,
            entity,
            null,
            TestContext.Current.CancellationToken
        );

        this.Connection.QueryFirst<EntityWithIdentityAndComputedProperties>(
                $"SELECT * FROM {Q("EntityWithIdentityAndComputedProperties")}"
            )
            .Should().BeEquivalentTo(entity);
    }

    [Fact]
    public void InsertEntity_ShouldIgnorePropertiesDenotedWithNotMappedAttribute()
    {
        var entity = Generate.Single<EntityWithNotMappedProperty>();
        entity.NotMappedValue = "ShouldNotBePersisted";

        this.manipulator.InsertEntity(
            this.Connection,
            entity,
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
    public void InsertEntity_ShouldInsertEntity()
    {
        var entity = Generate.Single<Entity>();

        this.manipulator.InsertEntity(this.Connection, entity, null, TestContext.Current.CancellationToken);

        this.Connection.QueryFirst<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entity);
    }

    [Fact]
    public void InsertEntity_ShouldReturnNumberOfAffectedRows()
    {
        var entity = Generate.Single<Entity>();

        this.manipulator.InsertEntity(this.Connection, entity, null, TestContext.Current.CancellationToken)
            .Should().Be(1);
    }

    [Fact]
    public void InsertEntity_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entity = Generate.Single<EntityWithDateTimeOffset>();

        this.manipulator.InsertEntity(this.Connection, entity, null, TestContext.Current.CancellationToken);

        this.Connection.QueryFirst<EntityWithDateTimeOffset>(
                $"SELECT * FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entity);
    }

    [Fact]
    public void InsertEntity_Transaction_ShouldUseTransaction()
    {
        var entity = Generate.Single<Entity>();

        using (var transaction = this.Connection.BeginTransaction())
        {
            this.manipulator.InsertEntity(
                    this.Connection,
                    entity,
                    transaction,
                    TestContext.Current.CancellationToken
                )
                .Should().Be(1);

            this.ExistsEntityInDb(entity, transaction)
                .Should().BeTrue();

            transaction.Rollback();
        }

        this.ExistsEntityInDb(entity)
            .Should().BeFalse();
    }

    [Fact]
    public async Task InsertEntityAsync_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var entity = Generate.Single<Entity>();

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        await Invoking(() =>
                this.manipulator.InsertEntityAsync(this.Connection, entity, null, cancellationToken)
            )
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);

        // Since the operation was cancelled, the entity should not have been inserted.
        this.ExistsEntityInDb(entity)
            .Should().BeFalse();
    }

    [Fact]
    public async Task InsertEntityAsync_EntityWithoutTableAttribute_ShouldUseEntityTypeNameAsTableName()
    {
        var entity = Generate.Single<Entity>();

        await this.manipulator.InsertEntityAsync(
            this.Connection,
            entity,
            null,
            TestContext.Current.CancellationToken
        );

        this.ExistsEntityInDb(entity)
            .Should().BeTrue();
    }

    [Fact]
    public async Task InsertEntityAsync_EntityWithTableAttribute_ShouldUseTableNameFromAttribute()
    {
        var entity = Generate.Single<EntityWithTableAttribute>();

        await this.manipulator.InsertEntityAsync(
            this.Connection,
            entity,
            null,
            TestContext.Current.CancellationToken
        );

        this.ExistsEntityInDb(entity)
            .Should().BeTrue();
    }

    [Fact]
    public async Task InsertEntityAsync_EnumSerializationModeIsIntegers_ShouldStoreEnumValuesAsIntegers()
    {
        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Integers;

        var entity = Generate.Single<EntityWithEnumStoredAsInteger>();

        await this.manipulator.InsertEntityAsync(this.Connection, entity, null, TestContext.Current.CancellationToken);

        (await this.Connection.QueryFirstAsync<Int32>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsInteger")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be((Int32)entity.Enum);
    }

    [Fact]
    public async Task InsertEntityAsync_EnumSerializationModeIsStrings_ShouldStoreEnumValuesAsStrings()
    {
        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Strings;

        var entity = Generate.Single<EntityWithEnumStoredAsString>();

        await this.manipulator.InsertEntityAsync(this.Connection, entity, null, TestContext.Current.CancellationToken);

        (await this.Connection.QueryAsync<String>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsString")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(entity.Enum.ToString());
    }

    [Fact]
    public async Task InsertEntityAsync_ShouldHandleComputedColumns()
    {
        var entity = Generate.Single<EntityWithComputedProperties>();

        await this.manipulator.InsertEntityAsync(
            this.Connection,
            entity,
            null,
            TestContext.Current.CancellationToken
        );

        (await this.Connection.QueryFirstAsync<EntityWithComputedProperties>(
                $"SELECT * FROM {Q("EntityWithComputedProperties")}"
            ))
            .Should().BeEquivalentTo(entity);
    }

    [Fact]
    public async Task InsertEntityAsync_ShouldHandleIdentityAndComputedColumns()
    {
        var entity = Generate.Single<EntityWithIdentityAndComputedProperties>();

        await this.manipulator.InsertEntityAsync(
            this.Connection,
            entity,
            null,
            TestContext.Current.CancellationToken
        );

        (await this.Connection.QueryFirstAsync<EntityWithIdentityAndComputedProperties>(
                $"SELECT * FROM {Q("EntityWithIdentityAndComputedProperties")}"
            )).Should()
            .BeEquivalentTo(entity);
    }

    [Fact]
    public async Task InsertEntityAsync_ShouldIgnorePropertiesDenotedWithNotMappedAttribute()
    {
        var entity = Generate.Single<EntityWithNotMappedProperty>();
        entity.NotMappedValue = "ShouldNotBePersisted";

        await this.manipulator.InsertEntityAsync(
            this.Connection,
            entity,
            null,
            TestContext.Current.CancellationToken
        );

        using var reader = await this.Connection.ExecuteReaderAsync(
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
    public async Task InsertEntityAsync_ShouldInsertEntity()
    {
        var entity = Generate.Single<Entity>();

        (await this.manipulator.InsertEntityAsync(this.Connection, entity, null, TestContext.Current.CancellationToken))
            .Should().Be(1);

        (await this.Connection.QueryFirstAsync<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entity);
    }

    [Fact]
    public async Task InsertEntityAsync_ShouldReturnNumberOfAffectedRows()
    {
        var entity = Generate.Single<Entity>();

        (await this.manipulator.InsertEntityAsync(this.Connection, entity, null, TestContext.Current.CancellationToken))
            .Should().Be(1);
    }

    [Fact]
    public async Task InsertEntityAsync_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entity = Generate.Single<EntityWithDateTimeOffset>();

        await this.manipulator.InsertEntityAsync(this.Connection, entity, null, TestContext.Current.CancellationToken);

        (await this.Connection.QueryFirstAsync<EntityWithDateTimeOffset>(
                $"SELECT * FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entity);
    }

    [Fact]
    public async Task InsertEntityAsync_Transaction_ShouldUseTransaction()
    {
        var entity = Generate.Single<Entity>();

        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            (await this.manipulator.InsertEntityAsync(
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

    private readonly IEntityManipulator manipulator;
}
