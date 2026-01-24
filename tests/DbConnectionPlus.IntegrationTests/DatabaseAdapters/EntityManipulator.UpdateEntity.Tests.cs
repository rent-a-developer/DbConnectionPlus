using RentADeveloper.DbConnectionPlus.DatabaseAdapters;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.DatabaseAdapters;

public sealed class
    EntityManipulator_UpdateEntity_Tests_MySql :
    EntityManipulator_UpdateEntity_Tests<MySqlTestDatabaseProvider>;

public sealed class
    EntityManipulator_UpdateEntity_Tests_Oracle :
    EntityManipulator_UpdateEntity_Tests<OracleTestDatabaseProvider>;

public sealed class
    EntityManipulator_UpdateEntity_Tests_PostgreSql :
    EntityManipulator_UpdateEntity_Tests<PostgreSqlTestDatabaseProvider>;

public sealed class
    EntityManipulator_UpdateEntity_Tests_Sqlite :
    EntityManipulator_UpdateEntity_Tests<SqliteTestDatabaseProvider>;

public sealed class
    EntityManipulator_UpdateEntity_Tests_SqlServer :
    EntityManipulator_UpdateEntity_Tests<SqlServerTestDatabaseProvider>;

public abstract class EntityManipulator_UpdateEntity_Tests
    <TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    /// <inheritdoc />
    protected EntityManipulator_UpdateEntity_Tests() =>
        this.manipulator = this.DatabaseAdapter.EntityManipulator;

    [Fact]
    public void UpdateEntity_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var entity = this.CreateEntityInDb<Entity>();
        var updatedEntity = Generate.UpdateFor(entity);

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        Invoking(() => this.manipulator.UpdateEntity(this.Connection, updatedEntity, null, cancellationToken))
            .Should().Throw<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);

        // Since the operation was cancelled, the entity should not have been updated.
        this.Connection.QueryFirst<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entity);
    }

    [Fact]
    public void UpdateEntity_EntityHasNoKeyProperty_ShouldThrow() =>
        Invoking(() =>
                this.manipulator.UpdateEntity(
                    this.Connection,
                    new EntityWithoutKeyProperty(),
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
    public void UpdateEntity_EntityWithoutTableAttribute_ShouldUseEntityTypeNameAsTableName()
    {
        var entity = this.CreateEntityInDb<Entity>();
        var updatedEntity = Generate.UpdateFor(entity);

        this.manipulator.UpdateEntity(this.Connection, updatedEntity, null, TestContext.Current.CancellationToken);

        this.Connection.QueryFirst<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(updatedEntity);
    }

    [Fact]
    public void UpdateEntity_EntityWithTableAttribute_ShouldUseTableNameFromAttribute()
    {
        var entity = this.CreateEntityInDb<EntityWithTableAttribute>();
        var updatedEntity = Generate.UpdateFor(entity);

        this.manipulator.UpdateEntity(
            this.Connection,
            updatedEntity,
            null,
            TestContext.Current.CancellationToken
        );

        this.Connection.QueryFirst<EntityWithTableAttribute>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(updatedEntity);
    }

    [Fact]
    public void UpdateEntity_EnumSerializationModeIsIntegers_ShouldStoreEnumValuesAsIntegers()
    {
        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Integers;

        var entity = Generate.Single<EntityWithEnumStoredAsInteger>();

        this.manipulator.InsertEntity(this.Connection, entity, null, TestContext.Current.CancellationToken);

        // Make sure the enums are stored as integers:
        this.Connection.QueryFirst<Int32>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsInteger")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be((Int32)entity.Enum);

        var updatedEntity = Generate.UpdateFor(entity);

        this.manipulator.UpdateEntity(
            this.Connection,
            updatedEntity,
            null,
            TestContext.Current.CancellationToken
        );

        // Make sure the enums are stored as integers:
        this.Connection.QueryFirst<Int32>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsInteger")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be((Int32)updatedEntity.Enum);
    }

    [Fact]
    public void UpdateEntity_EnumSerializationModeIsStrings_ShouldStoreEnumValuesAsStrings()
    {
        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Strings;

        var entity = Generate.Single<EntityWithEnumStoredAsString>();

        this.manipulator.InsertEntity(this.Connection, entity, null, TestContext.Current.CancellationToken);

        // Make sure the enums are stored as strings:
        this.Connection.Query<String>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsString")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entity.Enum.ToString());

        var updatedEntity = Generate.UpdateFor(entity);

        this.manipulator.UpdateEntity(
            this.Connection,
            updatedEntity,
            null,
            TestContext.Current.CancellationToken
        );

        // Make sure the enums are stored as strings:
        this.Connection.Query<String>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsString")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(updatedEntity.Enum.ToString());
    }

    [Fact]
    public void UpdateEntity_ShouldHandleComputedColumns()
    {
        var entity = this.CreateEntityInDb<EntityWithComputedProperties>();
        var updatedEntity = Generate.UpdateFor(entity);

        this.manipulator.UpdateEntity(
            this.Connection,
            updatedEntity,
            null,
            TestContext.Current.CancellationToken
        );

        this.Connection.QueryFirst<EntityWithComputedProperties>(
                $"SELECT * FROM {Q("EntityWithComputedProperties")}"
            )
            .Should().BeEquivalentTo(updatedEntity);
    }

    [Fact]
    public void UpdateEntity_ShouldHandleEntityWithCompositeKey()
    {
        var entity = this.CreateEntityInDb<EntityWithCompositeKey>();
        var updatedEntity = Generate.UpdateFor(entity);

        this.manipulator.UpdateEntity(
            this.Connection,
            updatedEntity,
            null,
            TestContext.Current.CancellationToken
        );

        this.Connection.QueryFirst<EntityWithCompositeKey>(
                $"SELECT * FROM {Q("EntityWithCompositeKey")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(updatedEntity);
    }

    [Fact]
    public void UpdateEntity_ShouldHandleIdentityAndComputedColumns()
    {
        var entity = this.CreateEntityInDb<EntityWithIdentityAndComputedProperties>();
        var updatedEntity = Generate.UpdateFor(entity);

        this.manipulator.UpdateEntity(
            this.Connection,
            updatedEntity,
            null,
            TestContext.Current.CancellationToken
        );

        this.Connection.QueryFirst<EntityWithIdentityAndComputedProperties>(
                $"SELECT * FROM {Q("EntityWithIdentityAndComputedProperties")}"
            )
            .Should().BeEquivalentTo(updatedEntity);
    }

    [Fact]
    public void UpdateEntity_ShouldIgnorePropertiesDenotedWithNotMappedAttribute()
    {
        var entity = this.CreateEntityInDb<EntityWithNotMappedProperty>();

        var updatedEntity = Generate.UpdateFor(entity);
        updatedEntity.NotMappedValue = "ShouldNotBePersisted";

        this.manipulator.UpdateEntity(
            this.Connection,
            updatedEntity,
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
    public void UpdateEntity_ShouldReturnNumberOfAffectedRows()
    {
        var entity = this.CreateEntityInDb<Entity>();
        var updatedEntity = Generate.UpdateFor(entity);

        this.manipulator.UpdateEntity(this.Connection, updatedEntity, null, TestContext.Current.CancellationToken)
            .Should().Be(1);

        var nonExistentEntity = Generate.Single<Entity>();

        this.manipulator.UpdateEntity(this.Connection, nonExistentEntity, null, TestContext.Current.CancellationToken)
            .Should().Be(0);
    }

    [Fact]
    public void UpdateEntity_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entity = this.CreateEntityInDb<EntityWithDateTimeOffset>();
        var updatedEntity = Generate.UpdateFor(entity);

        this.manipulator.UpdateEntity(
            this.Connection,
            updatedEntity,
            null,
            TestContext.Current.CancellationToken
        );

        this.Connection.QueryFirst<EntityWithDateTimeOffset>(
                $"SELECT * FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(updatedEntity);
    }

    [Fact]
    public void UpdateEntity_ShouldUpdateEntity()
    {
        var entity = this.CreateEntityInDb<Entity>();
        var updatedEntity = Generate.UpdateFor(entity);

        this.manipulator.UpdateEntity(this.Connection, updatedEntity, null, TestContext.Current.CancellationToken);

        this.Connection.QueryFirst<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(updatedEntity);
    }

    [Fact]
    public void UpdateEntity_Transaction_ShouldUseTransaction()
    {
        var entity = this.CreateEntityInDb<Entity>();

        using (var transaction = this.Connection.BeginTransaction())
        {
            var updatedEntity = Generate.UpdateFor(entity);

            this.manipulator.UpdateEntity(
                    this.Connection,
                    updatedEntity,
                    transaction,
                    TestContext.Current.CancellationToken
                )
                .Should().Be(1);

            this.Connection.QueryFirst<Entity>($"SELECT * FROM {Q("Entity")}", transaction)
                .Should().Be(updatedEntity);

            transaction.Rollback();
        }

        this.Connection.QueryFirst<Entity>($"SELECT * FROM {Q("Entity")}")
            .Should().Be(entity);
    }

    [Fact]
    public async Task UpdateEntityAsync_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var entity = this.CreateEntityInDb<Entity>();
        var updatedEntity = Generate.UpdateFor(entity);

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        await Invoking(() =>
                this.manipulator.UpdateEntityAsync(this.Connection, updatedEntity, null, cancellationToken)
            )
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);

        // Since the operation was cancelled, the entity should not have been updated.
        (await this.Connection.QueryFirstAsync<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entity);
    }

    [Fact]
    public Task UpdateEntityAsync_EntityHasNoKeyProperty_ShouldThrow() =>
        Invoking(() =>
                this.manipulator.UpdateEntityAsync(
                    this.Connection,
                    new EntityWithoutKeyProperty(),
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
    public async Task UpdateEntityAsync_EntityWithoutTableAttribute_ShouldUseEntityTypeNameAsTableName()
    {
        var entity = this.CreateEntityInDb<Entity>();
        var updatedEntity = Generate.UpdateFor(entity);

        await this.manipulator.UpdateEntityAsync(
            this.Connection,
            updatedEntity,
            null,
            TestContext.Current.CancellationToken
        );

        (await this.Connection.QueryFirstAsync<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(updatedEntity);
    }

    [Fact]
    public async Task UpdateEntityAsync_EntityWithTableAttribute_ShouldUseTableNameFromAttribute()
    {
        var entity = this.CreateEntityInDb<EntityWithTableAttribute>();
        var updatedEntity = Generate.UpdateFor(entity);

        await this.manipulator.UpdateEntityAsync(
            this.Connection,
            updatedEntity,
            null,
            TestContext.Current.CancellationToken
        );

        (await this.Connection.QueryFirstAsync<EntityWithTableAttribute>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(updatedEntity);
    }

    [Fact]
    public async Task UpdateEntityAsync_EnumSerializationModeIsIntegers_ShouldStoreEnumValuesAsIntegers()
    {
        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Integers;

        var entity = Generate.Single<EntityWithEnumStoredAsInteger>();

        await this.manipulator.InsertEntityAsync(this.Connection, entity, null, TestContext.Current.CancellationToken);

        // Make sure the enums are stored as integers:
        (await this.Connection.QueryFirstAsync<Int32>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsInteger")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be((Int32)entity.Enum);

        var updatedEntity = Generate.UpdateFor(entity);

        await this.manipulator.UpdateEntityAsync(
            this.Connection,
            updatedEntity,
            null,
            TestContext.Current.CancellationToken
        );

        // Make sure the enums are stored as integers:
        (await this.Connection.QueryFirstAsync<Int32>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsInteger")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be((Int32)updatedEntity.Enum);
    }

    [Fact]
    public async Task UpdateEntityAsync_EnumSerializationModeIsStrings_ShouldStoreEnumValuesAsStrings()
    {
        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Strings;

        var entity = Generate.Single<EntityWithEnumStoredAsString>();

        await this.manipulator.InsertEntityAsync(this.Connection, entity, null, TestContext.Current.CancellationToken);

        // Make sure the enums are stored as strings:
        (await this.Connection.QueryAsync<String>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsString")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(entity.Enum.ToString());

        var updatedEntity = Generate.UpdateFor(entity);

        await this.manipulator.UpdateEntityAsync(
            this.Connection,
            updatedEntity,
            null,
            TestContext.Current.CancellationToken
        );

        // Make sure the enums are stored as strings:
        (await this.Connection.QueryAsync<String>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsString")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(updatedEntity.Enum.ToString());
    }

    [Fact]
    public async Task UpdateEntityAsync_ShouldHandleComputedColumns()
    {
        var entity = this.CreateEntityInDb<EntityWithComputedProperties>();
        var updatedEntity = Generate.UpdateFor(entity);

        await this.manipulator.UpdateEntityAsync(
            this.Connection,
            updatedEntity,
            null,
            TestContext.Current.CancellationToken
        );

        (await this.Connection.QueryFirstAsync<EntityWithComputedProperties>(
                $"SELECT * FROM {Q("EntityWithComputedProperties")}"
            ))
            .Should().BeEquivalentTo(updatedEntity);
    }

    [Fact]
    public async Task UpdateEntityAsync_ShouldHandleEntityWithCompositeKey()
    {
        var entity = this.CreateEntityInDb<EntityWithCompositeKey>();
        var updatedEntity = Generate.UpdateFor(entity);

        await this.manipulator.UpdateEntityAsync(
            this.Connection,
            updatedEntity,
            null,
            TestContext.Current.CancellationToken
        );

        (await this.Connection.QueryFirstAsync<EntityWithCompositeKey>(
                $"SELECT * FROM {Q("EntityWithCompositeKey")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(updatedEntity);
    }

    [Fact]
    public async Task UpdateEntityAsync_ShouldHandleIdentityAndComputedColumns()
    {
        var entity = this.CreateEntityInDb<EntityWithIdentityAndComputedProperties>();
        var updatedEntity = Generate.UpdateFor(entity);

        await this.manipulator.UpdateEntityAsync(
            this.Connection,
            updatedEntity,
            null,
            TestContext.Current.CancellationToken
        );

        (await this.Connection.QueryFirstAsync<EntityWithIdentityAndComputedProperties>(
                $"SELECT * FROM {Q("EntityWithIdentityAndComputedProperties")}"
            ))
            .Should().BeEquivalentTo(updatedEntity);
    }

    [Fact]
    public async Task UpdateEntityAsync_ShouldIgnorePropertiesDenotedWithNotMappedAttribute()
    {
        var entity = this.CreateEntityInDb<EntityWithNotMappedProperty>();

        var updatedEntity = Generate.UpdateFor(entity);
        updatedEntity.NotMappedValue = "ShouldNotBePersisted";

        await this.manipulator.UpdateEntityAsync(
            this.Connection,
            updatedEntity,
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
    public async Task UpdateEntityAsync_ShouldReturnNumberOfAffectedRows()
    {
        var entity = this.CreateEntityInDb<Entity>();
        var updatedEntity = Generate.UpdateFor(entity);

        (await this.manipulator.UpdateEntityAsync(
                this.Connection,
                updatedEntity,
                null,
                TestContext.Current.CancellationToken
            ))
            .Should().Be(1);

        var nonExistentEntity = Generate.Single<Entity>();

        (await this.manipulator.UpdateEntityAsync(
                this.Connection,
                nonExistentEntity,
                null,
                TestContext.Current.CancellationToken
            ))
            .Should().Be(0);
    }

    [Fact]
    public async Task UpdateEntityAsync_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entity = this.CreateEntityInDb<EntityWithDateTimeOffset>();
        var updatedEntity = Generate.UpdateFor(entity);

        await this.manipulator.UpdateEntityAsync(
            this.Connection,
            updatedEntity,
            null,
            TestContext.Current.CancellationToken
        );

        (await this.Connection.QueryFirstAsync<EntityWithDateTimeOffset>(
                $"SELECT * FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(updatedEntity);
    }

    [Fact]
    public async Task UpdateEntityAsync_ShouldUpdateEntity()
    {
        var entity = this.CreateEntityInDb<Entity>();
        var updatedEntity = Generate.UpdateFor(entity);

        (await this.manipulator.UpdateEntityAsync(
                this.Connection,
                updatedEntity,
                null,
                TestContext.Current.CancellationToken
            ))
            .Should().Be(1);

        (await this.Connection.QueryFirstAsync<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(updatedEntity);
    }

    [Fact]
    public async Task UpdateEntityAsync_Transaction_ShouldUseTransaction()
    {
        var entity = this.CreateEntityInDb<Entity>();

        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            var updatedEntity = Generate.UpdateFor(entity);

            (await this.manipulator.UpdateEntityAsync(
                    this.Connection,
                    updatedEntity,
                    transaction,
                    TestContext.Current.CancellationToken
                ))
                .Should().Be(1);

            (await this.Connection.QueryFirstAsync<Entity>($"SELECT * FROM {Q("Entity")}", transaction))
                .Should().Be(updatedEntity);

            await transaction.RollbackAsync();
        }

        (await this.Connection.QueryFirstAsync<Entity>($"SELECT * FROM {Q("Entity")}"))
            .Should().Be(entity);
    }

    private readonly IEntityManipulator manipulator;
}
