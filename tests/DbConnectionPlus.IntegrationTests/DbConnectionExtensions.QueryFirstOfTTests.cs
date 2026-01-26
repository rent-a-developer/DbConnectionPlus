namespace RentADeveloper.DbConnectionPlus.IntegrationTests;

public sealed class
    DbConnectionExtensions_QueryFirstOfTTests_MySql :
    DbConnectionExtensions_QueryFirstOfTTests<MySqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QueryFirstOfTTests_Oracle :
    DbConnectionExtensions_QueryFirstOfTTests<OracleTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QueryFirstOfTTests_PostgreSql :
    DbConnectionExtensions_QueryFirstOfTTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QueryFirstOfTTests_Sqlite :
    DbConnectionExtensions_QueryFirstOfTTests<SqliteTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QueryFirstOfTTests_SqlServer :
    DbConnectionExtensions_QueryFirstOfTTests<SqlServerTestDatabaseProvider>;

public abstract class
    DbConnectionExtensions_QueryFirstOfTTests<TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    [Fact]
    public void QueryFirst_BuiltInType_CharTargetType_ColumnContainsStringWithLengthNotOne_ShouldThrow()
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            Invoking(() =>
                    this.Connection.QueryFirst<Char>(
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
                    $"one character long."
                );
        }

        Invoking(() =>
                this.Connection.QueryFirst<Char>(
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
                $"one character long."
            );
    }

    [Fact]
    public void QueryFirst_BuiltInType_CharTargetType_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<Char>();

        this.Connection.QueryFirst<Char>(
                $"SELECT '{character}'",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(character);
    }

    [Fact]
    public void QueryFirst_BuiltInType_ColumnValueCannotBeConvertedToTargetType_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirst<Int32>(
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
    public void QueryFirst_BuiltInType_EnumTargetType_ColumnContainsInvalidInteger_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirst<TestEnum>(
                    "SELECT 999",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"The first column returned by the SQL statement contains the value '999*' (System.*), which " +
                $"could not be converted to the type {typeof(TestEnum)}. See inner exception for details.*"
            );

    [Fact]
    public void QueryFirst_BuiltInType_EnumTargetType_ColumnContainsInvalidString_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirst<TestEnum>(
                    "SELECT 'NonExistent'",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"The first column returned by the SQL statement contains the value 'NonExistent' " +
                $"({typeof(String)}), which could not be converted to the type {typeof(TestEnum)}. See inner " +
                $"exception for details.*"
            );

    [Fact]
    public void QueryFirst_BuiltInType_EnumTargetType_ShouldConvertIntegerToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        this.Connection.QueryFirst<TestEnum>(
                $"SELECT {(Int32)enumValue}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(enumValue);
    }

    [Fact]
    public void QueryFirst_BuiltInType_EnumTargetType_ShouldConvertStringToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        this.Connection.QueryFirst<TestEnum>(
                $"SELECT '{enumValue.ToString()}'",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(enumValue);
    }

    [Fact]
    public void QueryFirst_BuiltInType_NonNullableTargetType_ColumnContainsNull_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirst<Int32>(
                    "SELECT NULL",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"The first column returned by the SQL statement contains a NULL value, which could not be converted " +
                $"to the type {typeof(Int32)}. See inner exception for details.*"
            );

    [Fact]
    public void QueryFirst_BuiltInType_NullableTargetType_ColumnContainsNull_ShouldReturnNull() =>
        this.Connection.QueryFirst<Int32?>(
                "SELECT NULL",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeNull();

    [Fact]
    public void QueryFirst_BuiltInType_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entities = this.CreateEntitiesInDb<EntityWithDateTimeOffset>(2);

        this.Connection.QueryFirst<DateTimeOffset>(
                $"SELECT {Q("DateTimeOffsetValue")} FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entities[0].DateTimeOffsetValue);
    }

    [Fact]
    public void QueryFirst_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        Invoking(() =>
                this.Connection.QueryFirst<Entity>(
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: cancellationToken
                )
            )
            .Should().Throw<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Fact]
    public void QueryFirst_CommandType_ShouldUseCommandType()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entities = this.CreateEntitiesInDb<Entity>(2);

        this.Connection.QueryFirst<Entity>(
                "GetEntities",
                commandType: CommandType.StoredProcedure,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entities[0]);
    }

    [Fact]
    public void QueryFirst_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(2);

        InterpolatedSqlStatement statement = $"SELECT * FROM {TemporaryTable(entities)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        this.Connection.QueryFirst<Entity>(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entities[0]);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public void QueryFirst_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(2);

        this.Connection.QueryFirst<Entity>(
                $"SELECT * FROM {TemporaryTable(entities)}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entities[0]);
    }

    [Fact]
    public void QueryFirst_EntityType_CharEntityProperty_ColumnContainsStringWithLengthNotOne_ShouldThrow()
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            Invoking(() =>
                    this.Connection.QueryFirst<EntityWithCharProperty>(
                        $"SELECT '' AS {Q("Char")}",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                )
                .Should().Throw<InvalidCastException>()
                .WithMessage(
                    $"The column 'Char' returned by the SQL statement contains a value that could not be converted " +
                    $"to the type {typeof(Char)} of the corresponding property of the type " +
                    $"{typeof(EntityWithCharProperty)}. See inner exception for details.*"
                )
                .WithInnerException<InvalidCastException>()
                .WithMessage(
                    $"Could not convert the string '' to the type {typeof(Char)}. The string must be " +
                    $"exactly one character long."
                );
        }

        Invoking(() =>
                this.Connection.QueryFirst<EntityWithCharProperty>(
                    $"SELECT 'ab' AS {Q("Char")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"The column 'Char' returned by the SQL statement contains a value that could not be converted " +
                $"to the type {typeof(Char)} of the corresponding property of the type " +
                $"{typeof(EntityWithCharProperty)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(Char)}. The string must be " +
                $"exactly one character long."
            );
    }

    [Fact]
    public void QueryFirst_EntityType_CharEntityProperty_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<Char>();

        this.Connection.QueryFirst<EntityWithCharProperty>(
                $"SELECT '{character}' AS {Q("Char")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(new EntityWithCharProperty { Char = character });
    }

    [Fact]
    public void QueryFirst_EntityType_ColumnDataTypeNotCompatibleWithEntityPropertyType_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirst<Entity>(
                    $"SELECT 123 AS {Q("TimeSpanValue")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<ArgumentException>()
            .WithMessage(
                $"The data type System.* of the column 'TimeSpanValue' returned by the SQL statement is not " +
                $"compatible with the property type {typeof(TimeSpan)} of the corresponding property of the type " +
                $"{typeof(Entity)}.*"
            );

    [Fact]
    public void QueryFirst_EntityType_ColumnHasNoName_ShouldThrow()
    {
        InterpolatedSqlStatement statement = this.TestDatabaseProvider switch
        {
            SqlServerTestDatabaseProvider =>
                $"SELECT 1",

            PostgreSqlTestDatabaseProvider or OracleTestDatabaseProvider =>
                $"SELECT 1 AS \" \"",

            _ =>
                $"SELECT 1 AS ''"
        };

        Invoking(() =>
                this.Connection.QueryFirst<Entity>(
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
    public void QueryFirst_EntityType_CompatiblePrivateConstructor_ShouldUsePrivateConstructor()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        this.Connection.QueryFirst<EntityWithPrivateConstructor>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entities[0]);
    }

    [Fact]
    public void QueryFirst_EntityType_CompatiblePublicConstructor_ShouldUsePublicConstructor()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        this.Connection.QueryFirst<EntityWithPublicConstructor>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entities[0]);
    }

    [Fact]
    public void QueryFirst_EntityType_EntityTypeHasNoCorrespondingPropertyForColumn_ShouldIgnoreColumn()
    {
        var entity = Invoking(() =>
                this.Connection.QueryFirst<EntityWithNonNullableProperty>(
                    $"SELECT 1 AS {Q("Id")}, 2 AS {Q("Value")}, 3 AS {Q("NonExistent")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().NotThrow().Subject;

        entity
            .Should().BeEquivalentTo(new EntityWithNonNullableProperty { Id = 1, Value = 2 });
    }

    [Fact]
    public void QueryFirst_EntityType_EntityTypeWithPropertiesWithDifferentCasing_ShouldMaterializeEntities()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);
        var entitiesWithDifferentCasingProperties = Generate.MapTo<EntityWithDifferentCasingProperties>(entities);

        this.Connection.QueryFirst<EntityWithDifferentCasingProperties>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entitiesWithDifferentCasingProperties[0]);
    }

    [Fact]
    public void QueryFirst_EntityType_EnumEntityProperty_ColumnContainsInvalidInteger_ShouldThrow() =>
        Invoking(() => this.Connection.QueryFirst<EntityWithEnumStoredAsInteger>(
                    $"SELECT 1 AS {Q("Id")}, 999 AS {Q("Enum")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"The column 'Enum' returned by the SQL statement contains a value that could not be converted " +
                $"to the type {typeof(TestEnum)} of the corresponding property of the type " +
                $"{typeof(EntityWithEnumStoredAsInteger)}. See inner exception for details.*"
            )
            .WithInnerException(typeof(InvalidCastException))
            .WithMessage(
                $"Could not convert the value '999*' (System.*) to an enum member of the type " +
                $"{typeof(TestEnum)}. That value does not match any of the values of the enum's members.*"
            );

    [Fact]
    public void QueryFirst_EntityType_EnumEntityProperty_ColumnContainsInvalidString_ShouldThrow() =>
        Invoking(() => this.Connection.QueryFirst<EntityWithEnumStoredAsString>(
                    $"SELECT 1 AS {Q("Id")}, 'NonExistent' AS {Q("Enum")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"The column 'Enum' returned by the SQL statement contains a value that could not be converted to " +
                $"the type {typeof(TestEnum)} of the corresponding property of the type " +
                $"{typeof(EntityWithEnumStoredAsString)}. See inner exception for details.*"
            )
            .WithInnerException(typeof(InvalidCastException))
            .WithMessage(
                $"Could not convert the string 'NonExistent' to an enum member of the type {typeof(TestEnum)}. " +
                $"That string does not match any of the names of the enum's members.*"
            );

    [Fact]
    public void QueryFirst_EntityType_EnumEntityProperty_ShouldConvertIntegerToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        this.Connection.QueryFirst<EntityWithEnumStoredAsInteger>(
                $"SELECT 1 AS {Q("Id")}, {(Int32)enumValue} AS {Q("Enum")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Enum
            .Should().Be(enumValue);
    }

    [Fact]
    public void QueryFirst_EntityType_EnumEntityProperty_ShouldConvertStringToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        this.Connection.QueryFirst<EntityWithEnumStoredAsInteger>(
                $"SELECT 1 AS {Q("Id")}, '{enumValue.ToString()}' AS {Q("Enum")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Enum
            .Should().Be(enumValue);
    }

    [Fact]
    public void QueryFirst_EntityType_NoCompatibleConstructor_NoParameterlessConstructor_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirst<EntityWithPublicConstructor>($"SELECT 1 AS {Q("NonExistent")}")
            )
            .Should().Throw<ArgumentException>()
            .WithMessage(
                $"Could not materialize an instance of the type {typeof(EntityWithPublicConstructor)}. The type " +
                $"either needs to have a parameterless constructor or a constructor whose parameters match the " +
                $"columns returned by the SQL statement, e.g. a constructor that has the following " +
                $"signature:{Environment.NewLine}" +
                $"(* NonExistent).*"
            );

    [Fact]
    public void
        QueryFirst_EntityType_NoCompatibleConstructor_PrivateParameterlessConstructor_ShouldUsePrivateConstructorAndProperties()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        this.Connection.QueryFirst<EntityWithPrivateParameterlessConstructor>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entities[0]);
    }

    [Fact]
    public void
        QueryFirst_EntityType_NoCompatibleConstructor_PublicParameterlessConstructor_ShouldUsePublicConstructorAndProperties()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        this.Connection.QueryFirst<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entities[0]);
    }

    [Fact]
    public void QueryFirst_EntityType_NonNullableEntityProperty_ColumnContainsNull_ShouldThrow()
    {
        this.Connection.ExecuteNonQuery(
            $"INSERT INTO {Q("EntityWithNonNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        Invoking(() =>
                this.Connection.QueryFirst<EntityWithNonNullableProperty>(
                    $"SELECT * FROM {Q("EntityWithNonNullableProperty")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"The column 'Value' returned by the SQL statement contains a " +
                $"NULL value, but the corresponding property of the type {typeof(EntityWithNonNullableProperty)} " +
                $"is non-nullable.*"
            );
    }

    [Fact]
    public void QueryFirst_EntityType_NullableEntityProperty_ColumnContainsNull_ShouldReturnNull()
    {
        this.Connection.ExecuteNonQuery(
            $"INSERT INTO {Q("EntityWithNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        this.Connection.QueryFirst<EntityWithNullableProperty>(
                $"SELECT * FROM {Q("EntityWithNullableProperty")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(new EntityWithNullableProperty { Id = 1, Value = null });
    }

    [Fact]
    public void QueryFirst_EntityType_ShouldMaterializeBinaryData()
    {
        var bytes = Generate.Single<Byte[]>();

        this.Connection.QueryFirst<EntityWithBinaryProperty>(
                $"SELECT {Parameter(bytes)} AS BinaryData",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(new EntityWithBinaryProperty { BinaryData = bytes });
    }

    [Fact]
    public void QueryFirst_EntityType_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entities = this.CreateEntitiesInDb<EntityWithDateTimeOffset>(2);

        this.Connection.QueryFirst<EntityWithDateTimeOffset>(
                $"SELECT * FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entities[0]);
    }

    [Fact]
    public void QueryFirst_EntityType_UnsupportedFieldType_ShouldThrow()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.HasUnsupportedDataType, "");

        var literal = this.TestDatabaseProvider.GetUnsupportedDataTypeLiteral();

        Invoking(() =>
                this.Connection.QueryFirst<EntityWithObjectProperty>(
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
    public void QueryFirst_InterpolatedParameter_ShouldPassInterpolatedParameter()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        this.Connection.QueryFirst<Entity>(
                $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entities[0].Id)}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entities[0]);
    }

    [Fact]
    public void QueryFirst_Parameter_ShouldPassParameter()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        var statement = new InterpolatedSqlStatement(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entities[0].Id)
        );

        this.Connection.QueryFirst<Entity>(statement, cancellationToken: TestContext.Current.CancellationToken)
            .Should().Be(entities[0]);
    }

    [Fact]
    public void QueryFirst_QueryReturnedNoRows_ShouldThrow() =>
        Invoking(() => this.Connection.QueryFirst<Entity>(
                    $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = -1",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidOperationException>()
            .WithMessage(
                "The SQL statement did not return any rows."
            );

    [Fact]
    public void QueryFirst_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        InterpolatedSqlStatement statement = $"SELECT {Q("Value")} AS Id FROM {TemporaryTable(entityIds)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        this.Connection.QueryFirst<Int64>(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entityIds[0]);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public void QueryFirst_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = this.CreateEntitiesInDb<Entity>(2);
        var entityIds = entities.Select(a => a.Id);

        this.Connection.QueryFirst<Entity>(
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
    public void QueryFirst_Transaction_ShouldUseTransaction()
    {
        using (var transaction = this.Connection.BeginTransaction())
        {
            var entities = this.CreateEntitiesInDb<Entity>(2, transaction);

            this.Connection.QueryFirst<Entity>(
                    $"SELECT * FROM {Q("Entity")}",
                    transaction,
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .Should().Be(entities[0]);

            transaction.Rollback();
        }

        Invoking(() => this.Connection.QueryFirst<Entity>(
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void QueryFirst_ValueTupleType_CharValueTupleField_ColumnContainsStringWithLengthNotOne_ShouldThrow()
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            Invoking(() =>
                    this.Connection.QueryFirst<ValueTuple<Char>>(
                        $"SELECT '' AS {Q("Value")}",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                )
                .Should().Throw<InvalidCastException>()
                .WithMessage(
                    $"The column 'Value' returned by the SQL statement contains a value that could not be converted " +
                    $"to the type {typeof(Char)} of the corresponding field of the value tuple type " +
                    $"{typeof(ValueTuple<Char>)}. See inner exception for details.*"
                )
                .WithInnerException<InvalidCastException>()
                .WithMessage(
                    $"Could not convert the string '' to the type {typeof(Char)}. The string must be " +
                    $"exactly one character long."
                );
        }

        Invoking(() =>
                this.Connection.QueryFirst<ValueTuple<Char>>(
                    $"SELECT 'ab' AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"The column 'Value' returned by the SQL statement contains a value that could not be converted " +
                $"to the type {typeof(Char)} of the corresponding field of the value tuple type " +
                $"{typeof(ValueTuple<Char>)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(Char)}. The string must be " +
                $"exactly one character long."
            );
    }

    [Fact]
    public void
        QueryFirst_ValueTupleType_CharValueTupleField_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<Char>();

        this.Connection.QueryFirst<ValueTuple<Char>>(
                $"SELECT '{character}'",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(ValueTuple.Create(character));
    }

    [Fact]
    public void QueryFirst_ValueTupleType_ColumnDataTypeNotCompatibleWithValueTupleFieldType_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirst<ValueTuple<TimeSpan>>(
                    $"SELECT 123 AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<ArgumentException>()
            .WithMessage(
                $"The data type System.* of the column 'Value' returned by the SQL statement is not compatible with " +
                $"the field type {typeof(TimeSpan)} of the corresponding field of the value tuple type " +
                $"{typeof(ValueTuple<TimeSpan>)}.*"
            );

    [Fact]
    public void QueryFirst_ValueTupleType_EnumValueTupleField_ColumnContainsInvalidInteger_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirst<ValueTuple<TestEnum>>(
                    $"SELECT 999 AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"The column 'Value' returned by the SQL statement contains a value that could not be converted to " +
                $"the type {typeof(TestEnum)} of the corresponding field of the value tuple type " +
                $"{typeof(ValueTuple<TestEnum>)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value '999*' (System.*) to an enum member of the type " +
                $"{typeof(TestEnum)}. That value does not match any of the values of the enum's members.*"
            );

    [Fact]
    public void QueryFirst_ValueTupleType_EnumValueTupleField_ColumnContainsInvalidString_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirst<ValueTuple<TestEnum>>(
                    $"SELECT 'NonExistent' AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"The column 'Value' returned by the SQL statement contains a value that could not be converted to " +
                $"the type {typeof(TestEnum)} of the corresponding field of the value tuple type " +
                $"{typeof(ValueTuple<TestEnum>)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'NonExistent' to an enum member of the type {typeof(TestEnum)}. " +
                $"That string does not match any of the names of the enum's members.*"
            );

    [Fact]
    public void QueryFirst_ValueTupleType_EnumValueTupleField_ShouldConvertIntegerToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        this.Connection.QueryFirst<ValueTuple<TestEnum>>(
                $"SELECT {(Int32)enumValue}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(ValueTuple.Create(enumValue));
    }

    [Fact]
    public void QueryFirst_ValueTupleType_EnumValueTupleField_ShouldConvertStringToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        this.Connection.QueryFirst<ValueTuple<TestEnum>>(
                $"SELECT '{enumValue}'",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(ValueTuple.Create(enumValue));
    }

    [Fact]
    public void QueryFirst_ValueTupleType_NonNullableValueTupleField_ColumnContainsNull_ShouldThrow()
    {
        this.Connection.ExecuteNonQuery(
            $"INSERT INTO {Q("EntityWithNonNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        Invoking(() =>
                this.Connection.QueryFirst<ValueTuple<Int32>>(
                    $"SELECT {Q("Value")} FROM {Q("EntityWithNonNullableProperty")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"The column 'Value' returned by the SQL statement contains a NULL value, but the corresponding " +
                $"field of the value tuple type {typeof(ValueTuple<Int32>)} is non-nullable.*"
            );
    }

    [Fact]
    public void QueryFirst_ValueTupleType_NullableValueTupleField_ColumnContainsNull_ShouldReturnNull()
    {
        this.Connection.ExecuteNonQuery(
            $"INSERT INTO {Q("EntityWithNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        this.Connection.QueryFirst<ValueTuple<Int32?>>(
                $"SELECT {Q("Value")} FROM {Q("EntityWithNullableProperty")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(new(null));
    }

    [Fact]
    public void QueryFirst_ValueTupleType_NumberOfColumnsDoesNotMatchNumberOfValueTupleFields_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirst<(Int32, Int32)>(
                    "SELECT 1",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<ArgumentException>()
            .WithMessage(
                $"The SQL statement returned 1 column, but the value tuple type {typeof((Int32, Int32))} has 2 " +
                $"fields. Make sure that the SQL statement returns the same number of columns as the number of " +
                $"fields in the value tuple type.*"
            );

    [Fact]
    public void QueryFirst_ValueTupleType_ShouldMaterializeBinaryData()
    {
        var bytes = Generate.Single<Byte[]>();

        this.Connection.QueryFirst<ValueTuple<Byte[]>>(
                $"SELECT {Parameter(bytes)} AS BinaryData",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(ValueTuple.Create(bytes));
    }

    [Fact]
    public void QueryFirst_ValueTupleType_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entities = this.CreateEntitiesInDb<EntityWithDateTimeOffset>(2);

        this.Connection.QueryFirst<(Int64 Id, DateTimeOffset DateTimeOffsetValue)>(
                $"SELECT {Q("Id")}, {Q("DateTimeOffsetValue")} FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be((entities[0].Id, entities[0].DateTimeOffsetValue));
    }

    [Fact]
    public void QueryFirst_ValueTupleType_UnsupportedFieldType_ShouldThrow()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.HasUnsupportedDataType, "");

        var literal = this.TestDatabaseProvider.GetUnsupportedDataTypeLiteral();

        Invoking(() =>
                this.Connection.QueryFirst<ValueTuple<Object>>(
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
    public async Task QueryFirstAsync_BuiltInType_CharTargetType_ColumnContainsStringWithLengthNotOne_ShouldThrow()
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            (await Invoking(() =>
                        this.Connection.QueryFirstAsync<Char>(
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
                    $"one character long."
                );
        }

        (await Invoking(() =>
                    this.Connection.QueryFirstAsync<Char>(
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
                $"one character long."
            );
    }

    [Fact]
    public async Task
        QueryFirstAsync_BuiltInType_CharTargetType_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<Char>();

        (await this.Connection.QueryFirstAsync<Char>(
                $"SELECT '{character}'",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(character);
    }

    [Fact]
    public Task QueryFirstAsync_BuiltInType_ColumnValueCannotBeConvertedToTargetType_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstAsync<Int32>(
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
    public Task QueryFirstAsync_BuiltInType_EnumTargetType_ColumnContainsInvalidInteger_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstAsync<TestEnum>(
                    "SELECT 999",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                $"The first column returned by the SQL statement contains the value '999*' (System.*), which " +
                $"could not be converted to the type {typeof(TestEnum)}. See inner exception for details.*"
            );

    [Fact]
    public Task QueryFirstAsync_BuiltInType_EnumTargetType_ColumnContainsInvalidString_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstAsync<TestEnum>(
                    "SELECT 'NonExistent'",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                $"The first column returned by the SQL statement contains the value 'NonExistent' " +
                $"({typeof(String)}), which could not be converted to the type {typeof(TestEnum)}. See inner " +
                $"exception for details.*"
            );

    [Fact]
    public async Task QueryFirstAsync_BuiltInType_EnumTargetType_ShouldConvertIntegerToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        (await this.Connection.QueryFirstAsync<TestEnum>(
                $"SELECT {(Int32)enumValue}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(enumValue);
    }

    [Fact]
    public async Task QueryFirstAsync_BuiltInType_EnumTargetType_ShouldConvertStringToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        (await this.Connection.QueryFirstAsync<TestEnum>(
                $"SELECT '{enumValue.ToString()}'",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(enumValue);
    }

    [Fact]
    public Task QueryFirstAsync_BuiltInType_NonNullableTargetType_ColumnContainsNull_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstAsync<Int32>(
                    "SELECT NULL",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                $"The first column returned by the SQL statement contains a NULL value, which could not be converted " +
                $"to the type {typeof(Int32)}. See inner exception for details.*"
            );

    [Fact]
    public async Task QueryFirstAsync_BuiltInType_NullableTargetType_ColumnContainsNull_ShouldReturnNull() =>
        (await this.Connection.QueryFirstAsync<Int32?>(
            "SELECT NULL",
            cancellationToken: TestContext.Current.CancellationToken
        ))
        .Should().BeNull();

    [Fact]
    public async Task QueryFirstAsync_BuiltInType_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entities = this.CreateEntitiesInDb<EntityWithDateTimeOffset>(2);

        (await this.Connection.QueryFirstAsync<DateTimeOffset>(
                $"SELECT {Q("DateTimeOffsetValue")} FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entities[0].DateTimeOffsetValue);
    }

    [Fact]
    public async Task QueryFirstAsync_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        await Invoking(() =>
                this.Connection.QueryFirstAsync<Entity>(
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: cancellationToken
                )
            )
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Fact]
    public async Task QueryFirstAsync_CommandType_ShouldUseCommandType()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entities = this.CreateEntitiesInDb<Entity>(2);

        (await this.Connection.QueryFirstAsync<Entity>(
                "GetEntities",
                commandType: CommandType.StoredProcedure,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entities[0]);
    }

    [Fact]
    public async Task
        QueryFirstAsync_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(2);

        InterpolatedSqlStatement statement = $"SELECT * FROM {TemporaryTable(entities)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        (await this.Connection.QueryFirstAsync<Entity>(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entities[0]);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public async Task
        QueryFirstAsync_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(2);

        (await this.Connection.QueryFirstAsync<Entity>(
                $"SELECT * FROM {TemporaryTable(entities)}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entities[0]);
    }

    [Fact]
    public async Task
        QueryFirstAsync_EntityType_CharEntityProperty_ColumnContainsStringWithLengthNotOne_ShouldThrow()
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            await Invoking(() =>
                    this.Connection.QueryFirstAsync<EntityWithCharProperty>(
                        $"SELECT '' AS {Q("Char")}",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                )
                .Should().ThrowAsync<InvalidCastException>()
                .WithMessage(
                    $"The column 'Char' returned by the SQL statement contains a value that could not be converted " +
                    $"to the type {typeof(Char)} of the corresponding property of the type " +
                    $"{typeof(EntityWithCharProperty)}. See inner exception for details.*"
                )
                .WithInnerException(typeof(InvalidCastException))
                .WithMessage(
                    $"Could not convert the string '' to the type {typeof(Char)}. The string must be " +
                    $"exactly one character long."
                );
        }

        await Invoking(() =>
                this.Connection.QueryFirstAsync<EntityWithCharProperty>(
                    $"SELECT 'ab' AS {Q("Char")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                $"The column 'Char' returned by the SQL statement contains a value that could not be converted " +
                $"to the type {typeof(Char)} of the corresponding property of the type " +
                $"{typeof(EntityWithCharProperty)}. See inner exception for details.*"
            )
            .WithInnerException(typeof(InvalidCastException))
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(Char)}. The string must be " +
                $"exactly one character long."
            );
    }

    [Fact]
    public async Task
        QueryFirstAsync_EntityType_CharEntityProperty_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<Char>();

        (await this.Connection.QueryFirstAsync<EntityWithCharProperty>(
                $"SELECT '{character}' AS {Q("Char")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(new EntityWithCharProperty { Char = character });
    }

    [Fact]
    public Task QueryFirstAsync_EntityType_ColumnDataTypeNotCompatibleWithEntityPropertyType_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstAsync<Entity>(
                    $"SELECT 123 AS {Q("TimeSpanValue")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage(
                $"The data type System.* of the column 'TimeSpanValue' returned by the SQL statement is not " +
                $"compatible with the property type {typeof(TimeSpan)} of the corresponding property of the type " +
                $"{typeof(Entity)}.*"
            );

    [Fact]
    public async Task QueryFirstAsync_EntityType_ColumnHasNoName_ShouldThrow()
    {
        InterpolatedSqlStatement statement = this.TestDatabaseProvider switch
        {
            SqlServerTestDatabaseProvider =>
                $"SELECT 1",

            PostgreSqlTestDatabaseProvider or OracleTestDatabaseProvider =>
                $"SELECT 1 AS \" \"",

            _ =>
                $"SELECT 1 AS ''"
        };

        await Invoking(() =>
                this.Connection.QueryFirstAsync<Entity>(
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
    public async Task QueryFirstAsync_EntityType_CompatiblePrivateConstructor_ShouldUsePrivateConstructor()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        (await this.Connection.QueryFirstAsync<EntityWithPrivateConstructor>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entities[0]);
    }

    [Fact]
    public async Task QueryFirstAsync_EntityType_CompatiblePublicConstructor_ShouldUsePublicConstructor()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        (await this.Connection.QueryFirstAsync<EntityWithPublicConstructor>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entities[0]);
    }

    [Fact]
    public async Task QueryFirstAsync_EntityType_EntityTypeHasNoCorrespondingPropertyForColumn_ShouldIgnoreColumn()
    {
        var entity = (await Invoking(() =>
                this.Connection.QueryFirstAsync<EntityWithNonNullableProperty>(
                    $"SELECT 1 AS {Q("Id")}, 2 AS {Q("Value")}, 3 AS {Q("NonExistent")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().NotThrowAsync()).Subject;

        entity
            .Should().BeEquivalentTo(new EntityWithNonNullableProperty { Id = 1, Value = 2 });
    }

    [Fact]
    public async Task QueryFirstAsync_EntityType_EntityTypeWithPropertiesWithDifferentCasing_ShouldMaterializeEntities()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);
        var entitiesWithDifferentCasingProperties = Generate.MapTo<EntityWithDifferentCasingProperties>(entities);

        (await this.Connection.QueryFirstAsync<EntityWithDifferentCasingProperties>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entitiesWithDifferentCasingProperties[0]);
    }

    [Fact]
    public async Task QueryFirstAsync_EntityType_EnumEntityProperty_ColumnContainsInvalidInteger_ShouldThrow() =>
        await Invoking(() => this.Connection.QueryFirstAsync<EntityWithEnumStoredAsInteger>(
                    $"SELECT 1 AS {Q("Id")}, 999 AS {Q("Enum")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                $"The column 'Enum' returned by the SQL statement contains a value that could not be converted to " +
                $"the type {typeof(TestEnum)} of the corresponding property of the type " +
                $"{typeof(EntityWithEnumStoredAsInteger)}. See inner exception for details.*"
            )
            .WithInnerException(typeof(InvalidCastException))
            .WithMessage(
                $"Could not convert the value '999*' (System.*) to an enum member of the type " +
                $"{typeof(TestEnum)}. That value does not match any of the values of the enum's members.*"
            );

    [Fact]
    public async Task QueryFirstAsync_EntityType_EnumEntityProperty_ColumnContainsInvalidString_ShouldThrow() =>
        await Invoking(() => this.Connection.QueryFirstAsync<EntityWithEnumStoredAsString>(
                    $"SELECT 1 AS {Q("Id")}, 'NonExistent' AS {Q("Enum")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                $"The column 'Enum' returned by the SQL statement contains a value that could not be converted to " +
                $"the type {typeof(TestEnum)} of the corresponding property of the type " +
                $"{typeof(EntityWithEnumStoredAsString)}. See inner exception for details.*"
            )
            .WithInnerException(typeof(InvalidCastException))
            .WithMessage(
                $"Could not convert the string 'NonExistent' to an enum member of the type {typeof(TestEnum)}. " +
                $"That string does not match any of the names of the enum's members.*"
            );

    [Fact]
    public async Task QueryFirstAsync_EntityType_EnumEntityProperty_ShouldConvertIntegerToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        (await this.Connection.QueryFirstAsync<EntityWithEnumStoredAsInteger>(
                $"SELECT 1 AS {Q("Id")}, {(Int32)enumValue} AS {Q("Enum")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Enum
            .Should().Be(enumValue);
    }

    [Fact]
    public async Task QueryFirstAsync_EntityType_EnumEntityProperty_ShouldConvertStringToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        (await this.Connection.QueryFirstAsync<EntityWithEnumStoredAsInteger>(
                $"SELECT 1 AS {Q("Id")}, '{enumValue.ToString()}' AS {Q("Enum")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Enum
            .Should().Be(enumValue);
    }

    [Fact]
    public Task QueryFirstAsync_EntityType_NoCompatibleConstructor_NoParameterlessConstructor_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstAsync<EntityWithPublicConstructor>($"SELECT 1 AS {Q("NonExistent")}")
            )
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage(
                $"Could not materialize an instance of the type {typeof(EntityWithPublicConstructor)}. The type " +
                $"either needs to have a parameterless constructor or a constructor whose parameters match the " +
                $"columns returned by the SQL statement, e.g. a constructor that has the following " +
                $"signature:{Environment.NewLine}" +
                $"(* NonExistent).*"
            );

    [Fact]
    public async Task
        QueryFirstAsync_EntityType_NoCompatibleConstructor_PrivateParameterlessConstructor_ShouldUsePrivateConstructorAndProperties()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        (await this.Connection.QueryFirstAsync<EntityWithPrivateParameterlessConstructor>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(entities[0]);
    }

    [Fact]
    public async Task
        QueryFirstAsync_EntityType_NoCompatibleConstructor_PublicParameterlessConstructor_ShouldUsePublicConstructorAndProperties()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        (await this.Connection.QueryFirstAsync<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entities[0]);
    }

    [Fact]
    public Task QueryFirstAsync_EntityType_NonNullableEntityProperty_ColumnContainsNull_ShouldThrow()
    {
        this.Connection.ExecuteNonQuery(
            $"INSERT INTO {Q("EntityWithNonNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        return Invoking(() =>
                this.Connection.QueryFirstAsync<EntityWithNonNullableProperty>(
                    $"SELECT * FROM {Q("EntityWithNonNullableProperty")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                $"The column 'Value' returned by the SQL statement contains a NULL value, but the corresponding " +
                $"property of the type {typeof(EntityWithNonNullableProperty)} is non-nullable.*"
            );
    }

    [Fact]
    public async Task QueryFirstAsync_EntityType_NullableEntityProperty_ColumnContainsNull_ShouldReturnNull()
    {
        await this.Connection.ExecuteNonQueryAsync(
            $"INSERT INTO {Q("EntityWithNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        (await this.Connection.QueryFirstAsync<EntityWithNullableProperty>(
                $"SELECT * FROM {Q("EntityWithNullableProperty")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(new EntityWithNullableProperty { Id = 1, Value = null });
    }

    [Fact]
    public async Task QueryFirstAsync_EntityType_ShouldMaterializeBinaryData()
    {
        var bytes = Generate.Single<Byte[]>();

        (await this.Connection.QueryFirstAsync<EntityWithBinaryProperty>(
                $"SELECT {Parameter(bytes)} AS BinaryData",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(new EntityWithBinaryProperty { BinaryData = bytes });
    }

    [Fact]
    public async Task QueryFirstAsync_EntityType_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entities = this.CreateEntitiesInDb<EntityWithDateTimeOffset>(2);

        (await this.Connection.QueryFirstAsync<EntityWithDateTimeOffset>(
                $"SELECT * FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entities[0]);
    }

    [Fact]
    public Task QueryFirstAsync_EntityType_UnsupportedFieldType_ShouldThrow()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.HasUnsupportedDataType, "");

        var literal = this.TestDatabaseProvider.GetUnsupportedDataTypeLiteral();

        return Invoking(() =>
                this.Connection.QueryFirstAsync<EntityWithObjectProperty>(
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
    public async Task QueryFirstAsync_InterpolatedParameter_ShouldPassInterpolatedParameter()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        (await this.Connection.QueryFirstAsync<Entity>(
                $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entities[0].Id)}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entities[0]);
    }

    [Fact]
    public async Task QueryFirstAsync_Parameter_ShouldPassParameter()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        var statement = new InterpolatedSqlStatement(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entities[0].Id)
        );

        (await this.Connection.QueryFirstAsync<Entity>(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entities[0]);
    }

    [Fact]
    public Task QueryFirstAsync_QueryReturnedNoRows_ShouldThrow() =>
        Invoking(() => this.Connection.QueryFirstAsync<Entity>(
                    $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = -1",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(
                "The SQL statement did not return any rows."
            );

    [Fact]
    public async Task
        QueryFirstAsync_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        InterpolatedSqlStatement statement =
            $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable(entityIds)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        (await this.Connection.QueryFirstAsync<Int64>(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entityIds[0]);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public async Task
        QueryFirstAsync_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = this.CreateEntitiesInDb<Entity>(2);
        var entityIds = entities.Select(a => a.Id).ToList();

        (await this.Connection.QueryFirstAsync<Entity>(
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
    public async Task QueryFirstAsync_Transaction_ShouldUseTransaction()
    {
        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            var entities = this.CreateEntitiesInDb<Entity>(2, transaction);

            (await this.Connection.QueryFirstAsync<Entity>(
                    $"SELECT * FROM {Q("Entity")}",
                    transaction,
                    cancellationToken: TestContext.Current.CancellationToken
                ))
                .Should().Be(entities[0]);

            await transaction.RollbackAsync();
        }

        await Invoking(() => this.Connection.QueryFirstAsync<Entity>(
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task
        QueryFirstAsync_ValueTupleType_CharValueTupleField_ColumnContainsStringWithLengthNotOne_ShouldThrow()
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            await Invoking(() =>
                    this.Connection.QueryFirstAsync<ValueTuple<Char>>(
                        $"SELECT '' AS {Q("Value")}",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                )
                .Should().ThrowAsync<InvalidCastException>()
                .WithMessage(
                    $"The column 'Value' returned by the SQL statement contains a value that could not be converted " +
                    $"to the type {typeof(Char)} of the corresponding field of the value tuple type " +
                    $"{typeof(ValueTuple<Char>)}. See inner exception for details.*"
                )
                .WithInnerException(typeof(InvalidCastException))
                .WithMessage(
                    $"Could not convert the string '' to the type {typeof(Char)}. The string must be " +
                    $"exactly one character long."
                );
        }

        await Invoking(() =>
                this.Connection.QueryFirstAsync<ValueTuple<Char>>(
                    $"SELECT 'ab' AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                $"The column 'Value' returned by the SQL statement contains a value that could not be converted " +
                $"to the type {typeof(Char)} of the corresponding field of the value tuple type " +
                $"{typeof(ValueTuple<Char>)}. See inner exception for details.*"
            )
            .WithInnerException(typeof(InvalidCastException))
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(Char)}. The string must be " +
                $"exactly one character long."
            );
    }

    [Fact]
    public async Task
        QueryFirstAsync_ValueTupleType_CharValueTupleField_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<Char>();

        (await this.Connection.QueryFirstAsync<ValueTuple<Char>>(
                $"SELECT '{character}'",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(ValueTuple.Create(character));
    }

    [Fact]
    public Task QueryFirstAsync_ValueTupleType_ColumnDataTypeNotCompatibleWithValueTupleFieldType_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstAsync<ValueTuple<TimeSpan>>(
                    $"SELECT 123 AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage(
                $"The data type System.* of the column 'Value' returned by the SQL statement is not compatible with " +
                $"the field type {typeof(TimeSpan)} of the corresponding field of the value tuple type " +
                $"{typeof(ValueTuple<TimeSpan>)}.*"
            );

    [Fact]
    public Task QueryFirstAsync_ValueTupleType_EnumValueTupleField_ColumnContainsInvalidInteger_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstAsync<ValueTuple<TestEnum>>(
                    $"SELECT 999 AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                $"The column 'Value' returned by the SQL statement contains a value that could not be converted to " +
                $"the type {typeof(TestEnum)} of the corresponding field of the value tuple type " +
                $"{typeof(ValueTuple<TestEnum>)}. See inner exception for details.*"
            )
            .WithInnerException(typeof(InvalidCastException))
            .WithMessage(
                $"Could not convert the value '999*' (System.*) to an enum member of the type " +
                $"{typeof(TestEnum)}. That value does not match any of the values of the enum's members.*"
            );

    [Fact]
    public Task QueryFirstAsync_ValueTupleType_EnumValueTupleField_ColumnContainsInvalidString_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstAsync<ValueTuple<TestEnum>>(
                    $"SELECT 'NonExistent' AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                $"The column 'Value' returned by the SQL statement contains a value that could not be converted to " +
                $"the type {typeof(TestEnum)} of the corresponding field of the value tuple type " +
                $"{typeof(ValueTuple<TestEnum>)}. See inner exception for details.*"
            )
            .WithInnerException(typeof(InvalidCastException))
            .WithMessage(
                $"Could not convert the string 'NonExistent' to an enum member of the type {typeof(TestEnum)}. " +
                $"That string does not match any of the names of the enum's members.*"
            );

    [Fact]
    public async Task QueryFirstAsync_ValueTupleType_EnumValueTupleField_ShouldConvertIntegerToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        (await this.Connection.QueryFirstAsync<ValueTuple<TestEnum>>(
                $"SELECT {(Int32)enumValue}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(ValueTuple.Create(enumValue));
    }

    [Fact]
    public async Task QueryFirstAsync_ValueTupleType_EnumValueTupleField_ShouldConvertStringToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        (await this.Connection.QueryFirstAsync<ValueTuple<TestEnum>>(
                $"SELECT '{enumValue}'",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(ValueTuple.Create(enumValue));
    }

    [Fact]
    public Task QueryFirstAsync_ValueTupleType_NonNullableValueTupleField_ColumnContainsNull_ShouldThrow()
    {
        this.Connection.ExecuteNonQuery(
            $"INSERT INTO {Q("EntityWithNonNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        return Invoking(() =>
                this.Connection.QueryFirstAsync<ValueTuple<Int32>>(
                    $"SELECT {Q("Value")} FROM {Q("EntityWithNonNullableProperty")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                $"The column 'Value' returned by the SQL statement contains a NULL value, but the corresponding " +
                $"field of the value tuple type {typeof(ValueTuple<Int32>)} is non-nullable.*"
            );
    }

    [Fact]
    public async Task QueryFirstAsync_ValueTupleType_NullableValueTupleField_ColumnContainsNull_ShouldReturnNull()
    {
        await this.Connection.ExecuteNonQueryAsync(
            $"INSERT INTO {Q("EntityWithNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        (await this.Connection.QueryFirstAsync<ValueTuple<Int32?>>(
                $"SELECT {Q("Value")} FROM {Q("EntityWithNullableProperty")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(new(null));
    }

    [Fact]
    public Task QueryFirstAsync_ValueTupleType_NumberOfColumnsDoesNotMatchNumberOfValueTupleFields_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryFirstAsync<(Int32, Int32)>(
                    "SELECT 1",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage(
                $"The SQL statement returned 1 column, but the value tuple type {typeof((Int32, Int32))} has 2 " +
                $"fields. Make sure that the SQL statement returns the same number of columns as the number of " +
                $"fields in the value tuple type.*"
            );

    [Fact]
    public async Task QueryFirstAsync_ValueTupleType_ShouldMaterializeBinaryData()
    {
        var bytes = Generate.Single<Byte[]>();

        (await this.Connection.QueryFirstAsync<ValueTuple<Byte[]>>(
                $"SELECT {Parameter(bytes)} AS BinaryData",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeEquivalentTo(ValueTuple.Create(bytes));
    }

    [Fact]
    public async Task QueryFirstAsync_ValueTupleType_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entities = this.CreateEntitiesInDb<EntityWithDateTimeOffset>(2);

        (await this.Connection.QueryFirstAsync<(Int64 Id, DateTimeOffset DateTimeOffsetValue)>(
                $"SELECT {Q("Id")}, {Q("DateTimeOffsetValue")} FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be((entities[0].Id, entities[0].DateTimeOffsetValue));
    }

    [Fact]
    public Task QueryFirstAsync_ValueTupleType_UnsupportedFieldType_ShouldThrow()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.HasUnsupportedDataType, "");

        var literal = this.TestDatabaseProvider.GetUnsupportedDataTypeLiteral();

        return Invoking(() =>
                this.Connection.QueryFirstAsync<ValueTuple<Object>>(
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
