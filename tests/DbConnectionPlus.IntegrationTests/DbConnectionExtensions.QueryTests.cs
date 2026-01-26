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
    [Fact]
    public void Query_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        Invoking(() =>
                this.Connection.Query($"SELECT * FROM {Q("Entity")}", cancellationToken: cancellationToken).ToList()
            )
            .Should().Throw<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Fact]
    public void Query_CommandType_ShouldUseCommandType()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entities = this.CreateEntitiesInDb<Entity>();

        var dynamicObjects = this.Connection.Query(
            "GetEntities",
            commandType: CommandType.StoredProcedure,
            cancellationToken: TestContext.Current.CancellationToken
        ).ToList();

        EntityAssertions.AssertDynamicObjectsMatchEntities(dynamicObjects, entities);
    }

    [Fact]
    public void Query_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterEnumerationIsFinished()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(2);

        InterpolatedSqlStatement statement = $"SELECT * FROM {TemporaryTable(entities)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        var enumerator = this.Connection.Query(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        ).GetEnumerator();

        enumerator.MoveNext()
            .Should().BeTrue();

        if (this.TestDatabaseProvider.SupportsCommandExecutionWhileDataReaderIsOpen)
        {
            this.ExistsTemporaryTableInDb(temporaryTableName)
                .Should().BeTrue();
        }

        enumerator.MoveNext()
            .Should().BeTrue();

        enumerator.MoveNext()
            .Should().BeFalse();

        enumerator.Dispose();

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public void Query_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>();

        var dynamicObjects = this.Connection.Query(
            $"SELECT * FROM {TemporaryTable(entities)}",
            cancellationToken: TestContext.Current.CancellationToken
        ).ToList();

        EntityAssertions.AssertDynamicObjectsMatchEntities(dynamicObjects, entities);
    }

    [Fact]
    public void Query_InterpolatedParameter_ShouldPassInterpolatedParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        var dynamicObjects = this.Connection.Query(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
            cancellationToken: TestContext.Current.CancellationToken
        ).ToList();

        EntityAssertions.AssertDynamicObjectsMatchEntities(dynamicObjects, [entity]);
    }

    [Fact]
    public void Query_Parameter_ShouldPassParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        var statement = new InterpolatedSqlStatement(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entity.Id)
        );

        var dynamicObjects = this.Connection.Query(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        ).ToList();

        EntityAssertions.AssertDynamicObjectsMatchEntities(dynamicObjects, [entity]);
    }

    [Fact]
    public void Query_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterEnumerationIsFinished()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        InterpolatedSqlStatement statement = $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable(entityIds)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        var enumerator = this.Connection.Query(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        ).GetEnumerator();

        enumerator.MoveNext()
            .Should().BeTrue();

        if (this.TestDatabaseProvider.SupportsCommandExecutionWhileDataReaderIsOpen)
        {
            this.ExistsTemporaryTableInDb(temporaryTableName)
                .Should().BeTrue();
        }

        enumerator.MoveNext()
            .Should().BeTrue();

        enumerator.MoveNext()
            .Should().BeFalse();

        enumerator.Dispose();

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public void Query_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids();

        var dynamicObjects = this.Connection.Query(
            $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable(entityIds)}",
            cancellationToken: TestContext.Current.CancellationToken
        ).ToList();

        for (var i = 0; i < entityIds.Count; i++)
        {
            ValueConverter.ConvertValueToType<Int64>((Object)dynamicObjects[i].Id)
                .Should().Be(entityIds[i]);
        }
    }

    [Fact]
    public void Query_ShouldReturnDynamicObjectsForQueryResult()
    {
        var entities = this.CreateEntitiesInDb<Entity>();

        var dynamicObjects = this.Connection.Query(
            $"SELECT * FROM {Q("Entity")}",
            cancellationToken: TestContext.Current.CancellationToken
        ).ToList();

        EntityAssertions.AssertDynamicObjectsMatchEntities(dynamicObjects, entities);
    }

    [Fact]
    public void Query_Transaction_ShouldUseTransaction()
    {
        using (var transaction = this.Connection.BeginTransaction())
        {
            var entities = this.CreateEntitiesInDb<Entity>(null, transaction);

            var dynamicObjects = this.Connection.Query(
                $"SELECT * FROM {Q("Entity")}",
                transaction,
                cancellationToken: TestContext.Current.CancellationToken
            ).ToList();

            EntityAssertions.AssertDynamicObjectsMatchEntities(dynamicObjects, entities);

            transaction.Rollback();
        }

        this.Connection.Query(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeEmpty();
    }

    [Fact]
    public async Task QueryAsync_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        await Invoking(() =>
                this.Connection.QueryAsync(
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: cancellationToken
                ).ToListAsync(cancellationToken).AsTask()
            )
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Fact]
    public async Task QueryAsync_CommandType_ShouldUseCommandType()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entities = this.CreateEntitiesInDb<Entity>();

        var dynamicObjects = await this.Connection.QueryAsync(
            "GetEntities",
            commandType: CommandType.StoredProcedure,
            cancellationToken: TestContext.Current.CancellationToken
        ).ToListAsync(TestContext.Current.CancellationToken);

        EntityAssertions.AssertDynamicObjectsMatchEntities(dynamicObjects, entities);
    }

    [Fact]
    public async Task QueryAsync_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterEnumerationIsFinished()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(2);

        InterpolatedSqlStatement statement = $"SELECT * FROM {TemporaryTable(entities)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        var enumerator = this.Connection.QueryAsync(
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

    [Fact]
    public async Task QueryAsync_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>();

        var dynamicObjects = await this.Connection.QueryAsync(
            $"SELECT * FROM {TemporaryTable(entities)}",
            cancellationToken: TestContext.Current.CancellationToken
        ).ToListAsync(TestContext.Current.CancellationToken);

        EntityAssertions.AssertDynamicObjectsMatchEntities(dynamicObjects, entities);
    }

    [Fact]
    public async Task QueryAsync_InterpolatedParameter_ShouldPassInterpolatedParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        var dynamicObjects = await this.Connection.QueryAsync(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
            cancellationToken: TestContext.Current.CancellationToken
        ).ToListAsync(TestContext.Current.CancellationToken);

        EntityAssertions.AssertDynamicObjectsMatchEntities(dynamicObjects, [entity]);
    }

    [Fact]
    public async Task QueryAsync_Parameter_ShouldPassParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        var statement = new InterpolatedSqlStatement(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entity.Id)
        );

        var dynamicObjects = await this.Connection.QueryAsync(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        ).ToListAsync(TestContext.Current.CancellationToken);

        EntityAssertions.AssertDynamicObjectsMatchEntities(dynamicObjects, [entity]);
    }

    [Fact]
    public async Task QueryAsync_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterEnumerationIsFinished()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        InterpolatedSqlStatement statement = $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable(entityIds)}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        var enumerator = this.Connection.QueryAsync(
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

    [Fact]
    public async Task QueryAsync_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids();

        var dynamicObjects = await this.Connection.QueryAsync(
            $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable(entityIds)}",
            cancellationToken: TestContext.Current.CancellationToken
        ).ToListAsync(TestContext.Current.CancellationToken);

        for (var i = 0; i < entityIds.Count; i++)
        {
            ValueConverter.ConvertValueToType<Int64>((Object)dynamicObjects[i].Id)
                .Should().Be(entityIds[i]);
        }
    }

    [Fact]
    public async Task QueryAsync_ShouldReturnDynamicObjectsForQueryResult()
    {
        var entities = this.CreateEntitiesInDb<Entity>();

        var dynamicObjects = await this.Connection.QueryAsync(
            $"SELECT * FROM {Q("Entity")}",
            cancellationToken: TestContext.Current.CancellationToken
        ).ToListAsync(TestContext.Current.CancellationToken);

        EntityAssertions.AssertDynamicObjectsMatchEntities(dynamicObjects, entities);
    }

    [Fact]
    public async Task QueryAsync_Transaction_ShouldUseTransaction()
    {
        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            var entities = this.CreateEntitiesInDb<Entity>(null, transaction);

            var dynamicObjects = await this.Connection.QueryAsync(
                $"SELECT * FROM {Q("Entity")}",
                transaction,
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken);

            EntityAssertions.AssertDynamicObjectsMatchEntities(dynamicObjects, entities);

            await transaction.RollbackAsync();
        }

        (await this.Connection.QueryAsync(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ).ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEmpty();
    }
}
