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
        bool useAsyncApi
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
            bool useAsyncApi
        )
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Integers;

        var entities = Generate.Multiple<EntityWithEnumStoredAsInteger>();

        await using var tableDisposer = await this.CallApi(
            useAsyncApi,
            this.Connection,
            null,
            "Objects",
            entities,
            typeof(EntityWithEnumStoredAsInteger),
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
            .Should().BeAnyOf(typeof(int), typeof(long));

        foreach (var entity in entities)
        {
            await reader.ReadAsync(TestContext.Current.CancellationToken);

            reader.GetInt32(0)
                .Should().Be((int)entity.Enum);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        BuildTemporaryTable_ComplexObjects_EnumSerializationModeIsStrings_ShouldStoreEnumValuesAsStrings(
            bool useAsyncApi
        )
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        var entities = Generate.Multiple<EntityWithEnumStoredAsString>();

        await using var tableDisposer = await this.CallApi(
            useAsyncApi,
            this.Connection,
            null,
            "Objects",
            entities,
            typeof(EntityWithEnumStoredAsString),
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
            .Should().Be(typeof(string));

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
            bool useAsyncApi
        )
    {
        Assert.SkipWhen(this.TestDatabaseProvider.TemporaryTableTextColumnInheritsCollationFromDatabase, "");

        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        await using var tableDisposer = await this.CallApi(
            useAsyncApi,
            this.Connection,
            null,
            "Objects",
            Generate.Multiple<EntityWithEnumStoredAsString>(),
            typeof(EntityWithEnumStoredAsString),
            TestContext.Current.CancellationToken
        );

        var columnCollation = this.GetCollationOfTemporaryTableColumn("Objects", "Enum");

        columnCollation
            .Should().Be(this.TestDatabaseProvider.DatabaseCollation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildTemporaryTable_ComplexObjects_Mapping_Attributes_ShouldUseAttributesMapping(
        bool useAsyncApi
    )
    {
        var entities = Generate.Multiple<MappingTestEntityAttributes>();
        entities.ForEach(a => a.NotMapped = "ShouldNotBePersisted");

        await using var tableDisposer = await this.CallApi(
            useAsyncApi,
            this.Connection,
            null,
            "Objects",
            entities,
            typeof(MappingTestEntityAttributes),
            TestContext.Current.CancellationToken
        );

        var reader = await this.Connection.ExecuteReaderAsync(
            $"SELECT * FROM {QT("Objects")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        reader.GetFieldNames()
            .Should().NotContain(nameof(MappingTestEntityAttributes.NotMapped));

        await reader.DisposeAsync();

        this.Connection.Query<MappingTestEntityAttributes>($"SELECT * FROM {QT("Objects")}")
            .Should().BeEquivalentTo(
                entities,
                options => options.Using<string>(context => context.Subject.Should().BeNull())
                    .When(info => info.Path.EndsWith("NotMapped"))
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildTemporaryTable_ComplexObjects_Mapping_FluentApi_ShouldUseFluentApiMapping(
        bool useAsyncApi
    )
    {
        MappingTestEntityFluentApi.Configure();

        var entities = Generate.Multiple<MappingTestEntityFluentApi>();
        entities.ForEach(a => a.NotMapped = "ShouldNotBePersisted");

        await using var tableDisposer = await this.CallApi(
            useAsyncApi,
            this.Connection,
            null,
            "Objects",
            entities,
            typeof(MappingTestEntityFluentApi),
            TestContext.Current.CancellationToken
        );

        var reader = await this.Connection.ExecuteReaderAsync(
            $"SELECT * FROM {QT("Objects")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        reader.GetFieldNames()
            .Should().NotContain(nameof(MappingTestEntityFluentApi.NotMapped));

        await reader.DisposeAsync();

        this.Connection.Query<MappingTestEntityFluentApi>($"SELECT * FROM {QT("Objects")}")
            .Should().BeEquivalentTo(
                entities,
                options => options.Using<string>(context => context.Subject.Should().BeNull())
                    .When(info => info.Path.EndsWith("NotMapped"))
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildTemporaryTable_ComplexObjects_NoMapping_ShouldUseEntityTypeNameAndPropertyNames(
        bool useAsyncApi
    )
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

        this.Connection.Query<MappingTestEntity>($"SELECT * FROM {QT("Objects")}")
            .Should().BeEquivalentTo(entities);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildTemporaryTable_ComplexObjects_ShouldCreateMultiColumnTable(bool useAsyncApi)
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
    public async Task BuildTemporaryTable_ComplexObjects_ShouldUseCollationOfDatabaseForTextColumns(bool useAsyncApi)
    {
        Assert.SkipWhen(this.TestDatabaseProvider.TemporaryTableTextColumnInheritsCollationFromDatabase, "");

        await using var tableDisposer = await this.CallApi(
            useAsyncApi,
            this.Connection,
            null,
            "Objects",
            Generate.Multiple<Entity>(),
            typeof(Entity),
            TestContext.Current.CancellationToken
        );

        var columnCollation = this.GetCollationOfTemporaryTableColumn("Objects", "StringValue");

        columnCollation
            .Should().Be(this.TestDatabaseProvider.DatabaseCollation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildTemporaryTable_ComplexObjects_WithNullables_ShouldHandleNullValues(bool useAsyncApi)
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
        bool useAsyncApi
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
            bool useAsyncApi
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
            .Should().BeAnyOf(typeof(int), typeof(long));

        foreach (var value in values)
        {
            await reader.ReadAsync(TestContext.Current.CancellationToken);

            reader.GetInt32(0)
                .Should().Be((int)value);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        BuildTemporaryTable_ScalarValues_EnumSerializationModeIsStrings_ShouldStoreEnumValuesAsStrings(
            bool useAsyncApi
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
            .Should().Be(typeof(string));

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
            bool useAsyncApi
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
        BuildTemporaryTable_ScalarValues_NullableEnumValues_ShouldFillTableWithEnumsAndNulls(bool useAsyncApi)
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
    public async Task BuildTemporaryTable_ScalarValues_ShouldCreateSingleColumnTable(bool useAsyncApi)
    {
        var values = Generate.Multiple<int>();

        await using var tableDisposer = await this.CallApi(
            useAsyncApi,
            this.Connection,
            null,
            "Values",
            values,
            typeof(int),
            TestContext.Current.CancellationToken
        );

        (await this.Connection.QueryAsync<int>(
                $"SELECT {Q("Value")} FROM {QT("Values")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(values);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildTemporaryTable_ScalarValues_ShouldUseCollationOfDatabaseForTextColumns(bool useAsyncApi)
    {
        Assert.SkipWhen(this.TestDatabaseProvider.TemporaryTableTextColumnInheritsCollationFromDatabase, "");

        await using var tableDisposer = await this.CallApi(
            useAsyncApi,
            this.Connection,
            null,
            "Values",
            Generate.Multiple<string>(),
            typeof(string),
            TestContext.Current.CancellationToken
        );

        var columnCollation = this.GetCollationOfTemporaryTableColumn("Values", "Value");

        columnCollation
            .Should().Be(this.TestDatabaseProvider.DatabaseCollation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildTemporaryTable_ScalarValuesWithNullValues_ShouldHandleNullValues(bool useAsyncApi)
    {
        var values = Generate.MultipleNullable<int>();

        await using var tableDisposer = await this.CallApi(
            useAsyncApi,
            this.Connection,
            null,
            "NullValues",
            values,
            typeof(int?),
            TestContext.Current.CancellationToken
        );

        (await this.Connection.QueryAsync<int?>(
                $"SELECT {Q("Value")} FROM {QT("NullValues")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(values);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildTemporaryTable_ShouldReturnDisposerThatDropsTableAsync(bool useAsyncApi)
    {
        var disposer = await this.CallApi(
            useAsyncApi,
            this.Connection,
            null,
            "Values",
            Generate.Multiple<int>(),
            typeof(int),
            TestContext.Current.CancellationToken
        );

        this.ExistsTemporaryTableInDb("Values")
            .Should().BeTrue();

        await disposer.DisposeAsync();

        this.ExistsTemporaryTableInDb("Values")
            .Should().BeFalse();
    }

    private Task<TemporaryTableDisposer> CallApi(
        bool useAsyncApi,
        DbConnection connection,
        DbTransaction? transaction,
        string name,
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
