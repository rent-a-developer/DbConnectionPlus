using RentADeveloper.DbConnectionPlus.DatabaseAdapters;
using RentADeveloper.DbConnectionPlus.Extensions;
using RentADeveloper.DbConnectionPlus.UnitTests.Assertions;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.DatabaseAdapters;

public sealed class
    TemporaryTableBuilderTests_MySql :
    TemporaryTableBuilderTests<MySqlTestDatabaseProvider>;

public sealed class
    TemporaryTableBuilderTests_Oracle :
    TemporaryTableBuilderTests<OracleTestDatabaseProvider>;

public sealed class
    TemporaryTableBuilderTests_PostgreSql :
    TemporaryTableBuilderTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    TemporaryTableBuilderTests_Sqlite :
    TemporaryTableBuilderTests<SqliteTestDatabaseProvider>;

public sealed class
    TemporaryTableBuilderTests_SqlServer :
    TemporaryTableBuilderTests<SqlServerTestDatabaseProvider>;

public abstract class TemporaryTableBuilderTests<TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    /// <inheritdoc />
    protected TemporaryTableBuilderTests() =>
        this.builder = this.DatabaseAdapter.TemporaryTableBuilder;

    [Fact]
    public void BuildTemporaryTable_ComplexObjects_DateTimeOffsetProperty_ShouldSupportDateTimeOffset()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var items = Generate.Multiple<TemporaryTableTestItemWithDateTimeOffset>();

        using var tableDisposer = this.builder.BuildTemporaryTable(
            this.Connection,
            null,
            "Objects",
            items,
            typeof(TemporaryTableTestItemWithDateTimeOffset),
            TestContext.Current.CancellationToken
        );

        this.Connection.Query<TemporaryTableTestItemWithDateTimeOffset>(
                $"SELECT * FROM {QT("Objects")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(items);
    }

    [Fact]
    public void BuildTemporaryTable_ComplexObjects_EnumSerializationModeIsIntegers_ShouldStoreEnumValuesAsIntegers()
    {
        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Integers;

        var entities = Generate.Multiple<EntityWithEnumProperty>();

        using var tableDisposer = this.builder.BuildTemporaryTable(
            this.Connection,
            null,
            "Objects",
            entities,
            typeof(EntityWithEnumProperty),
            TestContext.Current.CancellationToken
        );

        if (this.TestDatabaseProvider.CanRetrieveStructureOfTemporaryTables)
        {
            this.GetDataTypeOfTemporaryTableColumn("Objects", "Enum")
                .Should().Be(this.DatabaseAdapter.GetDataType(typeof(TestEnum), EnumSerializationMode.Integers));
        }

        using var reader = this.Connection.ExecuteReader(
            $"SELECT {Q("Enum")} FROM {QT("Objects")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        reader.GetFieldType(0)
            .Should().BeAnyOf(typeof(Int32), typeof(Int64));

        foreach (var entity in entities)
        {
            reader.Read();
            
            reader.GetInt32(0)
                .Should().Be((Int32)entity.Enum);
        }
    }

    [Fact]
    public void BuildTemporaryTable_ComplexObjects_EnumSerializationModeIsStrings_ShouldStoreEnumValuesAsStrings()
    {
        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Strings;

        var entities = Generate.Multiple<EntityWithEnumProperty>();

        using var tableDisposer = this.builder.BuildTemporaryTable(
            this.Connection,
            null,
            "Objects",
            entities,
            typeof(EntityWithEnumProperty),
            TestContext.Current.CancellationToken
        );

        if (this.TestDatabaseProvider.CanRetrieveStructureOfTemporaryTables)
        {
            this.DatabaseAdapter.GetDataType(typeof(TestEnum), EnumSerializationMode.Strings)
                .Should().StartWith(this.GetDataTypeOfTemporaryTableColumn("Objects", "Enum"));
        }

        using var reader = this.Connection.ExecuteReader(
            $"SELECT {Q("Enum")} FROM {QT("Objects")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        reader.GetFieldType(0)
            .Should().Be(typeof(String));

        foreach (var entity in entities)
        {
            reader.Read();
            
            reader.GetString(0)
                .Should().Be(entity.Enum.ToString());
        }
    }

    [Fact]
    public void
        BuildTemporaryTable_ComplexObjects_EnumSerializationModeIsStrings_ShouldUseCollationOfDatabaseForEnumColumns()
    {
        Assert.SkipWhen(this.TestDatabaseProvider.TemporaryTableTextColumnInheritsCollationFromDatabase, "");

        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Strings;

        using var tableDisposer = this.builder.BuildTemporaryTable(
            this.Connection,
            null,
            "Objects",
            Generate.Multiple<EntityWithEnumProperty>(),
            typeof(EntityWithEnumProperty),
            TestContext.Current.CancellationToken
        );

        var columnCollation = this.GetCollationOfTemporaryTableColumn("Objects", "Enum");

        columnCollation
            .Should().Be(this.TestDatabaseProvider.DatabaseCollation);
    }

    [Fact]
    public void BuildTemporaryTable_ComplexObjects_NotMappedProperties_ShouldNotCreateColumnsForNotMappedProperties()
    {
        var entities = Generate.Multiple<EntityWithNotMappedProperty>();
        entities.ForEach(a => a.NotMappedValue = "ShouldNotBePersisted");

        using var tableDisposer = this.builder.BuildTemporaryTable(
            this.Connection,
            null,
            "Objects",
            entities,
            typeof(EntityWithNotMappedProperty),
            TestContext.Current.CancellationToken
        );

        using var reader = this.Connection.ExecuteReader(
            $"SELECT * FROM {QT("Objects")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        reader.GetFieldNames()
            .Should().NotContain(nameof(EntityWithNotMappedProperty.NotMappedValue));
    }

    [Fact]
    public void BuildTemporaryTable_ComplexObjects_NullableProperties_ShouldHandleNullValues()
    {
        var itemsWithNulls = new List<TemporaryTableTestItemWithNullableProperties> { new() };

        using var tableDisposer = this.builder.BuildTemporaryTable(
            this.Connection,
            null,
            "Objects",
            itemsWithNulls,
            typeof(TemporaryTableTestItemWithNullableProperties),
            TestContext.Current.CancellationToken
        );

        this.Connection.Query<TemporaryTableTestItemWithNullableProperties>(
                $"SELECT * FROM {QT("Objects")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(itemsWithNulls);
    }

    [Fact]
    public void BuildTemporaryTable_ComplexObjects_ShouldCreateMultiColumnTable()
    {
        var items = Generate.Multiple<TemporaryTableTestItem>();

        using var tableDisposer = this.builder.BuildTemporaryTable(
            this.Connection,
            null,
            "Objects",
            items,
            typeof(TemporaryTableTestItem),
            TestContext.Current.CancellationToken
        );

        this.Connection.Query<TemporaryTableTestItem>(
                $"SELECT * FROM {QT("Objects")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(items);
    }

    [Fact]
    public void BuildTemporaryTable_ComplexObjects_ShouldUseCollationOfDatabaseForTextColumns()
    {
        Assert.SkipWhen(this.TestDatabaseProvider.TemporaryTableTextColumnInheritsCollationFromDatabase, "");

        using var tableDisposer = this.builder.BuildTemporaryTable(
            this.Connection,
            null,
            "Objects",
            Generate.Multiple<EntityWithStringProperty>(),
            typeof(EntityWithStringProperty),
            TestContext.Current.CancellationToken
        );

        var columnCollation = this.GetCollationOfTemporaryTableColumn("Objects", "String");

        columnCollation
            .Should().Be(this.TestDatabaseProvider.DatabaseCollation);
    }

    [Fact]
    public void BuildTemporaryTable_ScalarValues_DateTimeOffsetValues_ShouldSupportDateTimeOffset()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        DateTimeOffset[] values = [Generate.Single<DateTimeOffset>()];

        using var tableDisposer = this.builder.BuildTemporaryTable(
            this.Connection,
            null,
            "Values",
            values,
            typeof(DateTimeOffset),
            TestContext.Current.CancellationToken
        );

        this.Connection.Query<DateTimeOffset>(
                $"SELECT * FROM {QT("Values")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(values);
    }

    [Fact]
    public void BuildTemporaryTable_ScalarValues_EnumSerializationModeIsIntegers_ShouldStoreEnumValuesAsIntegers()
    {
        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Integers;

        var values = Generate.Multiple<TestEnum>();

        using var tableDisposer = this.builder.BuildTemporaryTable(
            this.Connection,
            null,
            "Values",
            values,
            typeof(TestEnum),
            TestContext.Current.CancellationToken
        );

        if (this.TestDatabaseProvider.CanRetrieveStructureOfTemporaryTables)
        {
            this.DatabaseAdapter.GetDataType(typeof(TestEnum), EnumSerializationMode.Integers)
                .Should().StartWith(this.GetDataTypeOfTemporaryTableColumn("Values", "Value"));
        }

        using var reader = this.Connection.ExecuteReader(
            $"SELECT {Q("Value")} FROM {QT("Values")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        reader.GetFieldType(0)
            .Should().BeAnyOf(typeof(Int32), typeof(Int64));

        foreach (var value in values)
        {
            reader.Read();
            
            reader.GetInt32(0)
                .Should().Be((Int32)value);
        }
    }

    [Fact]
    public void BuildTemporaryTable_ScalarValues_EnumSerializationModeIsStrings_ShouldStoreEnumValuesAsStrings()
    {
        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Strings;

        var values = Generate.Multiple<TestEnum>();

        using var tableDisposer = this.builder.BuildTemporaryTable(
            this.Connection,
            null,
            "Values",
            values,
            typeof(TestEnum),
            TestContext.Current.CancellationToken
        );

        if (this.TestDatabaseProvider.CanRetrieveStructureOfTemporaryTables)
        {
            this.DatabaseAdapter.GetDataType(typeof(TestEnum), EnumSerializationMode.Strings)
                .Should().StartWith(this.GetDataTypeOfTemporaryTableColumn("Values", "Value"));
        }

        using var reader = this.Connection.ExecuteReader(
            $"SELECT {Q("Value")} FROM {QT("Values")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        reader.GetFieldType(0)
            .Should().Be(typeof(String));

        foreach (var value in values)
        {
            reader.Read();
            
            reader.GetString(0)
                .Should().Be(value.ToString());
        }
    }

    [Fact]
    public void
        BuildTemporaryTable_ScalarValues_EnumSerializationModeIsStrings_ShouldUseCollationOfDatabaseForEnumColumns()
    {
        Assert.SkipWhen(this.TestDatabaseProvider.TemporaryTableTextColumnInheritsCollationFromDatabase, "");

        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Strings;

        using var tableDisposer = this.builder.BuildTemporaryTable(
            this.Connection,
            null,
            "Values",
            Generate.Multiple<TestEnum>(),
            typeof(TestEnum),
            TestContext.Current.CancellationToken
        );

        var columnCollation = this.GetCollationOfTemporaryTableColumn("Values", "Value");

        columnCollation
            .Should().Be(this.TestDatabaseProvider.DatabaseCollation);
    }

    [Fact]
    public void
        BuildTemporaryTable_ScalarValues_NullableEnumValues_ShouldFillTableWithEnumsAndNulls()
    {
        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Strings;

        var values = Generate.MultipleNullable<TestEnum>();

        using var tableDisposer = this.builder.BuildTemporaryTable(
            this.Connection,
            null,
            "Values",
            values,
            typeof(TestEnum?),
            TestContext.Current.CancellationToken
        );

        this.Connection.Query<TestEnum?>(
                $"SELECT {Q("Value")} FROM {QT("Values")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(values);
    }

    [Fact]
    public void BuildTemporaryTable_ScalarValues_ShouldCreateSingleColumnTable()
    {
        var values = Generate.Multiple<Int32>();

        using var tableDisposer = this.builder.BuildTemporaryTable(
            this.Connection,
            null,
            "Values",
            values,
            typeof(Int32),
            TestContext.Current.CancellationToken
        );

        this.Connection.Query<Int32>(
                $"SELECT {Q("Value")} FROM {QT("Values")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(values);
    }

    [Fact]
    public void BuildTemporaryTable_ScalarValues_ShouldUseCollationOfDatabaseForTextColumns()
    {
        Assert.SkipWhen(this.TestDatabaseProvider.TemporaryTableTextColumnInheritsCollationFromDatabase, "");

        using var tableDisposer = this.builder.BuildTemporaryTable(
            this.Connection,
            null,
            "Values",
            Generate.Multiple<String>(),
            typeof(String),
            TestContext.Current.CancellationToken
        );

        var columnCollation = this.GetCollationOfTemporaryTableColumn("Values", "Value");

        columnCollation
            .Should().Be(this.TestDatabaseProvider.DatabaseCollation);
    }

    [Fact]
    public void BuildTemporaryTable_ScalarValuesWithNullValues_ShouldHandleNullValues()
    {
        var values = Generate.MultipleNullable<Int32>();

        using var tableDisposer = this.builder.BuildTemporaryTable(
            this.Connection,
            null,
            "NullValues",
            values,
            typeof(Int32?),
            TestContext.Current.CancellationToken
        );

        this.Connection.Query<Int32?>(
                $"SELECT {Q("Value")} FROM {QT("NullValues")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(values);
    }

    [Fact]
    public void BuildTemporaryTable_ShouldReturnDisposerThatDropsTable()
    {
        var tableDisposer = this.builder.BuildTemporaryTable(
            this.Connection,
            null,
            "Values",
            Generate.Multiple<Int32>(),
            typeof(Int32),
            TestContext.Current.CancellationToken
        );

        this.ExistsTemporaryTableInDb("Values")
            .Should().BeTrue();

        tableDisposer.Dispose();

        this.ExistsTemporaryTableInDb("Values")
            .Should().BeFalse();
    }

    [Fact]
    public async Task BuildTemporaryTableAsync_ComplexObjects_DateTimeOffsetProperty_ShouldSupportDateTimeOffset()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var items = Generate.Multiple<TemporaryTableTestItemWithDateTimeOffset>();

        await using var tableDisposer = await this.builder.BuildTemporaryTableAsync(
            this.Connection,
            null,
            "Objects",
            items,
            typeof(TemporaryTableTestItemWithDateTimeOffset),
            TestContext.Current.CancellationToken
        );

        (await this.Connection.QueryAsync<TemporaryTableTestItemWithDateTimeOffset>(
                $"SELECT * FROM {QT("Objects")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(items);
    }

    [Fact]
    public async Task
        BuildTemporaryTableAsync_ComplexObjects_EnumSerializationModeIsIntegers_ShouldStoreEnumValuesAsIntegers()
    {
        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Integers;

        var entities = Generate.Multiple<EntityWithEnumProperty>();

        await using var tableDisposer = await this.builder.BuildTemporaryTableAsync(
            this.Connection,
            null,
            "Objects",
            entities,
            typeof(EntityWithEnumProperty),
            TestContext.Current.CancellationToken
        );

        if (this.TestDatabaseProvider.CanRetrieveStructureOfTemporaryTables)
        {
            this.DatabaseAdapter.GetDataType(typeof(TestEnum), EnumSerializationMode.Integers)
                .Should().StartWith(this.GetDataTypeOfTemporaryTableColumn("Objects", "Enum"));
        }

        await using var reader = await this.Connection.ExecuteReaderAsync(
            $"SELECT {Q("Enum")} FROM {QT("Objects")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        reader.GetFieldType(0)
            .Should().BeAnyOf(typeof(Int32), typeof(Int64));

        foreach (var entity in entities)
        {
            await reader.ReadAsync(TestContext.Current.CancellationToken);
            
            reader.GetInt32(0)
                .Should().Be((Int32)entity.Enum);
        }
    }

    [Fact]
    public async Task
        BuildTemporaryTableAsync_ComplexObjects_EnumSerializationModeIsStrings_ShouldStoreEnumValuesAsStrings()
    {
        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Strings;

        var entities = Generate.Multiple<EntityWithEnumProperty>();

        await using var tableDisposer = await this.builder.BuildTemporaryTableAsync(
            this.Connection,
            null,
            "Objects",
            entities,
            typeof(EntityWithEnumProperty),
            TestContext.Current.CancellationToken
        );

        if (this.TestDatabaseProvider.CanRetrieveStructureOfTemporaryTables)
        {
            this.DatabaseAdapter.GetDataType(typeof(TestEnum), EnumSerializationMode.Strings)
                .Should().StartWith(this.GetDataTypeOfTemporaryTableColumn("Objects", "Enum"));
        }

        await using var reader = await this.Connection.ExecuteReaderAsync(
            $"SELECT {Q("Enum")} FROM {QT("Objects")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        reader.GetFieldType(0)
            .Should().Be(typeof(String));

        foreach (var entity in entities)
        {
            await reader.ReadAsync(TestContext.Current.CancellationToken);
            
            reader.GetString(0)
                .Should().Be(entity.Enum.ToString());
        }
    }

    [Fact]
    public async Task
        BuildTemporaryTableAsync_ComplexObjects_EnumSerializationModeIsStrings_ShouldUseCollationOfDatabaseForEnumColumns()
    {
        Assert.SkipWhen(this.TestDatabaseProvider.TemporaryTableTextColumnInheritsCollationFromDatabase, "");

        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Strings;

        await using var tableDisposer = await this.builder.BuildTemporaryTableAsync(
            this.Connection,
            null,
            "Objects",
            Generate.Multiple<EntityWithEnumProperty>(),
            typeof(EntityWithEnumProperty),
            TestContext.Current.CancellationToken
        );

        var columnCollation = this.GetCollationOfTemporaryTableColumn("Objects", "Enum");

        columnCollation
            .Should().Be(this.TestDatabaseProvider.DatabaseCollation);
    }

    [Fact]
    public async Task
        BuildTemporaryTableAsync_ComplexObjects_NotMappedProperties_ShouldNotCreateColumnsForNotMappedProperties()
    {
        var entities = Generate.Multiple<EntityWithNotMappedProperty>();
        entities.ForEach(a => a.NotMappedValue = "ShouldNotBePersisted");

        await using var tableDisposer = await this.builder.BuildTemporaryTableAsync(
            this.Connection,
            null,
            "Objects",
            entities,
            typeof(EntityWithNotMappedProperty),
            TestContext.Current.CancellationToken
        );

        await using var reader = await this.Connection.ExecuteReaderAsync(
            $"SELECT * FROM {QT("Objects")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        reader.GetFieldNames()
            .Should().NotContain(nameof(EntityWithNotMappedProperty.NotMappedValue));
    }

    [Fact]
    public async Task BuildTemporaryTableAsync_ComplexObjects_ShouldCreateMultiColumnTable()
    {
        var items = Generate.Multiple<TemporaryTableTestItem>();

        await using var tableDisposer = await this.builder.BuildTemporaryTableAsync(
            this.Connection,
            null,
            "Objects",
            items,
            typeof(TemporaryTableTestItem),
            TestContext.Current.CancellationToken
        );

        (await this.Connection.QueryAsync<TemporaryTableTestItem>(
                $"SELECT * FROM {QT("Objects")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(items);
    }

    [Fact]
    public async Task BuildTemporaryTableAsync_ComplexObjects_ShouldUseCollationOfDatabaseForTextColumns()
    {
        Assert.SkipWhen(this.TestDatabaseProvider.TemporaryTableTextColumnInheritsCollationFromDatabase, "");

        await using var tableDisposer = await this.builder.BuildTemporaryTableAsync(
            this.Connection,
            null,
            "Objects",
            Generate.Multiple<EntityWithStringProperty>(),
            typeof(EntityWithStringProperty),
            TestContext.Current.CancellationToken
        );

        var columnCollation = this.GetCollationOfTemporaryTableColumn("Objects", "String");

        columnCollation
            .Should().Be(this.TestDatabaseProvider.DatabaseCollation);
    }

    [Fact]
    public async Task BuildTemporaryTableAsync_ComplexObjects_WithNullables_ShouldHandleNullValues()
    {
        var itemsWithNulls = new List<TemporaryTableTestItemWithNullableProperties> { new() };

        await using var tableDisposer = await this.builder.BuildTemporaryTableAsync(
            this.Connection,
            null,
            "Objects",
            itemsWithNulls,
            typeof(TemporaryTableTestItemWithNullableProperties),
            TestContext.Current.CancellationToken
        );

        (await this.Connection.QueryAsync<TemporaryTableTestItemWithNullableProperties>(
                $"SELECT * FROM {QT("Objects")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(itemsWithNulls);
    }

    [Fact]
    public async Task BuildTemporaryTableAsync_ScalarValues_DateTimeOffsetValues_ShouldSupportDateTimeOffset()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        DateTimeOffset[] values = [Generate.Single<DateTimeOffset>()];

        await using var tableDisposer = await this.builder.BuildTemporaryTableAsync(
            this.Connection,
            null,
            "Values",
            values,
            typeof(DateTimeOffset),
            TestContext.Current.CancellationToken
        );

        (await this.Connection.QueryAsync<DateTimeOffset>(
                $"SELECT * FROM {QT("Values")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(values);
    }

    [Fact]
    public async Task
        BuildTemporaryTableAsync_ScalarValues_EnumSerializationModeIsIntegers_ShouldStoreEnumValuesAsIntegers()
    {
        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Integers;

        var values = Generate.Multiple<TestEnum>();

        await using var tableDisposer = await this.builder.BuildTemporaryTableAsync(
            this.Connection,
            null,
            "Values",
            values,
            typeof(TestEnum),
            TestContext.Current.CancellationToken
        );

        if (this.TestDatabaseProvider.CanRetrieveStructureOfTemporaryTables)
        {
            this.DatabaseAdapter.GetDataType(typeof(TestEnum), EnumSerializationMode.Integers)
                .Should().StartWith(this.GetDataTypeOfTemporaryTableColumn("Values", "Value"));
        }

        await using var reader = await this.Connection.ExecuteReaderAsync(
            $"SELECT {Q("Value")} FROM {QT("Values")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        reader.GetFieldType(0)
            .Should().BeAnyOf(typeof(Int32), typeof(Int64));

        foreach (var value in values)
        {
            await reader.ReadAsync(TestContext.Current.CancellationToken);
            
            reader.GetInt32(0)
                .Should().Be((Int32)value);
        }
    }

    [Fact]
    public async Task
        BuildTemporaryTableAsync_ScalarValues_EnumSerializationModeIsStrings_ShouldStoreEnumValuesAsStrings()
    {
        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Strings;

        var values = Generate.Multiple<TestEnum>();

        await using var tableDisposer = await this.builder.BuildTemporaryTableAsync(
            this.Connection,
            null,
            "Values",
            values,
            typeof(TestEnum),
            TestContext.Current.CancellationToken
        );

        if (this.TestDatabaseProvider.CanRetrieveStructureOfTemporaryTables)
        {
            this.DatabaseAdapter.GetDataType(typeof(TestEnum), EnumSerializationMode.Strings)
                .Should().StartWith(this.GetDataTypeOfTemporaryTableColumn("Values", "Value"));
        }

        await using var reader = await this.Connection.ExecuteReaderAsync(
            $"SELECT {Q("Value")} FROM {QT("Values")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        reader.GetFieldType(0)
            .Should().Be(typeof(String));

        foreach (var value in values)
        {
            await reader.ReadAsync(TestContext.Current.CancellationToken);
            
            reader.GetString(0)
                .Should().Be(value.ToString());
        }
    }

    [Fact]
    public async Task
        BuildTemporaryTableAsync_ScalarValues_EnumSerializationModeIsStrings_ShouldUseCollationOfDatabaseForEnumColumns()
    {
        Assert.SkipWhen(this.TestDatabaseProvider.TemporaryTableTextColumnInheritsCollationFromDatabase, "");

        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Strings;

        await using var tableDisposer = await this.builder.BuildTemporaryTableAsync(
            this.Connection,
            null,
            "Values",
            Generate.Multiple<TestEnum>(),
            typeof(TestEnum),
            TestContext.Current.CancellationToken
        );

        var columnCollation = this.GetCollationOfTemporaryTableColumn("Values", "Value");

        columnCollation
            .Should().Be(this.TestDatabaseProvider.DatabaseCollation);
    }

    [Fact]
    public async Task
        BuildTemporaryTableAsync_ScalarValues_NullableEnumValues_ShouldFillTableWithEnumsAndNulls()
    {
        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Strings;

        var values = Generate.MultipleNullable<TestEnum>();

        await using var tableDisposer = await this.builder.BuildTemporaryTableAsync(
            this.Connection,
            null,
            "Values",
            values,
            typeof(TestEnum?),
            TestContext.Current.CancellationToken
        );

        (await this.Connection.QueryAsync<TestEnum?>(
                $"SELECT {Q("Value")} FROM {QT("Values")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(values);
    }

    [Fact]
    public async Task BuildTemporaryTableAsync_ScalarValues_ShouldCreateSingleColumnTable()
    {
        var values = Generate.Multiple<Int32>();

        await using var tableDisposer = await this.builder.BuildTemporaryTableAsync(
            this.Connection,
            null,
            "Values",
            values,
            typeof(Int32),
            TestContext.Current.CancellationToken
        );

        (await this.Connection.QueryAsync<Int32>(
                $"SELECT {Q("Value")} FROM {QT("Values")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(values);
    }

    [Fact]
    public async Task BuildTemporaryTableAsync_ScalarValues_ShouldUseCollationOfDatabaseForTextColumns()
    {
        Assert.SkipWhen(this.TestDatabaseProvider.TemporaryTableTextColumnInheritsCollationFromDatabase, "");

        await using var tableDisposer = await this.builder.BuildTemporaryTableAsync(
            this.Connection,
            null,
            "Values",
            Generate.Multiple<String>(),
            typeof(String),
            TestContext.Current.CancellationToken
        );

        var columnCollation = this.GetCollationOfTemporaryTableColumn("Values", "Value");

        columnCollation
            .Should().Be(this.TestDatabaseProvider.DatabaseCollation);
    }

    [Fact]
    public async Task BuildTemporaryTableAsync_ScalarValuesWithNullValues_ShouldHandleNullValues()
    {
        var values = Generate.MultipleNullable<Int32>();

        await using var tableDisposer = await this.builder.BuildTemporaryTableAsync(
            this.Connection,
            null,
            "NullValues",
            values,
            typeof(Int32?),
            TestContext.Current.CancellationToken
        );

        (await this.Connection.QueryAsync<Int32?>(
                $"SELECT {Q("Value")} FROM {QT("NullValues")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(values);
    }

    [Fact]
    public async Task BuildTemporaryTableAsync_ShouldReturnDisposerThatDropsTableAsync()
    {
        var disposer = await this.builder.BuildTemporaryTableAsync(
            this.Connection,
            null,
            "Values",
            Generate.Multiple<Int32>(),
            typeof(Int32),
            TestContext.Current.CancellationToken
        );

        this.ExistsTemporaryTableInDb("Values")
            .Should().BeTrue();

        await disposer.DisposeAsync();

        this.ExistsTemporaryTableInDb("Values")
            .Should().BeFalse();
    }

    private readonly ITemporaryTableBuilder builder;
}
