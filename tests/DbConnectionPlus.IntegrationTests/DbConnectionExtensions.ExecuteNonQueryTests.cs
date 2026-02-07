using System.Data.Common;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests;

public sealed class
    DbConnectionExtensions_ExecuteNonQueryTests_MySql :
    DbConnectionExtensions_ExecuteNonQueryTests<MySqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ExecuteNonQueryTests_Oracle :
    DbConnectionExtensions_ExecuteNonQueryTests<OracleTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ExecuteNonQueryTests_PostgreSql :
    DbConnectionExtensions_ExecuteNonQueryTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ExecuteNonQueryTests_Sqlite :
    DbConnectionExtensions_ExecuteNonQueryTests<SqliteTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ExecuteNonQueryTests_SqlServer :
    DbConnectionExtensions_ExecuteNonQueryTests<SqlServerTestDatabaseProvider>;

public abstract class
    DbConnectionExtensions_ExecuteNonQueryTests<TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteNonQuery_CancellationToken_ShouldCancelOperationIfCancellationIsRequested(
        Boolean useAsyncApi
    )
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var entity = this.CreateEntityInDb<Entity>();

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DelayNextDbCommand = true;

        await Invoking(() => CallApi(
                    useAsyncApi,
                    this.Connection,
                    $"DELETE FROM {Q("Entity")}",
                    cancellationToken: cancellationToken
                )
            )
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);

        // Since the operation was cancelled, the entity should still exist.
        this.ExistsEntityInDb(entity)
            .Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteNonQuery_CommandType_ShouldPassUseCommandType(Boolean useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProcedures, "");

        var entity = this.CreateEntityInDb<Entity>();

        await CallApi(
            useAsyncApi,
            this.Connection,
            Q("DeleteAllEntities"),
            commandType: CommandType.StoredProcedure,
            cancellationToken: TestContext.Current.CancellationToken
        );

        this.ExistsEntityInDb(entity)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteNonQuery_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterExecution(
        Boolean useAsyncApi
    )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = this.CreateEntitiesInDb<Entity>(5);
        var entitiesToDelete = entities.Take(2).ToList();

        InterpolatedSqlStatement statement =
            $"""
             DELETE FROM   {Q("Entity")}
             WHERE         EXISTS (
                               SELECT  1
                               FROM    {TemporaryTable(entitiesToDelete)} TEntitiesToDelete
                               WHERE   {Q("Entity")}.{Q("Id")} = TEntitiesToDelete.{Q("Id")} AND
                                       {Q("Entity")}.{Q("StringValue")} = TEntitiesToDelete.{Q("StringValue")} AND
                                       {Q("Entity")}.{Q("Int32Value")} = TEntitiesToDelete.{Q("Int32Value")}
                           )
             """;

        var temporaryTableName = statement.TemporaryTables[0].Name;

        (await CallApi(
                useAsyncApi,
                this.Connection,
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entitiesToDelete.Count);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        ExecuteNonQuery_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable(
            Boolean useAsyncApi
        )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = this.CreateEntitiesInDb<Entity>(5);
        var entitiesToDelete = entities.Take(2).ToList();

        await CallApi(
            useAsyncApi,
            this.Connection,
            $"""
             DELETE FROM   {Q("Entity")}
             WHERE         EXISTS (
                               SELECT  1
                               FROM    {TemporaryTable(entitiesToDelete)} TEntitiesToDelete
                               WHERE   {Q("Entity")}.{Q("Id")} = TEntitiesToDelete.{Q("Id")} AND
                                       {Q("Entity")}.{Q("StringValue")} = TEntitiesToDelete.{Q("StringValue")} AND
                                       {Q("Entity")}.{Q("Int32Value")} = TEntitiesToDelete.{Q("Int32Value")}
                           )
             """,
            cancellationToken: TestContext.Current.CancellationToken
        );

        foreach (var entity in entitiesToDelete)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeFalse();
        }

        foreach (var entity in entities.Except(entitiesToDelete))
        {
            this.ExistsEntityInDb(entity)
                .Should().BeTrue();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteNonQuery_InterpolatedParameter_ShouldPassInterpolatedParameter(Boolean useAsyncApi)
    {
        var entity = this.CreateEntityInDb<Entity>();

        await CallApi(
            useAsyncApi,
            this.Connection,
            $"DELETE FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        this.ExistsEntityInDb(entity)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteNonQuery_Parameter_ShouldPassParameter(Boolean useAsyncApi)
    {
        var entity = this.CreateEntityInDb<Entity>();

        var statement = new InterpolatedSqlStatement(
            $"DELETE FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entity.Id)
        );

        await CallApi(
            useAsyncApi,
            this.Connection,
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        this.ExistsEntityInDb(entity)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteNonQuery_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterExecution(
        Boolean useAsyncApi
    )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = this.CreateEntitiesInDb<Entity>(5);
        var entitiesToDelete = entities.Take(2).ToList();
        var idsOfEntitiesToDelete = entitiesToDelete.ConvertAll(a => a.Id);

        InterpolatedSqlStatement statement =
            $"""
             DELETE FROM    {Q("Entity")}
             WHERE          {Q("Id")} IN (SELECT {Q("Value")} FROM {TemporaryTable(idsOfEntitiesToDelete)})
             """;

        var temporaryTableName = statement.TemporaryTables[0].Name;

        (await CallApi(
                useAsyncApi,
                this.Connection,
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(idsOfEntitiesToDelete.Count);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        ExecuteNonQuery_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable(
            Boolean useAsyncApi
        )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = this.CreateEntitiesInDb<Entity>(5);
        var entitiesToDelete = entities.Take(2).ToList();
        var idsOfEntitiesToDelete = entitiesToDelete.ConvertAll(a => a.Id);

        await CallApi(
            useAsyncApi,
            this.Connection,
            $"""
             DELETE FROM    {Q("Entity")}
             WHERE          {Q("Id")} IN (SELECT {Q("Value")} FROM {TemporaryTable(idsOfEntitiesToDelete)})
             """,
            cancellationToken: TestContext.Current.CancellationToken
        );

        foreach (var entity in entitiesToDelete)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeFalse();
        }

        foreach (var entity in entities.Except(entitiesToDelete))
        {
            this.ExistsEntityInDb(entity)
                .Should().BeTrue();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteNonQuery_ShouldReturnNumberOfAffectedRows(Boolean useAsyncApi)
    {
        var entity = this.CreateEntityInDb<Entity>();

        (await CallApi(
                useAsyncApi,
                this.Connection,
                $"DELETE FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(1);

        (await CallApi(
                useAsyncApi,
                this.Connection,
                $"DELETE FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteNonQuery_Transaction_ShouldUseTransaction(Boolean useAsyncApi)
    {
        var entity = this.CreateEntityInDb<Entity>();

        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            await CallApi(
                useAsyncApi,
                this.Connection,
                $"DELETE FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
                transaction,
                cancellationToken: TestContext.Current.CancellationToken
            );

            this.ExistsEntityInDb(entity, transaction)
                .Should().BeFalse();

            await transaction.RollbackAsync();
        }

        this.ExistsEntityInDb(entity)
            .Should().BeTrue();
    }

    private static Task<Int32> CallApi(
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
            return connection.ExecuteNonQueryAsync(
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
                connection.ExecuteNonQuery(statement, transaction, commandTimeout, commandType, cancellationToken)
            );
        }
        catch (Exception ex)
        {
            return Task.FromException<Int32>(ex);
        }
    }
}
