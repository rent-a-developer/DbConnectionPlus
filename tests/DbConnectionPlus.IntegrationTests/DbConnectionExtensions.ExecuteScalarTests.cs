namespace RentADeveloper.DbConnectionPlus.IntegrationTests;

public sealed class
    DbConnectionExtensions_ExecuteScalarTests_MySql :
    DbConnectionExtensions_ExecuteScalarTests<MySqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ExecuteScalarTests_Oracle :
    DbConnectionExtensions_ExecuteScalarTests<OracleTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ExecuteScalarTests_PostgreSql :
    DbConnectionExtensions_ExecuteScalarTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ExecuteScalarTests_Sqlite :
    DbConnectionExtensions_ExecuteScalarTests<SqliteTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ExecuteScalarTests_SqlServer :
    DbConnectionExtensions_ExecuteScalarTests<SqlServerTestDatabaseProvider>;

public abstract class
    DbConnectionExtensions_ExecuteScalarTests<TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    [Fact]
    public void ExecuteScalar_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        Invoking(() =>
                this.Connection.ExecuteScalar<Int32>("SELECT 1", cancellationToken: cancellationToken)
            )
            .Should().Throw<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Fact]
    public void ExecuteScalar_ColumnValueCannotBeConvertedToTargetType_ShouldThrow() =>
        Invoking(() =>
                this.Connection.ExecuteScalar<Int32>(
                    "SELECT 'A'",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"The first column of the first row in the result set returned by the SQL statement contains the " +
                $"value 'A' ({typeof(String)}), which could not be converted to the type {typeof(Int32)}.*"
            );

    [Fact]
    public void ExecuteScalar_CommandType_ShouldUseCommandType()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entity = this.CreateEntityInDb<Entity>();

        this.Connection.ExecuteScalar<Int64>(
                $"GetFirstEntityId",
                commandType: CommandType.StoredProcedure,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entity.Id);
    }

    [Fact]
    public void ExecuteScalar_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(1);

        InterpolatedSqlStatement statement = $"""
                                              SELECT     {Q("StringValue")}
                                              FROM       {TemporaryTable(entities)}
                                              """;

        var temporaryTableName = statement.TemporaryTables.First().Name;

        this.Connection.ExecuteScalar<String>(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entities[0].StringValue);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public void ExecuteScalar_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(1);

        this.Connection.ExecuteScalar<String>(
                $"""
                 SELECT     {Q("StringValue")}
                 FROM       {TemporaryTable(entities)}
                 """,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entities[0].StringValue);
    }

    [Fact]
    public void ExecuteScalar_InterpolatedParameter_ShouldPassInterpolatedParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        this.Connection.ExecuteScalar<String>(
                $"SELECT {Q("StringValue")} FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entity.StringValue);
    }

    [Fact]
    public void ExecuteScalar_NoResultSet_ShouldReturnDefault()
    {
        this.Connection.ExecuteScalar<Object>(
                "SELECT 1 WHERE 0 = 1",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeNull();

        this.Connection.ExecuteScalar<Int32>(
                "SELECT 1 WHERE 0 = 1",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(0);
    }

    [Fact]
    public void ExecuteScalar_Parameter_ShouldPassParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        var statement = new InterpolatedSqlStatement(
            $"SELECT {Q("StringValue")} FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entity.Id)
        );

        this.Connection.ExecuteScalar<String>(statement, cancellationToken: TestContext.Current.CancellationToken)
            .Should().Be(entity.StringValue);
    }

    [Fact]
    public void ExecuteScalar_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(1);

        InterpolatedSqlStatement statement = $"SELECT {Q("Value")} FROM {TemporaryTable(entityIds)}";

        var temporaryTableName = statement.TemporaryTables.First().Name;

        this.Connection.ExecuteScalar<Int64>(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entityIds[0]);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public void ExecuteScalar_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(1);

        this.Connection.ExecuteScalar<Int64>(
                $"SELECT {Q("Value")} FROM {TemporaryTable(entityIds)}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entityIds[0]);
    }

    [Fact]
    public void ExecuteScalar_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entity = this.CreateEntityInDb<EntityWithDateTimeOffset>();

        this.Connection.ExecuteScalar<DateTimeOffset>(
                $"""
                 SELECT     {Q("DateTimeOffsetValue")}
                 FROM       {Q("EntityWithDateTimeOffset")}
                 """,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entity.DateTimeOffsetValue);
    }

    [Fact]
    public void ExecuteScalar_TargetTypeIsChar_ColumnValueIsStringWithLengthNotOne_ShouldThrow()
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            Invoking(() =>
                    this.Connection.ExecuteScalar<Char>(
                        "SELECT ''",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                )
                .Should().Throw<InvalidCastException>()
                .WithMessage(
                    $"The first column of the first row in the result set returned by the SQL statement contains " +
                    $"the value '' ({typeof(String)}), which could not be converted to the type {typeof(Char)}. See " +
                    $"inner exception for details.*"
                )
                .WithInnerException<InvalidCastException>()
                .WithMessage(
                    $"Could not convert the string '' to the type {typeof(Char)}. The string must be " +
                    $"exactly one character long."
                );
        }

        Invoking(() =>
                this.Connection.ExecuteScalar<Char>(
                    "SELECT 'ab'",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"The first column of the first row in the result set returned by the SQL statement contains the " +
                $"value 'ab' ({typeof(String)}), which could not be converted to the type {typeof(Char)}. See inner " +
                $"exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(Char)}. The string must be " +
                $"exactly one character long."
            );
    }

    [Fact]
    public void ExecuteScalar_TargetTypeIsChar_ColumnValueIsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<Char>();

        this.Connection.ExecuteScalar<Char>(
                $"SELECT '{character}'",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(character);
    }

    [Fact]
    public void ExecuteScalar_TargetTypeIsEnum_ColumnValueIsInteger_ShouldConvertIntegerToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        this.Connection.ExecuteScalar<TestEnum>(
                $"SELECT {(Int32)enumValue}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(enumValue);
    }

    [Fact]
    public void ExecuteScalar_TargetTypeIsEnum_ColumnValueIsInvalidInteger_ShouldThrow() =>
        Invoking(() =>
                this.Connection.ExecuteScalar<TestEnum>(
                    "SELECT 999",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"The first column of the first row in the result set returned by the SQL statement contains the " +
                $"value '999*' (System.*), which could not be converted to the type {typeof(TestEnum)}.*"
            );

    [Fact]
    public void ExecuteScalar_TargetTypeIsEnum_ColumnValueIsInvalidString_ShouldThrow() =>
        Invoking(() =>
                this.Connection.ExecuteScalar<TestEnum>(
                    "SELECT 'NonExistent'",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"The first column of the first row in the result set returned by the SQL statement contains the " +
                $"value 'NonExistent' ({typeof(String)}), which could not be converted to the type " +
                $"{typeof(TestEnum)}.*"
            );

    [Fact]
    public void ExecuteScalar_TargetTypeIsEnum_ColumnValueIsString_ShouldConvertStringToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        this.Connection.ExecuteScalar<TestEnum>(
                $"SELECT '{enumValue.ToString()}'",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(enumValue);
    }

    [Fact]
    public void ExecuteScalar_TargetTypeIsNonNullable_ColumnValueIsNull_ShouldThrow() =>
        Invoking(() =>
                this.Connection.ExecuteScalar<Int32>(
                    "SELECT NULL",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"The first column of the first row in the result set returned by the SQL statement contains a NULL " +
                $"value, which could not be converted to the type {typeof(Int32)}.*"
            );

    [Fact]
    public void ExecuteScalar_TargetTypeIsNullable_ColumnValueIsNull_ShouldReturnNull() =>
        this.Connection.ExecuteScalar<Int32?>(
                "SELECT NULL",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeNull();

    [Fact]
    public void ExecuteScalar_Transaction_ShouldUseTransaction()
    {
        using (var transaction = this.Connection.BeginTransaction())
        {
            var entity = this.CreateEntityInDb<Entity>(transaction);

            this.Connection.ExecuteScalar<String>(
                    $"SELECT {Q("StringValue")} FROM {Q("Entity")}",
                    transaction,
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .Should().Be(entity.StringValue);

            transaction.Rollback();
        }

        this.Connection.ExecuteScalar<String>(
                $"SELECT {Q("StringValue")} FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeNull();
    }

    [Fact]
    public async Task ExecuteScalarAsync_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        await Invoking(() =>
                this.Connection.ExecuteScalarAsync<Int32>(
                    "SELECT 1",
                    cancellationToken: cancellationToken
                )
            )
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Fact]
    public Task ExecuteScalarAsync_ColumnValueCannotBeConvertedToTargetType_ShouldThrow() =>
        Invoking(() =>
                this.Connection.ExecuteScalarAsync<Int32>(
                    "SELECT 'A'",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                $"The first column of the first row in the result set returned by the SQL statement contains the " +
                $"value 'A' ({typeof(String)}), which could not be converted to the type {typeof(Int32)}.*"
            );

    [Fact]
    public async Task ExecuteScalarAsync_CommandType_ShouldUseCommandType()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entity = this.CreateEntityInDb<Entity>();

        (await this.Connection.ExecuteScalarAsync<Int64>(
                $"GetFirstEntityId",
                commandType: CommandType.StoredProcedure,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entity.Id);
    }

    [Fact]
    public async Task ExecuteScalarAsync_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(1);

        InterpolatedSqlStatement statement = $"""
                                              SELECT     {Q("StringValue")}
                                              FROM       {TemporaryTable(entities)}
                                              """;

        var temporaryTableName = statement.TemporaryTables.First().Name;

        (await this.Connection.ExecuteScalarAsync<String>(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entities[0].StringValue);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public async Task
        ExecuteScalarAsync_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(1);

        (await this.Connection.ExecuteScalarAsync<String>(
                $"""
                 SELECT     {Q("StringValue")}
                 FROM       {TemporaryTable(entities)}
                 """,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entities[0].StringValue);
    }

    [Fact]
    public async Task ExecuteScalarAsync_InterpolatedParameter_ShouldPassInterpolatedParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        (await this.Connection.ExecuteScalarAsync<String>(
                $"SELECT {Q("StringValue")} FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entity.StringValue);
    }

    [Fact]
    public async Task ExecuteScalarAsync_NoResultSet_ShouldReturnDefault()
    {
        (await this.Connection.ExecuteScalarAsync<Object>(
                "SELECT 1 WHERE 0 = 1",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeNull();

        (await this.Connection.ExecuteScalarAsync<Int32>(
                "SELECT 1 WHERE 0 = 1",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(0);
    }

    [Fact]
    public async Task ExecuteScalarAsync_Parameter_ShouldPassParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        var statement = new InterpolatedSqlStatement(
            $"SELECT {Q("StringValue")} FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entity.Id)
        );

        (await this.Connection.ExecuteScalarAsync<String>(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entity.StringValue);
    }

    [Fact]
    public async Task ExecuteScalarAsync_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(1);

        InterpolatedSqlStatement statement = $"SELECT {Q("Value")} FROM {TemporaryTable(entityIds)}";

        var temporaryTableName = statement.TemporaryTables.First().Name;

        (await this.Connection.ExecuteScalarAsync<Int64>(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entityIds[0]);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public async Task
        ExecuteScalarAsync_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(1);

        (await this.Connection.ExecuteScalarAsync<Int64>(
                $"SELECT {Q("Value")} FROM {TemporaryTable(entityIds)}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entityIds[0]);
    }

    [Fact]
    public async Task ExecuteScalarAsync_ShouldSupportDateTimeOffsetValues()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entity = this.CreateEntityInDb<EntityWithDateTimeOffset>();

        (await this.Connection.ExecuteScalarAsync<DateTimeOffset>(
                $"""
                 SELECT     {Q("DateTimeOffsetValue")}
                 FROM       {Q("EntityWithDateTimeOffset")}
                 """,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entity.DateTimeOffsetValue);
    }

    [Fact]
    public async Task ExecuteScalarAsync_TargetTypeIsChar_ColumnValueIsStringWithLengthNotOne_ShouldThrow()
    {
        if (this.TestDatabaseProvider is not OracleTestDatabaseProvider)
        {
            // Oracle doesn't allow to return an empty string, because it treats empty strings as NULLs.

            (await Invoking(() =>
                        this.Connection.ExecuteScalarAsync<Char>(
                            "SELECT ''",
                            cancellationToken: TestContext.Current.CancellationToken
                        )
                    )
                    .Should().ThrowAsync<InvalidCastException>()
                    .WithMessage(
                        $"The first column of the first row in the result set returned by the SQL statement contains " +
                        $"the value '' ({typeof(String)}), which could not be converted to the type {typeof(Char)}. " +
                        $"See inner exception for details.*"
                    ))
                .WithInnerException<InvalidCastException>()
                .WithMessage(
                    $"Could not convert the string '' to the type {typeof(Char)}. The string must be " +
                    $"exactly one character long."
                );
        }

        (await Invoking(() =>
                    this.Connection.ExecuteScalarAsync<Char>(
                        "SELECT 'ab'",
                        cancellationToken: TestContext.Current.CancellationToken
                    )
                )
                .Should().ThrowAsync<InvalidCastException>()
                .WithMessage(
                    $"The first column of the first row in the result set returned by the SQL statement contains the " +
                    $"value 'ab' ({typeof(String)}), which could not be converted to the type {typeof(Char)}. See " +
                    $"inner exception for details.*"
                ))
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(Char)}. The string must be " +
                $"exactly one character long."
            );
    }

    [Fact]
    public async Task
        ExecuteScalarAsync_TargetTypeIsChar_ColumnValueIsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<Char>();

        (await this.Connection.ExecuteScalarAsync<Char>(
                $"SELECT '{character}'",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(character);
    }

    [Fact]
    public async Task ExecuteScalarAsync_TargetTypeIsEnum_ColumnValueIsInteger_ShouldConvertIntegerToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        (await this.Connection.ExecuteScalarAsync<TestEnum>(
                $"SELECT {(Int32)enumValue}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(enumValue);
    }

    [Fact]
    public Task ExecuteScalarAsync_TargetTypeIsEnum_ColumnValueIsInvalidInteger_ShouldThrow() =>
        Invoking(() =>
                this.Connection.ExecuteScalarAsync<TestEnum>(
                    "SELECT 999",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                $"The first column of the first row in the result set returned by the SQL statement contains the " +
                $"value '999*' (System.*), which could not be converted to the type {typeof(TestEnum)}.*"
            );

    [Fact]
    public Task ExecuteScalarAsync_TargetTypeIsEnum_ColumnValueIsInvalidString_ShouldThrow() =>
        Invoking(() =>
                this.Connection.ExecuteScalarAsync<TestEnum>(
                    "SELECT 'NonExistent'",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                $"The first column of the first row in the result set returned by the SQL statement contains the " +
                $"value 'NonExistent' ({typeof(String)}), which could not be converted to the type " +
                $"{typeof(TestEnum)}.*"
            );

    [Fact]
    public async Task ExecuteScalarAsync_TargetTypeIsEnum_ColumnValueIsString_ShouldConvertStringToEnum()
    {
        var enumValue = Generate.Single<TestEnum>();

        (await this.Connection.ExecuteScalarAsync<TestEnum>(
                $"SELECT '{enumValue}'",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(enumValue);
    }

    [Fact]
    public Task ExecuteScalarAsync_TargetTypeIsNonNullable_ColumnValueIsNull_ShouldThrow() =>
        Invoking(() =>
                this.Connection.ExecuteScalarAsync<Int32>(
                    "SELECT NULL",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidCastException>()
            .WithMessage(
                $"The first column of the first row in the result set returned by the SQL statement contains a NULL " +
                $"value, which could not be converted to the type {typeof(Int32)}.*"
            );

    [Fact]
    public async Task ExecuteScalarAsync_TargetTypeIsNullable_ColumnValueIsNull_ShouldReturnNull() =>
        (await this.Connection.ExecuteScalarAsync<Int32?>(
            "SELECT NULL",
            cancellationToken: TestContext.Current.CancellationToken
        ))
        .Should().BeNull();

    [Fact]
    public async Task ExecuteScalarAsync_Transaction_ShouldUseTransaction()
    {
        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            var entity = this.CreateEntityInDb<Entity>(transaction);

            (await this.Connection.ExecuteScalarAsync<String>(
                    $"SELECT {Q("StringValue")} FROM {Q("Entity")}",
                    transaction,
                    cancellationToken: TestContext.Current.CancellationToken
                ))
                .Should().Be(entity.StringValue);

            await transaction.RollbackAsync();
        }

        (await this.Connection.ExecuteScalarAsync<String>(
                $"SELECT {Q("StringValue")} FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeNull();
    }
}
