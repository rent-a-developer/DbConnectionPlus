namespace RentADeveloper.DbConnectionPlus.IntegrationTests;

public sealed class
    DbConnectionExtensions_ExecuteNonQueryTests_MySql :
    DbConnectionExtensions_ExecuteNonQueryTests<MySqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ExecuteNonQueryTests_Oracle :
    DbConnectionExtensions_ExecuteNonQueryTests<OracleTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ExecuteNonQueryTests_PostgreSql :
    DbConnectionExtensions_ExecuteNonQueryTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ExecuteNonQueryTests_Sqlite :
    DbConnectionExtensions_ExecuteNonQueryTests<SqliteTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ExecuteNonQueryTests_SqlServer :
    DbConnectionExtensions_ExecuteNonQueryTests<SqlServerTestDatabaseProvider>;

public abstract class
    DbConnectionExtensions_ExecuteNonQueryTests<TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    [Fact]
    public void ExecuteNonQuery_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var entity = this.CreateEntityInDb<Entity>();

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        Invoking(() =>
                this.Connection.ExecuteNonQuery(
                    $"DELETE FROM {Q("Entity")}",
                    cancellationToken: cancellationToken
                )
            )
            .Should().Throw<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);

        // Since the operation was cancelled, the entity should still exist.
        this.ExistsEntityInDb(entity)
            .Should().BeTrue();
    }

    [Fact]
    public void ExecuteNonQuery_CommandType_ShouldPassUseCommandType()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProcedures, "");

        var entity = this.CreateEntityInDb<Entity>();

        this.Connection.ExecuteNonQuery(
            Q("DeleteAllEntities"),
            commandType: CommandType.StoredProcedure,
            cancellationToken: TestContext.Current.CancellationToken
        );

        this.ExistsEntityInDb(entity)
            .Should().BeFalse();
    }

    [Fact]
    public void ExecuteNonQuery_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = this.CreateEntitiesInDb<Entity>(5);
        var entitiesToDelete = entities.Take(2).ToList();

        InterpolatedSqlStatement statement =
            $"""
             DELETE FROM   {Q("Entity")}
             WHERE         EXISTS (
                               SELECT  1
                               FROM    {TemporaryTable(entitiesToDelete)} TEntitiesToDelete
                               WHERE   {Q("Entity")}.{Q("Id")} = TEntitiesToDelete.{Q("Id")} AND
                                       {Q("Entity")}.{Q("StringValue")} = TEntitiesToDelete.{Q("StringValue")} AND
                                       {Q("Entity")}.{Q("Int32Value")} = TEntitiesToDelete.{Q("Int32Value")}
                           )
             """;

        var temporaryTableName = statement.TemporaryTables.First().Name;

        this.Connection.ExecuteNonQuery(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entitiesToDelete.Count);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public void ExecuteNonQuery_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = this.CreateEntitiesInDb<Entity>(5);
        var entitiesToDelete = entities.Take(2).ToList();

        this.Connection.ExecuteNonQuery(
                $"""
                 DELETE FROM   {Q("Entity")}
                 WHERE         EXISTS (
                                   SELECT  1
                                   FROM    {TemporaryTable(entitiesToDelete)} TEntitiesToDelete
                                   WHERE   {Q("Entity")}.{Q("Id")} = TEntitiesToDelete.{Q("Id")} AND
                                           {Q("Entity")}.{Q("StringValue")} = TEntitiesToDelete.{Q("StringValue")} AND
                                           {Q("Entity")}.{Q("Int32Value")} = TEntitiesToDelete.{Q("Int32Value")}
                               )
                 """,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(entitiesToDelete.Count);

        foreach (var entity in entitiesToDelete)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeFalse();
        }

        foreach (var entity in entities.Except(entitiesToDelete))
        {
            this.ExistsEntityInDb(entity)
                .Should().BeTrue();
        }
    }

    [Fact]
    public void ExecuteNonQuery_InterpolatedParameter_ShouldPassInterpolatedParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        this.Connection.ExecuteNonQuery(
            $"DELETE FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        this.ExistsEntityInDb(entity)
            .Should().BeFalse();
    }

    [Fact]
    public void ExecuteNonQuery_Parameter_ShouldPassParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        var statement = new InterpolatedSqlStatement(
            $"DELETE FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entity.Id)
        );

        this.Connection.ExecuteNonQuery(statement, cancellationToken: TestContext.Current.CancellationToken);

        this.ExistsEntityInDb(entity)
            .Should().BeFalse();
    }

    [Fact]
    public void ExecuteNonQuery_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = this.CreateEntitiesInDb<Entity>(5);
        var entitiesToDelete = entities.Take(2).ToList();
        var idsOfEntitiesToDelete = entitiesToDelete.Select(a => a.Id).ToList();

        InterpolatedSqlStatement statement =
            $"""
             DELETE FROM    {Q("Entity")}
             WHERE          {Q("Id")} IN (SELECT {Q("Value")} FROM {TemporaryTable(idsOfEntitiesToDelete)})
             """;

        var temporaryTableName = statement.TemporaryTables.First().Name;

        this.Connection.ExecuteNonQuery(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(idsOfEntitiesToDelete.Count);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public void ExecuteNonQuery_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = this.CreateEntitiesInDb<Entity>(5);
        var entitiesToDelete = entities.Take(2).ToList();
        var idsOfEntitiesToDelete = entitiesToDelete.Select(a => a.Id).ToList();

        this.Connection.ExecuteNonQuery(
            $"""
             DELETE FROM    {Q("Entity")}
             WHERE          {Q("Id")} IN (SELECT {Q("Value")} FROM {TemporaryTable(idsOfEntitiesToDelete)})
             """,
            cancellationToken: TestContext.Current.CancellationToken
        );

        foreach (var entity in entitiesToDelete)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeFalse();
        }

        foreach (var entity in entities.Except(entitiesToDelete))
        {
            this.ExistsEntityInDb(entity)
                .Should().BeTrue();
        }
    }

    [Fact]
    public void ExecuteNonQuery_ShouldReturnNumberOfAffectedRows()
    {
        var entity = this.CreateEntityInDb<Entity>();

        this.Connection.ExecuteNonQuery(
                $"DELETE FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(1);

        this.Connection.ExecuteNonQuery(
                $"DELETE FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(0);
    }

    [Fact]
    public void ExecuteNonQuery_Transaction_ShouldUseTransaction()
    {
        var entity = this.CreateEntityInDb<Entity>();

        using (var transaction = this.Connection.BeginTransaction())
        {
            this.Connection.ExecuteNonQuery(
                $"DELETE FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
                transaction,
                cancellationToken: TestContext.Current.CancellationToken
            );

            this.ExistsEntityInDb(entity, transaction)
                .Should().BeFalse();

            transaction.Rollback();
        }

        this.ExistsEntityInDb(entity)
            .Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteNonQueryAsync_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var entity = this.CreateEntityInDb<Entity>();

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        await Invoking(() =>
                this.Connection.ExecuteNonQueryAsync(
                    $"DELETE FROM {Q("Entity")}",
                    cancellationToken: cancellationToken
                )
            )
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);

        // Since the operation was cancelled, the entity should still exist.
        this.ExistsEntityInDb(entity)
            .Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteNonQueryAsync_CommandType_ShouldPassUseCommandType()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProcedures, "");

        var entity = this.CreateEntityInDb<Entity>();

        await this.Connection.ExecuteNonQueryAsync(
            Q("DeleteAllEntities"),
            commandType: CommandType.StoredProcedure,
            cancellationToken: TestContext.Current.CancellationToken
        );

        this.ExistsEntityInDb(entity)
            .Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteNonQueryAsync_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = this.CreateEntitiesInDb<Entity>(5);
        var entitiesToDelete = entities.Take(2).ToList();

        InterpolatedSqlStatement statement =
            $"""
             DELETE FROM   {Q("Entity")}
             WHERE         EXISTS (
                               SELECT  1
                               FROM    {TemporaryTable(entitiesToDelete)} TEntitiesToDelete
                               WHERE   {Q("Entity")}.{Q("Id")} = TEntitiesToDelete.{Q("Id")} AND
                                       {Q("Entity")}.{Q("StringValue")} = TEntitiesToDelete.{Q("StringValue")} AND
                                       {Q("Entity")}.{Q("Int32Value")} = TEntitiesToDelete.{Q("Int32Value")}
                           )
             """;

        var temporaryTableName = statement.TemporaryTables.First().Name;

        (await this.Connection.ExecuteNonQueryAsync(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(entitiesToDelete.Count);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public async Task
        ExecuteNonQueryAsync_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = this.CreateEntitiesInDb<Entity>(5);
        var entitiesToDelete = entities.Take(2).ToList();

        await this.Connection.ExecuteNonQueryAsync(
            $"""
             DELETE FROM   {Q("Entity")}
             WHERE         EXISTS (
                               SELECT  1
                               FROM    {TemporaryTable(entitiesToDelete)} TEntitiesToDelete
                               WHERE   {Q("Entity")}.{Q("Id")} = TEntitiesToDelete.{Q("Id")} AND
                                       {Q("Entity")}.{Q("StringValue")} = TEntitiesToDelete.{Q("StringValue")} AND
                                       {Q("Entity")}.{Q("Int32Value")} = TEntitiesToDelete.{Q("Int32Value")}
                           )
             """,
            cancellationToken: TestContext.Current.CancellationToken
        );

        foreach (var entity in entitiesToDelete)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeFalse();
        }

        foreach (var entity in entities.Except(entitiesToDelete))
        {
            this.ExistsEntityInDb(entity)
                .Should().BeTrue();
        }
    }

    [Fact]
    public async Task ExecuteNonQueryAsync_InterpolatedParameter_ShouldPassInterpolatedParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        await this.Connection.ExecuteNonQueryAsync(
            $"DELETE FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        this.ExistsEntityInDb(entity)
            .Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteNonQueryAsync_Parameter_ShouldPassParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        var statement = new InterpolatedSqlStatement(
            $"DELETE FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entity.Id)
        );

        await this.Connection.ExecuteNonQueryAsync(statement, cancellationToken: TestContext.Current.CancellationToken);

        this.ExistsEntityInDb(entity)
            .Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteNonQueryAsync_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterExecution()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = this.CreateEntitiesInDb<Entity>(5);
        var entitiesToDelete = entities.Take(2).ToList();
        var idsOfEntitiesToDelete = entitiesToDelete.Select(a => a.Id).ToList();

        InterpolatedSqlStatement statement =
            $"""
             DELETE FROM    {Q("Entity")}
             WHERE          {Q("Id")} IN (SELECT {Q("Value")} FROM {TemporaryTable(idsOfEntitiesToDelete)})
             """;

        var temporaryTableName = statement.TemporaryTables.First().Name;

        (await this.Connection.ExecuteNonQueryAsync(
                statement,
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(idsOfEntitiesToDelete.Count);

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public async Task
        ExecuteNonQueryAsync_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = this.CreateEntitiesInDb<Entity>(5);
        var entitiesToDelete = entities.Take(2).ToList();
        var idsOfEntitiesToDelete = entitiesToDelete.Select(a => a.Id).ToList();

        await this.Connection.ExecuteNonQueryAsync(
            $"""
             DELETE FROM    {Q("Entity")}
             WHERE          {Q("Id")} IN (SELECT {Q("Value")} FROM {TemporaryTable(idsOfEntitiesToDelete)})
             """,
            cancellationToken: TestContext.Current.CancellationToken
        );

        foreach (var entity in entitiesToDelete)
        {
            this.ExistsEntityInDb(entity)
                .Should().BeFalse();
        }

        foreach (var entity in entities.Except(entitiesToDelete))
        {
            this.ExistsEntityInDb(entity)
                .Should().BeTrue();
        }
    }

    [Fact]
    public async Task ExecuteNonQueryAsync_ShouldReturnNumberOfAffectedRows()
    {
        var entity = this.CreateEntityInDb<Entity>();

        (await this.Connection.ExecuteNonQueryAsync(
                $"DELETE FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(1);

        (await this.Connection.ExecuteNonQueryAsync(
                $"DELETE FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
                cancellationToken: TestContext.Current.CancellationToken
            ))
            .Should().Be(0);
    }

    [Fact]
    public async Task ExecuteNonQueryAsync_Transaction_ShouldUseTransaction()
    {
        var entity = this.CreateEntityInDb<Entity>();

        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            await this.Connection.ExecuteNonQueryAsync(
                $"DELETE FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
                transaction,
                cancellationToken: TestContext.Current.CancellationToken
            );

            this.ExistsEntityInDb(entity, transaction)
                .Should().BeFalse();

            await transaction.RollbackAsync();
        }

        this.ExistsEntityInDb(entity)
            .Should().BeTrue();
    }
}
