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
    [Fact]
    public void QueryFirst_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        Invoking(() =>
                this.Connection.QueryFirst(
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: cancellationToken
                )
            )
            .Should().Throw<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Fact]
    public void QueryFirst_CommandType_ShouldUseCommandType()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entities = this.CreateEntitiesInDb<Entity>(2);

        var dynamicObject = this.Connection.QueryFirst(
            "GetEntities",
            commandType: CommandType.StoredProcedure,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);
    }

    [Fact]
    public void QueryFirst_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(2);

        InterpolatedSqlStatement statement = $"SELECT * FROM {TemporaryTable(entities)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        var dynamicObject = this.Connection.QueryFirst(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public void
        QueryFirst_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(2);

        var dynamicObject = this.Connection.QueryFirst(
            $"SELECT * FROM {TemporaryTable(entities)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);
    }

    [Fact]
    public void QueryFirst_InterpolatedParameter_ShouldPassInterpolatedParameter()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        var dynamicObject = this.Connection.QueryFirst(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entities[0].Id)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);
    }

    [Fact]
    public void QueryFirst_Parameter_ShouldPassParameter()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        var statement = new InterpolatedSqlStatement(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entities[0].Id)
        );

        var dynamicObject = this.Connection.QueryFirst(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);
    }

    [Fact]
    public void QueryFirst_QueryReturnedNoRows_ShouldThrow() =>
        Invoking(() => this.Connection.QueryFirst(
                    $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = -1",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidOperationException>()
            .WithMessage(
                "The SQL statement did not return any rows."
            );

    [Fact]
    public void QueryFirst_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        InterpolatedSqlStatement statement = $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable(entityIds)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        var dynamicObject = this.Connection.QueryFirst(
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

    [Fact]
    public void
        QueryFirst_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        var dynamicObject = this.Connection.QueryFirst(
            $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable(entityIds)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        ValueConverter.ConvertValueToType<Int64>((Object)dynamicObject.Id)
            .Should().Be(entityIds[0]);
    }

    [Fact]
    public void QueryFirst_ShouldReturnDynamicObjectForFirstRow()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        var dynamicObject = this.Connection.QueryFirst(
            $"SELECT * FROM {Q("Entity")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);
    }

    [Fact]
    public void QueryFirst_Transaction_ShouldUseTransaction()
    {
        using (var transaction = this.Connection.BeginTransaction())
        {
            var entities = this.CreateEntitiesInDb<Entity>(2, transaction);

            var dynamicObject = this.Connection.QueryFirst(
                $"SELECT * FROM {Q("Entity")}",
                transaction,
                cancellationToken: TestContext.Current.CancellationToken
            );

            EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);

            transaction.Rollback();
        }

        Invoking(() => this.Connection.QueryFirst(
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidOperationException>()
            .WithMessage(
                "The SQL statement did not return any rows."
            );
    }

    [Fact]
    public async Task QueryFirstAsync_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        await Invoking(() =>
                this.Connection.QueryFirstAsync(
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: cancellationToken
                )
            )
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Fact]
    public async Task QueryFirstAsync_CommandType_ShouldUseCommandType()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entities = this.CreateEntitiesInDb<Entity>(2);

        var dynamicObject = await this.Connection.QueryFirstAsync(
            "GetEntities",
            commandType: CommandType.StoredProcedure,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);
    }

    [Fact]
    public async Task QueryFirstAsync_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(2);

        InterpolatedSqlStatement statement = $"SELECT * FROM {TemporaryTable(entities)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        var dynamicObject = await this.Connection.QueryFirstAsync(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public async Task
        QueryFirstAsync_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(2);

        var dynamicObject = await this.Connection.QueryFirstAsync(
            $"SELECT * FROM {TemporaryTable(entities)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);
    }

    [Fact]
    public async Task QueryFirstAsync_InterpolatedParameter_ShouldPassInterpolatedParameter()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        var dynamicObject = await this.Connection.QueryFirstAsync(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entities[0].Id)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);
    }

    [Fact]
    public async Task QueryFirstAsync_Parameter_ShouldPassParameter()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        var statement = new InterpolatedSqlStatement(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entities[0].Id)
        );

        var dynamicObject = await this.Connection.QueryFirstAsync(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);
    }


    [Fact]
    public Task QueryFirstAsync_QueryReturnedNoRows_ShouldThrow() =>
        Invoking(() => this.Connection.QueryFirstAsync(
                    $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = -1",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(
                "The SQL statement did not return any rows."
            );

    [Fact]
    public async Task QueryFirstAsync_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        InterpolatedSqlStatement statement = $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable(entityIds)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        var dynamicObject = await this.Connection.QueryFirstAsync(
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

    [Fact]
    public async Task
        QueryFirstAsync_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        var dynamicObject = await this.Connection.QueryFirstAsync(
            $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable(entityIds)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        ValueConverter.ConvertValueToType<Int64>((Object)dynamicObject.Id)
            .Should().Be(entityIds[0]);
    }

    [Fact]
    public async Task QueryFirstAsync_ShouldReturnDynamicObjectForFirstRow()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        var dynamicObject = await this.Connection.QueryFirstAsync(
            $"SELECT * FROM {Q("Entity")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);
    }

    [Fact]
    public async Task QueryFirstAsync_Transaction_ShouldUseTransaction()
    {
        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            var entities = this.CreateEntitiesInDb<Entity>(2, transaction);

            var dynamicObject = await this.Connection.QueryFirstAsync(
                $"SELECT * FROM {Q("Entity")}",
                transaction,
                cancellationToken: TestContext.Current.CancellationToken
            );

            EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);

            await transaction.RollbackAsync();
        }

        await Invoking(() => this.Connection.QueryFirstAsync(
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(
                "The SQL statement did not return any rows."
            );
    }
}
