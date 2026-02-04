using System.Data.Common;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests;

public sealed class
    DbConnectionExtensions_QuerySingleOrDefaultOfTTests_MySql :
    DbConnectionExtensions_QuerySingleOrDefaultOfTTests<MySqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QuerySingleOrDefaultOfTTests_Oracle :
    DbConnectionExtensions_QuerySingleOrDefaultOfTTests<OracleTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QuerySingleOrDefaultOfTTests_PostgreSql :
    DbConnectionExtensions_QuerySingleOrDefaultOfTTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QuerySingleOrDefaultOfTTests_Sqlite :
    DbConnectionExtensions_QuerySingleOrDefaultOfTTests<SqliteTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QuerySingleOrDefaultOfTTests_SqlServer :
    DbConnectionExtensions_QuerySingleOrDefaultOfTTests<SqlServerTestDatabaseProvider>;

public abstract class
    DbConnectionExtensions_QuerySingleOrDefaultOfTTests<TTestDatabaseProvider> : IntegrationTestsBase<
    TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        QuerySingleOrDefault_BuiltInType_CharTargetType_ColumnContainsStringWithLengthNotOne_ShouldThrow(
            Boolean useAsyncApi
        )
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            (await Invoking(() =>
                        CallApi<Char>(
                            useAsyncApi,
                            this.Connection,
                            "SELECT ''",
                            cancellationToken: TestContext.Current.CancellationToken
                        )
                    )
                    .Should().ThrowAsync<InvalidCastException>()
                    .WithMessage(
                        $"The first column returned by the SQL statement contains the value '' ({typeof(String)}), " +
                        $"which could not be converted to the type {typeof(Char)}. See inner exception for details.*"
                    ))
                .WithInnerException<InvalidCastException>()
                .WithMessage(
                    $"Could not convert the string '' to the type {typeof(Char)}. The string must be exactly " +
                    "one character long."
                );
        }

        (await Invoking(() =>
                    CallApi<Char>(
                        useAsyncApi,
                        this.Connection,
                        "SELECT 'ab'",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                )
                .Should().ThrowAsync<InvalidCastException>()
                .WithMessage(
                    $"The first column returned by the SQL statement contains the value 'ab' ({typeof(String)}), " +
                    $"which could not be converted to the type {typeof(Char)}. See inner exception for details.*"
                ))
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(Char)}. The string must be exactly " +
                "one character long."
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        QuerySingleOrDefault_BuiltInType_CharTargetType_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter(
            Boolean useAsyncApi
        )
    {
        var character = Generate.Single<Char>();

        (await CallApi<Char>(
                useAsyncApi,
                this.Connection,
                $"SELECT '{character}'",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(character);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task QuerySingleOrDefault_BuiltInType_ColumnValueCannotBeConvertedToTargetType_ShouldThrow(
        Boolean useAsyncApi
    ) =>
        Invoking(() =>
                CallApi<Int32>(
                    useAsyncApi,
                    this.Connection,
                    "SELECT 'A'",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                $"The first column returned by the SQL statement contains the value 'A' ({typeof(String)}), which " +
                $"could not be converted to the type {typeof(Int32)}. See inner exception for details.*"
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task QuerySingleOrDefault_BuiltInType_EnumTargetType_ColumnContainsInvalidInteger_ShouldThrow(
        Boolean useAsyncApi
    ) =>
        Invoking(() =>
                CallApi<TestEnum>(
                    useAsyncApi,
                    this.Connection,
                    "SELECT 999",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                "The first column returned by the SQL statement contains the value '999*' (System.*), which " +
                $"could not be converted to the type {typeof(TestEnum)}. See inner exception for details.*"
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task QuerySingleOrDefault_BuiltInType_EnumTargetType_ColumnContainsInvalidString_ShouldThrow(
        Boolean useAsyncApi
    ) =>
        Invoking(() =>
                CallApi<TestEnum>(
                    useAsyncApi,
                    this.Connection,
                    "SELECT 'NonExistent'",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                "The first column returned by the SQL statement contains the value 'NonExistent' " +
                $"({typeof(String)}), which could not be converted to the type {typeof(TestEnum)}. See inner " +
                "exception for details.*"
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingleOrDefault_BuiltInType_EnumTargetType_ShouldConvertIntegerToEnum(Boolean useAsyncApi)
    {
        var enumValue = Generate.Single<TestEnum>();

        (await CallApi<TestEnum>(
                useAsyncApi,
                this.Connection,
                $"SELECT {(Int32)enumValue}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(enumValue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingleOrDefault_BuiltInType_EnumTargetType_ShouldConvertStringToEnum(Boolean useAsyncApi)
    {
        var enumValue = Generate.Single<TestEnum>();

        (await CallApi<TestEnum>(
                useAsyncApi,
                this.Connection,
                $"SELECT '{enumValue.ToString()}'",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(enumValue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task QuerySingleOrDefault_BuiltInType_NonNullableTargetType_ColumnContainsNull_ShouldThrow(
        Boolean useAsyncApi
    ) =>
        Invoking(() =>
                CallApi<Int32>(
                    useAsyncApi,
                    this.Connection,
                    "SELECT NULL",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                "The first column returned by the SQL statement contains a NULL value, which could not be converted " +
                $"to the type {typeof(Int32)}. See inner exception for details.*"
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingleOrDefault_BuiltInType_NullableTargetType_ColumnContainsNull_ShouldReturnNull(
        Boolean useAsyncApi
    ) =>
        (await CallApi<Int32?>(
            useAsyncApi,
            this.Connection,
            "SELECT NULL",
            cancellationToken: TestContext.Current.CancellationToken
        ))
        .Should().BeNull();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingleOrDefault_BuiltInType_ShouldSupportDateTimeOffsetValues(Boolean useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entity = this.CreateEntityInDb<EntityWithDateTimeOffset>();

        (await CallApi<DateTimeOffset>(
                useAsyncApi,
                this.Connection,
                $"SELECT {Q("DateTimeOffsetValue")} FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entity.DateTimeOffsetValue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingleOrDefault_CancellationToken_ShouldCancelOperationIfCancellationIsRequested(
        Boolean useAsyncApi
    )
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        await Invoking(() =>
                CallApi<Entity>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: cancellationToken
                )
            )
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingleOrDefault_CommandType_ShouldUseCommandType(Boolean useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entity = this.CreateEntityInDb<Entity>();

        (await CallApi<Entity>(
                useAsyncApi,
                this.Connection,
                "GetFirstEntity",
                commandType: CommandType.StoredProcedure,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        QuerySingleOrDefault_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterExecution(Boolean useAsyncApi)
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entity = Generate.Single<Entity>();

        InterpolatedSqlStatement statement = $"SELECT * FROM {TemporaryTable([entity])}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        (await CallApi<Entity>(
                useAsyncApi,
                this.Connection,
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entity);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        QuerySingleOrDefault_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable(
            Boolean useAsyncApi
        )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entity = Generate.Single<Entity>();

        (await CallApi<Entity>(
                useAsyncApi,
                this.Connection,
                $"SELECT * FROM {TemporaryTable([entity])}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        QuerySingleOrDefault_EntityType_CharEntityProperty_ColumnContainsStringWithLengthNotOne_ShouldThrow(
            Boolean useAsyncApi
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
                )
                .Should().ThrowAsync<InvalidCastException>()
                .WithMessage(
                    "The column 'CharValue' returned by the SQL statement contains a value that could not be " +
                    $"converted to the type {typeof(Char)} of the corresponding property of the type " +
                    $"{typeof(Entity)}. See inner exception for details.*"
                )
                .WithInnerException(typeof(InvalidCastException))
                .WithMessage(
                    $"Could not convert the string '' to the type {typeof(Char)}. The string must be " +
                    "exactly one character long."
                );
        }

        await Invoking(() =>
                CallApi<Entity>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT 'ab' AS {Q("CharValue")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                "The column 'CharValue' returned by the SQL statement contains a value that could not be converted " +
                $"to the type {typeof(Char)} of the corresponding property of the type " +
                $"{typeof(Entity)}. See inner exception for details.*"
            )
            .WithInnerException(typeof(InvalidCastException))
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(Char)}. The string must be " +
                "exactly one character long."
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        QuerySingleOrDefault_EntityType_CharEntityProperty_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter(
            Boolean useAsyncApi
        )
    {
        var character = Generate.Single<Char>();

        (await CallApi<Entity>(
                useAsyncApi,
                this.Connection,
                $"SELECT '{character}' AS {Q("CharValue")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(new Entity { CharValue = character });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task QuerySingleOrDefault_EntityType_ColumnDataTypeNotCompatibleWithEntityPropertyType_ShouldThrow(
        Boolean useAsyncApi
    ) =>
        Invoking(() =>
                CallApi<Entity>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT 123 AS {Q("TimeSpanValue")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage(
                "The data type System.* of the column 'TimeSpanValue' returned by the SQL statement is not " +
                $"compatible with the property type {typeof(TimeSpan)} of the corresponding property of the type " +
                $"{typeof(Entity)}.*"
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingleOrDefault_EntityType_ColumnHasNoName_ShouldThrow(Boolean useAsyncApi)
    {
        InterpolatedSqlStatement statement = this.TestDatabaseProvider switch
        {
            SqlServerTestDatabaseProvider =>
                "SELECT 1",

            PostgreSqlTestDatabaseProvider or OracleTestDatabaseProvider =>
                "SELECT 1 AS \" \"",

            _ =>
                "SELECT 1 AS ''"
        };

        await Invoking(() =>
                CallApi<Entity>(
                    useAsyncApi,
                    this.Connection,
                    statement,
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage(
                "The 1st column returned by the SQL statement does not have a name. Make sure that all columns the " +
                "statement returns have a name.*"
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingleOrDefault_EntityType_CompatiblePrivateConstructor_ShouldUsePrivateConstructor(
        Boolean useAsyncApi
    )
    {
        var entity = this.CreateEntityInDb<Entity>();

        (await CallApi<EntityWithPrivateConstructor>(
                useAsyncApi,
                this.Connection,
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingleOrDefault_EntityType_CompatiblePublicConstructor_ShouldUsePublicConstructor(
        Boolean useAsyncApi
    )
    {
        var entity = this.CreateEntityInDb<Entity>();

        (await CallApi<EntityWithPublicConstructor>(
                useAsyncApi,
                this.Connection,
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        QuerySingleOrDefault_EntityType_EntityTypeHasNoCorrespondingPropertyForColumn_ShouldIgnoreColumn(
            Boolean useAsyncApi
        )
    {
        var entity = (await Invoking(() =>
                CallApi<Entity>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT 1 AS {Q("Id")}, 2 AS {Q("Int32Value")}, 3 AS {Q("NonExistent")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().NotThrowAsync()).Subject;

        entity
            .Should().BeEquivalentTo(new Entity { Id = 1, Int32Value = 2 });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        QuerySingleOrDefault_EntityType_EntityTypeWithPropertiesWithDifferentCasing_ShouldMaterializeEntities(
            Boolean useAsyncApi
        )
    {
        var entity = this.CreateEntityInDb<Entity>();
        var entityWithDifferentCasingProperties = Generate.MapTo<EntityWithDifferentCasingProperties>(entity);

        (await CallApi<EntityWithDifferentCasingProperties>(
                useAsyncApi,
                this.Connection,
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entityWithDifferentCasingProperties);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        QuerySingleOrDefault_EntityType_EnumEntityProperty_ColumnContainsInvalidInteger_ShouldThrow(
            Boolean useAsyncApi
        ) =>
        await Invoking(() => CallApi<EntityWithEnumStoredAsInteger>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT 1 AS {Q("Id")}, 999 AS {Q("Enum")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                "The column 'Enum' returned by the SQL statement contains a value that could not be converted to " +
                $"the type {typeof(TestEnum)} of the corresponding property of the type " +
                $"{typeof(EntityWithEnumStoredAsInteger)}. See inner exception for details.*"
            )
            .WithInnerException(typeof(InvalidCastException))
            .WithMessage(
                "Could not convert the value '999*' (System.*) to an enum member of the type " +
                $"{typeof(TestEnum)}. That value does not match any of the values of the enum's members.*"
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        QuerySingleOrDefault_EntityType_EnumEntityProperty_ColumnContainsInvalidString_ShouldThrow(
            Boolean useAsyncApi
        ) =>
        await Invoking(() => CallApi<EntityWithEnumStoredAsString>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT 1 AS {Q("Id")}, 'NonExistent' AS {Q("Enum")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                "The column 'Enum' returned by the SQL statement contains a value that could not be converted to " +
                $"the type {typeof(TestEnum)} of the corresponding property of the type " +
                $"{typeof(EntityWithEnumStoredAsString)}. See inner exception for details.*"
            )
            .WithInnerException(typeof(InvalidCastException))
            .WithMessage(
                $"Could not convert the string 'NonExistent' to an enum member of the type {typeof(TestEnum)}. " +
                "That string does not match any of the names of the enum's members.*"
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingleOrDefault_EntityType_EnumEntityProperty_ShouldConvertIntegerToEnum(Boolean useAsyncApi)
    {
        var enumValue = Generate.Single<TestEnum>();

        (await CallApi<EntityWithEnumStoredAsInteger>(
                useAsyncApi,
                this.Connection,
                $"SELECT 1 AS {Q("Id")}, {(Int32)enumValue} AS {Q("Enum")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))!
            .Enum
            .Should().Be(enumValue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingleOrDefault_EntityType_EnumEntityProperty_ShouldConvertStringToEnum(Boolean useAsyncApi)
    {
        var enumValue = Generate.Single<TestEnum>();

        (await CallApi<EntityWithEnumStoredAsInteger>(
                useAsyncApi,
                this.Connection,
                $"SELECT 1 AS {Q("Id")}, '{enumValue.ToString()}' AS {Q("Enum")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))!
            .Enum
            .Should().Be(enumValue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingleOrDefault_EntityType_Mapping_Attributes_ShouldUseAttributesMapping(Boolean useAsyncApi)
    {
        var entity = this.CreateEntityInDb<MappingTestEntityAttributes>();

        (await CallApi<MappingTestEntityAttributes>(
                useAsyncApi,
                this.Connection,
                $"SELECT * FROM {Q("MappingTestEntity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(
                entity,
                options => options.Using<String>(context => context.Subject.Should().BeNull())
                    .When(info => info.Path.EndsWith("NotMapped"))
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingleOrDefault_EntityType_Mapping_FluentApi_ShouldUseFluentApiMapping(Boolean useAsyncApi)
    {
        MappingTestEntityFluentApi.Configure();

        var entity = this.CreateEntityInDb<MappingTestEntityFluentApi>();

        (await CallApi<MappingTestEntityFluentApi>(
                useAsyncApi,
                this.Connection,
                $"SELECT * FROM {Q("MappingTestEntity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(
                entity,
                options => options.Using<String>(context => context.Subject.Should().BeNull())
                    .When(info => info.Path.EndsWith("NotMapped"))
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task QuerySingleOrDefault_EntityType_NoCompatibleConstructor_NoParameterlessConstructor_ShouldThrow(
        Boolean useAsyncApi
    ) =>
        Invoking(() =>
                CallApi<EntityWithPublicConstructor>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT 1 AS {Q("NonExistent")}"
                )
            )
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage(
                $"Could not materialize an instance of the type {typeof(EntityWithPublicConstructor)}. The type " +
                "either needs to have a parameterless constructor or a constructor whose parameters match the " +
                "columns returned by the SQL statement, e.g. a constructor that has the following " +
                $"signature:{Environment.NewLine}" +
                "(* NonExistent).*"
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        QuerySingleOrDefault_EntityType_NoCompatibleConstructor_PrivateParameterlessConstructor_ShouldUsePrivateConstructorAndProperties(
            Boolean useAsyncApi
        )
    {
        var entity = this.CreateEntityInDb<Entity>();

        (await CallApi<EntityWithPrivateParameterlessConstructor>(
                useAsyncApi,
                this.Connection,
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        QuerySingleOrDefault_EntityType_NoCompatibleConstructor_PublicParameterlessConstructor_ShouldUsePublicConstructorAndProperties(
            Boolean useAsyncApi
        )
    {
        var entity = this.CreateEntityInDb<Entity>();

        (await CallApi<Entity>(
                useAsyncApi,
                this.Connection,
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingleOrDefault_EntityType_NoMapping_ShouldUseEntityTypeNameAndPropertyNames(
        Boolean useAsyncApi
    )
    {
        var entity = this.CreateEntityInDb<MappingTestEntity>();

        (await CallApi<MappingTestEntity>(
                useAsyncApi,
                this.Connection,
                $"SELECT * FROM {Q("MappingTestEntity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task QuerySingleOrDefault_EntityType_NonNullableEntityProperty_ColumnContainsNull_ShouldThrow(
        Boolean useAsyncApi
    )
    {
        this.Connection.ExecuteNonQuery(
            $"INSERT INTO {Q("Entity")} ({Q("Id")}, {Q("BooleanValue")}) VALUES(1, NULL)"
        );

        return Invoking(() =>
                CallApi<Entity>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                "The column 'BooleanValue' returned by the SQL statement contains a NULL value, but the corresponding " +
                $"property of the type {typeof(Entity)} is non-nullable.*"
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingleOrDefault_EntityType_NullableEntityProperty_ColumnContainsNull_ShouldReturnNull(
        Boolean useAsyncApi
    )
    {
        await this.Connection.ExecuteNonQueryAsync(
            $"INSERT INTO {Q("EntityWithNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        (await CallApi<EntityWithNullableProperty>(
                useAsyncApi,
                this.Connection,
                $"SELECT * FROM {Q("EntityWithNullableProperty")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(new EntityWithNullableProperty { Id = 1, Value = null });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingleOrDefault_EntityType_ShouldSupportDateTimeOffsetValues(Boolean useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entity = this.CreateEntityInDb<EntityWithDateTimeOffset>();

        (await CallApi<EntityWithDateTimeOffset>(
                useAsyncApi,
                this.Connection,
                $"SELECT * FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task QuerySingleOrDefault_EntityType_UnsupportedFieldType_ShouldThrow(Boolean useAsyncApi)
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
            )
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage(
                "The data type System.* of the column 'Value' returned by the SQL statement is not supported.*"
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingleOrDefault_InterpolatedParameter_ShouldPassInterpolatedParameter(Boolean useAsyncApi)
    {
        var entity = this.CreateEntityInDb<Entity>();

        (await CallApi<Entity>(
                useAsyncApi,
                this.Connection,
                $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingleOrDefault_Parameter_ShouldPassParameter(Boolean useAsyncApi)
    {
        var entity = this.CreateEntityInDb<Entity>();

        var statement = new InterpolatedSqlStatement(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entity.Id)
        );

        (await CallApi<Entity>(
                useAsyncApi,
                this.Connection,
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingleOrDefault_QueryReturnedMoreThanOneRow_ShouldThrow(Boolean useAsyncApi)
    {
        this.CreateEntitiesInDb<Entity>(2);

        await Invoking(() => CallApi<Entity>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(
                "The SQL statement did return more than one row."
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingleOrDefault_QueryReturnedNoRows_ShouldThrow(Boolean useAsyncApi)
    {
        (await CallApi<Int32>(
                useAsyncApi,
                this.Connection,
                $"SELECT {Q("Id")} FROM {Q("Entity")} WHERE {Q("Id")} = -1",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(0);

        (await CallApi<Entity>(
                useAsyncApi,
                this.Connection,
                $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = -1",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeNull();

        (await CallApi<(Int64, String)>(
                useAsyncApi,
                this.Connection,
                $"SELECT {Q("Id")}, {Q("StringValue")} FROM {Q("Entity")} WHERE {Q("Id")} = -1",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(default);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        QuerySingleOrDefault_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterExecution(Boolean useAsyncApi)
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityId = Generate.Id();

        InterpolatedSqlStatement statement =
            $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable([entityId])}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        (await CallApi<Int64>(
                useAsyncApi,
                this.Connection,
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entityId);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        QuerySingleOrDefault_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable(
            Boolean useAsyncApi
        )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entity = this.CreateEntityInDb<Entity>();
        var entityId = entity.Id;

        (await CallApi<Entity>(
                useAsyncApi,
                this.Connection,
                $"""
                 SELECT *
                 FROM   {Q("Entity")}
                 WHERE  {Q("Id")} IN (SELECT {Q("Value")} FROM {TemporaryTable([entityId])})
                 """,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingleOrDefault_Transaction_ShouldUseTransaction(Boolean useAsyncApi)
    {
        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            var entity = this.CreateEntityInDb<Entity>(transaction);

            (await CallApi<Entity>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT * FROM {Q("Entity")}",
                    transaction,
                    cancellationToken: TestContext.Current.CancellationToken
                ))
                .Should().BeEquivalentTo(entity);

            await transaction.RollbackAsync();
        }

        (await CallApi<Entity>(
                useAsyncApi,
                this.Connection,
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        QuerySingleOrDefault_ValueTupleType_CharValueTupleField_ColumnContainsStringWithLengthNotOne_ShouldThrow(
            Boolean useAsyncApi
        )
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            await Invoking(() =>
                    CallApi<ValueTuple<Char>>(
                        useAsyncApi,
                        this.Connection,
                        $"SELECT '' AS {Q("Value")}",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                )
                .Should().ThrowAsync<InvalidCastException>()
                .WithMessage(
                    "The column 'Value' returned by the SQL statement contains a value that could not be converted " +
                    $"to the type {typeof(Char)} of the corresponding field of the value tuple type " +
                    $"{typeof(ValueTuple<Char>)}. See inner exception for details.*"
                )
                .WithInnerException(typeof(InvalidCastException))
                .WithMessage(
                    $"Could not convert the string '' to the type {typeof(Char)}. The string must be " +
                    "exactly one character long."
                );
        }

        await Invoking(() =>
                CallApi<ValueTuple<Char>>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT 'ab' AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                "The column 'Value' returned by the SQL statement contains a value that could not be converted " +
                $"to the type {typeof(Char)} of the corresponding field of the value tuple type " +
                $"{typeof(ValueTuple<Char>)}. See inner exception for details.*"
            )
            .WithInnerException(typeof(InvalidCastException))
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(Char)}. The string must be " +
                "exactly one character long."
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        QuerySingleOrDefault_ValueTupleType_CharValueTupleField_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter(
            Boolean useAsyncApi
        )
    {
        var character = Generate.Single<Char>();

        (await CallApi<ValueTuple<Char>>(
                useAsyncApi,
                this.Connection,
                $"SELECT '{character}'",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(ValueTuple.Create(character));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task
        QuerySingleOrDefault_ValueTupleType_ColumnDataTypeNotCompatibleWithValueTupleFieldType_ShouldThrow(
            Boolean useAsyncApi
        ) =>
        Invoking(() =>
                CallApi<ValueTuple<TimeSpan>>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT 123 AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage(
                "The data type System.* of the column 'Value' returned by the SQL statement is not compatible with " +
                $"the field type {typeof(TimeSpan)} of the corresponding field of the value tuple type " +
                $"{typeof(ValueTuple<TimeSpan>)}.*"
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task
        QuerySingleOrDefault_ValueTupleType_EnumValueTupleField_ColumnContainsInvalidInteger_ShouldThrow(
            Boolean useAsyncApi
        ) =>
        Invoking(() =>
                CallApi<ValueTuple<TestEnum>>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT 999 AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                "The column 'Value' returned by the SQL statement contains a value that could not be converted to " +
                $"the type {typeof(TestEnum)} of the corresponding field of the value tuple type " +
                $"{typeof(ValueTuple<TestEnum>)}. See inner exception for details.*"
            )
            .WithInnerException(typeof(InvalidCastException))
            .WithMessage(
                "Could not convert the value '999*' (System.*) to an enum member of the type " +
                $"{typeof(TestEnum)}. That value does not match any of the values of the enum's members.*"
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task
        QuerySingleOrDefault_ValueTupleType_EnumValueTupleField_ColumnContainsInvalidString_ShouldThrow(
            Boolean useAsyncApi
        ) =>
        Invoking(() =>
                CallApi<ValueTuple<TestEnum>>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT 'NonExistent' AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                "The column 'Value' returned by the SQL statement contains a value that could not be converted to " +
                $"the type {typeof(TestEnum)} of the corresponding field of the value tuple type " +
                $"{typeof(ValueTuple<TestEnum>)}. See inner exception for details.*"
            )
            .WithInnerException(typeof(InvalidCastException))
            .WithMessage(
                $"Could not convert the string 'NonExistent' to an enum member of the type {typeof(TestEnum)}. " +
                "That string does not match any of the names of the enum's members.*"
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingleOrDefault_ValueTupleType_EnumValueTupleField_ShouldConvertIntegerToEnum(
        Boolean useAsyncApi
    )
    {
        var enumValue = Generate.Single<TestEnum>();

        (await CallApi<ValueTuple<TestEnum>>(
                useAsyncApi,
                this.Connection,
                $"SELECT {(Int32)enumValue}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(ValueTuple.Create(enumValue));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingleOrDefault_ValueTupleType_EnumValueTupleField_ShouldConvertStringToEnum(
        Boolean useAsyncApi
    )
    {
        var enumValue = Generate.Single<TestEnum>();

        (await CallApi<ValueTuple<TestEnum>>(
                useAsyncApi,
                this.Connection,
                $"SELECT '{enumValue}'",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(ValueTuple.Create(enumValue));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task QuerySingleOrDefault_ValueTupleType_NonNullableValueTupleField_ColumnContainsNull_ShouldThrow(
        Boolean useAsyncApi
    )
    {
        this.Connection.ExecuteNonQuery(
            $"INSERT INTO {Q("Entity")} ({Q("Id")}, {Q("BooleanValue")}) VALUES(1, NULL)"
        );

        return Invoking(() =>
                CallApi<ValueTuple<Boolean>>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT {Q("BooleanValue")} FROM {Q("Entity")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                "The column 'BooleanValue' returned by the SQL statement contains a NULL value, but the corresponding " +
                $"field of the value tuple type {typeof(ValueTuple<Boolean>)} is non-nullable.*"
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        QuerySingleOrDefault_ValueTupleType_NullableValueTupleField_ColumnContainsNull_ShouldReturnNull(
            Boolean useAsyncApi
        )
    {
        await this.Connection.ExecuteNonQueryAsync(
            $"INSERT INTO {Q("EntityWithNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        (await CallApi<ValueTuple<Int32?>>(
                useAsyncApi,
                this.Connection,
                $"SELECT {Q("Value")} FROM {Q("EntityWithNullableProperty")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(new(null));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task
        QuerySingleOrDefault_ValueTupleType_NumberOfColumnsDoesNotMatchNumberOfValueTupleFields_ShouldThrow(
            Boolean useAsyncApi
        ) =>
        Invoking(() =>
                CallApi<(Int32, Int32)>(
                    useAsyncApi,
                    this.Connection,
                    "SELECT 1",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage(
                $"The SQL statement returned 1 column, but the value tuple type {typeof((Int32, Int32))} has 2 " +
                "fields. Make sure that the SQL statement returns the same number of columns as the number of " +
                "fields in the value tuple type.*"
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingleOrDefault_ValueTupleType_ShouldMaterializeBinaryData(Boolean useAsyncApi)
    {
        var bytes = Generate.Single<Byte[]>();

        (await CallApi<ValueTuple<Byte[]>>(
                useAsyncApi,
                this.Connection,
                $"SELECT {Parameter(bytes)} AS BinaryData",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(ValueTuple.Create(bytes));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingleOrDefault_ValueTupleType_ShouldSupportDateTimeOffsetValues(Boolean useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entity = this.CreateEntityInDb<EntityWithDateTimeOffset>();

        (await CallApi<(Int64 Id, DateTimeOffset DateTimeOffsetValue)>(
                useAsyncApi,
                this.Connection,
                $"SELECT {Q("Id")}, {Q("DateTimeOffsetValue")} FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo((entity.Id, entity.DateTimeOffsetValue));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task QuerySingleOrDefault_ValueTupleType_UnsupportedFieldType_ShouldThrow(Boolean useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.HasUnsupportedDataType, "");

        var literal = this.TestDatabaseProvider.GetUnsupportedDataTypeLiteral();

        return Invoking(() =>
                CallApi<ValueTuple<Object>>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT {literal} AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage(
                "The data type System.* of the column 'Value' returned by the SQL statement is not supported.*"
            );
    }

    private static Task<T?> CallApi<T>(
        Boolean useAsyncApi,
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
            return connection.QuerySingleOrDefaultAsync<T>(
                statement,
                transaction,
                commandTimeout,
                commandType,
                cancellationToken
            );
        }

        try
        {
            return Task.FromResult(
                connection.QuerySingleOrDefault<T>(
                    statement,
                    transaction,
                    commandTimeout,
                    commandType,
                    cancellationToken
                )
            );
        }
        catch (Exception ex)
        {
            return Task.FromException<T?>(ex);
        }
    }
}
