using System.Data.Common;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests;

public sealed class
    DbConnectionExtensions_ExistsTests_MySql :
    DbConnectionExtensions_ExistsTests<MySqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ExistsTests_Oracle :
    DbConnectionExtensions_ExistsTests<OracleTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ExistsTests_PostgreSql :
    DbConnectionExtensions_ExistsTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ExistsTests_Sqlite :
    DbConnectionExtensions_ExistsTests<SqliteTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ExistsTests_SqlServer :
    DbConnectionExtensions_ExistsTests<SqlServerTestDatabaseProvider>;

public abstract class
    DbConnectionExtensions_ExistsTests<TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Exists_CancellationToken_ShouldCancelOperationIfCancellationIsRequested(Boolean useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        await Invoking(() => CallApi(useAsyncApi, this.Connection, "SELECT 1", cancellationToken: cancellationToken))
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Exists_CommandType_ShouldUseCommandType(Boolean useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        this.CreateEntitiesInDb<Entity>(1);

        (await CallApi(
                useAsyncApi,
                this.Connection,
                "GetFirstEntityId",
                commandType: CommandType.StoredProcedure,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Exists_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterExecution(
        Boolean useAsyncApi
    )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(1);

        InterpolatedSqlStatement statement = $"""
                                              SELECT     1
                                              FROM       {TemporaryTable(entities)}
                                              WHERE      {Q("Id")} = {Parameter(entities[0].Id)}
                                              """;

        var temporaryTableName = statement.TemporaryTables[0].Name;

        (await CallApi(
                useAsyncApi,
                this.Connection,
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeTrue();

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        Exists_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable(
            Boolean useAsyncApi
        )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(1);

        (await CallApi(
                useAsyncApi,
                this.Connection,
                $"""
                 SELECT     1
                 FROM       {TemporaryTable(entities)}
                 WHERE      {Q("Id")} = {Parameter(entities[0].Id)}
                 """,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Exists_InterpolatedParameter_ShouldPassInterpolatedParameter(Boolean useAsyncApi)
    {
        var entity = this.CreateEntityInDb<Entity>();

        (await CallApi(
                useAsyncApi,
                this.Connection,
                $"SELECT 1 FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Exists_Parameter_ShouldPassParameter(Boolean useAsyncApi)
    {
        var entity = this.CreateEntityInDb<Entity>();

        var statement = new InterpolatedSqlStatement(
            $"SELECT 1 FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entity.Id)
        );

        (await CallApi(
                useAsyncApi,
                this.Connection,
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Exists_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterExecution(Boolean useAsyncApi)
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        InterpolatedSqlStatement statement =
            $"SELECT 1 FROM {TemporaryTable(entityIds)} WHERE {Q("Value")} = {Parameter(entityIds[0])}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        (await CallApi(
                useAsyncApi,
                this.Connection,
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeTrue();

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Exists_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable(
        Boolean useAsyncApi
    )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        (await CallApi(
                useAsyncApi,
                this.Connection,
                $"SELECT 1 FROM {TemporaryTable(entityIds)} WHERE {Q("Value")} = {Parameter(entityIds[0])}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Exists_ShouldReturnBooleanIndicatingWhetherQueryReturnedAtLeastOneRow(Boolean useAsyncApi)
    {
        var entity = this.CreateEntityInDb<Entity>();

        (await CallApi(
                useAsyncApi,
                this.Connection,
                $"SELECT 1 FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeTrue();

        (await CallApi(
                useAsyncApi,
                this.Connection,
                $"SELECT 1 FROM {Q("Entity")} WHERE {Q("Id")} = -1",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Exists_Transaction_ShouldUseTransaction(Boolean useAsyncApi)
    {
        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            var entity = this.CreateEntityInDb<Entity>(transaction);

            (await CallApi(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT 1 FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
                    transaction,
                    cancellationToken: TestContext.Current.CancellationToken
                ))
                .Should().BeTrue();

            await transaction.RollbackAsync();
        }

        (await CallApi(
                useAsyncApi,
                this.Connection,
                $"SELECT 1 FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeFalse();
    }

    private static Task<Boolean> CallApi(
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
            return connection.ExistsAsync(
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
                connection.Exists(statement, transaction, commandTimeout, commandType, cancellationToken)
            );
        }
        catch (Exception ex)
        {
            return Task.FromException<Boolean>(ex);
        }
    }
}
