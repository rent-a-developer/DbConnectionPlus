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
    [Fact]
    public void QuerySingle_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        Invoking(() =>
                this.Connection.QuerySingle($"SELECT * FROM {Q("Entity")}", cancellationToken: cancellationToken)
            )
            .Should().Throw<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Fact]
    public void QuerySingle_CommandType_ShouldUseCommandType()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entity = this.CreateEntityInDb<Entity>();

        var dynamicObject = this.Connection.QuerySingle(
            "GetFirstEntity",
            commandType: CommandType.StoredProcedure,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);
    }

    [Fact]
    public void QuerySingle_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entity = Generate.Single<Entity>();

        InterpolatedSqlStatement statement = $"SELECT * FROM {TemporaryTable([entity])}";

        var temporaryTableName = statement.TemporaryTables.First().Name;

        var dynamicObject = this.Connection.QuerySingle(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public void QuerySingle_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entity = Generate.Single<Entity>();

        var dynamicObject = this.Connection.QuerySingle(
            $"SELECT * FROM {TemporaryTable([entity])}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);
    }

    [Fact]
    public void QuerySingle_InterpolatedParameter_ShouldPassInterpolatedParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        var dynamicObject = this.Connection.QuerySingle(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);
    }

    [Fact]
    public void QuerySingle_Parameter_ShouldPassParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        var statement = new InterpolatedSqlStatement(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entity.Id)
        );

        var dynamicObject = this.Connection.QuerySingle(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);
    }

    [Fact]
    public void QuerySingle_QueryReturnedMoreThanOneRow_ShouldThrow()
    {
        this.CreateEntitiesInDb<Entity>(2);

        Invoking(() => this.Connection.QuerySingle(
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidOperationException>()
            .WithMessage(
                "The SQL statement did return more than one row."
            );
    }

    [Fact]
    public void QuerySingle_QueryReturnedNoRows_ShouldThrow() =>
        Invoking(() => this.Connection.QuerySingle(
                    $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = -1",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidOperationException>()
            .WithMessage(
                "The SQL statement did not return any rows."
            );

    [Fact]
    public void QuerySingle_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityId = Generate.Id();

        InterpolatedSqlStatement statement = $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable([entityId])}";

        var temporaryTableName = statement.TemporaryTables.First().Name;

        var dynamicObject = this.Connection.QuerySingle(
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

    [Fact]
    public void QuerySingle_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityId = Generate.Id();

        var dynamicObject = this.Connection.QuerySingle(
            $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable([entityId])}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        ValueConverter.ConvertValueToType<Int64>((Object)dynamicObject.Id)
            .Should().Be(entityId);
    }

    [Fact]
    public void QuerySingle_ShouldReturnDynamicObjectForFirstRow()
    {
        var entity = this.CreateEntityInDb<Entity>();

        var dynamicObject = this.Connection.QuerySingle(
            $"SELECT * FROM {Q("Entity")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);
    }

    [Fact]
    public void QuerySingle_Transaction_ShouldUseTransaction()
    {
        using (var transaction = this.Connection.BeginTransaction())
        {
            var entity = this.CreateEntityInDb<Entity>(transaction);

            var dynamicObject = this.Connection.QuerySingle(
                $"SELECT * FROM {Q("Entity")}",
                transaction,
                cancellationToken: TestContext.Current.CancellationToken
            );

            EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);

            transaction.Rollback();
        }

        Invoking(() => this.Connection.QuerySingle(
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task QuerySingleAsync_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        await Invoking(() =>
                this.Connection.QuerySingleAsync(
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: cancellationToken
                )
            )
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Fact]
    public async Task QuerySingleAsync_CommandType_ShouldUseCommandType()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entity = this.CreateEntityInDb<Entity>();

        var dynamicObject = await this.Connection.QuerySingleAsync(
            "GetFirstEntity",
            commandType: CommandType.StoredProcedure,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);
    }

    [Fact]
    public async Task QuerySingleAsync_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entity = Generate.Single<Entity>();

        InterpolatedSqlStatement statement = $"SELECT * FROM {TemporaryTable([entity])}";

        var temporaryTableName = statement.TemporaryTables.First().Name;

        var dynamicObject = await this.Connection.QuerySingleAsync(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public async Task
        QuerySingleAsync_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entity = Generate.Single<Entity>();

        var dynamicObject = await this.Connection.QuerySingleAsync(
            $"SELECT * FROM {TemporaryTable([entity])}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);
    }

    [Fact]
    public async Task QuerySingleAsync_InterpolatedParameter_ShouldPassInterpolatedParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        var dynamicObject = await this.Connection.QuerySingleAsync(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);
    }

    [Fact]
    public async Task QuerySingleAsync_Parameter_ShouldPassParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        var statement = new InterpolatedSqlStatement(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entity.Id)
        );

        var dynamicObject = await this.Connection.QuerySingleAsync(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);
    }

    [Fact]
    public async Task QuerySingleAsync_QueryReturnedMoreThanOneRow_ShouldThrow()
    {
        this.CreateEntitiesInDb<Entity>(2);

        await Invoking(() => this.Connection.QuerySingleAsync(
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(
                "The SQL statement did return more than one row."
            );
    }


    [Fact]
    public Task QuerySingleAsync_QueryReturnedNoRows_ShouldThrow() =>
        Invoking(() => this.Connection.QuerySingleAsync(
                    $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = -1",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(
                "The SQL statement did not return any rows."
            );

    [Fact]
    public async Task QuerySingleAsync_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityId = Generate.Id();

        InterpolatedSqlStatement statement = $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable([entityId])}";

        var temporaryTableName = statement.TemporaryTables.First().Name;

        var dynamicObject = await this.Connection.QuerySingleAsync(
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

    [Fact]
    public async Task
        QuerySingleAsync_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityId = Generate.Id();

        var dynamicObject = await this.Connection.QuerySingleAsync(
            $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable([entityId])}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        ValueConverter.ConvertValueToType<Int64>((Object)dynamicObject.Id)
            .Should().Be(entityId);
    }

    [Fact]
    public async Task QuerySingleAsync_ShouldReturnDynamicObjectForFirstRow()
    {
        var entity = this.CreateEntityInDb<Entity>();

        var dynamicObject = await this.Connection.QuerySingleAsync(
            $"SELECT * FROM {Q("Entity")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);
    }

    [Fact]
    public async Task QuerySingleAsync_Transaction_ShouldUseTransaction()
    {
        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            var entity = this.CreateEntityInDb<Entity>(transaction);

            var dynamicObject = await this.Connection.QuerySingleAsync(
                $"SELECT * FROM {Q("Entity")}",
                transaction,
                cancellationToken: TestContext.Current.CancellationToken
            );

            EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);

            await transaction.RollbackAsync();
        }

        await Invoking(() => this.Connection.QuerySingleAsync(
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .Should().ThrowAsync<InvalidOperationException>();
    }
}
