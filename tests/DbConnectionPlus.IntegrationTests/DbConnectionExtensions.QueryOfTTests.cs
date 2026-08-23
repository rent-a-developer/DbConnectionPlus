using System.Data.Common;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests;

public sealed class DbConnectionExtensions_QueryOfTTests_MySql
    : DbConnectionExtensions_QueryOfTTests<MySqlTestDatabaseProvider>;

public sealed class DbConnectionExtensions_QueryOfTTests_Oracle
    : DbConnectionExtensions_QueryOfTTests<OracleTestDatabaseProvider>;

public sealed class DbConnectionExtensions_QueryOfTTests_PostgreSql
    : DbConnectionExtensions_QueryOfTTests<PostgreSqlTestDatabaseProvider>;

public sealed class DbConnectionExtensions_QueryOfTTests_Sqlite
    : DbConnectionExtensions_QueryOfTTests<SqliteTestDatabaseProvider>;

public sealed class DbConnectionExtensions_QueryOfTTests_SqlServer
    : DbConnectionExtensions_QueryOfTTests<SqlServerTestDatabaseProvider>;

public abstract class DbConnectionExtensions_QueryOfTTests<TTestDatabaseProvider>
    : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_BuiltInType_CharTargetType_ColumnContainsStringWithLengthNotOne_ShouldThrow(
        bool useAsyncApi
    )
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            (
                await Invoking(() =>
                        CallApi<char>(
                                useAsyncApi,
                                this.Connection,
                                "SELECT ''",
                                cancellationToken: TestContext.Current.CancellationToken
                            )
                            .ToListAsync(TestContext.Current.CancellationToken)
                            .AsTask()
                    )
                    .Should()
                    .ThrowAsync<InvalidCastException>()
                    .WithMessage(
                        $"The first column returned by the SQL statement contains the value '' ({typeof(string)}), "
                            + $"which could not be converted to the type {typeof(char)}. See inner exception for details.*"
                    )
            )
                .WithInnerException<InvalidCastException>()
                .WithMessage(
                    $"Could not convert the string '' to the type {typeof(char)}. The string must be exactly "
                        + "one character long."
                );
        }

        (
            await Invoking(() =>
                    CallApi<char>(
                            useAsyncApi,
                            this.Connection,
                            "SELECT 'ab'",
                            cancellationToken: TestContext.Current.CancellationToken
                        )
                        .ToListAsync(TestContext.Current.CancellationToken)
                        .AsTask()
                )
                .Should()
                .ThrowAsync<InvalidCastException>()
                .WithMessage(
                    $"The first column returned by the SQL statement contains the value 'ab' ({typeof(string)}), "
                        + $"which could not be converted to the type {typeof(char)}. See inner exception for details.*"
                )
        )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(char)}. The string must be exactly "
                    + "one character long."
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_BuiltInType_CharTargetType_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter(
        bool useAsyncApi
    )
    {
        var character = Generate.Single<char>();

        (
            await CallApi<char>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT '{character}'",
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ToListAsync(TestContext.Current.CancellationToken)
        )
            .Should()
            .BeEquivalentTo([character]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Query_BuiltInType_ColumnValueCannotBeConvertedToTargetType_ShouldThrow(bool useAsyncApi) =>
        Invoking(() =>
                CallApi<int>(
                        useAsyncApi,
                        this.Connection,
                        "SELECT 'A'",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                    .ToListAsync(TestContext.Current.CancellationToken)
                    .AsTask()
            )
            .Should()
            .ThrowAsync<InvalidCastException>()
            .WithMessage(
                $"The first column returned by the SQL statement contains the value 'A' ({typeof(string)}), which "
                    + $"could not be converted to the type {typeof(int)}. See inner exception for details.*"
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Query_BuiltInType_EnumTargetType_ColumnContainsInvalidInteger_ShouldThrow(bool useAsyncApi) =>
        Invoking(() =>
                CallApi<TestEnum>(
                        useAsyncApi,
                        this.Connection,
                        "SELECT 999",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                    .ToListAsync(TestContext.Current.CancellationToken)
                    .AsTask()
            )
            .Should()
            .ThrowAsync<InvalidCastException>()
            .WithMessage(
                "The first column returned by the SQL statement contains the value '999*' (System.*), which "
                    + $"could not be converted to the type {typeof(TestEnum)}. See inner exception for details.*"
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Query_BuiltInType_EnumTargetType_ColumnContainsInvalidString_ShouldThrow(bool useAsyncApi) =>
        Invoking(() =>
                CallApi<TestEnum>(
                        useAsyncApi,
                        this.Connection,
                        "SELECT 'NonExistent'",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                    .ToListAsync(TestContext.Current.CancellationToken)
                    .AsTask()
            )
            .Should()
            .ThrowAsync<InvalidCastException>()
            .WithMessage(
                "The first column returned by the SQL statement contains the value 'NonExistent' "
                    + $"({typeof(string)}), which could not be converted to the type {typeof(TestEnum)}. See inner "
                    + "exception for details.*"
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_BuiltInType_EnumTargetType_ShouldConvertIntegerToEnum(bool useAsyncApi)
    {
        var enumValue = Generate.Single<TestEnum>();

        (
            await CallApi<TestEnum>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT {(int)enumValue}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ToListAsync(TestContext.Current.CancellationToken)
        )
            .Should()
            .BeEquivalentTo([enumValue]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_BuiltInType_EnumTargetType_ShouldConvertStringToEnum(bool useAsyncApi)
    {
        var enumValue = Generate.Single<TestEnum>();

        (
            await CallApi<TestEnum>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT '{enumValue.ToString()}'",
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ToListAsync(TestContext.Current.CancellationToken)
        )
            .Should()
            .BeEquivalentTo([enumValue]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Query_BuiltInType_NonNullableTargetType_ColumnContainsNull_ShouldThrow(bool useAsyncApi) =>
        Invoking(() =>
                CallApi<int>(
                        useAsyncApi,
                        this.Connection,
                        "SELECT NULL",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                    .ToListAsync(TestContext.Current.CancellationToken)
                    .AsTask()
            )
            .Should()
            .ThrowAsync<InvalidCastException>()
            .WithMessage(
                "The first column returned by the SQL statement contains a NULL value, which could not be converted "
                    + $"to the type {typeof(int)}. See inner exception for details.*"
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_BuiltInType_NullableTargetType_ColumnContainsNull_ShouldReturnNull(bool useAsyncApi) =>
        (
            await CallApi<int?>(
                    useAsyncApi,
                    this.Connection,
                    "SELECT NULL",
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ToListAsync(TestContext.Current.CancellationToken)
                .AsTask()
        )
            .Should()
            .BeEquivalentTo(new int?[] { null });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_BuiltInType_ShouldSupportDateTimeOffsetValues(bool useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entities = this.CreateEntitiesInDb<EntityWithDateTimeOffset>();

        (
            await CallApi<DateTimeOffset>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT {Q("DateTimeOffsetValue")} FROM {Q("EntityWithDateTimeOffset")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ToListAsync(TestContext.Current.CancellationToken)
        )
            .Should()
            .BeEquivalentTo(entities.Select(e => e.DateTimeOffsetValue));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_CancellationToken_ShouldCancelOperationIfCancellationIsRequested(bool useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DelayNextDbCommand = true;

        await Invoking(() =>
                CallApi<Entity>(
                        useAsyncApi,
                        this.Connection,
                        $"SELECT * FROM {Q("Entity")}",
                        cancellationToken: cancellationToken
                    )
                    .ToListAsync(cancellationToken)
                    .AsTask()
            )
            .Should()
            .ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_CommandType_ShouldUseCommandType(bool useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entities = this.CreateEntitiesInDb<Entity>();

        (
            await CallApi<Entity>(
                    useAsyncApi,
                    this.Connection,
                    "GetEntities",
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ToListAsync(TestContext.Current.CancellationToken)
        )
            .Should()
            .BeEquivalentTo(entities);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterEnumerationIsFinished(
        bool useAsyncApi
    )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(2);

        InterpolatedSqlStatement statement = $"SELECT * FROM {TemporaryTable(entities)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        var asyncEnumerator = CallApi<Entity>(
                useAsyncApi,
                this.Connection,
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .GetAsyncEnumerator();

        (await asyncEnumerator.MoveNextAsync()).Should().BeTrue();

        if (this.TestDatabaseProvider.SupportsCommandExecutionWhileDataReaderIsOpen)
        {
            this.ExistsTemporaryTableInDb(temporaryTableName).Should().BeTrue();
        }

        (await asyncEnumerator.MoveNextAsync()).Should().BeTrue();

        (await asyncEnumerator.MoveNextAsync()).Should().BeFalse();

        await asyncEnumerator.DisposeAsync();

        this.ExistsTemporaryTableInDb(temporaryTableName).Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable(
        bool useAsyncApi
    )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>();

        (
            await CallApi<Entity>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT * FROM {TemporaryTable(entities)}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ToListAsync(TestContext.Current.CancellationToken)
        )
            .Should()
            .BeEquivalentTo(entities);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_EntityType_CharEntityProperty_ColumnContainsStringWithLengthNotOne_ShouldThrow(
        bool useAsyncApi
    )
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            await Invoking(() =>
                    CallApi<Entity>(
                            useAsyncApi,
                            this.Connection,
                            $"SELECT '' AS {Q("CharValue")}",
                            cancellationToken: TestContext.Current.CancellationToken
                        )
                        .ToListAsync(TestContext.Current.CancellationToken)
                        .AsTask()
                )
                .Should()
                .ThrowAsync<InvalidCastException>()
                .WithMessage(
                    "The column 'CharValue' returned by the SQL statement contains a value that could not be "
                        + $"converted to the type {typeof(char)} of the corresponding property of the type "
                        + $"{typeof(Entity)}. See inner exception for details.*"
                )
                .WithInnerException(typeof(InvalidCastException))
                .WithMessage(
                    $"Could not convert the string '' to the type {typeof(char)}. The string must be "
                        + "exactly one character long."
                );
        }

        await Invoking(() =>
                CallApi<Entity>(
                        useAsyncApi,
                        this.Connection,
                        $"SELECT 'ab' AS {Q("CharValue")}",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                    .ToListAsync(TestContext.Current.CancellationToken)
                    .AsTask()
            )
            .Should()
            .ThrowAsync<InvalidCastException>()
            .WithMessage(
                "The column 'CharValue' returned by the SQL statement contains a value that could not be converted "
                    + $"to the type {typeof(char)} of the corresponding property of the type "
                    + $"{typeof(Entity)}. See inner exception for details.*"
            )
            .WithInnerException(typeof(InvalidCastException))
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(char)}. The string must be "
                    + "exactly one character long."
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_EntityType_CharEntityProperty_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter(
        bool useAsyncApi
    )
    {
        var character = Generate.Single<char>();

        (
            await CallApi<Entity>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT '{character}' AS {Q("CharValue")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ToListAsync(TestContext.Current.CancellationToken)
        )
            .Should()
            .BeEquivalentTo([new Entity { CharValue = character }]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Query_EntityType_ColumnDataTypeNotCompatibleWithEntityPropertyType_ShouldThrow(bool useAsyncApi) =>
        Invoking(() =>
                CallApi<Entity>(
                        useAsyncApi,
                        this.Connection,
                        $"SELECT 123 AS {Q("TimeSpanValue")}",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                    .ToListAsync(TestContext.Current.CancellationToken)
                    .AsTask()
            )
            .Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage(
                "The data type System.* of the column 'TimeSpanValue' returned by the SQL statement is not "
                    + $"compatible with the property type {typeof(TimeSpan)} of the corresponding property of the type "
                    + $"{typeof(Entity)}.*"
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_EntityType_ColumnHasNoName_ShouldThrow(bool useAsyncApi)
    {
        InterpolatedSqlStatement statement = this.TestDatabaseProvider switch
        {
            SqlServerTestDatabaseProvider => "SELECT 1",

            PostgreSqlTestDatabaseProvider or OracleTestDatabaseProvider => "SELECT 1 AS \" \"",

            _ => "SELECT 1 AS ''",
        };

        await Invoking(() =>
                CallApi<Entity>(
                        useAsyncApi,
                        this.Connection,
                        statement,
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                    .ToListAsync(TestContext.Current.CancellationToken)
                    .AsTask()
            )
            .Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage(
                "The 1st column returned by the SQL statement does not have a name. Make sure that all columns the "
                    + "statement returns have a name.*"
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_EntityType_CompatiblePrivateConstructor_ShouldUsePrivateConstructor(bool useAsyncApi)
    {
        var entities = this.CreateEntitiesInDb<Entity>();

        (
            await CallApi<EntityWithPrivateConstructor>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ToListAsync(TestContext.Current.CancellationToken)
        )
            .Should()
            .BeEquivalentTo(entities);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_EntityType_CompatiblePublicConstructor_ShouldUsePublicConstructor(bool useAsyncApi)
    {
        var entities = this.CreateEntitiesInDb<Entity>();

        (
            await CallApi<EntityWithPublicConstructor>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ToListAsync(TestContext.Current.CancellationToken)
        )
            .Should()
            .BeEquivalentTo(entities);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_EntityType_EntityTypeHasNoCorrespondingPropertyForColumn_ShouldIgnoreColumn(
        bool useAsyncApi
    )
    {
        var entities = (
            await Invoking(() =>
                    CallApi<Entity>(
                            useAsyncApi,
                            this.Connection,
                            $"SELECT 1 AS {Q("Id")}, 2 AS {Q("Int32Value")}, 3 AS {Q("NonExistent")}",
                            cancellationToken: TestContext.Current.CancellationToken
                        )
                        .ToListAsync(TestContext.Current.CancellationToken)
                        .AsTask()
                )
                .Should()
                .NotThrowAsync()
        ).Subject;

        entities.Should().BeEquivalentTo([new Entity { Id = 1, Int32Value = 2 }]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_EntityType_EntityTypeWithPropertiesWithDifferentCasing_ShouldMaterializeEntities(
        bool useAsyncApi
    )
    {
        var entities = this.CreateEntitiesInDb<Entity>();
        var entitiesWithDifferentCasingProperties = Generate.MapTo<EntityWithDifferentCasingProperties>(entities);

        (
            await CallApi<EntityWithDifferentCasingProperties>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ToListAsync(TestContext.Current.CancellationToken)
        )
            .Should()
            .BeEquivalentTo(entitiesWithDifferentCasingProperties);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_EntityType_EnumEntityProperty_ColumnContainsInvalidInteger_ShouldThrow(bool useAsyncApi) =>
        await Invoking(() =>
                CallApi<EntityWithEnumStoredAsInteger>(
                        useAsyncApi,
                        this.Connection,
                        $"SELECT 1 AS {Q("Id")}, 999 AS {Q("Enum")}",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                    .ToListAsync(TestContext.Current.CancellationToken)
                    .AsTask()
            )
            .Should()
            .ThrowAsync<InvalidCastException>()
            .WithMessage(
                "The column 'Enum' returned by the SQL statement contains a value that could not be converted to "
                    + $"the type {typeof(TestEnum)} of the corresponding property of the type "
                    + $"{typeof(EntityWithEnumStoredAsInteger)}. See inner exception for details.*"
            )
            .WithInnerException(typeof(InvalidCastException))
            .WithMessage(
                "Could not convert the value '999*' (System.*) to an enum member of the type "
                    + $"{typeof(TestEnum)}. That value does not match any of the values of the enum's members.*"
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_EntityType_EnumEntityProperty_ColumnContainsInvalidString_ShouldThrow(bool useAsyncApi) =>
        await Invoking(() =>
                CallApi<EntityWithEnumStoredAsString>(
                        useAsyncApi,
                        this.Connection,
                        $"SELECT 1 AS {Q("Id")}, 'NonExistent' AS {Q("Enum")}",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                    .ToListAsync(TestContext.Current.CancellationToken)
                    .AsTask()
            )
            .Should()
            .ThrowAsync<InvalidCastException>()
            .WithMessage(
                "The column 'Enum' returned by the SQL statement contains a value that could not be converted to "
                    + $"the type {typeof(TestEnum)} of the corresponding property of the type "
                    + $"{typeof(EntityWithEnumStoredAsString)}. See inner exception for details.*"
            )
            .WithInnerException(typeof(InvalidCastException))
            .WithMessage(
                $"Could not convert the string 'NonExistent' to an enum member of the type {typeof(TestEnum)}. "
                    + "That string does not match any of the names of the enum's members.*"
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_EntityType_EnumEntityProperty_ShouldConvertIntegerToEnum(bool useAsyncApi)
    {
        var enumValue = Generate.Single<TestEnum>();

        (
            await CallApi<EntityWithEnumStoredAsInteger>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT 1 AS {Q("Id")}, {(int)enumValue} AS {Q("Enum")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .FirstAsync()
        )
            .Enum.Should()
            .Be(enumValue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_EntityType_EnumEntityProperty_ShouldConvertStringToEnum(bool useAsyncApi)
    {
        var enumValue = Generate.Single<TestEnum>();

        (
            await CallApi<EntityWithEnumStoredAsInteger>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT 1 AS {Q("Id")}, '{enumValue.ToString()}' AS {Q("Enum")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .FirstAsync()
        )
            .Enum.Should()
            .Be(enumValue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_EntityType_Mapping_Attributes_ShouldUseAttributesMapping(bool useAsyncApi)
    {
        var entities = this.CreateEntitiesInDb<MappingTestEntityAttributes>();

        (
            await CallApi<MappingTestEntityAttributes>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT * FROM {Q("MappingTestEntity")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ToListAsync(TestContext.Current.CancellationToken)
        )
            .Should()
            .BeEquivalentTo(
                entities,
                options =>
                    options
                        .Using<string>(context => context.Subject.Should().BeNull())
                        .When(info => info.Path.EndsWith("NotMapped"))
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_EntityType_Mapping_FluentApi_ShouldUseFluentApiMapping(bool useAsyncApi)
    {
        MappingTestEntityFluentApi.Configure();

        var entities = this.CreateEntitiesInDb<MappingTestEntityFluentApi>();

        (
            await CallApi<MappingTestEntityFluentApi>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT * FROM {Q("MappingTestEntity")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ToListAsync(TestContext.Current.CancellationToken)
        )
            .Should()
            .BeEquivalentTo(
                entities,
                options =>
                    options
                        .Using<string>(context => context.Subject.Should().BeNull())
                        .When(info => info.Path.EndsWith("NotMapped"))
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_EntityType_Mapping_NoMapping_ShouldUseEntityTypeNameAndPropertyNames(bool useAsyncApi)
    {
        var entities = this.CreateEntitiesInDb<MappingTestEntity>();

        (
            await CallApi<MappingTestEntity>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT * FROM {Q("MappingTestEntity")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ToListAsync(TestContext.Current.CancellationToken)
        )
            .Should()
            .BeEquivalentTo(entities);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Query_EntityType_NoCompatibleConstructor_NoParameterlessConstructor_ShouldThrow(bool useAsyncApi) =>
        Invoking(() =>
                CallApi<EntityWithPublicConstructor>(useAsyncApi, this.Connection, $"SELECT 1 AS {Q("NonExistent")}")
                    .ToListAsync(TestContext.Current.CancellationToken)
                    .AsTask()
            )
            .Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage(
                $"Could not materialize an instance of the type {typeof(EntityWithPublicConstructor)}. The type "
                    + "either needs to have a parameterless constructor or a constructor whose parameters match the "
                    + "columns returned by the SQL statement, e.g. a constructor that has the following "
                    + $"signature:{Environment.NewLine}"
                    + "(* NonExistent).*"
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_EntityType_NoCompatibleConstructor_PrivateParameterlessConstructor_ShouldUsePrivateConstructorAndProperties(
        bool useAsyncApi
    )
    {
        var entities = this.CreateEntitiesInDb<Entity>();

        (
            await CallApi<EntityWithPrivateParameterlessConstructor>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ToListAsync(TestContext.Current.CancellationToken)
        )
            .Should()
            .BeEquivalentTo(entities);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_EntityType_NoCompatibleConstructor_PublicParameterlessConstructor_ShouldUsePublicConstructorAndProperties(
        bool useAsyncApi
    )
    {
        var entities = this.CreateEntitiesInDb<Entity>();

        (
            await CallApi<Entity>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ToListAsync(TestContext.Current.CancellationToken)
        )
            .Should()
            .BeEquivalentTo(entities);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Query_EntityType_NonNullableEntityProperty_ColumnContainsNull_ShouldThrow(bool useAsyncApi)
    {
        this.Connection.ExecuteNonQuery($"INSERT INTO {Q("Entity")} ({Q("Id")}, {Q("BooleanValue")}) VALUES(1, NULL)");

        return Invoking(() =>
                CallApi<Entity>(
                        useAsyncApi,
                        this.Connection,
                        $"SELECT * FROM {Q("Entity")}",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                    .ToListAsync(TestContext.Current.CancellationToken)
                    .AsTask()
            )
            .Should()
            .ThrowAsync<InvalidCastException>()
            .WithMessage(
                "The column 'BooleanValue' returned by the SQL statement contains a NULL value, but the "
                    + $"corresponding property of the type {typeof(Entity)} is non-nullable.*"
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_EntityType_NullableEntityProperty_ColumnContainsNull_ShouldReturnNull(bool useAsyncApi)
    {
        await this.Connection.ExecuteNonQueryAsync(
            $"INSERT INTO {Q("Entity")} ({Q("Id")}, {Q("NullableBooleanValue")}) VALUES(1, NULL)"
        );

        (
            await CallApi<Entity>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT {Q("Id")}, {Q("NullableBooleanValue")} FROM {Q("Entity")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ToListAsync(TestContext.Current.CancellationToken)
        )
            .Should()
            .BeEquivalentTo([new Entity { Id = 1, NullableBooleanValue = null }]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_EntityType_ShouldSupportDateTimeOffsetValues(bool useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entities = this.CreateEntitiesInDb<EntityWithDateTimeOffset>();

        (
            await CallApi<EntityWithDateTimeOffset>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT * FROM {Q("EntityWithDateTimeOffset")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ToListAsync(TestContext.Current.CancellationToken)
        )
            .Should()
            .BeEquivalentTo(entities);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Query_EntityType_UnsupportedFieldType_ShouldThrow(bool useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.HasUnsupportedDataType, "");

        var literal = this.TestDatabaseProvider.GetUnsupportedDataTypeLiteral();

        return Invoking(() =>
                CallApi<EntityWithObjectProperty>(
                        useAsyncApi,
                        this.Connection,
                        $"SELECT {literal} AS {Q("Value")}",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                    .ToListAsync(TestContext.Current.CancellationToken)
                    .AsTask()
            )
            .Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage(
                "The data type System.* of the column 'Value' returned by the SQL statement is not supported.*"
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_InterpolatedParameter_ShouldPassInterpolatedParameter(bool useAsyncApi)
    {
        var entity = this.CreateEntityInDb<Entity>();

        (
            await CallApi<Entity>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ToListAsync(TestContext.Current.CancellationToken)
        )
            .Should()
            .BeEquivalentTo([entity]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_Parameter_ShouldPassParameter(bool useAsyncApi)
    {
        var entity = this.CreateEntityInDb<Entity>();

        var statement = new InterpolatedSqlStatement(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entity.Id)
        );

        (
            await CallApi<Entity>(
                    useAsyncApi,
                    this.Connection,
                    statement,
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ToListAsync(TestContext.Current.CancellationToken)
        )
            .Should()
            .BeEquivalentTo([entity]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterEnumerationIsFinished(
        bool useAsyncApi
    )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        InterpolatedSqlStatement statement = $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable(entityIds)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        var asyncEnumerator = CallApi<Entity>(
                useAsyncApi,
                this.Connection,
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .GetAsyncEnumerator();

        (await asyncEnumerator.MoveNextAsync()).Should().BeTrue();

        if (this.TestDatabaseProvider.SupportsCommandExecutionWhileDataReaderIsOpen)
        {
            this.ExistsTemporaryTableInDb(temporaryTableName).Should().BeTrue();
        }

        (await asyncEnumerator.MoveNextAsync()).Should().BeTrue();

        (await asyncEnumerator.MoveNextAsync()).Should().BeFalse();

        await asyncEnumerator.DisposeAsync();

        this.ExistsTemporaryTableInDb(temporaryTableName).Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable(
        bool useAsyncApi
    )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = this.CreateEntitiesInDb<Entity>(5);
        var entityIds = entities.Take(2).Select(a => a.Id).ToList();

        (
            await CallApi<Entity>(
                    useAsyncApi,
                    this.Connection,
                    $"""
                    SELECT *
                    FROM   {Q("Entity")}
                    WHERE  {Q("Id")} IN (SELECT {Q("Value")} FROM {TemporaryTable(entityIds)})
                    """,
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ToListAsync(TestContext.Current.CancellationToken)
        ).Should().BeEquivalentTo(entities.Take(2));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_Transaction_ShouldUseTransaction(bool useAsyncApi)
    {
        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            var entities = this.CreateEntitiesInDb<Entity>(null, transaction);

            (
                await CallApi<Entity>(
                        useAsyncApi,
                        this.Connection,
                        $"SELECT * FROM {Q("Entity")}",
                        transaction,
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                    .ToListAsync(TestContext.Current.CancellationToken)
            )
                .Should()
                .BeEquivalentTo(entities);

            await transaction.RollbackAsync();
        }

        (
            await CallApi<Entity>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ToListAsync(TestContext.Current.CancellationToken)
        )
            .Should()
            .BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_ValueTupleType_CharValueTupleField_ColumnContainsStringWithLengthNotOne_ShouldThrow(
        bool useAsyncApi
    )
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            await Invoking(() =>
                    CallApi<ValueTuple<char>>(
                            useAsyncApi,
                            this.Connection,
                            $"SELECT '' AS {Q("Value")}",
                            cancellationToken: TestContext.Current.CancellationToken
                        )
                        .ToListAsync(TestContext.Current.CancellationToken)
                        .AsTask()
                )
                .Should()
                .ThrowAsync<InvalidCastException>()
                .WithMessage(
                    "The column 'Value' returned by the SQL statement contains a value that could not be converted "
                        + $"to the type {typeof(char)} of the corresponding field of the value tuple type "
                        + $"{typeof(ValueTuple<char>)}. See inner exception for details.*"
                )
                .WithInnerException(typeof(InvalidCastException))
                .WithMessage(
                    $"Could not convert the string '' to the type {typeof(char)}. The string must be "
                        + "exactly one character long."
                );
        }

        await Invoking(() =>
                CallApi<ValueTuple<char>>(
                        useAsyncApi,
                        this.Connection,
                        $"SELECT 'ab' AS {Q("Value")}",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                    .ToListAsync(TestContext.Current.CancellationToken)
                    .AsTask()
            )
            .Should()
            .ThrowAsync<InvalidCastException>()
            .WithMessage(
                "The column 'Value' returned by the SQL statement contains a value that could not be converted "
                    + $"to the type {typeof(char)} of the corresponding field of the value tuple type "
                    + $"{typeof(ValueTuple<char>)}. See inner exception for details.*"
            )
            .WithInnerException(typeof(InvalidCastException))
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(char)}. The string must be "
                    + "exactly one character long."
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_ValueTupleType_CharValueTupleField_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter(
        bool useAsyncApi
    )
    {
        var character = Generate.Single<char>();

        (
            await CallApi<ValueTuple<char>>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT '{character}'",
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ToListAsync(TestContext.Current.CancellationToken)
        )
            .Should()
            .BeEquivalentTo([ValueTuple.Create(character)]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Query_ValueTupleType_ColumnDataTypeNotCompatibleWithValueTupleFieldType_ShouldThrow(bool useAsyncApi) =>
        Invoking(() =>
                CallApi<ValueTuple<TimeSpan>>(
                        useAsyncApi,
                        this.Connection,
                        $"SELECT 123 AS {Q("Value")}",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                    .ToListAsync(TestContext.Current.CancellationToken)
                    .AsTask()
            )
            .Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage(
                "The data type System.* of the column 'Value' returned by the SQL statement is not compatible with "
                    + $"the field type {typeof(TimeSpan)} of the corresponding field of the value tuple type "
                    + $"{typeof(ValueTuple<TimeSpan>)}.*"
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Query_ValueTupleType_EnumValueTupleField_ColumnContainsInvalidInteger_ShouldThrow(bool useAsyncApi) =>
        Invoking(() =>
                CallApi<ValueTuple<TestEnum>>(
                        useAsyncApi,
                        this.Connection,
                        $"SELECT 999 AS {Q("Value")}",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                    .ToListAsync(TestContext.Current.CancellationToken)
                    .AsTask()
            )
            .Should()
            .ThrowAsync<InvalidCastException>()
            .WithMessage(
                "The column 'Value' returned by the SQL statement contains a value that could not be converted to "
                    + $"the type {typeof(TestEnum)} of the corresponding field of the value tuple type "
                    + $"{typeof(ValueTuple<TestEnum>)}. See inner exception for details.*"
            )
            .WithInnerException(typeof(InvalidCastException))
            .WithMessage(
                "Could not convert the value '999*' (System.*) to an enum member of the type "
                    + $"{typeof(TestEnum)}. That value does not match any of the values of the enum's members.*"
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Query_ValueTupleType_EnumValueTupleField_ColumnContainsInvalidString_ShouldThrow(bool useAsyncApi) =>
        Invoking(() =>
                CallApi<ValueTuple<TestEnum>>(
                        useAsyncApi,
                        this.Connection,
                        $"SELECT 'NonExistent' AS {Q("Value")}",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                    .ToListAsync(TestContext.Current.CancellationToken)
                    .AsTask()
            )
            .Should()
            .ThrowAsync<InvalidCastException>()
            .WithMessage(
                "The column 'Value' returned by the SQL statement contains a value that could not be converted to "
                    + $"the type {typeof(TestEnum)} of the corresponding field of the value tuple type "
                    + $"{typeof(ValueTuple<TestEnum>)}. See inner exception for details.*"
            )
            .WithInnerException(typeof(InvalidCastException))
            .WithMessage(
                $"Could not convert the string 'NonExistent' to an enum member of the type {typeof(TestEnum)}. "
                    + "That string does not match any of the names of the enum's members.*"
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_ValueTupleType_EnumValueTupleField_ShouldConvertIntegerToEnum(bool useAsyncApi)
    {
        var enumValue = Generate.Single<TestEnum>();

        (
            await CallApi<ValueTuple<TestEnum>>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT {(int)enumValue}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ToListAsync(TestContext.Current.CancellationToken)
        )
            .Should()
            .BeEquivalentTo([ValueTuple.Create(enumValue)]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_ValueTupleType_EnumValueTupleField_ShouldConvertStringToEnum(bool useAsyncApi)
    {
        var enumValue = Generate.Single<TestEnum>();

        (
            await CallApi<ValueTuple<TestEnum>>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT '{enumValue}'",
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ToListAsync(TestContext.Current.CancellationToken)
        )
            .Should()
            .BeEquivalentTo([ValueTuple.Create(enumValue)]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Query_ValueTupleType_NonNullableValueTupleField_ColumnContainsNull_ShouldThrow(bool useAsyncApi)
    {
        this.Connection.ExecuteNonQuery($"INSERT INTO {Q("Entity")} ({Q("Id")}, {Q("BooleanValue")}) VALUES(1, NULL)");

        return Invoking(() =>
                CallApi<ValueTuple<bool>>(
                        useAsyncApi,
                        this.Connection,
                        $"SELECT {Q("BooleanValue")} FROM {Q("Entity")}",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                    .ToListAsync(TestContext.Current.CancellationToken)
                    .AsTask()
            )
            .Should()
            .ThrowAsync<InvalidCastException>()
            .WithMessage(
                "The column 'BooleanValue' returned by the SQL statement contains a NULL value, but the "
                    + $"corresponding field of the value tuple type {typeof(ValueTuple<bool>)} is non-nullable.*"
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_ValueTupleType_NullableValueTupleField_ColumnContainsNull_ShouldReturnNull(bool useAsyncApi)
    {
        await this.Connection.ExecuteNonQueryAsync(
            $"INSERT INTO {Q("Entity")} ({Q("Id")}, {Q("NullableBooleanValue")}) VALUES(1, NULL)"
        );

        (
            await CallApi<ValueTuple<bool?>>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT {Q("NullableBooleanValue")} FROM {Q("Entity")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ToListAsync(TestContext.Current.CancellationToken)
        )
            .Should()
            .BeEquivalentTo([new ValueTuple<bool?>(null)]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Query_ValueTupleType_NumberOfColumnsDoesNotMatchNumberOfValueTupleFields_ShouldThrow(
        bool useAsyncApi
    ) =>
        Invoking(() =>
                CallApi<(int, int)>(
                        useAsyncApi,
                        this.Connection,
                        "SELECT 1",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                    .ToListAsync(TestContext.Current.CancellationToken)
                    .AsTask()
            )
            .Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage(
                $"The SQL statement returned 1 column, but the value tuple type {typeof((int, int))} has 2 "
                    + "fields. Make sure that the SQL statement returns the same number of columns as the number of "
                    + "fields in the value tuple type.*"
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_ValueTupleType_ShouldMaterializeBinaryData(bool useAsyncApi)
    {
        var bytes = Generate.Single<byte[]>();

        (
            await CallApi<ValueTuple<byte[]>>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT {Parameter(bytes)} AS BinaryData",
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ToListAsync(TestContext.Current.CancellationToken)
        )
            .Should()
            .BeEquivalentTo([ValueTuple.Create(bytes)]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_ValueTupleType_ShouldSupportDateTimeOffsetValues(bool useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entities = this.CreateEntitiesInDb<EntityWithDateTimeOffset>();

        (
            await CallApi<(long Id, DateTimeOffset DateTimeOffsetValue)>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT {Q("Id")}, {Q("DateTimeOffsetValue")} FROM {Q("EntityWithDateTimeOffset")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ToListAsync(TestContext.Current.CancellationToken)
        )
            .Should()
            .BeEquivalentTo(entities.Select(e => (e.Id, e.DateTimeOffsetValue)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Query_ValueTupleType_UnsupportedFieldType_ShouldThrow(bool useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.HasUnsupportedDataType, "");

        var literal = this.TestDatabaseProvider.GetUnsupportedDataTypeLiteral();

        return Invoking(() =>
                CallApi<ValueTuple<object>>(
                        useAsyncApi,
                        this.Connection,
                        $"SELECT {literal} AS {Q("Value")}",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                    .ToListAsync(TestContext.Current.CancellationToken)
                    .AsTask()
            )
            .Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage(
                "The data type System.* of the column 'Value' returned by the SQL statement is not supported.*"
            );
    }

    private static IAsyncEnumerable<T> CallApi<T>(
        bool useAsyncApi,
        DbConnection connection,
        InterpolatedSqlStatement statement,
        DbTransaction? transaction = null,
        TimeSpan? commandTimeout = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default
    )
    {
        if (useAsyncApi)
        {
            return connection.QueryAsync<T>(statement, transaction, commandTimeout, commandType, cancellationToken);
        }

        return connection
            .Query<T>(statement, transaction, commandTimeout, commandType, cancellationToken)
            .ToAsyncEnumerable();
    }
}
