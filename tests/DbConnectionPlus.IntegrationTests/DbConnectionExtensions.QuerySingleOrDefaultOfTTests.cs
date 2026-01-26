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
    [Fact]
    public void QuerySingleOrDefault_BuiltInType_CharTargetType_ColumnContainsStringWithLengthNotOne_ShouldThrow()
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            Invoking(() =>
                    this.Connection.QuerySingleOrDefault<Char>(
                        "SELECT ''",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                )
                .Should().Throw<InvalidCastException>()
                .WithMessage(
                    $"The first column returned by the SQL statement contains the value '' ({typeof(String)}), which " +
                    $"could not be converted to the type {typeof(Char)}. See inner exception for details.*"
                )
                .WithInnerException<InvalidCastException>()
                .WithMessage(
                    $"Could not convert the string '' to the type {typeof(Char)}. The string must be exactly " +
                    "one character long."
                );
        }

        Invoking(() =>
                this.Connection.QuerySingleOrDefault<Char>(
                    "SELECT 'ab'",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"The first column returned by the SQL statement contains the value 'ab' ({typeof(String)}), which " +
                $"could not be converted to the type {typeof(Char)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(Char)}. The string must be exactly " +
                "one character long."
            );
    }

    [Fact]
    public void
        QuerySingleOrDefault_BuiltInType_CharTargetType_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<Char>();

        this.Connection.QuerySingleOrDefault<Char>(
                $"SELECT '{character}'",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(character);
    }

    [Fact]
    public void QuerySingleOrDefault_BuiltInType_ColumnValueCannotBeConvertedToTargetType_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QuerySingleOrDefault<Int32>(
                    "SELECT 'A'",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"The first column returned by the SQL statement contains the value 'A' ({typeof(String)}), which " +
                $"could not be converted to the type {typeof(Int32)}. See inner exception for details.*"
            );

    [Fact]
    public void QuerySingleOrDefault_BuiltInType_EnumTargetType_ColumnContainsInvalidInteger_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QuerySingleOrDefault<TestEnum>(
                    "SELECT 999",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The first column returned by the SQL statement contains the value '999*' (System.*), which " +
                $"could not be converted to the type {typeof(TestEnum)}. See inner exception for details.*"
            );

    [Fact]
    public void QuerySingleOrDefault_BuiltInType_EnumTargetType_ColumnContainsInvalidString_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QuerySingleOrDefault<TestEnum>(
                    "SELECT 'NonExistent'",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The first column returned by the SQL statement contains the value 'NonExistent' " +
                $"({typeof(String)}), which could not be converted to the type {typeof(TestEnum)}. See inner " +
                "exception for details.*"
            );

    [Fact]
    public void QuerySingleOrDefault_BuiltInType_EnumTargetType_ShouldConvertIntegerToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        this.Connection.QuerySingleOrDefault<TestEnum>(
                $"SELECT {(Int32)enumValue}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(enumValue);
    }

    [Fact]
    public void QuerySingleOrDefault_BuiltInType_EnumTargetType_ShouldConvertStringToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        this.Connection.QuerySingleOrDefault<TestEnum>(
                $"SELECT '{enumValue.ToString()}'",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(enumValue);
    }

    [Fact]
    public void QuerySingleOrDefault_BuiltInType_NonNullableTargetType_ColumnContainsNull_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QuerySingleOrDefault<Int32>(
                    "SELECT NULL",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The first column returned by the SQL statement contains a NULL value, which could not be converted " +
                $"to the type {typeof(Int32)}. See inner exception for details.*"
            );

    [Fact]
    public void QuerySingleOrDefault_BuiltInType_NullableTargetType_ColumnContainsNull_ShouldReturnNull() =>
        this.Connection.QuerySingleOrDefault<Int32?>(
                "SELECT NULL",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeNull();

    [Fact]
    public void QuerySingleOrDefault_BuiltInType_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entity = this.CreateEntityInDb<EntityWithDateTimeOffset>();

        this.Connection.QuerySingleOrDefault<DateTimeOffset>(
                $"SELECT {Q("DateTimeOffsetValue")} FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entity.DateTimeOffsetValue);
    }

    [Fact]
    public void QuerySingleOrDefault_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        Invoking(() =>
                this.Connection.QuerySingleOrDefault<Entity>(
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: cancellationToken
                )
            )
            .Should().Throw<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Fact]
    public void QuerySingleOrDefault_CommandType_ShouldUseCommandType()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entity = this.CreateEntityInDb<Entity>();

        this.Connection.QuerySingleOrDefault<Entity>(
                "GetFirstEntity",
                commandType: CommandType.StoredProcedure,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entity);
    }

    [Fact]
    public void QuerySingleOrDefault_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entity = Generate.Single<Entity>();

        InterpolatedSqlStatement statement = $"SELECT * FROM {TemporaryTable([entity])}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        this.Connection.QuerySingleOrDefault<Entity>(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entity);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public void
        QuerySingleOrDefault_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entity = Generate.Single<Entity>();

        this.Connection.QuerySingleOrDefault<Entity>(
                $"SELECT * FROM {TemporaryTable([entity])}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entity);
    }

    [Fact]
    public void QuerySingleOrDefault_EntityType_CharEntityProperty_ColumnContainsStringWithLengthNotOne_ShouldThrow()
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            Invoking(() =>
                    this.Connection.QuerySingleOrDefault<EntityWithCharProperty>(
                        $"SELECT '' AS {Q("Char")}",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                )
                .Should().Throw<InvalidCastException>()
                .WithMessage(
                    "The column 'Char' returned by the SQL statement contains a value that could not be converted " +
                    $"to the type {typeof(Char)} of the corresponding property of the type " +
                    $"{typeof(EntityWithCharProperty)}. See inner exception for details.*"
                )
                .WithInnerException<InvalidCastException>()
                .WithMessage(
                    $"Could not convert the string '' to the type {typeof(Char)}. The string must be " +
                    "exactly one character long."
                );
        }

        Invoking(() =>
                this.Connection.QuerySingleOrDefault<EntityWithCharProperty>(
                    $"SELECT 'ab' AS {Q("Char")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Char' returned by the SQL statement contains a value that could not be converted " +
                $"to the type {typeof(Char)} of the corresponding property of the type " +
                $"{typeof(EntityWithCharProperty)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(Char)}. The string must be " +
                "exactly one character long."
            );
    }

    [Fact]
    public void
        QuerySingleOrDefault_EntityType_CharEntityProperty_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<Char>();

        this.Connection.QuerySingleOrDefault<EntityWithCharProperty>(
                $"SELECT '{character}' AS {Q("Char")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(new EntityWithCharProperty { Char = character });
    }

    [Fact]
    public void QuerySingleOrDefault_EntityType_ColumnDataTypeNotCompatibleWithEntityPropertyType_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QuerySingleOrDefault<Entity>(
                    $"SELECT 123 AS {Q("TimeSpanValue")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<ArgumentException>()
            .WithMessage(
                "The data type System.* of the column 'TimeSpanValue' returned by the SQL statement is not " +
                $"compatible with the property type {typeof(TimeSpan)} of the corresponding property of the type " +
                $"{typeof(Entity)}.*"
            );

    [Fact]
    public void QuerySingleOrDefault_EntityType_ColumnHasNoName_ShouldThrow()
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

        Invoking(() =>
                this.Connection.QuerySingleOrDefault<Entity>(
                    statement,
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<ArgumentException>()
            .WithMessage(
                "The 1st column returned by the SQL statement does not have a name. Make sure that all columns the " +
                "statement returns have a name.*"
            );
    }

    [Fact]
    public void QuerySingleOrDefault_EntityType_CompatiblePrivateConstructor_ShouldUsePrivateConstructor()
    {
        var entity = this.CreateEntityInDb<Entity>();

        this.Connection.QuerySingleOrDefault<EntityWithPrivateConstructor>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entity);
    }

    [Fact]
    public void QuerySingleOrDefault_EntityType_CompatiblePublicConstructor_ShouldUsePublicConstructor()
    {
        var entity = this.CreateEntityInDb<Entity>();

        this.Connection.QuerySingleOrDefault<EntityWithPublicConstructor>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entity);
    }

    [Fact]
    public void QuerySingleOrDefault_EntityType_EntityTypeHasNoCorrespondingPropertyForColumn_ShouldIgnoreColumn()
    {
        var entity = Invoking(() =>
                this.Connection.QuerySingleOrDefault<EntityWithNonNullableProperty>(
                    $"SELECT 1 AS {Q("Id")}, 2 AS {Q("Value")}, 3 AS {Q("NonExistent")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().NotThrow().Subject;

        entity
            .Should().BeEquivalentTo(new EntityWithNonNullableProperty { Id = 1, Value = 2 });
    }

    [Fact]
    public void QuerySingleOrDefault_EntityType_EntityTypeWithPropertiesWithDifferentCasing_ShouldMaterializeEntities()
    {
        var entity = this.CreateEntityInDb<Entity>();
        var entityWithDifferentCasingProperties = Generate.MapTo<EntityWithDifferentCasingProperties>(entity);

        this.Connection.QuerySingleOrDefault<EntityWithDifferentCasingProperties>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entityWithDifferentCasingProperties);
    }

    [Fact]
    public void QuerySingleOrDefault_EntityType_EnumEntityProperty_ColumnContainsInvalidInteger_ShouldThrow() =>
        Invoking(() => this.Connection.QuerySingleOrDefault<EntityWithEnumStoredAsInteger>(
                    $"SELECT 1 AS {Q("Id")}, 999 AS {Q("Enum")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Enum' returned by the SQL statement contains a value that could not be converted " +
                $"to the type {typeof(TestEnum)} of the corresponding property of the type " +
                $"{typeof(EntityWithEnumStoredAsInteger)}. See inner exception for details.*"
            )
            .WithInnerException(typeof(InvalidCastException))
            .WithMessage(
                "Could not convert the value '999*' (System.*) to an enum member of the type " +
                $"{typeof(TestEnum)}. That value does not match any of the values of the enum's members.*"
            );

    [Fact]
    public void QuerySingleOrDefault_EntityType_EnumEntityProperty_ColumnContainsInvalidString_ShouldThrow() =>
        Invoking(() => this.Connection.QuerySingleOrDefault<EntityWithEnumStoredAsString>(
                    $"SELECT 1 AS {Q("Id")}, 'NonExistent' AS {Q("Enum")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
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

    [Fact]
    public void QuerySingleOrDefault_EntityType_EnumEntityProperty_ShouldConvertIntegerToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        this.Connection.QuerySingleOrDefault<EntityWithEnumStoredAsInteger>(
                $"SELECT 1 AS {Q("Id")}, {(Int32)enumValue} AS {Q("Enum")}",
                cancellationToken: TestContext.Current.CancellationToken
            )!
            .Enum
            .Should().Be(enumValue);
    }

    [Fact]
    public void QuerySingleOrDefault_EntityType_EnumEntityProperty_ShouldConvertStringToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        this.Connection.QuerySingleOrDefault<EntityWithEnumStoredAsInteger>(
                $"SELECT 1 AS {Q("Id")}, '{enumValue.ToString()}' AS {Q("Enum")}",
                cancellationToken: TestContext.Current.CancellationToken
            )!
            .Enum
            .Should().Be(enumValue);
    }

    [Fact]
    public void QuerySingleOrDefault_EntityType_NoCompatibleConstructor_NoParameterlessConstructor_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QuerySingleOrDefault<EntityWithPublicConstructor>($"SELECT 1 AS {Q("NonExistent")}")
            )
            .Should().Throw<ArgumentException>()
            .WithMessage(
                $"Could not materialize an instance of the type {typeof(EntityWithPublicConstructor)}. The type " +
                "either needs to have a parameterless constructor or a constructor whose parameters match the " +
                "columns returned by the SQL statement, e.g. a constructor that has the following " +
                $"signature:{Environment.NewLine}" +
                "(* NonExistent).*"
            );

    [Fact]
    public void
        QuerySingleOrDefault_EntityType_NoCompatibleConstructor_PrivateParameterlessConstructor_ShouldUsePrivateConstructorAndProperties()
    {
        var entity = this.CreateEntityInDb<Entity>();

        this.Connection.QuerySingleOrDefault<EntityWithPrivateParameterlessConstructor>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entity);
    }

    [Fact]
    public void
        QuerySingleOrDefault_EntityType_NoCompatibleConstructor_PublicParameterlessConstructor_ShouldUsePublicConstructorAndProperties()
    {
        var entity = this.CreateEntityInDb<Entity>();

        this.Connection.QuerySingleOrDefault<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entity);
    }

    [Fact]
    public void QuerySingleOrDefault_EntityType_NonNullableEntityProperty_ColumnContainsNull_ShouldThrow()
    {
        this.Connection.ExecuteNonQuery(
            $"INSERT INTO {Q("EntityWithNonNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        Invoking(() =>
                this.Connection.QuerySingleOrDefault<EntityWithNonNullableProperty>(
                    $"SELECT * FROM {Q("EntityWithNonNullableProperty")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Value' returned by the SQL statement contains a " +
                $"NULL value, but the corresponding property of the type {typeof(EntityWithNonNullableProperty)} " +
                "is non-nullable.*"
            );
    }

    [Fact]
    public void QuerySingleOrDefault_EntityType_NullableEntityProperty_ColumnContainsNull_ShouldReturnNull()
    {
        this.Connection.ExecuteNonQuery(
            $"INSERT INTO {Q("EntityWithNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        this.Connection.QuerySingleOrDefault<EntityWithNullableProperty>(
                $"SELECT * FROM {Q("EntityWithNullableProperty")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(new EntityWithNullableProperty { Id = 1, Value = null });
    }

    [Fact]
    public void QuerySingleOrDefault_EntityType_ShouldMaterializeBinaryData()
    {
        var bytes = Generate.Single<Byte[]>();

        this.Connection.QuerySingleOrDefault<EntityWithBinaryProperty>(
                $"SELECT {Parameter(bytes)} AS BinaryData",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(new EntityWithBinaryProperty { BinaryData = bytes });
    }

    [Fact]
    public void QuerySingleOrDefault_EntityType_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entity = this.CreateEntityInDb<EntityWithDateTimeOffset>();

        this.Connection.QuerySingleOrDefault<EntityWithDateTimeOffset>(
                $"SELECT * FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entity);
    }

    [Fact]
    public void QuerySingleOrDefault_EntityType_UnsupportedFieldType_ShouldThrow()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.HasUnsupportedDataType, "");

        var literal = this.TestDatabaseProvider.GetUnsupportedDataTypeLiteral();

        Invoking(() =>
                this.Connection.QuerySingleOrDefault<EntityWithObjectProperty>(
                    $"SELECT {literal} AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<ArgumentException>()
            .WithMessage(
                "The data type System.* of the column 'Value' returned by the SQL statement is not supported.*"
            );
    }

    [Fact]
    public void QuerySingleOrDefault_InterpolatedParameter_ShouldPassInterpolatedParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        this.Connection.QuerySingleOrDefault<Entity>(
                $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entity);
    }

    [Fact]
    public void QuerySingleOrDefault_Parameter_ShouldPassParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        var statement = new InterpolatedSqlStatement(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entity.Id)
        );

        this.Connection.QuerySingleOrDefault<Entity>(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entity);
    }

    [Fact]
    public void QuerySingleOrDefault_QueryReturnedMoreThanOneRow_ShouldThrow()
    {
        this.CreateEntitiesInDb<Entity>(2);

        Invoking(() => this.Connection.QuerySingleOrDefault<Entity>(
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidOperationException>()
            .WithMessage(
                "The SQL statement did return more than one row."
            );
    }

    [Fact]
    public void QuerySingleOrDefault_QueryReturnedNoRows_ShouldReturnDefault()
    {
        this.Connection.QuerySingleOrDefault<Int32>(
                $"SELECT {Q("Id")} FROM {Q("Entity")} WHERE {Q("Id")} = -1",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(0);

        this.Connection.QuerySingleOrDefault<Entity>(
                $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = -1",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeNull();

        this.Connection.QuerySingleOrDefault<(Int64, String)>(
                $"SELECT {Q("Id")}, {Q("StringValue")} FROM {Q("Entity")} WHERE {Q("Id")} = -1",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(default);
    }

    [Fact]
    public void QuerySingleOrDefault_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityId = Generate.Id();

        InterpolatedSqlStatement statement = $"SELECT {Q("Value")} AS Id FROM {TemporaryTable([entityId])}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        this.Connection.QuerySingleOrDefault<Int64>(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entityId);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public void
        QuerySingleOrDefault_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entity = this.CreateEntityInDb<Entity>();
        var entityId = entity.Id;

        this.Connection.QuerySingleOrDefault<Entity>(
                $"""
                 SELECT *
                 FROM   {Q("Entity")}
                 WHERE  {Q("Id")} IN (SELECT {Q("Value")} FROM {TemporaryTable([entityId])})
                 """,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entity);
    }

    [Fact]
    public void QuerySingleOrDefault_Transaction_ShouldUseTransaction()
    {
        using (var transaction = this.Connection.BeginTransaction())
        {
            var entity = this.CreateEntityInDb<Entity>(transaction);

            this.Connection.QuerySingleOrDefault<Entity>(
                    $"SELECT * FROM {Q("Entity")}",
                    transaction,
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .Should().Be(entity);

            transaction.Rollback();
        }

        this.Connection.QuerySingleOrDefault<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeNull();
    }

    [Fact]
    public void
        QuerySingleOrDefault_ValueTupleType_CharValueTupleField_ColumnContainsStringWithLengthNotOne_ShouldThrow()
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            Invoking(() =>
                    this.Connection.QuerySingleOrDefault<ValueTuple<Char>>(
                        $"SELECT '' AS {Q("Value")}",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                )
                .Should().Throw<InvalidCastException>()
                .WithMessage(
                    "The column 'Value' returned by the SQL statement contains a value that could not be converted " +
                    $"to the type {typeof(Char)} of the corresponding field of the value tuple type " +
                    $"{typeof(ValueTuple<Char>)}. See inner exception for details.*"
                )
                .WithInnerException<InvalidCastException>()
                .WithMessage(
                    $"Could not convert the string '' to the type {typeof(Char)}. The string must be " +
                    "exactly one character long."
                );
        }

        Invoking(() =>
                this.Connection.QuerySingleOrDefault<ValueTuple<Char>>(
                    $"SELECT 'ab' AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Value' returned by the SQL statement contains a value that could not be converted " +
                $"to the type {typeof(Char)} of the corresponding field of the value tuple type " +
                $"{typeof(ValueTuple<Char>)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(Char)}. The string must be " +
                "exactly one character long."
            );
    }

    [Fact]
    public void
        QuerySingleOrDefault_ValueTupleType_CharValueTupleField_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<Char>();

        this.Connection.QuerySingleOrDefault<ValueTuple<Char>>(
                $"SELECT '{character}'",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(ValueTuple.Create(character));
    }

    [Fact]
    public void QuerySingleOrDefault_ValueTupleType_ColumnDataTypeNotCompatibleWithValueTupleFieldType_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QuerySingleOrDefault<ValueTuple<TimeSpan>>(
                    $"SELECT 123 AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<ArgumentException>()
            .WithMessage(
                "The data type System.* of the column 'Value' returned by the SQL statement is not compatible with " +
                $"the field type {typeof(TimeSpan)} of the corresponding field of the value tuple type " +
                $"{typeof(ValueTuple<TimeSpan>)}.*"
            );

    [Fact]
    public void QuerySingleOrDefault_ValueTupleType_EnumValueTupleField_ColumnContainsInvalidInteger_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QuerySingleOrDefault<ValueTuple<TestEnum>>(
                    $"SELECT 999 AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Value' returned by the SQL statement contains a value that could not be converted to " +
                $"the type {typeof(TestEnum)} of the corresponding field of the value tuple type " +
                $"{typeof(ValueTuple<TestEnum>)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                "Could not convert the value '999*' (System.*) to an enum member of the type " +
                $"{typeof(TestEnum)}. That value does not match any of the values of the enum's members.*"
            );

    [Fact]
    public void QuerySingleOrDefault_ValueTupleType_EnumValueTupleField_ColumnContainsInvalidString_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QuerySingleOrDefault<ValueTuple<TestEnum>>(
                    $"SELECT 'NonExistent' AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Value' returned by the SQL statement contains a value that could not be converted to " +
                $"the type {typeof(TestEnum)} of the corresponding field of the value tuple type " +
                $"{typeof(ValueTuple<TestEnum>)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'NonExistent' to an enum member of the type {typeof(TestEnum)}. " +
                "That string does not match any of the names of the enum's members.*"
            );

    [Fact]
    public void QuerySingleOrDefault_ValueTupleType_EnumValueTupleField_ShouldConvertIntegerToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        this.Connection.QuerySingleOrDefault<ValueTuple<TestEnum>>(
                $"SELECT {(Int32)enumValue}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(ValueTuple.Create(enumValue));
    }

    [Fact]
    public void QuerySingleOrDefault_ValueTupleType_EnumValueTupleField_ShouldConvertStringToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        this.Connection.QuerySingleOrDefault<ValueTuple<TestEnum>>(
                $"SELECT '{enumValue}'",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(ValueTuple.Create(enumValue));
    }

    [Fact]
    public void QuerySingleOrDefault_ValueTupleType_NonNullableValueTupleField_ColumnContainsNull_ShouldThrow()
    {
        this.Connection.ExecuteNonQuery(
            $"INSERT INTO {Q("EntityWithNonNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        Invoking(() =>
                this.Connection.QuerySingleOrDefault<ValueTuple<Int32>>(
                    $"SELECT {Q("Value")} FROM {Q("EntityWithNonNullableProperty")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Value' returned by the SQL statement contains a NULL value, but the corresponding " +
                $"field of the value tuple type {typeof(ValueTuple<Int32>)} is non-nullable.*"
            );
    }

    [Fact]
    public void QuerySingleOrDefault_ValueTupleType_NullableValueTupleField_ColumnContainsNull_ShouldReturnNull()
    {
        this.Connection.ExecuteNonQuery(
            $"INSERT INTO {Q("EntityWithNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        this.Connection.QuerySingleOrDefault<ValueTuple<Int32?>>(
                $"SELECT {Q("Value")} FROM {Q("EntityWithNullableProperty")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(new(null));
    }

    [Fact]
    public void QuerySingleOrDefault_ValueTupleType_NumberOfColumnsDoesNotMatchNumberOfValueTupleFields_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QuerySingleOrDefault<(Int32, Int32)>(
                    "SELECT 1",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<ArgumentException>()
            .WithMessage(
                $"The SQL statement returned 1 column, but the value tuple type {typeof((Int32, Int32))} has 2 " +
                "fields. Make sure that the SQL statement returns the same number of columns as the number of " +
                "fields in the value tuple type.*"
            );

    [Fact]
    public void QuerySingleOrDefault_ValueTupleType_ShouldMaterializeBinaryData()
    {
        var bytes = Generate.Single<Byte[]>();

        this.Connection.QuerySingleOrDefault<ValueTuple<Byte[]>>(
                $"SELECT {Parameter(bytes)} AS BinaryData",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(ValueTuple.Create(bytes));
    }

    [Fact]
    public void QuerySingleOrDefault_ValueTupleType_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entity = this.CreateEntityInDb<EntityWithDateTimeOffset>();

        this.Connection.QuerySingleOrDefault<(Int64 Id, DateTimeOffset DateTimeOffsetValue)>(
                $"SELECT {Q("Id")}, {Q("DateTimeOffsetValue")} FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be((entity.Id, entity.DateTimeOffsetValue));
    }

    [Fact]
    public void QuerySingleOrDefault_ValueTupleType_UnsupportedFieldType_ShouldThrow()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.HasUnsupportedDataType, "");

        var literal = this.TestDatabaseProvider.GetUnsupportedDataTypeLiteral();

        Invoking(() =>
                this.Connection.QuerySingleOrDefault<ValueTuple<Object>>(
                    $"SELECT {literal} AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<ArgumentException>()
            .WithMessage(
                "The data type System.* of the column 'Value' returned by the SQL statement is not supported.*"
            );
    }

    [Fact]
    public async Task
        QuerySingleOrDefaultAsync_BuiltInType_CharTargetType_ColumnContainsStringWithLengthNotOne_ShouldThrow()
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            (await Invoking(() =>
                        this.Connection.QuerySingleOrDefaultAsync<Char>(
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
                    this.Connection.QuerySingleOrDefaultAsync<Char>(
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

    [Fact]
    public async Task
        QuerySingleOrDefaultAsync_BuiltInType_CharTargetType_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<Char>();

        (await this.Connection.QuerySingleOrDefaultAsync<Char>(
                $"SELECT '{character}'",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(character);
    }

    [Fact]
    public Task QuerySingleOrDefaultAsync_BuiltInType_ColumnValueCannotBeConvertedToTargetType_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QuerySingleOrDefaultAsync<Int32>(
                    "SELECT 'A'",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                $"The first column returned by the SQL statement contains the value 'A' ({typeof(String)}), which " +
                $"could not be converted to the type {typeof(Int32)}. See inner exception for details.*"
            );

    [Fact]
    public Task QuerySingleOrDefaultAsync_BuiltInType_EnumTargetType_ColumnContainsInvalidInteger_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QuerySingleOrDefaultAsync<TestEnum>(
                    "SELECT 999",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                "The first column returned by the SQL statement contains the value '999*' (System.*), which " +
                $"could not be converted to the type {typeof(TestEnum)}. See inner exception for details.*"
            );

    [Fact]
    public Task QuerySingleOrDefaultAsync_BuiltInType_EnumTargetType_ColumnContainsInvalidString_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QuerySingleOrDefaultAsync<TestEnum>(
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

    [Fact]
    public async Task QuerySingleOrDefaultAsync_BuiltInType_EnumTargetType_ShouldConvertIntegerToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        (await this.Connection.QuerySingleOrDefaultAsync<TestEnum>(
                $"SELECT {(Int32)enumValue}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(enumValue);
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_BuiltInType_EnumTargetType_ShouldConvertStringToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        (await this.Connection.QuerySingleOrDefaultAsync<TestEnum>(
                $"SELECT '{enumValue.ToString()}'",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(enumValue);
    }

    [Fact]
    public Task QuerySingleOrDefaultAsync_BuiltInType_NonNullableTargetType_ColumnContainsNull_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QuerySingleOrDefaultAsync<Int32>(
                    "SELECT NULL",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                "The first column returned by the SQL statement contains a NULL value, which could not be converted " +
                $"to the type {typeof(Int32)}. See inner exception for details.*"
            );

    [Fact]
    public async Task QuerySingleOrDefaultAsync_BuiltInType_NullableTargetType_ColumnContainsNull_ShouldReturnNull() =>
        (await this.Connection.QuerySingleOrDefaultAsync<Int32?>(
            "SELECT NULL",
            cancellationToken: TestContext.Current.CancellationToken
        ))
        .Should().BeNull();

    [Fact]
    public async Task QuerySingleOrDefaultAsync_BuiltInType_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entity = this.CreateEntityInDb<EntityWithDateTimeOffset>();

        (await this.Connection.QuerySingleOrDefaultAsync<DateTimeOffset>(
                $"SELECT {Q("DateTimeOffsetValue")} FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entity.DateTimeOffsetValue);
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        await Invoking(() =>
                this.Connection.QuerySingleOrDefaultAsync<Entity>(
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: cancellationToken
                )
            )
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_CommandType_ShouldUseCommandType()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entity = this.CreateEntityInDb<Entity>();

        (await this.Connection.QuerySingleOrDefaultAsync<Entity>(
                "GetFirstEntity",
                commandType: CommandType.StoredProcedure,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entity);
    }

    [Fact]
    public async Task
        QuerySingleOrDefaultAsync_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entity = Generate.Single<Entity>();

        InterpolatedSqlStatement statement = $"SELECT * FROM {TemporaryTable([entity])}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        (await this.Connection.QuerySingleOrDefaultAsync<Entity>(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entity);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public async Task
        QuerySingleOrDefaultAsync_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entity = Generate.Single<Entity>();

        (await this.Connection.QuerySingleOrDefaultAsync<Entity>(
                $"SELECT * FROM {TemporaryTable([entity])}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entity);
    }

    [Fact]
    public async Task
        QuerySingleOrDefaultAsync_EntityType_CharEntityProperty_ColumnContainsStringWithLengthNotOne_ShouldThrow()
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            await Invoking(() =>
                    this.Connection.QuerySingleOrDefaultAsync<EntityWithCharProperty>(
                        $"SELECT '' AS {Q("Char")}",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                )
                .Should().ThrowAsync<InvalidCastException>()
                .WithMessage(
                    "The column 'Char' returned by the SQL statement contains a value that could not be converted " +
                    $"to the type {typeof(Char)} of the corresponding property of the type " +
                    $"{typeof(EntityWithCharProperty)}. See inner exception for details.*"
                )
                .WithInnerException(typeof(InvalidCastException))
                .WithMessage(
                    $"Could not convert the string '' to the type {typeof(Char)}. The string must be " +
                    "exactly one character long."
                );
        }

        await Invoking(() =>
                this.Connection.QuerySingleOrDefaultAsync<EntityWithCharProperty>(
                    $"SELECT 'ab' AS {Q("Char")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                "The column 'Char' returned by the SQL statement contains a value that could not be converted " +
                $"to the type {typeof(Char)} of the corresponding property of the type " +
                $"{typeof(EntityWithCharProperty)}. See inner exception for details.*"
            )
            .WithInnerException(typeof(InvalidCastException))
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(Char)}. The string must be " +
                "exactly one character long."
            );
    }

    [Fact]
    public async Task
        QuerySingleOrDefaultAsync_EntityType_CharEntityProperty_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<Char>();

        (await this.Connection.QuerySingleOrDefaultAsync<EntityWithCharProperty>(
                $"SELECT '{character}' AS {Q("Char")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(new EntityWithCharProperty { Char = character });
    }

    [Fact]
    public Task QuerySingleOrDefaultAsync_EntityType_ColumnDataTypeNotCompatibleWithEntityPropertyType_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QuerySingleOrDefaultAsync<Entity>(
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

    [Fact]
    public async Task QuerySingleOrDefaultAsync_EntityType_ColumnHasNoName_ShouldThrow()
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
                this.Connection.QuerySingleOrDefaultAsync<Entity>(
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

    [Fact]
    public async Task QuerySingleOrDefaultAsync_EntityType_CompatiblePrivateConstructor_ShouldUsePrivateConstructor()
    {
        var entity = this.CreateEntityInDb<Entity>();

        (await this.Connection.QuerySingleOrDefaultAsync<EntityWithPrivateConstructor>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entity);
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_EntityType_CompatiblePublicConstructor_ShouldUsePublicConstructor()
    {
        var entity = this.CreateEntityInDb<Entity>();

        (await this.Connection.QuerySingleOrDefaultAsync<EntityWithPublicConstructor>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entity);
    }

    [Fact]
    public async Task
        QuerySingleOrDefaultAsync_EntityType_EntityTypeHasNoCorrespondingPropertyForColumn_ShouldIgnoreColumn()
    {
        var entity = (await Invoking(() =>
                this.Connection.QuerySingleOrDefaultAsync<EntityWithNonNullableProperty>(
                    $"SELECT 1 AS {Q("Id")}, 2 AS {Q("Value")}, 3 AS {Q("NonExistent")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().NotThrowAsync()).Subject;

        entity
            .Should().BeEquivalentTo(new EntityWithNonNullableProperty { Id = 1, Value = 2 });
    }

    [Fact]
    public async Task
        QuerySingleOrDefaultAsync_EntityType_EntityTypeWithPropertiesWithDifferentCasing_ShouldMaterializeEntities()
    {
        var entity = this.CreateEntityInDb<Entity>();
        var entityWithDifferentCasingProperties = Generate.MapTo<EntityWithDifferentCasingProperties>(entity);

        (await this.Connection.QuerySingleOrDefaultAsync<EntityWithDifferentCasingProperties>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entityWithDifferentCasingProperties);
    }

    [Fact]
    public async Task
        QuerySingleOrDefaultAsync_EntityType_EnumEntityProperty_ColumnContainsInvalidInteger_ShouldThrow() =>
        await Invoking(() => this.Connection.QuerySingleOrDefaultAsync<EntityWithEnumStoredAsInteger>(
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

    [Fact]
    public async Task
        QuerySingleOrDefaultAsync_EntityType_EnumEntityProperty_ColumnContainsInvalidString_ShouldThrow() =>
        await Invoking(() => this.Connection.QuerySingleOrDefaultAsync<EntityWithEnumStoredAsString>(
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

    [Fact]
    public async Task QuerySingleOrDefaultAsync_EntityType_EnumEntityProperty_ShouldConvertIntegerToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        (await this.Connection.QuerySingleOrDefaultAsync<EntityWithEnumStoredAsInteger>(
                $"SELECT 1 AS {Q("Id")}, {(Int32)enumValue} AS {Q("Enum")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))!
            .Enum
            .Should().Be(enumValue);
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_EntityType_EnumEntityProperty_ShouldConvertStringToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        (await this.Connection.QuerySingleOrDefaultAsync<EntityWithEnumStoredAsInteger>(
                $"SELECT 1 AS {Q("Id")}, '{enumValue.ToString()}' AS {Q("Enum")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))!
            .Enum
            .Should().Be(enumValue);
    }

    [Fact]
    public Task QuerySingleOrDefaultAsync_EntityType_NoCompatibleConstructor_NoParameterlessConstructor_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QuerySingleOrDefaultAsync<EntityWithPublicConstructor>(
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

    [Fact]
    public async Task
        QuerySingleOrDefaultAsync_EntityType_NoCompatibleConstructor_PrivateParameterlessConstructor_ShouldUsePrivateConstructorAndProperties()
    {
        var entity = this.CreateEntityInDb<Entity>();

        (await this.Connection.QuerySingleOrDefaultAsync<EntityWithPrivateParameterlessConstructor>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entity);
    }

    [Fact]
    public async Task
        QuerySingleOrDefaultAsync_EntityType_NoCompatibleConstructor_PublicParameterlessConstructor_ShouldUsePublicConstructorAndProperties()
    {
        var entity = this.CreateEntityInDb<Entity>();

        (await this.Connection.QuerySingleOrDefaultAsync<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entity);
    }

    [Fact]
    public Task QuerySingleOrDefaultAsync_EntityType_NonNullableEntityProperty_ColumnContainsNull_ShouldThrow()
    {
        this.Connection.ExecuteNonQuery(
            $"INSERT INTO {Q("EntityWithNonNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        return Invoking(() =>
                this.Connection.QuerySingleOrDefaultAsync<EntityWithNonNullableProperty>(
                    $"SELECT * FROM {Q("EntityWithNonNullableProperty")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                "The column 'Value' returned by the SQL statement contains a NULL value, but the corresponding " +
                $"property of the type {typeof(EntityWithNonNullableProperty)} is non-nullable.*"
            );
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_EntityType_NullableEntityProperty_ColumnContainsNull_ShouldReturnNull()
    {
        await this.Connection.ExecuteNonQueryAsync(
            $"INSERT INTO {Q("EntityWithNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        (await this.Connection.QuerySingleOrDefaultAsync<EntityWithNullableProperty>(
                $"SELECT * FROM {Q("EntityWithNullableProperty")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(new EntityWithNullableProperty { Id = 1, Value = null });
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_EntityType_ShouldMaterializeBinaryData()
    {
        var bytes = Generate.Single<Byte[]>();

        (await this.Connection.QuerySingleOrDefaultAsync<EntityWithBinaryProperty>(
                $"SELECT {Parameter(bytes)} AS BinaryData",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(new EntityWithBinaryProperty { BinaryData = bytes });
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_EntityType_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entity = this.CreateEntityInDb<EntityWithDateTimeOffset>();

        (await this.Connection.QuerySingleOrDefaultAsync<EntityWithDateTimeOffset>(
                $"SELECT * FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entity);
    }

    [Fact]
    public Task QuerySingleOrDefaultAsync_EntityType_UnsupportedFieldType_ShouldThrow()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.HasUnsupportedDataType, "");

        var literal = this.TestDatabaseProvider.GetUnsupportedDataTypeLiteral();

        return Invoking(() =>
                this.Connection.QuerySingleOrDefaultAsync<EntityWithObjectProperty>(
                    $"SELECT {literal} AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage(
                "The data type System.* of the column 'Value' returned by the SQL statement is not supported.*"
            );
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_InterpolatedParameter_ShouldPassInterpolatedParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        (await this.Connection.QuerySingleOrDefaultAsync<Entity>(
                $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entity);
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_Parameter_ShouldPassParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        var statement = new InterpolatedSqlStatement(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entity.Id)
        );

        (await this.Connection.QuerySingleOrDefaultAsync<Entity>(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entity);
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_QueryReturnedMoreThanOneRow_ShouldThrow()
    {
        this.CreateEntitiesInDb<Entity>(2);

        await Invoking(() => this.Connection.QuerySingleOrDefaultAsync<Entity>(
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(
                "The SQL statement did return more than one row."
            );
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_QueryReturnedNoRows_ShouldThrow()
    {
        (await this.Connection.QuerySingleOrDefaultAsync<Int32>(
                $"SELECT {Q("Id")} FROM {Q("Entity")} WHERE {Q("Id")} = -1",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(0);

        (await this.Connection.QuerySingleOrDefaultAsync<Entity>(
                $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = -1",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeNull();

        (await this.Connection.QuerySingleOrDefaultAsync<(Int64, String)>(
                $"SELECT {Q("Id")}, {Q("StringValue")} FROM {Q("Entity")} WHERE {Q("Id")} = -1",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(default);
    }

    [Fact]
    public async Task
        QuerySingleOrDefaultAsync_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityId = Generate.Id();

        InterpolatedSqlStatement statement =
            $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable([entityId])}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        (await this.Connection.QuerySingleOrDefaultAsync<Int64>(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entityId);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public async Task
        QuerySingleOrDefaultAsync_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entity = this.CreateEntityInDb<Entity>();
        var entityId = entity.Id;

        (await this.Connection.QuerySingleOrDefaultAsync<Entity>(
                $"""
                 SELECT *
                 FROM   {Q("Entity")}
                 WHERE  {Q("Id")} IN (SELECT {Q("Value")} FROM {TemporaryTable([entityId])})
                 """,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entity);
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_Transaction_ShouldUseTransaction()
    {
        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            var entity = this.CreateEntityInDb<Entity>(transaction);

            (await this.Connection.QuerySingleOrDefaultAsync<Entity>(
                    $"SELECT * FROM {Q("Entity")}",
                    transaction,
                    cancellationToken: TestContext.Current.CancellationToken
                ))
                .Should().Be(entity);

            await transaction.RollbackAsync();
        }

        (await this.Connection.QuerySingleOrDefaultAsync<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeNull();
    }

    [Fact]
    public async Task
        QuerySingleOrDefaultAsync_ValueTupleType_CharValueTupleField_ColumnContainsStringWithLengthNotOne_ShouldThrow()
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            await Invoking(() =>
                    this.Connection.QuerySingleOrDefaultAsync<ValueTuple<Char>>(
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
                this.Connection.QuerySingleOrDefaultAsync<ValueTuple<Char>>(
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

    [Fact]
    public async Task
        QuerySingleOrDefaultAsync_ValueTupleType_CharValueTupleField_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<Char>();

        (await this.Connection.QuerySingleOrDefaultAsync<ValueTuple<Char>>(
                $"SELECT '{character}'",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(ValueTuple.Create(character));
    }

    [Fact]
    public Task
        QuerySingleOrDefaultAsync_ValueTupleType_ColumnDataTypeNotCompatibleWithValueTupleFieldType_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QuerySingleOrDefaultAsync<ValueTuple<TimeSpan>>(
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

    [Fact]
    public Task
        QuerySingleOrDefaultAsync_ValueTupleType_EnumValueTupleField_ColumnContainsInvalidInteger_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QuerySingleOrDefaultAsync<ValueTuple<TestEnum>>(
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

    [Fact]
    public Task
        QuerySingleOrDefaultAsync_ValueTupleType_EnumValueTupleField_ColumnContainsInvalidString_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QuerySingleOrDefaultAsync<ValueTuple<TestEnum>>(
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

    [Fact]
    public async Task QuerySingleOrDefaultAsync_ValueTupleType_EnumValueTupleField_ShouldConvertIntegerToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        (await this.Connection.QuerySingleOrDefaultAsync<ValueTuple<TestEnum>>(
                $"SELECT {(Int32)enumValue}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(ValueTuple.Create(enumValue));
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_ValueTupleType_EnumValueTupleField_ShouldConvertStringToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        (await this.Connection.QuerySingleOrDefaultAsync<ValueTuple<TestEnum>>(
                $"SELECT '{enumValue}'",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(ValueTuple.Create(enumValue));
    }

    [Fact]
    public Task QuerySingleOrDefaultAsync_ValueTupleType_NonNullableValueTupleField_ColumnContainsNull_ShouldThrow()
    {
        this.Connection.ExecuteNonQuery(
            $"INSERT INTO {Q("EntityWithNonNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        return Invoking(() =>
                this.Connection.QuerySingleOrDefaultAsync<ValueTuple<Int32>>(
                    $"SELECT {Q("Value")} FROM {Q("EntityWithNonNullableProperty")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                "The column 'Value' returned by the SQL statement contains a NULL value, but the corresponding " +
                $"field of the value tuple type {typeof(ValueTuple<Int32>)} is non-nullable.*"
            );
    }

    [Fact]
    public async Task
        QuerySingleOrDefaultAsync_ValueTupleType_NullableValueTupleField_ColumnContainsNull_ShouldReturnNull()
    {
        await this.Connection.ExecuteNonQueryAsync(
            $"INSERT INTO {Q("EntityWithNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        (await this.Connection.QuerySingleOrDefaultAsync<ValueTuple<Int32?>>(
                $"SELECT {Q("Value")} FROM {Q("EntityWithNullableProperty")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(new(null));
    }

    [Fact]
    public Task
        QuerySingleOrDefaultAsync_ValueTupleType_NumberOfColumnsDoesNotMatchNumberOfValueTupleFields_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QuerySingleOrDefaultAsync<(Int32, Int32)>(
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

    [Fact]
    public async Task QuerySingleOrDefaultAsync_ValueTupleType_ShouldMaterializeBinaryData()
    {
        var bytes = Generate.Single<Byte[]>();

        (await this.Connection.QuerySingleOrDefaultAsync<ValueTuple<Byte[]>>(
                $"SELECT {Parameter(bytes)} AS BinaryData",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(ValueTuple.Create(bytes));
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_ValueTupleType_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entity = this.CreateEntityInDb<EntityWithDateTimeOffset>();

        (await this.Connection.QuerySingleOrDefaultAsync<(Int64 Id, DateTimeOffset DateTimeOffsetValue)>(
                $"SELECT {Q("Id")}, {Q("DateTimeOffsetValue")} FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be((entity.Id, entity.DateTimeOffsetValue));
    }

    [Fact]
    public Task QuerySingleOrDefaultAsync_ValueTupleType_UnsupportedFieldType_ShouldThrow()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.HasUnsupportedDataType, "");

        var literal = this.TestDatabaseProvider.GetUnsupportedDataTypeLiteral();

        return Invoking(() =>
                this.Connection.QuerySingleOrDefaultAsync<ValueTuple<Object>>(
                    $"SELECT {literal} AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage(
                "The data type System.* of the column 'Value' returned by the SQL statement is not supported.*"
            );
    }
}
