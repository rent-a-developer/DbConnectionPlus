namespace RentADeveloper.DbConnectionPlus.IntegrationTests;

public sealed class
    DbConnectionExtensions_TemporaryTableTests_MySql :
    DbConnectionExtensions_TemporaryTableTests<MySqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_TemporaryTableTests_PostgreSql :
    DbConnectionExtensions_TemporaryTableTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_TemporaryTableTests_Sqlite :
    DbConnectionExtensions_TemporaryTableTests<SqliteTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_TemporaryTableTests_SqlServer :
    DbConnectionExtensions_TemporaryTableTests<SqlServerTestDatabaseProvider>;

public abstract class
    DbConnectionExtensions_TemporaryTableTests<TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    [Fact]
    public void
        TemporaryTable_ComplexObjects_EnumProperty_EnumSerializationModeIsIntegers_ShouldSerializeEnumToInteger()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Integers;

        var entities = Generate.Multiple<EntityWithEnumStoredAsInteger>();

        this.Connection.Query<Int32>(
                $"SELECT {Q("Enum")} FROM {TemporaryTable(entities)}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entities.Select(a => (Int32)a.Enum));
    }

    [Fact]
    public void TemporaryTable_ComplexObjects_EnumProperty_EnumSerializationModeIsStrings_ShouldSerializeEnumToString()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        var entities = Generate.Multiple<EntityWithEnumStoredAsString>();

        this.Connection.Query<String>(
                $"SELECT {Q("Enum")} FROM {TemporaryTable(entities)}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entities.Select(a => a.Enum.ToString()));
    }

    [Fact]
    public void TemporaryTable_ComplexObjects_ShouldBePassedAsMultiColumnTemporaryTableToSqlStatement()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>();

        this.Connection.Query<Entity>(
                $"SELECT * FROM {TemporaryTable(entities)}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entities);
    }

    [Fact]
    public void TemporaryTable_ScalarValues_Enums_EnumSerializationModeIsIntegers_ShouldSerializeEnumToInteger()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Integers;

        var enumValues = Generate.Multiple<TestEnum>();

        this.Connection
            .Query<Int32>(
                $"SELECT {Q("Value")} FROM {TemporaryTable(enumValues)}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(enumValues.Select(a => (Int32)a));
    }

    [Fact]
    public void TemporaryTable_ScalarValues_Enums_EnumSerializationModeIsStrings_ShouldSerializeEnumToString()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        var enumValues = Generate.Multiple<TestEnum>();

        this.Connection
            .Query<String>(
                $"SELECT {Q("Value")} FROM {TemporaryTable(enumValues)}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(enumValues.Select(a => a.ToString()));
    }

    [Fact]
    public void TemporaryTable_ScalarValues_ShouldBePassedAsSingleColumnTemporaryTableToSqlStatement()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids();

        this.Connection
            .Query<Int32>(
                $"SELECT {Q("Value")} FROM {TemporaryTable(entityIds)}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entityIds);
    }

    [Fact]
    public async Task
        TemporaryTableAsync_ComplexObjects_EnumProperty_EnumSerializationModeIsIntegers_ShouldSerializeEnumToInteger()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Integers;

        var entities = Generate.Multiple<EntityWithEnumStoredAsInteger>();

        (await this.Connection.QueryAsync<Int32>(
                $"SELECT {Q("Enum")} FROM {TemporaryTable(entities)}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(entities.Select(a => (Int32)a.Enum));
    }

    [Fact]
    public async Task
        TemporaryTableAsync_ComplexObjects_EnumProperty_EnumSerializationModeIsStrings_ShouldSerializeEnumToString()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        var entities = Generate.Multiple<EntityWithEnumStoredAsString>();

        (await this.Connection.QueryAsync<String>(
                $"SELECT {Q("Enum")} FROM {TemporaryTable(entities)}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(entities.Select(a => a.Enum.ToString()));
    }

    [Fact]
    public async Task TemporaryTableAsync_ComplexObjects_ShouldBePassedAsMultiColumnTemporaryTableToSqlStatement()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>();

        (await this.Connection.QueryAsync<Entity>(
                $"SELECT * FROM {TemporaryTable(entities)}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(entities);
    }

    [Fact]
    public async Task
        TemporaryTableAsync_ScalarValues_Enums_EnumSerializationModeIsIntegers_ShouldSerializeEnumToInteger()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Integers;

        var enumValues = Generate.Multiple<TestEnum>();

        (await this.Connection
                .QueryAsync<Int32>(
                    $"SELECT {Q("Value")} FROM {TemporaryTable(enumValues)}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(enumValues.Select(a => (Int32)a));
    }

    [Fact]
    public async Task
        TemporaryTableAsync_ScalarValues_Enums_EnumSerializationModeIsStrings_ShouldSerializeEnumToString()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        var enumValues = Generate.Multiple<TestEnum>();

        (await this.Connection
                .QueryAsync<String>(
                    $"SELECT {Q("Value")} FROM {TemporaryTable(enumValues)}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(enumValues.Select(a => a.ToString()));
    }

    [Fact]
    public async Task TemporaryTableAsync_ScalarValues_ShouldBePassedAsSingleColumnTemporaryTableToSqlStatement()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids();

        (await this.Connection
                .QueryAsync<Int32>(
                    $"SELECT {Q("Value")} FROM {TemporaryTable(entityIds)}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(entityIds);
    }
}
