namespace RentADeveloper.DbConnectionPlus.IntegrationTests;

public sealed class
    DbConnectionExtensions_ExistsTests_MySql :
    DbConnectionExtensions_ExistsTests<MySqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ExistsTests_Oracle :
    DbConnectionExtensions_ExistsTests<OracleTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ExistsTests_PostgreSql :
    DbConnectionExtensions_ExistsTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ExistsTests_Sqlite :
    DbConnectionExtensions_ExistsTests<SqliteTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ExistsTests_SqlServer :
    DbConnectionExtensions_ExistsTests<SqlServerTestDatabaseProvider>;

public abstract class
    DbConnectionExtensions_ExistsTests<TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    [Fact]
    public void Exists_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        Invoking(() => this.Connection.Exists("SELECT 1", cancellationToken: cancellationToken))
            .Should().Throw<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Fact]
    public void Exists_CommandType_ShouldUseCommandType()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        this.CreateEntitiesInDb<Entity>(1);

        this.Connection.Exists(
                "GetFirstEntityId",
                commandType: CommandType.StoredProcedure,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeTrue();
    }

    [Fact]
    public void Exists_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(1);

        InterpolatedSqlStatement statement = $"""
                                              SELECT     1
                                              FROM       {TemporaryTable(entities)}
                                              WHERE      {Q("Id")} = {Parameter(entities[0].Id)}
                                              """;

        var temporaryTableName = statement.TemporaryTables.First().Name;

        this.Connection.Exists(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeTrue();

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public void Exists_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(1);

        this.Connection.Exists(
                $"""
                 SELECT     1
                 FROM       {TemporaryTable(entities)}
                 WHERE      {Q("Id")} = {Parameter(entities[0].Id)}
                 """,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeTrue();
    }

    [Fact]
    public void Exists_InterpolatedParameter_ShouldPassInterpolatedParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        this.Connection.Exists(
                $"SELECT 1 FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeTrue();
    }

    [Fact]
    public void Exists_Parameter_ShouldPassParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        var statement = new InterpolatedSqlStatement(
            $"SELECT 1 FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entity.Id)
        );

        this.Connection.Exists(statement, cancellationToken: TestContext.Current.CancellationToken)
            .Should().BeTrue();
    }

    [Fact]
    public void Exists_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        InterpolatedSqlStatement statement =
            $"SELECT 1 FROM {TemporaryTable(entityIds)} WHERE {Q("Value")} = {Parameter(entityIds[0])}";

        var temporaryTableName = statement.TemporaryTables.First().Name;

        this.Connection.Exists(statement, cancellationToken: TestContext.Current.CancellationToken)
            .Should().BeTrue();

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public void Exists_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        this.Connection.Exists(
                $"SELECT 1 FROM {TemporaryTable(entityIds)} WHERE {Q("Value")} = {Parameter(entityIds[0])}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeTrue();
    }

    [Fact]
    public void Exists_ShouldReturnBooleanIndicatingWhetherQueryReturnedAtLeastOneRow()
    {
        var entity = this.CreateEntityInDb<Entity>();

        this.Connection.Exists(
                $"SELECT 1 FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeTrue();

        this.Connection.Exists(
                $"SELECT 1 FROM {Q("Entity")} WHERE {Q("Id")} = -1",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeFalse();
    }

    [Fact]
    public void Exists_Transaction_ShouldUseTransaction()
    {
        using (var transaction = this.Connection.BeginTransaction())
        {
            var entity = this.CreateEntityInDb<Entity>(transaction);

            this.Connection.Exists(
                    $"SELECT 1 FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
                    transaction,
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .Should().BeTrue();

            transaction.Rollback();
        }

        this.Connection.Exists(
                $"SELECT 1 FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        await Invoking(() => this.Connection.ExistsAsync("SELECT 1", cancellationToken: cancellationToken))
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Fact]
    public async Task ExistsAsync_CommandType_ShouldUseCommandType()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        this.CreateEntitiesInDb<Entity>(1);

        (await this.Connection.ExistsAsync(
                "GetFirstEntityId",
                commandType: CommandType.StoredProcedure,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(1);

        InterpolatedSqlStatement statement = $"""
                                              SELECT     1
                                              FROM       {TemporaryTable(entities)}
                                              WHERE      {Q("Id")} = {Parameter(entities[0].Id)}
                                              """;

        var temporaryTableName = statement.TemporaryTables.First().Name;

        (await this.Connection.ExistsAsync(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeTrue();

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public async Task
        ExistsAsync_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>(1);

        (await this.Connection.ExistsAsync(
                $"""
                 SELECT     1
                 FROM       {TemporaryTable(entities)}
                 WHERE      {Q("Id")} = {Parameter(entities[0].Id)}
                 """,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_InterpolatedParameter_ShouldPassInterpolatedParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        (await this.Connection.ExistsAsync(
                $"SELECT 1 FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_Parameter_ShouldPassParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        var statement = new InterpolatedSqlStatement(
            $"SELECT 1 FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entity.Id)
        );

        (await this.Connection.ExistsAsync(statement, cancellationToken: TestContext.Current.CancellationToken))
            .Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        InterpolatedSqlStatement statement =
            $"SELECT 1 FROM {TemporaryTable(entityIds)} WHERE {Q("Value")} = {Parameter(entityIds[0])}";

        var temporaryTableName = statement.TemporaryTables.First().Name;

        (await this.Connection.ExistsAsync(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeTrue();

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        (await this.Connection.ExistsAsync(
                $"SELECT 1 FROM {TemporaryTable(entityIds)} WHERE {Q("Value")} = {Parameter(entityIds[0])}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnBooleanIndicatingWhetherQueryReturnedAtLeastOneRow()
    {
        var entity = this.CreateEntityInDb<Entity>();

        (await this.Connection.ExistsAsync(
                $"SELECT 1 FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeTrue();

        (await this.Connection.ExistsAsync(
                $"SELECT 1 FROM {Q("Entity")} WHERE {Q("Id")} = -1",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_Transaction_ShouldUseTransaction()
    {
        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            var entity = this.CreateEntityInDb<Entity>(transaction);

            (await this.Connection.ExistsAsync(
                    $"SELECT 1 FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
                    transaction,
                    cancellationToken: TestContext.Current.CancellationToken
                ))
                .Should().BeTrue();

            await transaction.RollbackAsync();
        }

        (await this.Connection.ExistsAsync(
                $"SELECT 1 FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().BeFalse();
    }
}
