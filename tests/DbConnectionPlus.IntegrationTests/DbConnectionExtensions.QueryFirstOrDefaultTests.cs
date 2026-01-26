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
    [Fact]
    public void QueryFirstOrDefault_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        Invoking(() =>
                this.Connection.QueryFirstOrDefault(
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: cancellationToken
                )
            )
            .Should().Throw<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Fact]
    public void QueryFirstOrDefault_CommandType_ShouldUseCommandType()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entities = this.CreateEntitiesInDb<Entity>(2);

        var dynamicObject = this.Connection.QueryFirstOrDefault(
            "GetEntities",
            commandType: CommandType.StoredProcedure,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);
    }

    [Fact]
    public void QueryFirstOrDefault_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(2);

        InterpolatedSqlStatement statement = $"SELECT * FROM {TemporaryTable(entities)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        var dynamicObject = this.Connection.QueryFirstOrDefault(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public void
        QueryFirstOrDefault_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(2);

        var dynamicObject = this.Connection.QueryFirstOrDefault(
            $"SELECT * FROM {TemporaryTable(entities)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);
    }

    [Fact]
    public void QueryFirstOrDefault_InterpolatedParameter_ShouldPassInterpolatedParameter()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        var dynamicObject = this.Connection.QueryFirstOrDefault(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entities[0].Id)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);
    }

    [Fact]
    public void QueryFirstOrDefault_Parameter_ShouldPassParameter()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        var statement = new InterpolatedSqlStatement(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entities[0].Id)
        );

        var dynamicObject = this.Connection.QueryFirstOrDefault(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);
    }

    [Fact]
    public void QueryFirstOrDefault_QueryReturnedNoRows_ShouldReturnNull() =>
        ((Object?)this.Connection.QueryFirstOrDefault(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = -1",
            cancellationToken: TestContext.Current.CancellationToken
        ))
        .Should().BeNull();

    [Fact]
    public void QueryFirstOrDefault_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        InterpolatedSqlStatement statement = $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable(entityIds)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        var dynamicObject = this.Connection.QueryFirstOrDefault(
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
        QueryFirstOrDefault_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        var dynamicObject = this.Connection.QueryFirstOrDefault(
            $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable(entityIds)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        ValueConverter.ConvertValueToType<Int64>((Object)dynamicObject!.Id)
            .Should().Be(entityIds[0]);
    }

    [Fact]
    public void QueryFirstOrDefault_ShouldReturnDynamicObjectForFirstRow()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        var dynamicObject = this.Connection.QueryFirstOrDefault(
            $"SELECT * FROM {Q("Entity")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);
    }

    [Fact]
    public void QueryFirstOrDefault_Transaction_ShouldUseTransaction()
    {
        using (var transaction = this.Connection.BeginTransaction())
        {
            var entities = this.CreateEntitiesInDb<Entity>(2, transaction);

            var dynamicObject = this.Connection.QueryFirstOrDefault(
                $"SELECT * FROM {Q("Entity")}",
                transaction,
                cancellationToken: TestContext.Current.CancellationToken
            );

            EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);

            transaction.Rollback();
        }

        ((Object?)this.Connection.QueryFirstOrDefault(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeNull();
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        await Invoking(() =>
                this.Connection.QueryFirstOrDefaultAsync(
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: cancellationToken
                )
            )
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_CommandType_ShouldUseCommandType()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entities = this.CreateEntitiesInDb<Entity>(2);

        var dynamicObject = await this.Connection.QueryFirstOrDefaultAsync(
            "GetEntities",
            commandType: CommandType.StoredProcedure,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(2);

        InterpolatedSqlStatement statement = $"SELECT * FROM {TemporaryTable(entities)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        var dynamicObject = await this.Connection.QueryFirstOrDefaultAsync(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public async Task
        QueryFirstOrDefaultAsync_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(2);

        var dynamicObject = await this.Connection.QueryFirstOrDefaultAsync(
            $"SELECT * FROM {TemporaryTable(entities)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_InterpolatedParameter_ShouldPassInterpolatedParameter()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        var dynamicObject = await this.Connection.QueryFirstOrDefaultAsync(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entities[0].Id)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_Parameter_ShouldPassParameter()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        var statement = new InterpolatedSqlStatement(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entities[0].Id)
        );

        var dynamicObject = await this.Connection.QueryFirstOrDefaultAsync(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);
    }


    [Fact]
    public async Task QueryFirstOrDefaultAsync_QueryReturnedNoRows_ShouldReturnNull() =>
        ((Object?)await this.Connection.QueryFirstOrDefaultAsync(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = -1",
            cancellationToken: TestContext.Current.CancellationToken
        ))
        .Should().BeNull();

    [Fact]
    public async Task QueryFirstOrDefaultAsync_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        InterpolatedSqlStatement statement = $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable(entityIds)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        var dynamicObject = await this.Connection.QueryFirstOrDefaultAsync(
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
        QueryFirstOrDefaultAsync_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        var dynamicObject = await this.Connection.QueryFirstOrDefaultAsync(
            $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable(entityIds)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        ValueConverter.ConvertValueToType<Int64>((Object)dynamicObject!.Id)
            .Should().Be(entityIds[0]);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_ShouldReturnDynamicObjectForFirstRow()
    {
        var entities = this.CreateEntitiesInDb<Entity>(2);

        var dynamicObject = await this.Connection.QueryFirstOrDefaultAsync(
            $"SELECT * FROM {Q("Entity")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_Transaction_ShouldUseTransaction()
    {
        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            var entities = this.CreateEntitiesInDb<Entity>(2, transaction);

            var dynamicObject = await this.Connection.QueryFirstOrDefaultAsync(
                $"SELECT * FROM {Q("Entity")}",
                transaction,
                cancellationToken: TestContext.Current.CancellationToken
            );

            EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entities[0]);

            await transaction.RollbackAsync();
        }

        ((Object?)await this.Connection.QueryFirstOrDefaultAsync(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeNull();
    }
}
