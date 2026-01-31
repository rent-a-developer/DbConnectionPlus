using System.Collections;
using System.Data.Common;
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildTemporaryTable_ComplexObjects_DateTimeOffsetProperty_ShouldSupportDateTimeOffset(
        Boolean useAsyncApi
    )
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var items = Generate.Multiple<TemporaryTableTestItemWithDateTimeOffset>();

        await using var tableDisposer = await this.CallApi(
            useAsyncApi,
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        BuildTemporaryTable_ComplexObjects_EnumSerializationModeIsIntegers_ShouldStoreEnumValuesAsIntegers(
            Boolean useAsyncApi
        )
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Integers;

        var entities = Generate.Multiple<EntityWithEnumProperty>();

        await using var tableDisposer = await this.CallApi(
            useAsyncApi,
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        BuildTemporaryTable_ComplexObjects_EnumSerializationModeIsStrings_ShouldStoreEnumValuesAsStrings(
            Boolean useAsyncApi
        )
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        var entities = Generate.Multiple<EntityWithEnumProperty>();

        await using var tableDisposer = await this.CallApi(
            useAsyncApi,
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        BuildTemporaryTable_ComplexObjects_EnumSerializationModeIsStrings_ShouldUseCollationOfDatabaseForEnumColumns(
            Boolean useAsyncApi
        )
    {
        Assert.SkipWhen(this.TestDatabaseProvider.TemporaryTableTextColumnInheritsCollationFromDatabase, "");

        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        await using var tableDisposer = await this.CallApi(
            useAsyncApi,
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildTemporaryTable_ComplexObjects_Mapping_Attributes_ShouldUseAttributesMapping(Boolean useAsyncApi)
    {
        var entities = Generate.Multiple<MappingTestEntityAttributes>();
        entities.ForEach(a => a.NotMappedColumn = "ShouldNotBePersisted");

        await using var tableDisposer = await this.CallApi(
            useAsyncApi,
            this.Connection,
            null,
            "Objects",
            entities,
            typeof(MappingTestEntityAttributes),
            TestContext.Current.CancellationToken
        );

        await using var reader = await this.Connection.ExecuteReaderAsync(
            $"SELECT * FROM {QT("Objects")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        reader.GetFieldNames()
            .Should().NotContain(nameof(MappingTestEntityAttributes.NotMappedColumn));

        foreach (var entity in entities)
        {
            var readBackEntity = this.Connection.QueryFirstOrDefault<MappingTestEntityAttributes>(
                $"""
                 SELECT *
                 FROM   {QT("Objects")}
                 WHERE  {Q("KeyColumn1")} = {Parameter(entity.KeyColumn1_)} AND 
                        {Q("KeyColumn2")} = {Parameter(entity.KeyColumn2_)}
                 """
            );

            readBackEntity
                .Should().NotBeNull();

            readBackEntity.ValueColumn_
                .Should().Be(entity.ValueColumn_);

            readBackEntity.ComputedColumn_
                .Should().Be(entity.ComputedColumn_);

            readBackEntity.IdentityColumn_
                .Should().Be(entity.IdentityColumn_);

            readBackEntity.NotMappedColumn
                .Should().BeNull();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildTemporaryTable_ComplexObjects_Mapping_FluentApi_ShouldUseFluentApiMapping(Boolean useAsyncApi)
    {
        Configure(config =>
        {
            config.Entity<MappingTestEntityFluentApi>()
                .ToTable("MappingTestEntity");

            config.Entity<MappingTestEntityFluentApi>()
                .Property(a => a.KeyColumn1_)
                .HasColumnName("KeyColumn1")
                .IsKey();

            config.Entity<MappingTestEntityFluentApi>()
                .Property(a => a.KeyColumn2_)
                .HasColumnName("KeyColumn2")
                .IsKey();

            config.Entity<MappingTestEntityFluentApi>()
                .Property(a => a.ValueColumn_)
                .HasColumnName("ValueColumn");

            config.Entity<MappingTestEntityFluentApi>()
                .Property(a => a.ComputedColumn_)
                .HasColumnName("ComputedColumn")
                .IsComputed();

            config.Entity<MappingTestEntityFluentApi>()
                .Property(a => a.IdentityColumn_)
                .HasColumnName("IdentityColumn")
                .IsIdentity();

            config.Entity<MappingTestEntityFluentApi>()
                .Property(a => a.NotMappedColumn)
                .IsIgnored();
        }
        );

        var entities = Generate.Multiple<MappingTestEntityFluentApi>();
        entities.ForEach(a => a.NotMappedColumn = "ShouldNotBePersisted");

        await using var tableDisposer = await this.CallApi(
            useAsyncApi,
            this.Connection,
            null,
            "Objects",
            entities,
            typeof(MappingTestEntityFluentApi),
            TestContext.Current.CancellationToken
        );

        await using var reader = await this.Connection.ExecuteReaderAsync(
            $"SELECT * FROM {QT("Objects")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        reader.GetFieldNames()
            .Should().NotContain(nameof(MappingTestEntityFluentApi.NotMappedColumn));

        foreach (var entity in entities)
        {
            var readBackEntity = this.Connection.QueryFirstOrDefault<MappingTestEntityFluentApi>(
                $"""
                 SELECT *
                 FROM   {QT("Objects")}
                 WHERE  {Q("KeyColumn1")} = {Parameter(entity.KeyColumn1_)} AND 
                        {Q("KeyColumn2")} = {Parameter(entity.KeyColumn2_)}
                 """
            );

            readBackEntity
                .Should().NotBeNull();

            readBackEntity.ValueColumn_
                .Should().Be(entity.ValueColumn_);

            readBackEntity.ComputedColumn_
                .Should().Be(entity.ComputedColumn_);

            readBackEntity.IdentityColumn_
                .Should().Be(entity.IdentityColumn_);

            readBackEntity.NotMappedColumn
                .Should().BeNull();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildTemporaryTable_ComplexObjects_NoMapping_ShouldUseEntityTypeNameAndPropertyNames(Boolean useAsyncApi)
    {
        var entities = Generate.Multiple<MappingTestEntity>();

        await using var tableDisposer = await this.CallApi(
            useAsyncApi,
            this.Connection,
            null,
            "Objects",
            entities,
            typeof(MappingTestEntity),
            TestContext.Current.CancellationToken
        );

        await using var reader = await this.Connection.ExecuteReaderAsync(
            $"SELECT * FROM {QT("Objects")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        foreach (var entity in entities)
        {
            var readBackEntity = this.Connection.QueryFirstOrDefault<MappingTestEntity>(
                $"""
                 SELECT *
                 FROM   {QT("Objects")}
                 WHERE  {Q("KeyColumn1")} = {Parameter(entity.KeyColumn1)} AND 
                        {Q("KeyColumn2")} = {Parameter(entity.KeyColumn2)}
                 """
            );

            readBackEntity
                .Should().NotBeNull();

            readBackEntity.ValueColumn
                .Should().Be(entity.ValueColumn);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildTemporaryTable_ComplexObjects_ShouldCreateMultiColumnTable(Boolean useAsyncApi)
    {
        var items = Generate.Multiple<TemporaryTableTestItem>();

        await using var tableDisposer = await this.CallApi(
            useAsyncApi,
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildTemporaryTable_ComplexObjects_ShouldUseCollationOfDatabaseForTextColumns(
        Boolean useAsyncApi
    )
    {
        Assert.SkipWhen(this.TestDatabaseProvider.TemporaryTableTextColumnInheritsCollationFromDatabase, "");

        await using var tableDisposer = await this.CallApi(
            useAsyncApi,
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildTemporaryTable_ComplexObjects_WithNullables_ShouldHandleNullValues(Boolean useAsyncApi)
    {
        var itemsWithNulls = new List<TemporaryTableTestItemWithNullableProperties> { new() };

        await using var tableDisposer = await this.CallApi(
            useAsyncApi,
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildTemporaryTable_ScalarValues_DateTimeOffsetValues_ShouldSupportDateTimeOffset(
        Boolean useAsyncApi
    )
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        DateTimeOffset[] values = [Generate.Single<DateTimeOffset>()];

        await using var tableDisposer = await this.CallApi(
            useAsyncApi,
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        BuildTemporaryTable_ScalarValues_EnumSerializationModeIsIntegers_ShouldStoreEnumValuesAsIntegers(
            Boolean useAsyncApi
        )
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Integers;

        var values = Generate.Multiple<TestEnum>();

        await using var tableDisposer = await this.CallApi(
            useAsyncApi,
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        BuildTemporaryTable_ScalarValues_EnumSerializationModeIsStrings_ShouldStoreEnumValuesAsStrings(
            Boolean useAsyncApi
        )
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        var values = Generate.Multiple<TestEnum>();

        await using var tableDisposer = await this.CallApi(
            useAsyncApi,
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        BuildTemporaryTable_ScalarValues_EnumSerializationModeIsStrings_ShouldUseCollationOfDatabaseForEnumColumns(
            Boolean useAsyncApi
        )
    {
        Assert.SkipWhen(this.TestDatabaseProvider.TemporaryTableTextColumnInheritsCollationFromDatabase, "");

        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        await using var tableDisposer = await this.CallApi(
            useAsyncApi,
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        BuildTemporaryTable_ScalarValues_NullableEnumValues_ShouldFillTableWithEnumsAndNulls(Boolean useAsyncApi)
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        var values = Generate.MultipleNullable<TestEnum>();

        await using var tableDisposer = await this.CallApi(
            useAsyncApi,
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildTemporaryTable_ScalarValues_ShouldCreateSingleColumnTable(Boolean useAsyncApi)
    {
        var values = Generate.Multiple<Int32>();

        await using var tableDisposer = await this.CallApi(
            useAsyncApi,
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildTemporaryTable_ScalarValues_ShouldUseCollationOfDatabaseForTextColumns(
        Boolean useAsyncApi
    )
    {
        Assert.SkipWhen(this.TestDatabaseProvider.TemporaryTableTextColumnInheritsCollationFromDatabase, "");

        await using var tableDisposer = await this.CallApi(
            useAsyncApi,
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildTemporaryTable_ScalarValuesWithNullValues_ShouldHandleNullValues(Boolean useAsyncApi)
    {
        var values = Generate.MultipleNullable<Int32>();

        await using var tableDisposer = await this.CallApi(
            useAsyncApi,
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildTemporaryTable_ShouldReturnDisposerThatDropsTableAsync(Boolean useAsyncApi)
    {
        var disposer = await this.CallApi(
            useAsyncApi,
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

    private Task<TemporaryTableDisposer> CallApi(
        Boolean useAsyncApi,
        DbConnection connection,
        DbTransaction? transaction,
        String name,
        IEnumerable values,
        Type valuesType,
        CancellationToken cancellationToken = default
    )
    {
        if (useAsyncApi)
        {
            return this.builder.BuildTemporaryTableAsync(
                connection,
                transaction,
                name,
                values,
                valuesType,
                cancellationToken
            );
        }

        try
        {
            return Task.FromResult(
                this.builder.BuildTemporaryTable(connection, transaction, name, values, valuesType, cancellationToken)
            );
        }
        catch (Exception ex)
        {
            return Task.FromException<TemporaryTableDisposer>(ex);
        }
    }

    private readonly ITemporaryTableBuilder builder;
}
