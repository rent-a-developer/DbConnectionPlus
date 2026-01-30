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
        this.Connection.QuerySingle<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entity);
    }

    [Fact]
    public void UpdateEntity_MissingKeyProperty_ShouldThrow() =>
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

        this.Connection.QuerySingle<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(updatedEntity);
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

        this.Connection.QuerySingle<EntityWithTableAttribute>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(updatedEntity);
    }

    [Fact]
    public void UpdateEntity_EnumSerializationModeIsIntegers_ShouldStoreEnumValuesAsIntegers()
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Integers;

        var entity = Generate.Single<EntityWithEnumStoredAsInteger>();

        this.manipulator.InsertEntity(this.Connection, entity, null, TestContext.Current.CancellationToken);

        // Make sure the enum is stored as integer:
        this.Connection.QuerySingle<Int32>(
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

        // Make sure the enum is stored as integer:
        this.Connection.QuerySingle<Int32>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsInteger")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be((Int32)updatedEntity.Enum);
    }

    [Fact]
    public void UpdateEntity_EnumSerializationModeIsStrings_ShouldStoreEnumValuesAsStrings()
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        var entity = Generate.Single<EntityWithEnumStoredAsString>();

        this.manipulator.InsertEntity(this.Connection, entity, null, TestContext.Current.CancellationToken);

        // Make sure the enum is stored as string:
        this.Connection.QuerySingle<String>(
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

        // Make sure the enum is stored as string:
        this.Connection.QuerySingle<String>(
                $"SELECT {Q("Enum")} FROM {Q("EntityWithEnumStoredAsString")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(updatedEntity.Enum.ToString());
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

        this.Connection.QuerySingle<EntityWithCompositeKey>(
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

        updatedEntity
            .Should().BeEquivalentTo(
                this.Connection.QuerySingle<EntityWithIdentityAndComputedProperties>(
                    $"SELECT * FROM {Q("EntityWithIdentityAndComputedProperties")}"
                )
            );
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

        this.Connection.QuerySingle<EntityWithDateTimeOffset>(
                $"SELECT * FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(updatedEntity);
    }

    [Fact]
    public void UpdateEntity_ShouldUpdateEntity()
    {
        var entity = this.CreateEntityInDb<Entity>();
        var updatedEntity = Generate.UpdateFor(entity);

        this.manipulator.UpdateEntity(this.Connection, updatedEntity, null, TestContext.Current.CancellationToken);

        this.Connection.QuerySingle<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(updatedEntity);
    }

    [Fact]
    public void UpdateEntity_ShouldUseConfiguredColumnNames()
    {
        var entity = this.CreateEntityInDb<EntityWithColumnAttributes>();
        var updatedEntity = Generate.UpdateFor(entity);

        this.manipulator.UpdateEntity(this.Connection, updatedEntity, null, TestContext.Current.CancellationToken);

        this.Connection.QuerySingle<EntityWithColumnAttributes>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(updatedEntity);
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

            this.Connection.QuerySingle<Entity>($"SELECT * FROM {Q("Entity")}", transaction)
                .Should().BeEquivalentTo(updatedEntity);

            transaction.Rollback();
        }

        this.Connection.QuerySingle<Entity>($"SELECT * FROM {Q("Entity")}")
            .Should().BeEquivalentTo(entity);
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
        (await this.Connection.QuerySingleAsync<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entity);
    }

    [Fact]
    public Task UpdateEntityAsync_MissingKeyProperty_ShouldThrow() =>
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

        (await this.Connection.QuerySingleAsync<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(updatedEntity);
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

        (await this.Connection.QuerySingleAsync<EntityWithTableAttribute>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(updatedEntity);
    }

    [Fact]
    public async Task UpdateEntityAsync_EnumSerializationModeIsIntegers_ShouldStoreEnumValuesAsIntegers()
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

        await this.manipulator.UpdateEntityAsync(
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

    [Fact]
    public async Task UpdateEntityAsync_EnumSerializationModeIsStrings_ShouldStoreEnumValuesAsStrings()
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

        await this.manipulator.UpdateEntityAsync(
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

        (await this.Connection.QuerySingleAsync<EntityWithCompositeKey>(
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

        updatedEntity
            .Should().BeEquivalentTo(
                await this.Connection.QuerySingleAsync<EntityWithIdentityAndComputedProperties>(
                    $"SELECT * FROM {Q("EntityWithIdentityAndComputedProperties")}"
                )
            );
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

        (await this.Connection.QuerySingleAsync<EntityWithDateTimeOffset>(
                $"SELECT * FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(updatedEntity);
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

        (await this.Connection.QuerySingleAsync<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(updatedEntity);
    }

    [Fact]
    public async Task UpdateEntityAsync_ShouldUseConfiguredColumnNames()
    {
        var entity = this.CreateEntityInDb<EntityWithColumnAttributes>();
        var updatedEntity = Generate.UpdateFor(entity);

        await this.manipulator.UpdateEntityAsync(
            this.Connection,
            updatedEntity,
            null,
            TestContext.Current.CancellationToken
        );

        (await this.Connection.QuerySingleAsync<EntityWithColumnAttributes>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(updatedEntity);
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

            (await this.Connection.QuerySingleAsync<Entity>($"SELECT * FROM {Q("Entity")}", transaction))
                .Should().BeEquivalentTo(updatedEntity);

            await transaction.RollbackAsync();
        }

        (await this.Connection.QuerySingleAsync<Entity>($"SELECT * FROM {Q("Entity")}"))
            .Should().BeEquivalentTo(entity);
    }

    private readonly IEntityManipulator manipulator;
}
