using System.Data.Common;
using RentADeveloper.DbConnectionPlus.Converters;
using RentADeveloper.DbConnectionPlus.IntegrationTests.Assertions;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests;

public sealed class
    DbConnectionExtensions_QueryFirstOrDefaultTests_MySql :
    DbConnectionExtensions_QueryFirstOrDefaultTests<MySqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QueryFirstOrDefaultTests_Oracle :
    DbConnectionExtensions_QueryFirstOrDefaultTests<OracleTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QueryFirstOrDefaultTests_PostgreSql :
    DbConnectionExtensions_QueryFirstOrDefaultTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QueryFirstOrDefaultTests_Sqlite :
    DbConnectionExtensions_QueryFirstOrDefaultTests<SqliteTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QueryFirstOrDefaultTests_SqlServer :
    DbConnectionExtensions_QueryFirstOrDefaultTests<SqlServerTestDatabaseProvider>;

public abstract class
    DbConnectionExtensions_QueryFirstOrDefaultTests<TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QueryFirstOrDefault_CancellationToken_ShouldCancelOperationIfCancellationIsRequested(
        Boolean useAsyncApi
    )
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        await Invoking(() =>
                CallApi(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: cancellationToken
                )
            )
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QueryFirstOrDefault_CommandType_ShouldUseCommandType(Boolean useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entities = this.CreateEntitiesInDb<Entity>(2);

        var dynamicObject = await CallApi(
            useAsyncApi,
            this.Connection,
            "GetEntities",
            commandType: CommandType.StoredProcedure,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QueryFirstOrDefault_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterExecution(
        Boolean useAsyncApi
    )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(2);

        InterpolatedSqlStatement statement = $"SELECT * FROM {TemporaryTable(entities)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        var dynamicObject = await CallApi(
            useAsyncApi,
            this.Connection,
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        QueryFirstOrDefault_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable(
            Boolean useAsyncApi
        )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(2);

        var dynamicObject = await CallApi(
            useAsyncApi,
            this.Connection,
            $"SELECT * FROM {TemporaryTable(entities)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QueryFirstOrDefault_InterpolatedParameter_ShouldPassInterpolatedParameter(Boolean useAsyncApi)
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        var dynamicObject = await CallApi(
            useAsyncApi,
            this.Connection,
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entities[0].Id)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QueryFirstOrDefault_Parameter_ShouldPassParameter(Boolean useAsyncApi)
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        var statement = new InterpolatedSqlStatement(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entities[0].Id)
        );

        var dynamicObject = await CallApi(
            useAsyncApi,
            this.Connection,
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);
    }


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QueryFirstOrDefault_QueryReturnedNoRows_ShouldReturnNull(Boolean useAsyncApi) =>
        ((Object?)await CallApi(
            useAsyncApi,
            this.Connection,
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = -1",
            cancellationToken: TestContext.Current.CancellationToken
        ))
        .Should().BeNull();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QueryFirstOrDefault_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterExecution(
        Boolean useAsyncApi
    )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        InterpolatedSqlStatement statement = $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable(entityIds)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        var dynamicObject = await CallApi(
            useAsyncApi,
            this.Connection,
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        ((Object?)dynamicObject)
            .Should().NotBeNull();

        ((Object?)dynamicObject.Id)
            .Should().Be(entityIds[0]);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        QueryFirstOrDefault_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable(
            Boolean useAsyncApi
        )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        var dynamicObject = await CallApi(
            useAsyncApi,
            this.Connection,
            $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable(entityIds)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        ValueConverter.ConvertValueToType<Int64>((Object)dynamicObject!.Id)
            .Should().Be(entityIds[0]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QueryFirstOrDefault_ShouldReturnDynamicObjectForFirstRow(Boolean useAsyncApi)
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        var dynamicObject = await CallApi(
            useAsyncApi,
            this.Connection,
            $"SELECT * FROM {Q("Entity")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QueryFirstOrDefault_Transaction_ShouldUseTransaction(Boolean useAsyncApi)
    {
        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            var entities = this.CreateEntitiesInDb<Entity>(2, transaction);

            var dynamicObject = await CallApi(
                useAsyncApi,
                this.Connection,
                $"SELECT * FROM {Q("Entity")}",
                transaction,
                cancellationToken: TestContext.Current.CancellationToken
            );

            EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);

            await transaction.RollbackAsync();
        }

        ((Object?)await CallApi(
                useAsyncApi,
                this.Connection,
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeNull();
    }

    private static Task<dynamic?> CallApi(
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
            return connection.QueryFirstOrDefaultAsync(
                statement,
                transaction,
                commandTimeout,
                commandType,
                cancellationToken
            );
        }

        try
        {
            return Task.FromResult<dynamic?>(
                connection.QueryFirstOrDefault(
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
            return Task.FromException<dynamic?>(ex);
        }
    }
}
