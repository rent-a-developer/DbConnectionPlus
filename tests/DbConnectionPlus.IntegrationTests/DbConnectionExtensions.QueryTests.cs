using System.Data.Common;
using RentADeveloper.DbConnectionPlus.Converters;
using RentADeveloper.DbConnectionPlus.IntegrationTests.Assertions;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests;

public sealed class
    DbConnectionExtensions_QueryTests_MySql :
    DbConnectionExtensions_QueryTests<MySqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QueryTests_Oracle :
    DbConnectionExtensions_QueryTests<OracleTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QueryTests_PostgreSql :
    DbConnectionExtensions_QueryTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QueryTests_Sqlite :
    DbConnectionExtensions_QueryTests<SqliteTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QueryTests_SqlServer :
    DbConnectionExtensions_QueryTests<SqlServerTestDatabaseProvider>;

public abstract class
    DbConnectionExtensions_QueryTests<TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_CancellationToken_ShouldCancelOperationIfCancellationIsRequested(Boolean useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DelayNextDbCommand = true;

        await Invoking(() =>
                CallApi(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: cancellationToken
                ).ToListAsync(cancellationToken).AsTask()
            )
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_CommandType_ShouldUseCommandType(Boolean useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entities = this.CreateEntitiesInDb<Entity>();

        var dynamicObjects = await CallApi(
            useAsyncApi,
            this.Connection,
            "GetEntities",
            commandType: CommandType.StoredProcedure,
            cancellationToken: TestContext.Current.CancellationToken
        ).ToListAsync(TestContext.Current.CancellationToken);

        EntityAssertions.AssertDynamicObjectsMatchEntities(dynamicObjects, entities);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterEnumerationIsFinished(
        Boolean useAsyncApi
    )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(2);

        InterpolatedSqlStatement statement = $"SELECT * FROM {TemporaryTable(entities)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        var enumerator = CallApi(
            useAsyncApi,
            this.Connection,
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        ).GetAsyncEnumerator();

        (await enumerator.MoveNextAsync())
            .Should().BeTrue();

        if (this.TestDatabaseProvider.SupportsCommandExecutionWhileDataReaderIsOpen)
        {
            this.ExistsTemporaryTableInDb(temporaryTableName)
                .Should().BeTrue();
        }

        (await enumerator.MoveNextAsync())
            .Should().BeTrue();

        (await enumerator.MoveNextAsync())
            .Should().BeFalse();

        await enumerator.DisposeAsync();

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable(
        Boolean useAsyncApi
    )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>();

        var dynamicObjects = await CallApi(
            useAsyncApi,
            this.Connection,
            $"SELECT * FROM {TemporaryTable(entities)}",
            cancellationToken: TestContext.Current.CancellationToken
        ).ToListAsync(TestContext.Current.CancellationToken);

        EntityAssertions.AssertDynamicObjectsMatchEntities(dynamicObjects, entities);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_InterpolatedParameter_ShouldPassInterpolatedParameter(Boolean useAsyncApi)
    {
        var entity = this.CreateEntityInDb<Entity>();

        var dynamicObjects = await CallApi(
            useAsyncApi,
            this.Connection,
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
            cancellationToken: TestContext.Current.CancellationToken
        ).ToListAsync(TestContext.Current.CancellationToken);

        EntityAssertions.AssertDynamicObjectsMatchEntities(dynamicObjects, [entity]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_Parameter_ShouldPassParameter(Boolean useAsyncApi)
    {
        var entity = this.CreateEntityInDb<Entity>();

        var statement = new InterpolatedSqlStatement(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entity.Id)
        );

        var dynamicObjects = await CallApi(
            useAsyncApi,
            this.Connection,
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        ).ToListAsync(TestContext.Current.CancellationToken);

        EntityAssertions.AssertDynamicObjectsMatchEntities(dynamicObjects, [entity]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterEnumerationIsFinished(
        Boolean useAsyncApi
    )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        InterpolatedSqlStatement statement = $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable(entityIds)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        var enumerator = CallApi(
            useAsyncApi,
            this.Connection,
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        ).GetAsyncEnumerator();

        (await enumerator.MoveNextAsync())
            .Should().BeTrue();

        if (this.TestDatabaseProvider.SupportsCommandExecutionWhileDataReaderIsOpen)
        {
            this.ExistsTemporaryTableInDb(temporaryTableName)
                .Should().BeTrue();
        }

        (await enumerator.MoveNextAsync())
            .Should().BeTrue();

        (await enumerator.MoveNextAsync())
            .Should().BeFalse();

        await enumerator.DisposeAsync();

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable(
        Boolean useAsyncApi
    )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids();

        var dynamicObjects = await CallApi(
            useAsyncApi,
            this.Connection,
            $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable(entityIds)}",
            cancellationToken: TestContext.Current.CancellationToken
        ).ToListAsync(TestContext.Current.CancellationToken);

        for (var i = 0; i < entityIds.Count; i++)
        {
            ValueConverter.ConvertValueToType<Int64>((Object)dynamicObjects[i].Id)
                .Should().Be(entityIds[i]);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_ShouldReturnDynamicObjectsForQueryResult(Boolean useAsyncApi)
    {
        var entities = this.CreateEntitiesInDb<Entity>();

        var dynamicObjects = await CallApi(
            useAsyncApi,
            this.Connection,
            $"SELECT * FROM {Q("Entity")}",
            cancellationToken: TestContext.Current.CancellationToken
        ).ToListAsync(TestContext.Current.CancellationToken);

        EntityAssertions.AssertDynamicObjectsMatchEntities(dynamicObjects, entities);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_Transaction_ShouldUseTransaction(Boolean useAsyncApi)
    {
        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            var entities = this.CreateEntitiesInDb<Entity>(null, transaction);

            var dynamicObjects = await CallApi(
                useAsyncApi,
                this.Connection,
                $"SELECT * FROM {Q("Entity")}",
                transaction,
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken);

            EntityAssertions.AssertDynamicObjectsMatchEntities(dynamicObjects, entities);

            await transaction.RollbackAsync();
        }

        (await CallApi(
                useAsyncApi,
                this.Connection,
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEmpty();
    }

    private static IAsyncEnumerable<dynamic> CallApi(
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
            return connection.QueryAsync(
                statement,
                transaction,
                commandTimeout,
                commandType,
                cancellationToken
            );
        }

        return connection.Query(
            statement,
            transaction,
            commandTimeout,
            commandType,
            cancellationToken
        ).ToAsyncEnumerable();
    }
}
