using System.Data.Common;
using RentADeveloper.DbConnectionPlus.Converters;
using RentADeveloper.DbConnectionPlus.IntegrationTests.Assertions;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests;

public sealed class
    DbConnectionExtensions_QuerySingleTests_MySql :
    DbConnectionExtensions_QuerySingleTests<MySqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QuerySingleTests_Oracle :
    DbConnectionExtensions_QuerySingleTests<OracleTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QuerySingleTests_PostgreSql :
    DbConnectionExtensions_QuerySingleTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QuerySingleTests_Sqlite :
    DbConnectionExtensions_QuerySingleTests<SqliteTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QuerySingleTests_SqlServer :
    DbConnectionExtensions_QuerySingleTests<SqlServerTestDatabaseProvider>;

public abstract class
    DbConnectionExtensions_QuerySingleTests<TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingle_CancellationToken_ShouldCancelOperationIfCancellationIsRequested(Boolean useAsyncApi)
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
    public async Task QuerySingle_CommandType_ShouldUseCommandType(Boolean useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entity = this.CreateEntityInDb<Entity>();

        var dynamicObject = await CallApi(
            useAsyncApi,
            this.Connection,
            "GetFirstEntity",
            commandType: CommandType.StoredProcedure,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingle_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterExecution(
        Boolean useAsyncApi
    )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entity = Generate.Single<Entity>();

        InterpolatedSqlStatement statement = $"SELECT * FROM {TemporaryTable([entity])}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        var dynamicObject = await CallApi(
            useAsyncApi,
            this.Connection,
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        QuerySingle_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable(
            Boolean useAsyncApi
        )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entity = Generate.Single<Entity>();

        var dynamicObject = await CallApi(
            useAsyncApi,
            this.Connection,
            $"SELECT * FROM {TemporaryTable([entity])}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingle_InterpolatedParameter_ShouldPassInterpolatedParameter(Boolean useAsyncApi)
    {
        var entity = this.CreateEntityInDb<Entity>();

        var dynamicObject = await CallApi(
            useAsyncApi,
            this.Connection,
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingle_Parameter_ShouldPassParameter(Boolean useAsyncApi)
    {
        var entity = this.CreateEntityInDb<Entity>();

        var statement = new InterpolatedSqlStatement(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entity.Id)
        );

        var dynamicObject = await CallApi(
            useAsyncApi,
            this.Connection,
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingle_QueryReturnedMoreThanOneRow_ShouldThrow(Boolean useAsyncApi)
    {
        this.CreateEntitiesInDb<Entity>(2);

        await Invoking(() => CallApi(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(
                "The SQL statement did return more than one row."
            );
    }


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task QuerySingle_QueryReturnedNoRows_ShouldThrow(Boolean useAsyncApi) =>
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
    public async Task QuerySingle_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterExecution(Boolean useAsyncApi)
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityId = Generate.Id();

        InterpolatedSqlStatement statement = $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable([entityId])}";

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
            .Should().Be(entityId);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        QuerySingle_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable(
            Boolean useAsyncApi
        )
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityId = Generate.Id();

        var dynamicObject = await CallApi(
            useAsyncApi,
            this.Connection,
            $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable([entityId])}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        ValueConverter.ConvertValueToType<Int64>((Object)dynamicObject.Id)
            .Should().Be(entityId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingle_ShouldReturnDynamicObjectForFirstRow(Boolean useAsyncApi)
    {
        var entity = this.CreateEntityInDb<Entity>();

        var dynamicObject = await CallApi(
            useAsyncApi,
            this.Connection,
            $"SELECT * FROM {Q("Entity")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuerySingle_Transaction_ShouldUseTransaction(Boolean useAsyncApi)
    {
        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            var entity = this.CreateEntityInDb<Entity>(transaction);

            var dynamicObject = await CallApi(
                useAsyncApi,
                this.Connection,
                $"SELECT * FROM {Q("Entity")}",
                transaction,
                cancellationToken: TestContext.Current.CancellationToken
            );

            EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);

            await transaction.RollbackAsync();
        }

        await Invoking(() => CallApi(
                    useAsyncApi,
                    this.Connection,
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidOperationException>();
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
            return connection.QuerySingleAsync(
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
                connection.QuerySingle(
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
