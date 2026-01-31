using System.Data.Common;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests;

public sealed class
    DbConnectionExtensions_ExecuteReaderTests_MySql :
    DbConnectionExtensions_ExecuteReaderTests<MySqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ExecuteReaderTests_Oracle :
    DbConnectionExtensions_ExecuteReaderTests<OracleTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ExecuteReaderTests_PostgreSql :
    DbConnectionExtensions_ExecuteReaderTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ExecuteReaderTests_Sqlite :
    DbConnectionExtensions_ExecuteReaderTests<SqliteTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ExecuteReaderTests_SqlServer :
    DbConnectionExtensions_ExecuteReaderTests<SqlServerTestDatabaseProvider>;

public abstract class
    DbConnectionExtensions_ExecuteReaderTests<TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteReader_CancellationToken_ShouldCancelOperationIfCancellationIsRequested(
        Boolean useAsyncApi
    )
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        await Invoking(async () =>
                {
                    await using var reader = await CallApi(
                        useAsyncApi,
                        this.Connection,
                        $"SELECT * FROM {Q("Entity")}",
                        cancellationToken: cancellationToken
                    );
                }
            )
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteReader_CommandBehavior_ShouldUseCommandBehavior(Boolean useAsyncApi)
    {
        var reader = await CallApi(
            useAsyncApi,
            this.Connection,
            $"SELECT * FROM {Q("Entity")}",
            commandBehavior: CommandBehavior.CloseConnection,
            cancellationToken: TestContext.Current.CancellationToken
        );

        await reader.DisposeAsync();

        this.Connection.State
            .Should().Be(ConnectionState.Closed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteReader_CommandType_ShouldUseCommandType(Boolean useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entities = this.CreateEntitiesInDb<Entity>();

        await using var reader = await CallApi(
            useAsyncApi,
            this.Connection,
            Q("GetEntityIdsAndStringValues"),
            commandType: CommandType.StoredProcedure,
            cancellationToken: TestContext.Current.CancellationToken
        );

        foreach (var entity in entities)
        {
            (await reader.ReadAsync(TestContext.Current.CancellationToken))
                .Should().BeTrue();

            reader.GetInt64(0)
                .Should().Be(entity.Id);

            reader.GetString(1)
                .Should().Be(entity.StringValue);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        ExecuteReader_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterDataReaderDisposal(Boolean useAsyncApi)
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>();

        InterpolatedSqlStatement statement = $"""
                                              SELECT     {Q("Id")}
                                              FROM       {TemporaryTable(entities)}
                                              """;
        var temporaryTableName = statement.TemporaryTables[0].Name;

        var reader = await CallApi(
            useAsyncApi,
            this.Connection,
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        if (this.TestDatabaseProvider.SupportsCommandExecutionWhileDataReaderIsOpen)
        {
            this.ExistsTemporaryTableInDb(temporaryTableName)
                .Should().BeTrue();
        }

        await reader.DisposeAsync();

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        ExecuteReader_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable(
            Boolean useAsyncApi
        )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>();

        await using var reader = await CallApi(
            useAsyncApi,
            this.Connection,
            $"""
             SELECT     {Q("Id")}, {Q("StringValue")}, {Q("DecimalValue")}
             FROM       {TemporaryTable(entities)}
             """,
            cancellationToken: TestContext.Current.CancellationToken
        );

        foreach (var entity in entities)
        {
            (await reader.ReadAsync(TestContext.Current.CancellationToken))
                .Should().BeTrue();

            reader.GetInt64(0)
                .Should().Be(entity.Id);

            reader.GetString(1)
                .Should().Be(entity.StringValue);

            reader.GetDecimal(2)
                .Should().Be(entity.DecimalValue);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteReader_InterpolatedParameter_ShouldPassInterpolatedParameter(Boolean useAsyncApi)
    {
        var entity = this.CreateEntityInDb<Entity>();

        await using var reader = await CallApi(
            useAsyncApi,
            this.Connection,
            $"SELECT {Q("StringValue")} FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        (await reader.ReadAsync(TestContext.Current.CancellationToken))
            .Should().BeTrue();

        reader.GetString(0)
            .Should().Be(entity.StringValue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteReader_Parameter_ShouldPassParameter(Boolean useAsyncApi)
    {
        var entity = this.CreateEntityInDb<Entity>();

        var statement = new InterpolatedSqlStatement(
            $"SELECT {Q("StringValue")} FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entity.Id)
        );

        await using var reader = await CallApi(
            useAsyncApi,
            this.Connection,
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        (await reader.ReadAsync(TestContext.Current.CancellationToken))
            .Should().BeTrue();

        reader.GetString(0)
            .Should().Be(entity.StringValue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteReader_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterDataReaderDisposal(
        Boolean useAsyncApi
    )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        InterpolatedSqlStatement statement = $"SELECT {Q("Value")} FROM {TemporaryTable(entityIds)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        var reader = await CallApi(
            useAsyncApi,
            this.Connection,
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        if (this.TestDatabaseProvider.SupportsCommandExecutionWhileDataReaderIsOpen)
        {
            this.ExistsTemporaryTableInDb(temporaryTableName)
                .Should().BeTrue();
        }

        await reader.DisposeAsync();

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        ExecuteReader_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable(
            Boolean useAsyncApi
        )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids();

        await using var reader = await CallApi(
            useAsyncApi,
            this.Connection,
            $"SELECT {Q("Value")} FROM {TemporaryTable(entityIds)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        foreach (var entityId in entityIds)
        {
            (await reader.ReadAsync(TestContext.Current.CancellationToken))
                .Should().BeTrue();

            reader.GetInt64(0)
                .Should().Be(entityId);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteReader_ShouldReturnDataReaderForQueryResult(Boolean useAsyncApi)
    {
        var entities = this.CreateEntitiesInDb<Entity>();

        await using var reader = await CallApi(
            useAsyncApi,
            this.Connection,
            $"SELECT {Q("Id")}, {Q("StringValue")} FROM {Q("Entity")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        foreach (var entity in entities)
        {
            (await reader.ReadAsync(TestContext.Current.CancellationToken))
                .Should().BeTrue();

            reader.GetInt64(0)
                .Should().Be(entity.Id);

            reader.GetString(1)
                .Should().Be(entity.StringValue);
        }

        (await reader.ReadAsync(TestContext.Current.CancellationToken))
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteReader_Transaction_ShouldUseTransaction(Boolean useAsyncApi)
    {
        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            var entities = this.CreateEntitiesInDb<Entity>(null, transaction);

            var reader = await CallApi(
                useAsyncApi,
                this.Connection,
                $"SELECT {Q("Id")}, {Q("StringValue")} FROM {Q("Entity")}",
                transaction,
                cancellationToken: TestContext.Current.CancellationToken
            );

            reader.HasRows
                .Should().BeTrue();

            foreach (var entity in entities)
            {
                (await reader.ReadAsync(TestContext.Current.CancellationToken))
                    .Should().BeTrue();

                reader.GetInt64(0)
                    .Should().Be(entity.Id);

                reader.GetString(1)
                    .Should().Be(entity.StringValue);
            }

            await reader.DisposeAsync();

            await transaction.RollbackAsync();
        }

        (await CallApi(
                useAsyncApi,
                this.Connection,
                $"SELECT {Q("Id")}, {Q("StringValue")} FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )).HasRows
            .Should().BeFalse();
    }

    private static Task<DbDataReader> CallApi(
        Boolean useAsyncApi,
        DbConnection connection,
        InterpolatedSqlStatement statement,
        DbTransaction? transaction = null,
        TimeSpan? commandTimeout = null,
        CommandBehavior commandBehavior = CommandBehavior.Default,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default
    )
    {
        if (useAsyncApi)
        {
            return connection.ExecuteReaderAsync(
                statement,
                transaction,
                commandTimeout,
                commandBehavior,
                commandType,
                cancellationToken
            );
        }

        try
        {
            return Task.FromResult(
                connection.ExecuteReader(
                    statement,
                    transaction,
                    commandTimeout,
                    commandBehavior,
                    commandType,
                    cancellationToken
                )
            );
        }
        catch (Exception ex)
        {
            return Task.FromException<DbDataReader>(ex);
        }
    }
}
