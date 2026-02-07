using System.Data.Common;

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
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteScalar_CancellationToken_ShouldCancelOperationIfCancellationIsRequested(
        Boolean useAsyncApi
    )
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DelayNextDbCommand = true;

        await Invoking(() =>
                CallApi<Int32>(
                    useAsyncApi,
                    this.Connection,
                    "SELECT 1",
                    cancellationToken: cancellationToken
                )
            )
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task ExecuteScalar_ColumnValueCannotBeConvertedToTargetType_ShouldThrow(Boolean useAsyncApi) =>
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
                "The first column of the first row in the result set returned by the SQL statement contains the " +
                $"value 'A' ({typeof(String)}), which could not be converted to the type {typeof(Int32)}.*"
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteScalar_CommandType_ShouldUseCommandType(Boolean useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entity = this.CreateEntityInDb<Entity>();

        (await CallApi<Int64>(
                useAsyncApi,
                this.Connection,
                "GetFirstEntityId",
                commandType: CommandType.StoredProcedure,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entity.Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteScalar_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterExecution(
        Boolean useAsyncApi
    )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(1);

        InterpolatedSqlStatement statement = $"""
                                              SELECT     {Q("StringValue")}
                                              FROM       {TemporaryTable(entities)}
                                              """;

        var temporaryTableName = statement.TemporaryTables[0].Name;

        (await CallApi<String>(
                useAsyncApi,
                this.Connection,
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entities[0].StringValue);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        ExecuteScalar_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable(
            Boolean useAsyncApi
        )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(1);

        (await CallApi<String>(
                useAsyncApi,
                this.Connection,
                $"""
                 SELECT     {Q("StringValue")}
                 FROM       {TemporaryTable(entities)}
                 """,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entities[0].StringValue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteScalar_InterpolatedParameter_ShouldPassInterpolatedParameter(Boolean useAsyncApi)
    {
        var entity = this.CreateEntityInDb<Entity>();

        (await CallApi<String>(
                useAsyncApi,
                this.Connection,
                $"SELECT {Q("StringValue")} FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entity.StringValue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteScalar_NoResultSet_ShouldReturnDefault(Boolean useAsyncApi)
    {
        (await CallApi<Object>(
                useAsyncApi,
                this.Connection,
                "SELECT 1 WHERE 0 = 1",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeNull();

        (await CallApi<Int32>(
                useAsyncApi,
                this.Connection,
                "SELECT 1 WHERE 0 = 1",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteScalar_Parameter_ShouldPassParameter(Boolean useAsyncApi)
    {
        var entity = this.CreateEntityInDb<Entity>();

        var statement = new InterpolatedSqlStatement(
            $"SELECT {Q("StringValue")} FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entity.Id)
        );

        (await CallApi<String>(
                useAsyncApi,
                this.Connection,
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entity.StringValue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteScalar_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterExecution(
        Boolean useAsyncApi
    )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(1);

        InterpolatedSqlStatement statement = $"SELECT {Q("Value")} FROM {TemporaryTable(entityIds)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        (await CallApi<Int64>(
                useAsyncApi,
                this.Connection,
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entityIds[0]);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        ExecuteScalar_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable(
            Boolean useAsyncApi
        )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(1);

        (await CallApi<Int64>(
                useAsyncApi,
                this.Connection,
                $"SELECT {Q("Value")} FROM {TemporaryTable(entityIds)}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entityIds[0]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteScalar_ShouldSupportDateTimeOffsetValues(Boolean useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsDateTimeOffset, "");

        var entity = this.CreateEntityInDb<EntityWithDateTimeOffset>();

        (await CallApi<DateTimeOffset>(
                useAsyncApi,
                this.Connection,
                $"""
                 SELECT     {Q("DateTimeOffsetValue")}
                 FROM       {Q("EntityWithDateTimeOffset")}
                 """,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entity.DateTimeOffsetValue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteScalar_TargetTypeIsChar_ColumnValueIsStringWithLengthNotOne_ShouldThrow(
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
                        "The first column of the first row in the result set returned by the SQL statement contains " +
                        $"the value '' ({typeof(String)}), which could not be converted to the type {typeof(Char)}. " +
                        "See inner exception for details.*"
                    ))
                .WithInnerException<InvalidCastException>()
                .WithMessage(
                    $"Could not convert the string '' to the type {typeof(Char)}. The string must be " +
                    "exactly one character long."
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
                    "The first column of the first row in the result set returned by the SQL statement contains the " +
                    $"value 'ab' ({typeof(String)}), which could not be converted to the type {typeof(Char)}. See " +
                    "inner exception for details.*"
                ))
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(Char)}. The string must be " +
                "exactly one character long."
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        ExecuteScalar_TargetTypeIsChar_ColumnValueIsStringWithLengthOne_ShouldGetFirstCharacter(Boolean useAsyncApi)
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
    public async Task ExecuteScalar_TargetTypeIsEnum_ColumnValueIsInteger_ShouldConvertIntegerToEnum(
        Boolean useAsyncApi
    )
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
    public Task ExecuteScalar_TargetTypeIsEnum_ColumnValueIsInvalidInteger_ShouldThrow(Boolean useAsyncApi) =>
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
                "The first column of the first row in the result set returned by the SQL statement contains the " +
                $"value '999*' (System.*), which could not be converted to the type {typeof(TestEnum)}.*"
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task ExecuteScalar_TargetTypeIsEnum_ColumnValueIsInvalidString_ShouldThrow(Boolean useAsyncApi) =>
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
                "The first column of the first row in the result set returned by the SQL statement contains the " +
                $"value 'NonExistent' ({typeof(String)}), which could not be converted to the type " +
                $"{typeof(TestEnum)}.*"
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteScalar_TargetTypeIsEnum_ColumnValueIsString_ShouldConvertStringToEnum(Boolean useAsyncApi)
    {
        var enumValue = Generate.Single<TestEnum>();

        (await CallApi<TestEnum>(
                useAsyncApi,
                this.Connection,
                $"SELECT '{enumValue}'",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(enumValue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task ExecuteScalar_TargetTypeIsNonNullable_ColumnValueIsNull_ShouldThrow(Boolean useAsyncApi) =>
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
                "The first column of the first row in the result set returned by the SQL statement contains a NULL " +
                $"value, which could not be converted to the type {typeof(Int32)}.*"
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteScalar_TargetTypeIsNullable_ColumnValueIsNull_ShouldReturnNull(Boolean useAsyncApi) =>
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
    public async Task ExecuteScalar_Transaction_ShouldUseTransaction(Boolean useAsyncApi)
    {
        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            var entity = this.CreateEntityInDb<Entity>(transaction);

            (await CallApi<String>(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT {Q("StringValue")} FROM {Q("Entity")}",
                    transaction,
                    cancellationToken: TestContext.Current.CancellationToken
                ))
                .Should().Be(entity.StringValue);

            await transaction.RollbackAsync();
        }

        (await CallApi<String>(
                useAsyncApi,
                this.Connection,
                $"SELECT {Q("StringValue")} FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeNull();
    }

    private static Task<TTarget> CallApi<TTarget>(
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
            return connection.ExecuteScalarAsync<TTarget>(
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
                connection.ExecuteScalar<TTarget>(
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
            return Task.FromException<TTarget>(ex);
        }
    }
}
