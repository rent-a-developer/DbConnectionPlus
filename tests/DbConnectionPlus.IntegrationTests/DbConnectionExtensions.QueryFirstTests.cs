using System.Data.Common;
using RentADeveloper.DbConnectionPlus.Converters;
using RentADeveloper.DbConnectionPlus.IntegrationTests.Assertions;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests;

public sealed class
    DbConnectionExtensions_QueryFirstTests_MySql :
    DbConnectionExtensions_QueryFirstTests<MySqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QueryFirstTests_Oracle :
    DbConnectionExtensions_QueryFirstTests<OracleTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QueryFirstTests_PostgreSql :
    DbConnectionExtensions_QueryFirstTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QueryFirstTests_Sqlite :
    DbConnectionExtensions_QueryFirstTests<SqliteTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QueryFirstTests_SqlServer :
    DbConnectionExtensions_QueryFirstTests<SqlServerTestDatabaseProvider>;

public abstract class
    DbConnectionExtensions_QueryFirstTests<TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QueryFirst_CancellationToken_ShouldCancelOperationIfCancellationIsRequested(Boolean useAsyncApi)
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
                )
            )
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QueryFirst_CommandType_ShouldUseCommandType(Boolean useAsyncApi)
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
    public async Task QueryFirst_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterExecution(
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
        QueryFirst_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable(
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
    public async Task QueryFirst_InterpolatedParameter_ShouldPassInterpolatedParameter(Boolean useAsyncApi)
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
    public async Task QueryFirst_Parameter_ShouldPassParameter(Boolean useAsyncApi)
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
    public Task QueryFirst_QueryReturnedNoRows_ShouldThrow(Boolean useAsyncApi) =>
        Invoking(() => CallApi(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = -1",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(
                "The SQL statement did not return any rows."
            );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QueryFirst_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterExecution(Boolean useAsyncApi)
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
        QueryFirst_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable(
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

        ValueConverter.ConvertValueToType<Int64>((Object)dynamicObject.Id)
            .Should().Be(entityIds[0]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QueryFirst_ShouldReturnDynamicObjectForFirstRow(Boolean useAsyncApi)
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
    public async Task QueryFirst_Transaction_ShouldUseTransaction(Boolean useAsyncApi)
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

        await Invoking(() => CallApi(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(
                "The SQL statement did not return any rows."
            );
    }

    private static Task<dynamic> CallApi(
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
            return connection.QueryFirstAsync(
                statement,
                transaction,
                commandTimeout,
                commandType,
                cancellationToken
            );
        }

        try
        {
            return Task.FromResult<dynamic>(
                connection.QueryFirst(
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
            return Task.FromException<dynamic>(ex);
        }
    }
}
