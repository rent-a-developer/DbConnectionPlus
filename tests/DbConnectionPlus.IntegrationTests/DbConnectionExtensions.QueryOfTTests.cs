namespace RentADeveloper.DbConnectionPlus.IntegrationTests;

public sealed class
    DbConnectionExtensions_QueryOfTTests_MySql :
    DbConnectionExtensions_QueryOfTTests<MySqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QueryOfTTests_Oracle :
    DbConnectionExtensions_QueryOfTTests<OracleTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QueryOfTTests_PostgreSql :
    DbConnectionExtensions_QueryOfTTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QueryOfTTests_Sqlite :
    DbConnectionExtensions_QueryOfTTests<SqliteTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QueryOfTTests_SqlServer :
    DbConnectionExtensions_QueryOfTTests<SqlServerTestDatabaseProvider>;

public abstract class
    DbConnectionExtensions_QueryOfTTests<TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    [Fact]
    public void Query_BuiltInType_CharTargetType_ColumnContainsStringWithLengthNotOne_ShouldThrow()
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            Invoking(() =>
                    this.Connection.Query<Char>(
                        "SELECT ''",
                        cancellationToken: TestContext.Current.CancellationToken
                    ).ToList()
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
                this.Connection.Query<Char>(
                    "SELECT 'ab'",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToList()
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
    public void Query_BuiltInType_CharTargetType_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<Char>();

        this.Connection.Query<Char>(
                $"SELECT '{character}'",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo([character]);
    }

    [Fact]
    public void Query_BuiltInType_ColumnValueCannotBeConvertedToTargetType_ShouldThrow() =>
        Invoking(() =>
                this.Connection.Query<Int32>(
                    "SELECT 'A'",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToList()
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"The first column returned by the SQL statement contains the value 'A' ({typeof(String)}), which " +
                $"could not be converted to the type {typeof(Int32)}. See inner exception for details.*"
            );

    [Fact]
    public void Query_BuiltInType_EnumTargetType_ColumnContainsInvalidInteger_ShouldThrow() =>
        Invoking(() =>
                this.Connection.Query<TestEnum>(
                    "SELECT 999",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToList()
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"The first column returned by the SQL statement contains the value '999*' (System.*), which " +
                $"could not be converted to the type {typeof(TestEnum)}. See inner exception for details.*"
            );

    [Fact]
    public void Query_BuiltInType_EnumTargetType_ColumnContainsInvalidString_ShouldThrow() =>
        Invoking(() =>
                this.Connection.Query<TestEnum>(
                    "SELECT 'NonExistent'",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToList()
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"The first column returned by the SQL statement contains the value 'NonExistent' " +
                $"({typeof(String)}), which could not be converted to the type {typeof(TestEnum)}. See inner " +
                $"exception for details.*"
            );

    [Fact]
    public void Query_BuiltInType_EnumTargetType_ShouldConvertIntegerToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        this.Connection.Query<TestEnum>(
                $"SELECT {(Int32)enumValue}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo([enumValue]);
    }

    [Fact]
    public void Query_BuiltInType_EnumTargetType_ShouldConvertStringToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        this.Connection.Query<TestEnum>(
                $"SELECT '{enumValue.ToString()}'",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo([enumValue]);
    }

    [Fact]
    public void Query_BuiltInType_NonNullableTargetType_ColumnContainsNull_ShouldThrow() =>
        Invoking(() =>
                this.Connection.Query<Int32>(
                    "SELECT NULL",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToList()
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"The first column returned by the SQL statement contains a NULL value, which could not be converted " +
                $"to the type {typeof(Int32)}. See inner exception for details.*"
            );

    [Fact]
    public void Query_BuiltInType_NullableTargetType_ColumnContainsNull_ShouldReturnNull() =>
        this.Connection.Query<Int32?>(
                "SELECT NULL",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(new Int32?[] { null });

    [Fact]
    public void Query_BuiltInType_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entities = this.CreateEntitiesInDb<EntityWithDateTimeOffset>();

        this.Connection.Query<DateTimeOffset>(
                $"SELECT {Q("DateTimeOffsetValue")} FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entities.Select(e => e.DateTimeOffsetValue));
    }

    [Fact]
    public void Query_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        Invoking(() =>
                this.Connection.Query<Entity>(
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: cancellationToken
                ).ToList()
            )
            .Should().Throw<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Fact]
    public void Query_CommandType_ShouldUseCommandType()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entities = this.CreateEntitiesInDb<Entity>();

        this.Connection.Query<Entity>(
                "GetEntities",
                commandType: CommandType.StoredProcedure,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entities);
    }

    [Fact]
    public void Query_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterEnumerationIsFinished()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(2);

        InterpolatedSqlStatement statement = $"SELECT * FROM {TemporaryTable(entities)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        var enumerator = this.Connection.Query<Entity>(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        ).GetEnumerator();

        enumerator.MoveNext()
            .Should().BeTrue();

        if (this.TestDatabaseProvider.SupportsCommandExecutionWhileDataReaderIsOpen)
        {
            this.ExistsTemporaryTableInDb(temporaryTableName)
                .Should().BeTrue();
        }

        enumerator.MoveNext()
            .Should().BeTrue();

        enumerator.MoveNext()
            .Should().BeFalse();

        enumerator.Dispose();

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public void Query_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable()
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
    public void Query_EntityType_CharEntityProperty_ColumnContainsStringWithLengthNotOne_ShouldThrow()
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            Invoking(() =>
                    this.Connection.Query<EntityWithCharProperty>(
                        $"SELECT '' AS {Q("Char")}",
                        cancellationToken: TestContext.Current.CancellationToken
                    ).ToList()
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
                this.Connection.Query<EntityWithCharProperty>(
                    $"SELECT 'ab' AS {Q("Char")}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToList()
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
    public void Query_EntityType_CharEntityProperty_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<Char>();

        this.Connection.Query<EntityWithCharProperty>(
                $"SELECT '{character}' AS {Q("Char")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo([new EntityWithCharProperty { Char = character }]);
    }

    [Fact]
    public void Query_EntityType_ColumnDataTypeNotCompatibleWithEntityPropertyType_ShouldThrow() =>
        Invoking(() =>
                this.Connection.Query<Entity>(
                    $"SELECT 123 AS {Q("TimeSpanValue")}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToList()
            )
            .Should().Throw<ArgumentException>()
            .WithMessage(
                $"The data type System.* of the column 'TimeSpanValue' returned by the SQL statement is not " +
                $"compatible with the property type {typeof(TimeSpan)} of the corresponding property of the type " +
                $"{typeof(Entity)}.*"
            );

    [Fact]
    public void Query_EntityType_ColumnHasNoName_ShouldThrow()
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
                this.Connection.Query<Entity>(
                    statement,
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToList()
            )
            .Should().Throw<ArgumentException>()
            .WithMessage(
                "The 1st column returned by the SQL statement does not have a name. Make sure that all columns the " +
                "statement returns have a name.*"
            );
    }

    [Fact]
    public void Query_EntityType_CompatiblePrivateConstructor_ShouldUsePrivateConstructor()
    {
        var entities = this.CreateEntitiesInDb<Entity>();

        this.Connection.Query<EntityWithPrivateConstructor>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entities);
    }

    [Fact]
    public void Query_EntityType_CompatiblePublicConstructor_ShouldUsePublicConstructor()
    {
        var entities = this.CreateEntitiesInDb<Entity>();

        this.Connection.Query<EntityWithPublicConstructor>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entities);
    }

    [Fact]
    public void Query_EntityType_EntityTypeHasNoCorrespondingPropertyForColumn_ShouldIgnoreColumn()
    {
        var entities = Invoking(() =>
                this.Connection.Query<EntityWithNonNullableProperty>(
                    $"SELECT 1 AS {Q("Id")}, 2 AS {Q("Value")}, 3 AS {Q("NonExistent")}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToList()
            )
            .Should().NotThrow().Subject;

        entities
            .Should().BeEquivalentTo([new EntityWithNonNullableProperty { Id = 1, Value = 2 }]);
    }

    [Fact]
    public void Query_EntityType_EntityTypeWithPropertiesWithDifferentCasing_ShouldMaterializeEntities()
    {
        var entities = this.CreateEntitiesInDb<Entity>();
        var entitiesWithDifferentCasingProperties = Generate.MapTo<EntityWithDifferentCasingProperties>(entities);

        this.Connection.Query<EntityWithDifferentCasingProperties>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entitiesWithDifferentCasingProperties);
    }

    [Fact]
    public void Query_EntityType_EnumEntityProperty_ColumnContainsInvalidInteger_ShouldThrow() =>
        Invoking(() => this.Connection.Query<EntityWithEnumStoredAsInteger>(
                    $"SELECT 1 AS {Q("Id")}, 999 AS {Q("Enum")}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToList()
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
    public void Query_EntityType_EnumEntityProperty_ColumnContainsInvalidString_ShouldThrow() =>
        Invoking(() => this.Connection.Query<EntityWithEnumStoredAsString>(
                    $"SELECT 1 AS {Q("Id")}, 'NonExistent' AS {Q("Enum")}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToList()
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
    public void Query_EntityType_EnumEntityProperty_ShouldConvertIntegerToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        this.Connection.Query<EntityWithEnumStoredAsInteger>(
                $"SELECT 1 AS {Q("Id")}, {(Int32)enumValue} AS {Q("Enum")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Single().Enum
            .Should().Be(enumValue);
    }

    [Fact]
    public void Query_EntityType_EnumEntityProperty_ShouldConvertStringToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        this.Connection.Query<EntityWithEnumStoredAsInteger>(
                $"SELECT 1 AS {Q("Id")}, '{enumValue.ToString()}' AS {Q("Enum")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Single().Enum
            .Should().Be(enumValue);
    }

    [Fact]
    public void Query_EntityType_NoCompatibleConstructor_NoParameterlessConstructor_ShouldThrow() =>
        Invoking(() =>
                this.Connection.Query<EntityWithPublicConstructor>($"SELECT 1 AS {Q("NonExistent")}")
                    .ToList()
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
        Query_EntityType_NoCompatibleConstructor_PrivateParameterlessConstructor_ShouldUsePrivateConstructorAndProperties()
    {
        var entities = this.CreateEntitiesInDb<Entity>();

        this.Connection.Query<EntityWithPrivateParameterlessConstructor>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entities);
    }

    [Fact]
    public void
        Query_EntityType_NoCompatibleConstructor_PublicParameterlessConstructor_ShouldUsePublicConstructorAndProperties()
    {
        var entities = this.CreateEntitiesInDb<Entity>();

        this.Connection.Query<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entities);
    }

    [Fact]
    public void Query_EntityType_NonNullableEntityProperty_ColumnContainsNull_ShouldThrow()
    {
        this.Connection.ExecuteNonQuery(
            $"INSERT INTO {Q("EntityWithNonNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        Invoking(() =>
                this.Connection.Query<EntityWithNonNullableProperty>(
                    $"SELECT * FROM {Q("EntityWithNonNullableProperty")}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToList()
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"The column 'Value' returned by the SQL statement contains a " +
                $"NULL value, but the corresponding property of the type {typeof(EntityWithNonNullableProperty)} " +
                $"is non-nullable.*"
            );
    }

    [Fact]
    public void Query_EntityType_NullableEntityProperty_ColumnContainsNull_ShouldReturnNull()
    {
        this.Connection.ExecuteNonQuery(
            $"INSERT INTO {Q("EntityWithNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        this.Connection.Query<EntityWithNullableProperty>(
                $"SELECT * FROM {Q("EntityWithNullableProperty")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo([new EntityWithNullableProperty { Id = 1, Value = null }]);
    }

    [Fact]
    public void Query_EntityType_ShouldMaterializeBinaryData()
    {
        var bytes = Generate.Single<Byte[]>();

        this.Connection.Query<EntityWithBinaryProperty>(
                $"SELECT {Parameter(bytes)} AS BinaryData",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo([new EntityWithBinaryProperty { BinaryData = bytes }]);
    }

    [Fact]
    public void Query_EntityType_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entities = this.CreateEntitiesInDb<EntityWithDateTimeOffset>();

        this.Connection.Query<EntityWithDateTimeOffset>(
                $"SELECT * FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entities);
    }

    [Fact]
    public void Query_EntityType_UnsupportedFieldType_ShouldThrow()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.HasUnsupportedDataType, "");

        var literal = this.TestDatabaseProvider.GetUnsupportedDataTypeLiteral();

        Invoking(() =>
                this.Connection.Query<EntityWithObjectProperty>(
                    $"SELECT {literal} AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToList()
            )
            .Should().Throw<ArgumentException>()
            .WithMessage(
                "The data type System.* of the column 'Value' returned by the SQL statement is not supported.*"
            );
    }

    [Fact]
    public void Query_InterpolatedParameter_ShouldPassInterpolatedParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        this.Connection.Query<Entity>(
                $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo([entity]);
    }

    [Fact]
    public void Query_Parameter_ShouldPassParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        var statement = new InterpolatedSqlStatement(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entity.Id)
        );

        this.Connection.Query<Entity>(statement, cancellationToken: TestContext.Current.CancellationToken)
            .Should().BeEquivalentTo([entity]);
    }

    [Fact]
    public void Query_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterEnumerationIsFinished()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        InterpolatedSqlStatement statement = $"SELECT {Q("Value")} AS Id FROM {TemporaryTable(entityIds)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        var enumerator = this.Connection.Query<Entity>(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        ).GetEnumerator();

        enumerator.MoveNext()
            .Should().BeTrue();

        if (this.TestDatabaseProvider.SupportsCommandExecutionWhileDataReaderIsOpen)
        {
            this.ExistsTemporaryTableInDb(temporaryTableName)
                .Should().BeTrue();
        }

        enumerator.MoveNext()
            .Should().BeTrue();

        enumerator.MoveNext()
            .Should().BeFalse();

        enumerator.Dispose();

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public void Query_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = this.CreateEntitiesInDb<Entity>(5);
        var entityIds = entities.Take(2).Select(a => a.Id).ToList();

        this.Connection.Query<Entity>(
                $"""
                 SELECT *
                 FROM   {Q("Entity")}
                 WHERE  {Q("Id")} IN (SELECT {Q("Value")} FROM {TemporaryTable(entityIds)})
                 """,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entities.Take(2));
    }

    [Fact]
    public void Query_Transaction_ShouldUseTransaction()
    {
        using (var transaction = this.Connection.BeginTransaction())
        {
            var entities = this.CreateEntitiesInDb<Entity>(null, transaction);

            this.Connection.Query<Entity>(
                    $"SELECT * FROM {Q("Entity")}",
                    transaction,
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .Should().BeEquivalentTo(entities);

            transaction.Rollback();
        }

        this.Connection.Query<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEmpty();
    }

    [Fact]
    public void Query_ValueTupleType_CharValueTupleField_ColumnContainsStringWithLengthNotOne_ShouldThrow()
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            Invoking(() =>
                    this.Connection.Query<ValueTuple<Char>>(
                        $"SELECT '' AS {Q("Value")}",
                        cancellationToken: TestContext.Current.CancellationToken
                    ).ToList()
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
                this.Connection.Query<ValueTuple<Char>>(
                    $"SELECT 'ab' AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToList()
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
        Query_ValueTupleType_CharValueTupleField_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<Char>();

        this.Connection.Query<ValueTuple<Char>>(
                $"SELECT '{character}'",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo([ValueTuple.Create(character)]);
    }

    [Fact]
    public void Query_ValueTupleType_ColumnDataTypeNotCompatibleWithValueTupleFieldType_ShouldThrow() =>
        Invoking(() =>
                this.Connection.Query<ValueTuple<TimeSpan>>(
                    $"SELECT 123 AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToList()
            )
            .Should().Throw<ArgumentException>()
            .WithMessage(
                $"The data type System.* of the column 'Value' returned by the SQL statement is not compatible with " +
                $"the field type {typeof(TimeSpan)} of the corresponding field of the value tuple type " +
                $"{typeof(ValueTuple<TimeSpan>)}.*"
            );

    [Fact]
    public void Query_ValueTupleType_EnumValueTupleField_ColumnContainsInvalidInteger_ShouldThrow() =>
        Invoking(() =>
                this.Connection.Query<ValueTuple<TestEnum>>(
                    $"SELECT 999 AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToList()
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
    public void Query_ValueTupleType_EnumValueTupleField_ColumnContainsInvalidString_ShouldThrow() =>
        Invoking(() =>
                this.Connection.Query<ValueTuple<TestEnum>>(
                    $"SELECT 'NonExistent' AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToList()
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
    public void Query_ValueTupleType_EnumValueTupleField_ShouldConvertIntegerToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        this.Connection.Query<ValueTuple<TestEnum>>(
                $"SELECT {(Int32)enumValue}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo([ValueTuple.Create(enumValue)]);
    }

    [Fact]
    public void Query_ValueTupleType_EnumValueTupleField_ShouldConvertStringToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        this.Connection.Query<ValueTuple<TestEnum>>(
                $"SELECT '{enumValue}'",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo([ValueTuple.Create(enumValue)]);
    }

    [Fact]
    public void Query_ValueTupleType_NonNullableValueTupleField_ColumnContainsNull_ShouldThrow()
    {
        this.Connection.ExecuteNonQuery(
            $"INSERT INTO {Q("EntityWithNonNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        Invoking(() =>
                this.Connection.Query<ValueTuple<Int32>>(
                    $"SELECT {Q("Value")} FROM {Q("EntityWithNonNullableProperty")}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToList()
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"The column 'Value' returned by the SQL statement contains a NULL value, but the corresponding " +
                $"field of the value tuple type {typeof(ValueTuple<Int32>)} is non-nullable.*"
            );
    }

    [Fact]
    public void Query_ValueTupleType_NullableValueTupleField_ColumnContainsNull_ShouldReturnNull()
    {
        this.Connection.ExecuteNonQuery(
            $"INSERT INTO {Q("EntityWithNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        this.Connection.Query<ValueTuple<Int32?>>(
                $"SELECT {Q("Value")} FROM {Q("EntityWithNullableProperty")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo([new ValueTuple<Int32?>(null)]);
    }

    [Fact]
    public void Query_ValueTupleType_NumberOfColumnsDoesNotMatchNumberOfValueTupleFields_ShouldThrow() =>
        Invoking(() =>
                this.Connection.Query<(Int32, Int32)>(
                    "SELECT 1",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToList()
            )
            .Should().Throw<ArgumentException>()
            .WithMessage(
                $"The SQL statement returned 1 column, but the value tuple type {typeof((Int32, Int32))} has 2 " +
                $"fields. Make sure that the SQL statement returns the same number of columns as the number of " +
                $"fields in the value tuple type.*"
            );

    [Fact]
    public void Query_ValueTupleType_ShouldMaterializeBinaryData()
    {
        var bytes = Generate.Single<Byte[]>();

        this.Connection.Query<ValueTuple<Byte[]>>(
                $"SELECT {Parameter(bytes)} AS BinaryData",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo([ValueTuple.Create(bytes)]);
    }

    [Fact]
    public void Query_ValueTupleType_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entities = this.CreateEntitiesInDb<EntityWithDateTimeOffset>();

        this.Connection.Query<(Int64 Id, DateTimeOffset DateTimeOffsetValue)>(
                $"SELECT {Q("Id")}, {Q("DateTimeOffsetValue")} FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEquivalentTo(entities.Select(e => (e.Id, e.DateTimeOffsetValue)));
    }

    [Fact]
    public void Query_ValueTupleType_UnsupportedFieldType_ShouldThrow()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.HasUnsupportedDataType, "");

        var literal = this.TestDatabaseProvider.GetUnsupportedDataTypeLiteral();

        Invoking(() =>
                this.Connection.Query<ValueTuple<Object>>(
                    $"SELECT {literal} AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToList()
            )
            .Should().Throw<ArgumentException>()
            .WithMessage(
                "The data type System.* of the column 'Value' returned by the SQL statement is not supported.*"
            );
    }

    [Fact]
    public async Task QueryAsync_BuiltInType_CharTargetType_ColumnContainsStringWithLengthNotOne_ShouldThrow()
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            (await Invoking(() =>
                        this.Connection.QueryAsync<Char>(
                            "SELECT ''",
                            cancellationToken: TestContext.Current.CancellationToken
                        ).ToListAsync(TestContext.Current.CancellationToken).AsTask()
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
                    this.Connection.QueryAsync<Char>(
                        "SELECT 'ab'",
                        cancellationToken: TestContext.Current.CancellationToken
                    ).ToListAsync(TestContext.Current.CancellationToken).AsTask()
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
    public async Task QueryAsync_BuiltInType_CharTargetType_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<Char>();

        (await this.Connection.QueryAsync<Char>(
                $"SELECT '{character}'",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo([character]);
    }

    [Fact]
    public Task QueryAsync_BuiltInType_ColumnValueCannotBeConvertedToTargetType_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryAsync<Int32>(
                    "SELECT 'A'",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToListAsync(TestContext.Current.CancellationToken).AsTask()
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                $"The first column returned by the SQL statement contains the value 'A' ({typeof(String)}), which " +
                $"could not be converted to the type {typeof(Int32)}. See inner exception for details.*"
            );

    [Fact]
    public Task QueryAsync_BuiltInType_EnumTargetType_ColumnContainsInvalidInteger_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryAsync<TestEnum>(
                    "SELECT 999",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToListAsync(TestContext.Current.CancellationToken).AsTask()
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                $"The first column returned by the SQL statement contains the value '999*' (System.*), which " +
                $"could not be converted to the type {typeof(TestEnum)}. See inner exception for details.*"
            );

    [Fact]
    public Task QueryAsync_BuiltInType_EnumTargetType_ColumnContainsInvalidString_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryAsync<TestEnum>(
                    "SELECT 'NonExistent'",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToListAsync(TestContext.Current.CancellationToken).AsTask()
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                $"The first column returned by the SQL statement contains the value 'NonExistent' " +
                $"({typeof(String)}), which could not be converted to the type {typeof(TestEnum)}. See inner " +
                $"exception for details.*"
            );

    [Fact]
    public async Task QueryAsync_BuiltInType_EnumTargetType_ShouldConvertIntegerToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        (await this.Connection.QueryAsync<TestEnum>(
                $"SELECT {(Int32)enumValue}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo([enumValue]);
    }

    [Fact]
    public async Task QueryAsync_BuiltInType_EnumTargetType_ShouldConvertStringToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        (await this.Connection.QueryAsync<TestEnum>(
                $"SELECT '{enumValue.ToString()}'",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo([enumValue]);
    }

    [Fact]
    public Task QueryAsync_BuiltInType_NonNullableTargetType_ColumnContainsNull_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryAsync<Int32>(
                    "SELECT NULL",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToListAsync(TestContext.Current.CancellationToken).AsTask()
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                $"The first column returned by the SQL statement contains a NULL value, which could not be converted " +
                $"to the type {typeof(Int32)}. See inner exception for details.*"
            );

    [Fact]
    public async Task QueryAsync_BuiltInType_NullableTargetType_ColumnContainsNull_ShouldReturnNull() =>
        (await this.Connection.QueryAsync<Int32?>(
            "SELECT NULL",
            cancellationToken: TestContext.Current.CancellationToken
        ).ToListAsync(TestContext.Current.CancellationToken).AsTask())
        .Should().BeEquivalentTo(new Int32?[] { null });

    [Fact]
    public async Task QueryAsync_BuiltInType_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entities = this.CreateEntitiesInDb<EntityWithDateTimeOffset>();

        (await this.Connection.QueryAsync<DateTimeOffset>(
                $"SELECT {Q("DateTimeOffsetValue")} FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(entities.Select(e => e.DateTimeOffsetValue));
    }

    [Fact]
    public async Task QueryAsync_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        await Invoking(() =>
                this.Connection.QueryAsync<Entity>(
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: cancellationToken
                ).ToListAsync(cancellationToken).AsTask()
            )
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Fact]
    public async Task QueryAsync_CommandType_ShouldUseCommandType()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entities = this.CreateEntitiesInDb<Entity>();

        (await this.Connection.QueryAsync<Entity>(
                "GetEntities",
                commandType: CommandType.StoredProcedure,
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(entities);
    }

    [Fact]
    public async Task
        QueryAsync_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterEnumerationIsFinished()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(2);

        InterpolatedSqlStatement statement = $"SELECT * FROM {TemporaryTable(entities)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        var asyncEnumerator = this.Connection.QueryAsync<Entity>(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        ).GetAsyncEnumerator();

        (await asyncEnumerator.MoveNextAsync())
            .Should().BeTrue();

        if (this.TestDatabaseProvider.SupportsCommandExecutionWhileDataReaderIsOpen)
        {
            this.ExistsTemporaryTableInDb(temporaryTableName)
                .Should().BeTrue();
        }

        (await asyncEnumerator.MoveNextAsync())
            .Should().BeTrue();

        (await asyncEnumerator.MoveNextAsync())
            .Should().BeFalse();

        await asyncEnumerator.DisposeAsync();

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public async Task
        QueryAsync_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable()
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
        QueryAsync_EntityType_CharEntityProperty_ColumnContainsStringWithLengthNotOne_ShouldThrow()
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            await Invoking(() =>
                    this.Connection.QueryAsync<EntityWithCharProperty>(
                        $"SELECT '' AS {Q("Char")}",
                        cancellationToken: TestContext.Current.CancellationToken
                    ).ToListAsync(TestContext.Current.CancellationToken).AsTask()
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
                this.Connection.QueryAsync<EntityWithCharProperty>(
                    $"SELECT 'ab' AS {Q("Char")}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToListAsync(TestContext.Current.CancellationToken).AsTask()
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
        QueryAsync_EntityType_CharEntityProperty_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<Char>();

        (await this.Connection.QueryAsync<EntityWithCharProperty>(
                $"SELECT '{character}' AS {Q("Char")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo([new EntityWithCharProperty { Char = character }]);
    }

    [Fact]
    public Task QueryAsync_EntityType_ColumnDataTypeNotCompatibleWithEntityPropertyType_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryAsync<Entity>(
                    $"SELECT 123 AS {Q("TimeSpanValue")}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToListAsync(TestContext.Current.CancellationToken).AsTask()
            )
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage(
                $"The data type System.* of the column 'TimeSpanValue' returned by the SQL statement is not " +
                $"compatible with the property type {typeof(TimeSpan)} of the corresponding property of the type " +
                $"{typeof(Entity)}.*"
            );

    [Fact]
    public async Task QueryAsync_EntityType_ColumnHasNoName_ShouldThrow()
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
                this.Connection.QueryAsync<Entity>(
                    statement,
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToListAsync(TestContext.Current.CancellationToken).AsTask()
            )
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage(
                "The 1st column returned by the SQL statement does not have a name. Make sure that all columns the " +
                "statement returns have a name.*"
            );
    }

    [Fact]
    public async Task QueryAsync_EntityType_CompatiblePrivateConstructor_ShouldUsePrivateConstructor()
    {
        var entities = this.CreateEntitiesInDb<Entity>();

        (await this.Connection.QueryAsync<EntityWithPrivateConstructor>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(entities);
    }

    [Fact]
    public async Task QueryAsync_EntityType_CompatiblePublicConstructor_ShouldUsePublicConstructor()
    {
        var entities = this.CreateEntitiesInDb<Entity>();

        (await this.Connection.QueryAsync<EntityWithPublicConstructor>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(entities);
    }

    [Fact]
    public async Task QueryAsync_EntityType_EntityTypeHasNoCorrespondingPropertyForColumn_ShouldIgnoreColumn()
    {
        var entities = (await Invoking(() =>
                this.Connection.QueryAsync<EntityWithNonNullableProperty>(
                    $"SELECT 1 AS {Q("Id")}, 2 AS {Q("Value")}, 3 AS {Q("NonExistent")}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToListAsync(TestContext.Current.CancellationToken).AsTask()
            )
            .Should().NotThrowAsync()).Subject;

        entities
            .Should().BeEquivalentTo([new EntityWithNonNullableProperty { Id = 1, Value = 2 }]);
    }

    [Fact]
    public async Task QueryAsync_EntityType_EntityTypeWithPropertiesWithDifferentCasing_ShouldMaterializeEntities()
    {
        var entities = this.CreateEntitiesInDb<Entity>();
        var entitiesWithDifferentCasingProperties = Generate.MapTo<EntityWithDifferentCasingProperties>(entities);

        (await this.Connection.QueryAsync<EntityWithDifferentCasingProperties>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(entitiesWithDifferentCasingProperties);
    }

    [Fact]
    public async Task QueryAsync_EntityType_EnumEntityProperty_ColumnContainsInvalidInteger_ShouldThrow() =>
        await Invoking(() => this.Connection.QueryAsync<EntityWithEnumStoredAsInteger>(
                    $"SELECT 1 AS {Q("Id")}, 999 AS {Q("Enum")}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToListAsync(TestContext.Current.CancellationToken).AsTask()
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
    public async Task QueryAsync_EntityType_EnumEntityProperty_ColumnContainsInvalidString_ShouldThrow() =>
        await Invoking(() => this.Connection.QueryAsync<EntityWithEnumStoredAsString>(
                    $"SELECT 1 AS {Q("Id")}, 'NonExistent' AS {Q("Enum")}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToListAsync(TestContext.Current.CancellationToken).AsTask()
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
    public async Task QueryAsync_EntityType_EnumEntityProperty_ShouldConvertIntegerToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        (await this.Connection.QueryAsync<EntityWithEnumStoredAsInteger>(
                $"SELECT 1 AS {Q("Id")}, {(Int32)enumValue} AS {Q("Enum")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).FirstAsync())
            .Enum
            .Should().Be(enumValue);
    }

    [Fact]
    public async Task QueryAsync_EntityType_EnumEntityProperty_ShouldConvertStringToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        (await this.Connection.QueryAsync<EntityWithEnumStoredAsInteger>(
                $"SELECT 1 AS {Q("Id")}, '{enumValue.ToString()}' AS {Q("Enum")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).FirstAsync())
            .Enum
            .Should().Be(enumValue);
    }

    [Fact]
    public Task QueryAsync_EntityType_NoCompatibleConstructor_NoParameterlessConstructor_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryAsync<EntityWithPublicConstructor>($"SELECT 1 AS {Q("NonExistent")}")
                    .ToListAsync(TestContext.Current.CancellationToken).AsTask()
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
        QueryAsync_EntityType_NoCompatibleConstructor_PrivateParameterlessConstructor_ShouldUsePrivateConstructorAndProperties()
    {
        var entities = this.CreateEntitiesInDb<Entity>();

        (await this.Connection.QueryAsync<EntityWithPrivateParameterlessConstructor>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(entities);
    }

    [Fact]
    public async Task
        QueryAsync_EntityType_NoCompatibleConstructor_PublicParameterlessConstructor_ShouldUsePublicConstructorAndProperties()
    {
        var entities = this.CreateEntitiesInDb<Entity>();

        (await this.Connection.QueryAsync<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(entities);
    }

    [Fact]
    public Task QueryAsync_EntityType_NonNullableEntityProperty_ColumnContainsNull_ShouldThrow()
    {
        this.Connection.ExecuteNonQuery(
            $"INSERT INTO {Q("EntityWithNonNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        return Invoking(() =>
                this.Connection.QueryAsync<EntityWithNonNullableProperty>(
                    $"SELECT * FROM {Q("EntityWithNonNullableProperty")}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToListAsync(TestContext.Current.CancellationToken).AsTask()
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                $"The column 'Value' returned by the SQL statement contains a NULL value, but the corresponding " +
                $"property of the type {typeof(EntityWithNonNullableProperty)} is non-nullable.*"
            );
    }

    [Fact]
    public async Task QueryAsync_EntityType_NullableEntityProperty_ColumnContainsNull_ShouldReturnNull()
    {
        await this.Connection.ExecuteNonQueryAsync(
            $"INSERT INTO {Q("EntityWithNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        (await this.Connection.QueryAsync<EntityWithNullableProperty>(
                $"SELECT * FROM {Q("EntityWithNullableProperty")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo([new EntityWithNullableProperty { Id = 1, Value = null }]);
    }

    [Fact]
    public async Task QueryAsync_EntityType_ShouldMaterializeBinaryData()
    {
        var bytes = Generate.Single<Byte[]>();

        (await this.Connection.QueryAsync<EntityWithBinaryProperty>(
                $"SELECT {Parameter(bytes)} AS BinaryData",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo([new EntityWithBinaryProperty { BinaryData = bytes }]);
    }

    [Fact]
    public async Task QueryAsync_EntityType_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entities = this.CreateEntitiesInDb<EntityWithDateTimeOffset>();

        (await this.Connection.QueryAsync<EntityWithDateTimeOffset>(
                $"SELECT * FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(entities);
    }

    [Fact]
    public Task QueryAsync_EntityType_UnsupportedFieldType_ShouldThrow()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.HasUnsupportedDataType, "");

        var literal = this.TestDatabaseProvider.GetUnsupportedDataTypeLiteral();

        return Invoking(() =>
                this.Connection.QueryAsync<EntityWithObjectProperty>(
                    $"SELECT {literal} AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToListAsync(TestContext.Current.CancellationToken).AsTask()
            )
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage(
                "The data type System.* of the column 'Value' returned by the SQL statement is not supported.*"
            );
    }

    [Fact]
    public async Task QueryAsync_InterpolatedParameter_ShouldPassInterpolatedParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        (await this.Connection.QueryAsync<Entity>(
                $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo([entity]);
    }

    [Fact]
    public async Task QueryAsync_Parameter_ShouldPassParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        var statement = new InterpolatedSqlStatement(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entity.Id)
        );

        (await this.Connection.QueryAsync<Entity>(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo([entity]);
    }

    [Fact]
    public async Task
        QueryAsync_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterEnumerationIsFinished()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        InterpolatedSqlStatement statement =
            $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable(entityIds)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        var asyncEnumerator = this.Connection.QueryAsync<Entity>(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        ).GetAsyncEnumerator();

        (await asyncEnumerator.MoveNextAsync())
            .Should().BeTrue();

        if (this.TestDatabaseProvider.SupportsCommandExecutionWhileDataReaderIsOpen)
        {
            this.ExistsTemporaryTableInDb(temporaryTableName)
                .Should().BeTrue();
        }

        (await asyncEnumerator.MoveNextAsync())
            .Should().BeTrue();

        (await asyncEnumerator.MoveNextAsync())
            .Should().BeFalse();

        await asyncEnumerator.DisposeAsync();

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public async Task
        QueryAsync_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = this.CreateEntitiesInDb<Entity>(5);
        var entityIds = entities.Take(2).Select(a => a.Id).ToList();

        (await this.Connection.QueryAsync<Entity>(
                $"""
                 SELECT *
                 FROM   {Q("Entity")}
                 WHERE  {Q("Id")} IN (SELECT {Q("Value")} FROM {TemporaryTable(entityIds)})
                 """,
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(entities.Take(2));
    }

    [Fact]
    public async Task QueryAsync_Transaction_ShouldUseTransaction()
    {
        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            var entities = this.CreateEntitiesInDb<Entity>(null, transaction);

            (await this.Connection.QueryAsync<Entity>(
                    $"SELECT * FROM {Q("Entity")}",
                    transaction,
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToListAsync(TestContext.Current.CancellationToken))
                .Should().BeEquivalentTo(entities);

            await transaction.RollbackAsync();
        }

        (await this.Connection.QueryAsync<Entity>(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEmpty();
    }

    [Fact]
    public async Task QueryAsync_ValueTupleType_CharValueTupleField_ColumnContainsStringWithLengthNotOne_ShouldThrow()
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            await Invoking(() =>
                    this.Connection.QueryAsync<ValueTuple<Char>>(
                        $"SELECT '' AS {Q("Value")}",
                        cancellationToken: TestContext.Current.CancellationToken
                    ).ToListAsync(TestContext.Current.CancellationToken).AsTask()
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
                this.Connection.QueryAsync<ValueTuple<Char>>(
                    $"SELECT 'ab' AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToListAsync(TestContext.Current.CancellationToken).AsTask()
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
        QueryAsync_ValueTupleType_CharValueTupleField_ColumnContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<Char>();

        (await this.Connection.QueryAsync<ValueTuple<Char>>(
                $"SELECT '{character}'",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo([ValueTuple.Create(character)]);
    }

    [Fact]
    public Task QueryAsync_ValueTupleType_ColumnDataTypeNotCompatibleWithValueTupleFieldType_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryAsync<ValueTuple<TimeSpan>>(
                    $"SELECT 123 AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToListAsync(TestContext.Current.CancellationToken).AsTask()
            )
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage(
                $"The data type System.* of the column 'Value' returned by the SQL statement is not compatible with " +
                $"the field type {typeof(TimeSpan)} of the corresponding field of the value tuple type " +
                $"{typeof(ValueTuple<TimeSpan>)}.*"
            );

    [Fact]
    public Task QueryAsync_ValueTupleType_EnumValueTupleField_ColumnContainsInvalidInteger_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryAsync<ValueTuple<TestEnum>>(
                    $"SELECT 999 AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToListAsync(TestContext.Current.CancellationToken).AsTask()
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
    public Task QueryAsync_ValueTupleType_EnumValueTupleField_ColumnContainsInvalidString_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryAsync<ValueTuple<TestEnum>>(
                    $"SELECT 'NonExistent' AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToListAsync(TestContext.Current.CancellationToken).AsTask()
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
    public async Task QueryAsync_ValueTupleType_EnumValueTupleField_ShouldConvertIntegerToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        (await this.Connection.QueryAsync<ValueTuple<TestEnum>>(
                $"SELECT {(Int32)enumValue}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo([ValueTuple.Create(enumValue)]);
    }

    [Fact]
    public async Task QueryAsync_ValueTupleType_EnumValueTupleField_ShouldConvertStringToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        (await this.Connection.QueryAsync<ValueTuple<TestEnum>>(
                $"SELECT '{enumValue}'",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo([ValueTuple.Create(enumValue)]);
    }

    [Fact]
    public Task QueryAsync_ValueTupleType_NonNullableValueTupleField_ColumnContainsNull_ShouldThrow()
    {
        this.Connection.ExecuteNonQuery(
            $"INSERT INTO {Q("EntityWithNonNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        return Invoking(() =>
                this.Connection.QueryAsync<ValueTuple<Int32>>(
                    $"SELECT {Q("Value")} FROM {Q("EntityWithNonNullableProperty")}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToListAsync(TestContext.Current.CancellationToken).AsTask()
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                $"The column 'Value' returned by the SQL statement contains a NULL value, but the corresponding " +
                $"field of the value tuple type {typeof(ValueTuple<Int32>)} is non-nullable.*"
            );
    }

    [Fact]
    public async Task QueryAsync_ValueTupleType_NullableValueTupleField_ColumnContainsNull_ShouldReturnNull()
    {
        await this.Connection.ExecuteNonQueryAsync(
            $"INSERT INTO {Q("EntityWithNullableProperty")} ({Q("Id")}, {Q("Value")}) VALUES(1, NULL)"
        );

        (await this.Connection.QueryAsync<ValueTuple<Int32?>>(
                $"SELECT {Q("Value")} FROM {Q("EntityWithNullableProperty")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo([new ValueTuple<Int32?>(null)]);
    }

    [Fact]
    public Task QueryAsync_ValueTupleType_NumberOfColumnsDoesNotMatchNumberOfValueTupleFields_ShouldThrow() =>
        Invoking(() =>
                this.Connection.QueryAsync<(Int32, Int32)>(
                    "SELECT 1",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToListAsync(TestContext.Current.CancellationToken).AsTask()
            )
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage(
                $"The SQL statement returned 1 column, but the value tuple type {typeof((Int32, Int32))} has 2 " +
                $"fields. Make sure that the SQL statement returns the same number of columns as the number of " +
                $"fields in the value tuple type.*"
            );

    [Fact]
    public async Task QueryAsync_ValueTupleType_ShouldMaterializeBinaryData()
    {
        var bytes = Generate.Single<Byte[]>();

        (await this.Connection.QueryAsync<ValueTuple<Byte[]>>(
                $"SELECT {Parameter(bytes)} AS BinaryData",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo([ValueTuple.Create(bytes)]);
    }

    [Fact]
    public async Task QueryAsync_ValueTupleType_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entities = this.CreateEntitiesInDb<EntityWithDateTimeOffset>();

        (await this.Connection.QueryAsync<(Int64 Id, DateTimeOffset DateTimeOffsetValue)>(
                $"SELECT {Q("Id")}, {Q("DateTimeOffsetValue")} FROM {Q("EntityWithDateTimeOffset")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(entities.Select(e => (e.Id, e.DateTimeOffsetValue)));
    }

    [Fact]
    public Task QueryAsync_ValueTupleType_UnsupportedFieldType_ShouldThrow()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.HasUnsupportedDataType, "");

        var literal = this.TestDatabaseProvider.GetUnsupportedDataTypeLiteral();

        return Invoking(() =>
                this.Connection.QueryAsync<ValueTuple<Object>>(
                    $"SELECT {literal} AS {Q("Value")}",
                    cancellationToken: TestContext.Current.CancellationToken
                ).ToListAsync(TestContext.Current.CancellationToken).AsTask()
            )
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage(
                "The data type System.* of the column 'Value' returned by the SQL statement is not supported.*"
            );
    }
}
