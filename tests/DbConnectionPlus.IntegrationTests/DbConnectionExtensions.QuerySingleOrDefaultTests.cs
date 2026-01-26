using RentADeveloper.DbConnectionPlus.Converters;
using RentADeveloper.DbConnectionPlus.IntegrationTests.Assertions;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests;

public sealed class
    DbConnectionExtensions_QuerySingleOrDefaultTests_MySql :
    DbConnectionExtensions_QuerySingleOrDefaultTests<MySqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QuerySingleOrDefaultTests_Oracle :
    DbConnectionExtensions_QuerySingleOrDefaultTests<OracleTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QuerySingleOrDefaultTests_PostgreSql :
    DbConnectionExtensions_QuerySingleOrDefaultTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QuerySingleOrDefaultTests_Sqlite :
    DbConnectionExtensions_QuerySingleOrDefaultTests<SqliteTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_QuerySingleOrDefaultTests_SqlServer :
    DbConnectionExtensions_QuerySingleOrDefaultTests<SqlServerTestDatabaseProvider>;

public abstract class
    DbConnectionExtensions_QuerySingleOrDefaultTests
    <TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    [Fact]
    public void QuerySingleOrDefault_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        Invoking(() =>
                this.Connection.QuerySingleOrDefault(
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: cancellationToken
                )
            )
            .Should().Throw<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Fact]
    public void QuerySingleOrDefault_CommandType_ShouldUseCommandType()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entity = this.CreateEntityInDb<Entity>();

        var dynamicObject = this.Connection.QuerySingleOrDefault(
            "GetFirstEntity",
            commandType: CommandType.StoredProcedure,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);
    }

    [Fact]
    public void QuerySingleOrDefault_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entity = Generate.Single<Entity>();

        InterpolatedSqlStatement statement = $"SELECT * FROM {TemporaryTable([entity])}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        var dynamicObject = this.Connection.QuerySingleOrDefault(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public void
        QuerySingleOrDefault_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entity = Generate.Single<Entity>();

        var dynamicObject = this.Connection.QuerySingleOrDefault(
            $"SELECT * FROM {TemporaryTable([entity])}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);
    }

    [Fact]
    public void QuerySingleOrDefault_InterpolatedParameter_ShouldPassInterpolatedParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        var dynamicObject = this.Connection.QuerySingleOrDefault(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);
    }

    [Fact]
    public void QuerySingleOrDefault_Parameter_ShouldPassParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        var statement = new InterpolatedSqlStatement(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entity.Id)
        );

        var dynamicObject = this.Connection.QuerySingleOrDefault(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);
    }

    [Fact]
    public void QuerySingleOrDefault_QueryReturnedMoreThanOneRow_ShouldThrow()
    {
        this.CreateEntitiesInDb<Entity>(2);

        Invoking(() => this.Connection.QuerySingleOrDefault(
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
    public void QuerySingleOrDefault_QueryReturnedNoRows_ShouldReturnNull() =>
        ((Object?)this.Connection.QuerySingleOrDefault(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = -1",
            cancellationToken: TestContext.Current.CancellationToken
        ))
        .Should().BeNull();

    [Fact]
    public void QuerySingleOrDefault_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityId = Generate.Id();

        InterpolatedSqlStatement statement = $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable([entityId])}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        var dynamicObject = this.Connection.QuerySingleOrDefault(
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
    public void
        QuerySingleOrDefault_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityId = Generate.Id();

        var dynamicObject = this.Connection.QuerySingleOrDefault(
            $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable([entityId])}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        ValueConverter.ConvertValueToType<Int64>((Object)dynamicObject!.Id)
            .Should().Be(entityId);
    }

    [Fact]
    public void QuerySingleOrDefault_ShouldReturnDynamicObjectForFirstRow()
    {
        var entity = this.CreateEntityInDb<Entity>();

        var dynamicObject = this.Connection.QuerySingleOrDefault(
            $"SELECT * FROM {Q("Entity")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);
    }

    [Fact]
    public void QuerySingleOrDefault_Transaction_ShouldUseTransaction()
    {
        using (var transaction = this.Connection.BeginTransaction())
        {
            var entity = this.CreateEntityInDb<Entity>(transaction);

            var dynamicObject = this.Connection.QuerySingleOrDefault(
                $"SELECT * FROM {Q("Entity")}",
                transaction,
                cancellationToken: TestContext.Current.CancellationToken
            );

            EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);

            transaction.Rollback();
        }

        ((Object?)this.Connection.QuerySingleOrDefault(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeNull();
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        await Invoking(() =>
                this.Connection.QuerySingleOrDefaultAsync(
                    $"SELECT * FROM {Q("Entity")}",
                    cancellationToken: cancellationToken
                )
            )
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_CommandType_ShouldUseCommandType()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entity = this.CreateEntityInDb<Entity>();

        var dynamicObject = await this.Connection.QuerySingleOrDefaultAsync(
            "GetFirstEntity",
            commandType: CommandType.StoredProcedure,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entity = Generate.Single<Entity>();

        InterpolatedSqlStatement statement = $"SELECT * FROM {TemporaryTable([entity])}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        var dynamicObject = await this.Connection.QuerySingleOrDefaultAsync(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public async Task
        QuerySingleOrDefaultAsync_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entity = Generate.Single<Entity>();

        var dynamicObject = await this.Connection.QuerySingleOrDefaultAsync(
            $"SELECT * FROM {TemporaryTable([entity])}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_InterpolatedParameter_ShouldPassInterpolatedParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        var dynamicObject = await this.Connection.QuerySingleOrDefaultAsync(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_Parameter_ShouldPassParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        var statement = new InterpolatedSqlStatement(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entity.Id)
        );

        var dynamicObject = await this.Connection.QuerySingleOrDefaultAsync(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_QueryReturnedMoreThanOneRow_ShouldThrow()
    {
        this.CreateEntitiesInDb<Entity>(2);

        await Invoking(() => this.Connection.QuerySingleOrDefaultAsync(
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
    public async Task QuerySingleOrDefaultAsync_QueryReturnedNoRows_ShouldReturnNull() =>
        ((Object?)await this.Connection.QuerySingleOrDefaultAsync(
            $"SELECT * FROM {Q("Entity")} WHERE {Q("Id")} = -1",
            cancellationToken: TestContext.Current.CancellationToken
        ))
        .Should().BeNull();

    [Fact]
    public async Task QuerySingleOrDefaultAsync_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityId = Generate.Id();

        InterpolatedSqlStatement statement = $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable([entityId])}";

        var temporaryTableName = statement.TemporaryTables[0].Name;

        var dynamicObject = await this.Connection.QuerySingleOrDefaultAsync(
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
        QuerySingleOrDefaultAsync_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityId = Generate.Id();

        var dynamicObject = await this.Connection.QuerySingleOrDefaultAsync(
            $"SELECT {Q("Value")} AS {Q("Id")} FROM {TemporaryTable([entityId])}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        ValueConverter.ConvertValueToType<Int64>((Object)dynamicObject!.Id)
            .Should().Be(entityId);
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_ShouldReturnDynamicObjectForFirstRow()
    {
        var entity = this.CreateEntityInDb<Entity>();

        var dynamicObject = await this.Connection.QuerySingleOrDefaultAsync(
            $"SELECT * FROM {Q("Entity")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_Transaction_ShouldUseTransaction()
    {
        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            var entity = this.CreateEntityInDb<Entity>(transaction);

            var dynamicObject = await this.Connection.QuerySingleOrDefaultAsync(
                $"SELECT * FROM {Q("Entity")}",
                transaction,
                cancellationToken: TestContext.Current.CancellationToken
            );

            EntityAssertions.AssertDynamicObjectMatchesEntity(dynamicObject, entity);

            await transaction.RollbackAsync();
        }

        ((Object?)await this.Connection.QuerySingleOrDefaultAsync(
                $"SELECT * FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeNull();
    }
}
