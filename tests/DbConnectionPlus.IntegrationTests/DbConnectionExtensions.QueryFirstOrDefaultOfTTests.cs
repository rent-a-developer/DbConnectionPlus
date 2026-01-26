namespace RentADeveloper.DbConnectionPlus.IntegrationTests;

public sealed class
    DbConnectionExtensions_QueryFirstOrDefaultOfTTests_MySql :
    DbConnectionExtensions_QueryFirstOrDefaultOfTTests<MySqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QueryFirstOrDefaultOfTTests_Oracle :
    DbConnectionExtensions_QueryFirstOrDefaultOfTTests<OracleTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QueryFirstOrDefaultOfTTests_PostgreSql :
    DbConnectionExtensions_QueryFirstOrDefaultOfTTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QueryFirstOrDefaultOfTTests_Sqlite :
    DbConnectionExtensions_QueryFirstOrDefaultOfTTests<SqliteTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QueryFirstOrDefaultOfTTests_SqlServer :
    DbConnectionExtensions_QueryFirstOrDefaultOfTTests<SqlServerTestDatabaseProvider>;

public abstract class
    DbConnectionExtensions_QueryFirstOrDefaultOfTTests<TTestDatabaseProvider> : IntegrationTestsBase<
    TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    [Fact]
    public void QueryFirstOrDefault_BuiltInType_CharTargetType_ColumnContainsStringWithLengthNotOne_ShouldThrow()
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            Invoking(() =>
                    this.Connection.QueryFirstOrDefault<Char>(
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
                this.Connection.QueryFirstOrDefault<Char>(
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
        QueryFirstOrDefault_BuiltInType_CharTargetType_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<Char>();

        this.Connection.QueryFirstOrDefault<Char>(
                $"SELECT '{character}'",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(character);
    }

    [Fact]
    public void QueryFirstOrDefault_BuiltInType_ColumnValueCannotBeConvertedToTargetType_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstOrDefault<Int32>(
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
    public void QueryFirstOrDefault_BuiltInType_EnumTargetType_ColumnContainsInvalidInteger_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstOrDefault<TestEnum>(
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
    public void QueryFirstOrDefault_BuiltInType_EnumTargetType_ColumnContainsInvalidString_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstOrDefault<TestEnum>(
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
    public void QueryFirstOrDefault_BuiltInType_EnumTargetType_ShouldConvertIntegerToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        this.Connection.QueryFirstOrDefault<TestEnum>(
                $"SELECT {(Int32)enumValue}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(enumValue);
    }

    [Fact]
    public void QueryFirstOrDefault_BuiltInType_EnumTargetType_ShouldConvertStringToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        this.Connection.QueryFirstOrDefault<TestEnum>(
                $"SELECT '{enumValue.ToString()}'",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(enumValue);
    }

    [Fact]
    public void QueryFirstOrDefault_BuiltInType_NonNullableTargetType_ColumnContainsNull_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstOrDefault<Int32>(
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
    public void QueryFirstOrDefault_BuiltInType_NullableTargetType_ColumnContainsNull_ShouldReturnNull() =>
        this.Connection.QueryFirstOrDefault<Int32?>(
                "SELECT NULL",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeNull();

    [Fact]
    public void QueryFirstOrDefault_BuiltInType_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entities = this.CreateEntitiesInDb<EntityWithDateTimeOffset>(2);

        this.Connection.QueryFirstOrDefault<DateTimeOffset>(
                $"SELECT {Q("DateTimeOffsetValue")} FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entities[0].DateTimeOffsetValue);
    }

    [Fact]
    public void QueryFirstOrDefault_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        Invoking(() =>
                this.Connection.QueryFirstOrDefault<Entity>(
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: cancellationToken
                )
            )
            .Should().Throw<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Fact]
    public void QueryFirstOrDefault_CommandType_ShouldUseCommandType()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entities = this.CreateEntitiesInDb<Entity>(2);

        this.Connection.QueryFirstOrDefault<Entity>(
                "GetEntities",
                commandType: CommandType.StoredProcedure,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entities[0]);
    }

    [Fact]
    public void QueryFirstOrDefault_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(2);

        InterpolatedSqlStatement statement = $"SELECT * FROM {TemporaryTable(entities)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        this.Connection.QueryFirstOrDefault<Entity>(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entities[0]);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public void
        QueryFirstOrDefault_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(2);

        this.Connection.QueryFirstOrDefault<Entity>(
                $"SELECT * FROM {TemporaryTable(entities)}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entities[0]);
    }

    [Fact]
    public void QueryFirstOrDefault_EntityType_CharEntityProperty_ColumnContainsStringWithLengthNotOne_ShouldThrow()
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            Invoking(() =>
                    this.Connection.QueryFirstOrDefault<EntityWithCharProperty>(
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
                this.Connection.QueryFirstOrDefault<EntityWithCharProperty>(
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
        QueryFirstOrDefault_EntityType_CharEntityProperty_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<Char>();

        this.Connection.QueryFirstOrDefault<EntityWithCharProperty>(
                $"SELECT '{character}' AS {Q("Char")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(new EntityWithCharProperty { Char = character });
    }

    [Fact]
    public void QueryFirstOrDefault_EntityType_ColumnDataTypeNotCompatibleWithEntityPropertyType_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstOrDefault<Entity>(
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
    public void QueryFirstOrDefault_EntityType_ColumnHasNoName_ShouldThrow()
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
                this.Connection.QueryFirstOrDefault<Entity>(
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
    public void QueryFirstOrDefault_EntityType_CompatiblePrivateConstructor_ShouldUsePrivateConstructor()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        this.Connection.QueryFirstOrDefault<EntityWithPrivateConstructor>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entities[0]);
    }

    [Fact]
    public void QueryFirstOrDefault_EntityType_CompatiblePublicConstructor_ShouldUsePublicConstructor()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        this.Connection.QueryFirstOrDefault<EntityWithPublicConstructor>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entities[0]);
    }

    [Fact]
    public void QueryFirstOrDefault_EntityType_EntityTypeHasNoCorrespondingPropertyForColumn_ShouldIgnoreColumn()
    {
        var entity = Invoking(() =>
                this.Connection.QueryFirstOrDefault<EntityWithNonNullableProperty>(
                    $"SELECT 1 AS {Q("Id")}, 2 AS {Q("Value")}, 3 AS {Q("NonExistent")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().NotThrow().Subject;

        entity
            .Should().BeEquivalentTo(new EntityWithNonNullableProperty { Id = 1, Value = 2 });
    }

    [Fact]
    public void QueryFirstOrDefault_EntityType_EntityTypeWithPropertiesWithDifferentCasing_ShouldMaterializeEntities()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);
        var entitiesWithDifferentCasingProperties = Generate.MapTo<EntityWithDifferentCasingProperties>(entities);

        this.Connection.QueryFirstOrDefault<EntityWithDifferentCasingProperties>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entitiesWithDifferentCasingProperties[0]);
    }

    [Fact]
    public void QueryFirstOrDefault_EntityType_EnumEntityProperty_ColumnContainsInvalidInteger_ShouldThrow() =>
        Invoking(() => this.Connection.QueryFirstOrDefault<EntityWithEnumStoredAsInteger>(
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
    public void QueryFirstOrDefault_EntityType_EnumEntityProperty_ColumnContainsInvalidString_ShouldThrow() =>
        Invoking(() => this.Connection.QueryFirstOrDefault<EntityWithEnumStoredAsString>(
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
    public void QueryFirstOrDefault_EntityType_EnumEntityProperty_ShouldConvertIntegerToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        this.Connection.QueryFirstOrDefault<EntityWithEnumStoredAsInteger>(
                $"SELECT 1 AS {Q("Id")}, {(Int32)enumValue} AS {Q("Enum")}",
                cancellationToken: TestContext.Current.CancellationToken
            )!
            .Enum
            .Should().Be(enumValue);
    }

    [Fact]
    public void QueryFirstOrDefault_EntityType_EnumEntityProperty_ShouldConvertStringToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        this.Connection.QueryFirstOrDefault<EntityWithEnumStoredAsInteger>(
                $"SELECT 1 AS {Q("Id")}, '{enumValue.ToString()}' AS {Q("Enum")}",
                cancellationToken: TestContext.Current.CancellationToken
            )!
            .Enum
            .Should().Be(enumValue);
    }

    [Fact]
    public void QueryFirstOrDefault_EntityType_NoCompatibleConstructor_NoParameterlessConstructor_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstOrDefault<EntityWithPublicConstructor>($"SELECT 1 AS {Q("NonExistent")}")
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
        QueryFirstOrDefault_EntityType_NoCompatibleConstructor_PrivateParameterlessConstructor_ShouldUsePrivateConstructorAndProperties()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        this.Connection.QueryFirstOrDefault<EntityWithPrivateParameterlessConstructor>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entities[0]);
    }

    [Fact]
    public void
        QueryFirstOrDefault_EntityType_NoCompatibleConstructor_PublicParameterlessConstructor_ShouldUsePublicConstructorAndProperties()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        this.Connection.QueryFirstOrDefault<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entities[0]);
    }

    [Fact]
    public void QueryFirstOrDefault_EntityType_NonNullableEntityProperty_ColumnContainsNull_ShouldThrow()
    {
        this.Connection.ExecuteNonQuery(
            $"INSERT INTO {Q("EntityWithNonNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        Invoking(() =>
                this.Connection.QueryFirstOrDefault<EntityWithNonNullableProperty>(
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
    public void QueryFirstOrDefault_EntityType_NullableEntityProperty_ColumnContainsNull_ShouldReturnNull()
    {
        this.Connection.ExecuteNonQuery(
            $"INSERT INTO {Q("EntityWithNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        this.Connection.QueryFirstOrDefault<EntityWithNullableProperty>(
                $"SELECT * FROM {Q("EntityWithNullableProperty")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(new EntityWithNullableProperty { Id = 1, Value = null });
    }

    [Fact]
    public void QueryFirstOrDefault_EntityType_ShouldMaterializeBinaryData()
    {
        var bytes = Generate.Single<Byte[]>();

        this.Connection.QueryFirstOrDefault<EntityWithBinaryProperty>(
                $"SELECT {Parameter(bytes)} AS BinaryData",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(new EntityWithBinaryProperty { BinaryData = bytes });
    }

    [Fact]
    public void QueryFirstOrDefault_EntityType_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entities = this.CreateEntitiesInDb<EntityWithDateTimeOffset>(2);

        this.Connection.QueryFirstOrDefault<EntityWithDateTimeOffset>(
                $"SELECT * FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entities[0]);
    }

    [Fact]
    public void QueryFirstOrDefault_EntityType_UnsupportedFieldType_ShouldThrow()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.HasUnsupportedDataType, "");

        var literal = this.TestDatabaseProvider.GetUnsupportedDataTypeLiteral();

        Invoking(() =>
                this.Connection.QueryFirstOrDefault<EntityWithObjectProperty>(
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
    public void QueryFirstOrDefault_InterpolatedParameter_ShouldPassInterpolatedParameter()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        this.Connection.QueryFirstOrDefault<Entity>(
                $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entities[0].Id)}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entities[0]);
    }

    [Fact]
    public void QueryFirstOrDefault_Parameter_ShouldPassParameter()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        var statement = new InterpolatedSqlStatement(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entities[0].Id)
        );

        this.Connection.QueryFirstOrDefault<Entity>(statement, cancellationToken: TestContext.Current.CancellationToken)
            .Should().Be(entities[0]);
    }

    [Fact]
    public void QueryFirstOrDefault_QueryReturnedNoRows_ShouldReturnDefault()
    {
        this.Connection.QueryFirstOrDefault<Int32>(
                $"SELECT {Q("Id")} FROM {Q("Entity")} WHERE {Q("Id")} = -1",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(0);

        this.Connection.QueryFirstOrDefault<Entity>(
                $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = -1",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeNull();

        this.Connection.QueryFirstOrDefault<(Int64, String)>(
                $"SELECT {Q("Id")}, {Q("StringValue")} FROM {Q("Entity")} WHERE {Q("Id")} = -1",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(default);
    }

    [Fact]
    public void QueryFirstOrDefault_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        InterpolatedSqlStatement statement = $"SELECT {Q("Value")} AS Id FROM {TemporaryTable(entityIds)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        this.Connection.QueryFirstOrDefault<Int64>(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entityIds[0]);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public void
        QueryFirstOrDefault_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = this.CreateEntitiesInDb<Entity>(2);
        var entityIds = entities.Select(a => a.Id);

        this.Connection.QueryFirstOrDefault<Entity>(
                $"""
                 SELECT *
                 FROM   {Q("Entity")}
                 WHERE  {Q("Id")} IN (SELECT {Q("Value")} FROM {TemporaryTable(entityIds)})
                 """,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entities[0]);
    }

    [Fact]
    public void QueryFirstOrDefault_Transaction_ShouldUseTransaction()
    {
        using (var transaction = this.Connection.BeginTransaction())
        {
            var entities = this.CreateEntitiesInDb<Entity>(2, transaction);

            this.Connection.QueryFirstOrDefault<Entity>(
                    $"SELECT * FROM {Q("Entity")}",
                    transaction,
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .Should().Be(entities[0]);

            transaction.Rollback();
        }

        this.Connection.QueryFirstOrDefault<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeNull();
    }

    [Fact]
    public void
        QueryFirstOrDefault_ValueTupleType_CharValueTupleField_ColumnContainsStringWithLengthNotOne_ShouldThrow()
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            Invoking(() =>
                    this.Connection.QueryFirstOrDefault<ValueTuple<Char>>(
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
                this.Connection.QueryFirstOrDefault<ValueTuple<Char>>(
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
        QueryFirstOrDefault_ValueTupleType_CharValueTupleField_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<Char>();

        this.Connection.QueryFirstOrDefault<ValueTuple<Char>>(
                $"SELECT '{character}'",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(ValueTuple.Create(character));
    }

    [Fact]
    public void QueryFirstOrDefault_ValueTupleType_ColumnDataTypeNotCompatibleWithValueTupleFieldType_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstOrDefault<ValueTuple<TimeSpan>>(
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
    public void QueryFirstOrDefault_ValueTupleType_EnumValueTupleField_ColumnContainsInvalidInteger_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstOrDefault<ValueTuple<TestEnum>>(
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
    public void QueryFirstOrDefault_ValueTupleType_EnumValueTupleField_ColumnContainsInvalidString_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstOrDefault<ValueTuple<TestEnum>>(
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
    public void QueryFirstOrDefault_ValueTupleType_EnumValueTupleField_ShouldConvertIntegerToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        this.Connection.QueryFirstOrDefault<ValueTuple<TestEnum>>(
                $"SELECT {(Int32)enumValue}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(ValueTuple.Create(enumValue));
    }

    [Fact]
    public void QueryFirstOrDefault_ValueTupleType_EnumValueTupleField_ShouldConvertStringToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        this.Connection.QueryFirstOrDefault<ValueTuple<TestEnum>>(
                $"SELECT '{enumValue}'",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(ValueTuple.Create(enumValue));
    }

    [Fact]
    public void QueryFirstOrDefault_ValueTupleType_NonNullableValueTupleField_ColumnContainsNull_ShouldThrow()
    {
        this.Connection.ExecuteNonQuery(
            $"INSERT INTO {Q("EntityWithNonNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        Invoking(() =>
                this.Connection.QueryFirstOrDefault<ValueTuple<Int32>>(
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
    public void QueryFirstOrDefault_ValueTupleType_NullableValueTupleField_ColumnContainsNull_ShouldReturnNull()
    {
        this.Connection.ExecuteNonQuery(
            $"INSERT INTO {Q("EntityWithNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        this.Connection.QueryFirstOrDefault<ValueTuple<Int32?>>(
                $"SELECT {Q("Value")} FROM {Q("EntityWithNullableProperty")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(new(null));
    }

    [Fact]
    public void QueryFirstOrDefault_ValueTupleType_NumberOfColumnsDoesNotMatchNumberOfValueTupleFields_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstOrDefault<(Int32, Int32)>(
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
    public void QueryFirstOrDefault_ValueTupleType_ShouldMaterializeBinaryData()
    {
        var bytes = Generate.Single<Byte[]>();

        this.Connection.QueryFirstOrDefault<ValueTuple<Byte[]>>(
                $"SELECT {Parameter(bytes)} AS BinaryData",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(ValueTuple.Create(bytes));
    }

    [Fact]
    public void QueryFirstOrDefault_ValueTupleType_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entities = this.CreateEntitiesInDb<EntityWithDateTimeOffset>(2);

        this.Connection.QueryFirstOrDefault<(Int64 Id, DateTimeOffset DateTimeOffsetValue)>(
                $"SELECT {Q("Id")}, {Q("DateTimeOffsetValue")} FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be((entities[0].Id, entities[0].DateTimeOffsetValue));
    }

    [Fact]
    public void QueryFirstOrDefault_ValueTupleType_UnsupportedFieldType_ShouldThrow()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.HasUnsupportedDataType, "");

        var literal = this.TestDatabaseProvider.GetUnsupportedDataTypeLiteral();

        Invoking(() =>
                this.Connection.QueryFirstOrDefault<ValueTuple<Object>>(
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
        QueryFirstOrDefaultAsync_BuiltInType_CharTargetType_ColumnContainsStringWithLengthNotOne_ShouldThrow()
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            (await Invoking(() =>
                        this.Connection.QueryFirstOrDefaultAsync<Char>(
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
                    this.Connection.QueryFirstOrDefaultAsync<Char>(
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
        QueryFirstOrDefaultAsync_BuiltInType_CharTargetType_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<Char>();

        (await this.Connection.QueryFirstOrDefaultAsync<Char>(
                $"SELECT '{character}'",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(character);
    }

    [Fact]
    public Task QueryFirstOrDefaultAsync_BuiltInType_ColumnValueCannotBeConvertedToTargetType_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstOrDefaultAsync<Int32>(
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
    public Task QueryFirstOrDefaultAsync_BuiltInType_EnumTargetType_ColumnContainsInvalidInteger_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstOrDefaultAsync<TestEnum>(
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
    public Task QueryFirstOrDefaultAsync_BuiltInType_EnumTargetType_ColumnContainsInvalidString_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstOrDefaultAsync<TestEnum>(
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
    public async Task QueryFirstOrDefaultAsync_BuiltInType_EnumTargetType_ShouldConvertIntegerToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        (await this.Connection.QueryFirstOrDefaultAsync<TestEnum>(
                $"SELECT {(Int32)enumValue}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(enumValue);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_BuiltInType_EnumTargetType_ShouldConvertStringToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        (await this.Connection.QueryFirstOrDefaultAsync<TestEnum>(
                $"SELECT '{enumValue.ToString()}'",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(enumValue);
    }

    [Fact]
    public Task QueryFirstOrDefaultAsync_BuiltInType_NonNullableTargetType_ColumnContainsNull_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstOrDefaultAsync<Int32>(
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
    public async Task QueryFirstOrDefaultAsync_BuiltInType_NullableTargetType_ColumnContainsNull_ShouldReturnNull() =>
        (await this.Connection.QueryFirstOrDefaultAsync<Int32?>(
            "SELECT NULL",
            cancellationToken: TestContext.Current.CancellationToken
        ))
        .Should().BeNull();

    [Fact]
    public async Task QueryFirstOrDefaultAsync_BuiltInType_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entities = this.CreateEntitiesInDb<EntityWithDateTimeOffset>(2);

        (await this.Connection.QueryFirstOrDefaultAsync<DateTimeOffset>(
                $"SELECT {Q("DateTimeOffsetValue")} FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entities[0].DateTimeOffsetValue);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        await Invoking(() =>
                this.Connection.QueryFirstOrDefaultAsync<Entity>(
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: cancellationToken
                )
            )
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_CommandType_ShouldUseCommandType()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entities = this.CreateEntitiesInDb<Entity>(2);

        (await this.Connection.QueryFirstOrDefaultAsync<Entity>(
                "GetEntities",
                commandType: CommandType.StoredProcedure,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entities[0]);
    }

    [Fact]
    public async Task
        QueryFirstOrDefaultAsync_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(2);

        InterpolatedSqlStatement statement = $"SELECT * FROM {TemporaryTable(entities)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        (await this.Connection.QueryFirstOrDefaultAsync<Entity>(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entities[0]);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public async Task
        QueryFirstOrDefaultAsync_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(2);

        (await this.Connection.QueryFirstOrDefaultAsync<Entity>(
                $"SELECT * FROM {TemporaryTable(entities)}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entities[0]);
    }

    [Fact]
    public async Task
        QueryFirstOrDefaultAsync_EntityType_CharEntityProperty_ColumnContainsStringWithLengthNotOne_ShouldThrow()
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            await Invoking(() =>
                    this.Connection.QueryFirstOrDefaultAsync<EntityWithCharProperty>(
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
                this.Connection.QueryFirstOrDefaultAsync<EntityWithCharProperty>(
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
        QueryFirstOrDefaultAsync_EntityType_CharEntityProperty_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<Char>();

        (await this.Connection.QueryFirstOrDefaultAsync<EntityWithCharProperty>(
                $"SELECT '{character}' AS {Q("Char")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(new EntityWithCharProperty { Char = character });
    }

    [Fact]
    public Task QueryFirstOrDefaultAsync_EntityType_ColumnDataTypeNotCompatibleWithEntityPropertyType_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstOrDefaultAsync<Entity>(
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
    public async Task QueryFirstOrDefaultAsync_EntityType_ColumnHasNoName_ShouldThrow()
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
                this.Connection.QueryFirstOrDefaultAsync<Entity>(
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
    public async Task QueryFirstOrDefaultAsync_EntityType_CompatiblePrivateConstructor_ShouldUsePrivateConstructor()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        (await this.Connection.QueryFirstOrDefaultAsync<EntityWithPrivateConstructor>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entities[0]);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_EntityType_CompatiblePublicConstructor_ShouldUsePublicConstructor()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        (await this.Connection.QueryFirstOrDefaultAsync<EntityWithPublicConstructor>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entities[0]);
    }

    [Fact]
    public async Task
        QueryFirstOrDefaultAsync_EntityType_EntityTypeHasNoCorrespondingPropertyForColumn_ShouldIgnoreColumn()
    {
        var entity = (await Invoking(() =>
                this.Connection.QueryFirstOrDefaultAsync<EntityWithNonNullableProperty>(
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
        QueryFirstOrDefaultAsync_EntityType_EntityTypeWithPropertiesWithDifferentCasing_ShouldMaterializeEntities()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);
        var entitiesWithDifferentCasingProperties = Generate.MapTo<EntityWithDifferentCasingProperties>(entities);

        (await this.Connection.QueryFirstOrDefaultAsync<EntityWithDifferentCasingProperties>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entitiesWithDifferentCasingProperties[0]);
    }

    [Fact]
    public async Task
        QueryFirstOrDefaultAsync_EntityType_EnumEntityProperty_ColumnContainsInvalidInteger_ShouldThrow() =>
        await Invoking(() => this.Connection.QueryFirstOrDefaultAsync<EntityWithEnumStoredAsInteger>(
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
        QueryFirstOrDefaultAsync_EntityType_EnumEntityProperty_ColumnContainsInvalidString_ShouldThrow() =>
        await Invoking(() => this.Connection.QueryFirstOrDefaultAsync<EntityWithEnumStoredAsString>(
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
    public async Task QueryFirstOrDefaultAsync_EntityType_EnumEntityProperty_ShouldConvertIntegerToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        (await this.Connection.QueryFirstOrDefaultAsync<EntityWithEnumStoredAsInteger>(
                $"SELECT 1 AS {Q("Id")}, {(Int32)enumValue} AS {Q("Enum")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))!
            .Enum
            .Should().Be(enumValue);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_EntityType_EnumEntityProperty_ShouldConvertStringToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        (await this.Connection.QueryFirstOrDefaultAsync<EntityWithEnumStoredAsInteger>(
                $"SELECT 1 AS {Q("Id")}, '{enumValue.ToString()}' AS {Q("Enum")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))!
            .Enum
            .Should().Be(enumValue);
    }

    [Fact]
    public Task QueryFirstOrDefaultAsync_EntityType_NoCompatibleConstructor_NoParameterlessConstructor_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstOrDefaultAsync<EntityWithPublicConstructor>($"SELECT 1 AS {Q("NonExistent")}")
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
        QueryFirstOrDefaultAsync_EntityType_NoCompatibleConstructor_PrivateParameterlessConstructor_ShouldUsePrivateConstructorAndProperties()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        (await this.Connection.QueryFirstOrDefaultAsync<EntityWithPrivateParameterlessConstructor>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entities[0]);
    }

    [Fact]
    public async Task
        QueryFirstOrDefaultAsync_EntityType_NoCompatibleConstructor_PublicParameterlessConstructor_ShouldUsePublicConstructorAndProperties()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        (await this.Connection.QueryFirstOrDefaultAsync<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entities[0]);
    }

    [Fact]
    public Task QueryFirstOrDefaultAsync_EntityType_NonNullableEntityProperty_ColumnContainsNull_ShouldThrow()
    {
        this.Connection.ExecuteNonQuery(
            $"INSERT INTO {Q("EntityWithNonNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        return Invoking(() =>
                this.Connection.QueryFirstOrDefaultAsync<EntityWithNonNullableProperty>(
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
    public async Task QueryFirstOrDefaultAsync_EntityType_NullableEntityProperty_ColumnContainsNull_ShouldReturnNull()
    {
        await this.Connection.ExecuteNonQueryAsync(
            $"INSERT INTO {Q("EntityWithNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        (await this.Connection.QueryFirstOrDefaultAsync<EntityWithNullableProperty>(
                $"SELECT * FROM {Q("EntityWithNullableProperty")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(new EntityWithNullableProperty { Id = 1, Value = null });
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_EntityType_ShouldMaterializeBinaryData()
    {
        var bytes = Generate.Single<Byte[]>();

        (await this.Connection.QueryFirstOrDefaultAsync<EntityWithBinaryProperty>(
                $"SELECT {Parameter(bytes)} AS BinaryData",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(new EntityWithBinaryProperty { BinaryData = bytes });
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_EntityType_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entities = this.CreateEntitiesInDb<EntityWithDateTimeOffset>(2);

        (await this.Connection.QueryFirstOrDefaultAsync<EntityWithDateTimeOffset>(
                $"SELECT * FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entities[0]);
    }

    [Fact]
    public Task QueryFirstOrDefaultAsync_EntityType_UnsupportedFieldType_ShouldThrow()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.HasUnsupportedDataType, "");

        var literal = this.TestDatabaseProvider.GetUnsupportedDataTypeLiteral();

        return Invoking(() =>
                this.Connection.QueryFirstOrDefaultAsync<EntityWithObjectProperty>(
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
    public async Task QueryFirstOrDefaultAsync_InterpolatedParameter_ShouldPassInterpolatedParameter()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        (await this.Connection.QueryFirstOrDefaultAsync<Entity>(
                $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entities[0].Id)}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entities[0]);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_Parameter_ShouldPassParameter()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        var statement = new InterpolatedSqlStatement(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entities[0].Id)
        );

        (await this.Connection.QueryFirstOrDefaultAsync<Entity>(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entities[0]);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_QueryReturnedNoRows_ShouldReturnDefault()
    {
        (await this.Connection.QueryFirstOrDefaultAsync<Int32>(
                $"SELECT {Q("Id")} FROM {Q("Entity")} WHERE {Q("Id")} = -1",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(0);

        (await this.Connection.QueryFirstOrDefaultAsync<Entity>(
                $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = -1",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeNull();

        (await this.Connection.QueryFirstOrDefaultAsync<(Int64, String)>(
                $"SELECT {Q("Id")}, {Q("StringValue")} FROM {Q("Entity")} WHERE {Q("Id")} = -1",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(default);
    }

    [Fact]
    public async Task
        QueryFirstOrDefaultAsync_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        InterpolatedSqlStatement statement =
            $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable(entityIds)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        (await this.Connection.QueryFirstOrDefaultAsync<Int64>(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entityIds[0]);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public async Task
        QueryFirstOrDefaultAsync_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = this.CreateEntitiesInDb<Entity>(2);
        var entityIds = entities.ConvertAll(a => a.Id);

        (await this.Connection.QueryFirstOrDefaultAsync<Entity>(
                $"""
                 SELECT *
                 FROM   {Q("Entity")}
                 WHERE  {Q("Id")} IN (SELECT {Q("Value")} FROM {TemporaryTable(entityIds)})
                 """,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entities[0]);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_Transaction_ShouldUseTransaction()
    {
        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            var entities = this.CreateEntitiesInDb<Entity>(2, transaction);

            (await this.Connection.QueryFirstOrDefaultAsync<Entity>(
                    $"SELECT * FROM {Q("Entity")}",
                    transaction,
                    cancellationToken: TestContext.Current.CancellationToken
                ))
                .Should().Be(entities[0]);

            await transaction.RollbackAsync();
        }

        (await this.Connection.QueryFirstOrDefaultAsync<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeNull();
    }

    [Fact]
    public async Task
        QueryFirstOrDefaultAsync_ValueTupleType_CharValueTupleField_ColumnContainsStringWithLengthNotOne_ShouldThrow()
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            await Invoking(() =>
                    this.Connection.QueryFirstOrDefaultAsync<ValueTuple<Char>>(
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
                this.Connection.QueryFirstOrDefaultAsync<ValueTuple<Char>>(
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
        QueryFirstOrDefaultAsync_ValueTupleType_CharValueTupleField_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<Char>();

        (await this.Connection.QueryFirstOrDefaultAsync<ValueTuple<Char>>(
                $"SELECT '{character}'",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(ValueTuple.Create(character));
    }

    [Fact]
    public Task
        QueryFirstOrDefaultAsync_ValueTupleType_ColumnDataTypeNotCompatibleWithValueTupleFieldType_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstOrDefaultAsync<ValueTuple<TimeSpan>>(
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
        QueryFirstOrDefaultAsync_ValueTupleType_EnumValueTupleField_ColumnContainsInvalidInteger_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstOrDefaultAsync<ValueTuple<TestEnum>>(
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
    public Task QueryFirstOrDefaultAsync_ValueTupleType_EnumValueTupleField_ColumnContainsInvalidString_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstOrDefaultAsync<ValueTuple<TestEnum>>(
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
    public async Task QueryFirstOrDefaultAsync_ValueTupleType_EnumValueTupleField_ShouldConvertIntegerToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        (await this.Connection.QueryFirstOrDefaultAsync<ValueTuple<TestEnum>>(
                $"SELECT {(Int32)enumValue}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(ValueTuple.Create(enumValue));
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_ValueTupleType_EnumValueTupleField_ShouldConvertStringToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        (await this.Connection.QueryFirstOrDefaultAsync<ValueTuple<TestEnum>>(
                $"SELECT '{enumValue}'",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(ValueTuple.Create(enumValue));
    }

    [Fact]
    public Task QueryFirstOrDefaultAsync_ValueTupleType_NonNullableValueTupleField_ColumnContainsNull_ShouldThrow()
    {
        this.Connection.ExecuteNonQuery(
            $"INSERT INTO {Q("EntityWithNonNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        return Invoking(() =>
                this.Connection.QueryFirstOrDefaultAsync<ValueTuple<Int32>>(
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
        QueryFirstOrDefaultAsync_ValueTupleType_NullableValueTupleField_ColumnContainsNull_ShouldReturnNull()
    {
        await this.Connection.ExecuteNonQueryAsync(
            $"INSERT INTO {Q("EntityWithNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        (await this.Connection.QueryFirstOrDefaultAsync<ValueTuple<Int32?>>(
                $"SELECT {Q("Value")} FROM {Q("EntityWithNullableProperty")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(new(null));
    }

    [Fact]
    public Task
        QueryFirstOrDefaultAsync_ValueTupleType_NumberOfColumnsDoesNotMatchNumberOfValueTupleFields_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstOrDefaultAsync<(Int32, Int32)>(
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
    public async Task QueryFirstOrDefaultAsync_ValueTupleType_ShouldMaterializeBinaryData()
    {
        var bytes = Generate.Single<Byte[]>();

        (await this.Connection.QueryFirstOrDefaultAsync<ValueTuple<Byte[]>>(
                $"SELECT {Parameter(bytes)} AS BinaryData",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(ValueTuple.Create(bytes));
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_ValueTupleType_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entities = this.CreateEntitiesInDb<EntityWithDateTimeOffset>(2);

        (await this.Connection.QueryFirstOrDefaultAsync<(Int64 Id, DateTimeOffset DateTimeOffsetValue)>(
                $"SELECT {Q("Id")}, {Q("DateTimeOffsetValue")} FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be((entities[0].Id, entities[0].DateTimeOffsetValue));
    }

    [Fact]
    public Task QueryFirstOrDefaultAsync_ValueTupleType_UnsupportedFieldType_ShouldThrow()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.HasUnsupportedDataType, "");

        var literal = this.TestDatabaseProvider.GetUnsupportedDataTypeLiteral();

        return Invoking(() =>
                this.Connection.QueryFirstOrDefaultAsync<ValueTuple<Object>>(
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
