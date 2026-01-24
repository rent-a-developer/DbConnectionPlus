namespace RentADeveloper.DbConnectionPlus.IntegrationTests.DbCommands;

public sealed class
    DbCommandBuilderTests_MySql :
    DbCommandBuilderTests<MySqlTestDatabaseProvider>;

public sealed class
    DbCommandBuilderTests_Oracle :
    DbCommandBuilderTests<OracleTestDatabaseProvider>;

public sealed class
    DbCommandBuilderTests_PostgreSql :
    DbCommandBuilderTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    DbCommandBuilderTests_Sqlite :
    DbCommandBuilderTests<SqliteTestDatabaseProvider>;

public sealed class
    DbCommandBuilderTests_SqlServer :
    DbCommandBuilderTests<SqlServerTestDatabaseProvider>;

public abstract class DbCommandBuilderTests<TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    [Fact]
    public void BuildDbCommand_ShouldCreateTemporaryTables()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids();
        var entities = Generate.Multiple<Entity>();

        InterpolatedSqlStatement statement = $"""
                                              SELECT     Value
                                              FROM       {TemporaryTable(entityIds)} AS Ids
                                              INNER JOIN {TemporaryTable(entities)} AS Entities
                                              ON         Entities.Id = Ids.Value
                                              """;

        var (command, _) = DbCommandBuilder.BuildDbCommand(statement, this.DatabaseAdapter, this.Connection);

        var temporaryTables = statement.TemporaryTables;

        command.CommandText
            .Should().Be(
                $"""
                 SELECT     Value
                 FROM       {QT(temporaryTables[0].Name)} AS Ids
                 INNER JOIN {QT(temporaryTables[1].Name)} AS Entities
                 ON         Entities.Id = Ids.Value
                 """
            );

        this.ExistsTemporaryTableInDb(temporaryTables[0].Name)
            .Should().BeTrue();

        this.ExistsTemporaryTableInDb(temporaryTables[1].Name)
            .Should().BeTrue();

        this.Connection.Query<Int64>($"SELECT {Q("Value")} FROM {QT(temporaryTables[0].Name)}")
            .Should().BeEquivalentTo(entityIds);

        this.Connection.Query<Entity>($"SELECT * FROM {QT(temporaryTables[1].Name)}")
            .Should().BeEquivalentTo(entities);
    }

    [Fact]
    public void BuildDbCommand_ShouldReturnDisposerForCommandWhichDisposesTemporaryTables()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids();
        var entities = Generate.Multiple<Entity>();

        InterpolatedSqlStatement statement = $"""
                                              SELECT     Value
                                              FROM       {TemporaryTable(entityIds)} AS Ids
                                              INNER JOIN {TemporaryTable(entities)} AS Entities
                                              ON         Entities.Id = Ids.Value
                                              """;

        var (_, commandDisposer) = DbCommandBuilder.BuildDbCommand(statement, this.DatabaseAdapter, this.Connection);

        var temporaryTables = statement.TemporaryTables;

        this.ExistsTemporaryTableInDb(temporaryTables[0].Name)
            .Should().BeTrue();

        this.ExistsTemporaryTableInDb(temporaryTables[1].Name)
            .Should().BeTrue();

        commandDisposer.Dispose();

        this.ExistsTemporaryTableInDb(temporaryTables[0].Name)
            .Should().BeFalse();

        this.ExistsTemporaryTableInDb(temporaryTables[1].Name)
            .Should().BeFalse();
    }

    [Fact]
    public void BuildDbCommand_ShouldSetCommandTimeout()
    {
        var timeout = Generate.Single<TimeSpan>();

        var (command, _) = DbCommandBuilder.BuildDbCommand(
            "SELECT 1",
            this.DatabaseAdapter,
            this.Connection,
            null,
            timeout
        );

        command.CommandTimeout
            .Should().Be((Int32)timeout.TotalSeconds);
    }

    [Fact]
    public void BuildDbCommand_ShouldSetCommandType()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProcedures, "");

        var (command, _) = DbCommandBuilder.BuildDbCommand(
            "GetEntities",
            this.DatabaseAdapter,
            this.Connection,
            commandType: CommandType.StoredProcedure
        );

        command.CommandType
            .Should().Be(CommandType.StoredProcedure);
    }

    [Fact]
    public void BuildDbCommand_ShouldSetConnection()
    {
        var (command, _) = DbCommandBuilder.BuildDbCommand("SELECT 1", this.DatabaseAdapter, this.Connection);

        command.Connection
            .Should().BeSameAs(this.Connection);
    }

    [Fact]
    public void BuildDbCommand_ShouldSetParameters()
    {
        var entityId = Generate.Id();
        var dateTimeValue = DateTime.UtcNow;
        var stringValue = Generate.Single<String>();

        var (command, _) = DbCommandBuilder.BuildDbCommand(
            $"""
             SELECT *
             FROM   Entity
             WHERE  Id = {Parameter(entityId)} AND
                    DateTimeValue = {Parameter(dateTimeValue)} AND
                    StringValue = {Parameter(stringValue)}
             """,
            this.DatabaseAdapter,
            this.Connection
        );

        command.CommandText
            .Should().Be(
                $"""
                 SELECT *
                 FROM   Entity
                 WHERE  Id = {P("EntityId")} AND
                        DateTimeValue = {P("DateTimeValue")} AND
                        StringValue = {P("StringValue")}
                 """
            );

        command.Parameters.Count
            .Should().Be(3);

        command.Parameters["EntityId"]
            .Value.Should().Be(entityId);

        command.Parameters["DateTimeValue"]
            .Value.Should().Be(dateTimeValue);

        command.Parameters["StringValue"]
            .Value.Should().Be(stringValue);
    }

    [Fact]
    public void BuildDbCommand_ShouldSetTransaction()
    {
        using var transaction = this.Connection.BeginTransaction();

        var (command, _) = DbCommandBuilder.BuildDbCommand(
            "SELECT 1",
            this.DatabaseAdapter,
            this.Connection,
            transaction
        );

        command.Transaction
            .Should().BeSameAs(transaction);
    }

    [Fact]
    public void BuildDbCommand_ShouldUseCancellationToken()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        var (command, _) = DbCommandBuilder.BuildDbCommand(
            this.TestDatabaseProvider.DelayTwoSecondsStatement,
            this.DatabaseAdapter,
            this.Connection,
            null,
            null,
            CommandType.Text,
            cancellationToken
        );

        var exception = Invoking(() => command.ExecuteNonQuery())
            .Should().Throw<Exception>().Subject.First();

        this.DatabaseAdapter.WasSqlStatementCancelledByCancellationToken(exception, cancellationToken)
            .Should().BeTrue();
    }

    [Fact]
    public async Task BuildDbCommandAsync_ShouldCreateTemporaryTables()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids();
        var entities = Generate.Multiple<Entity>();

        InterpolatedSqlStatement statement = $"""
                                              SELECT     Value
                                              FROM       {TemporaryTable(entityIds)} AS Ids
                                              INNER JOIN {TemporaryTable(entities)} AS Entities
                                              ON         Entities.Id = Ids.Value
                                              """;

        var (command, _) = await DbCommandBuilder.BuildDbCommandAsync(statement, this.DatabaseAdapter, this.Connection);

        var temporaryTables = statement.TemporaryTables;

        command.CommandText
            .Should().Be(
                $"""
                 SELECT     Value
                 FROM       {QT(temporaryTables[0].Name)} AS Ids
                 INNER JOIN {QT(temporaryTables[1].Name)} AS Entities
                 ON         Entities.Id = Ids.Value
                 """
            );

        this.ExistsTemporaryTableInDb(temporaryTables[0].Name)
            .Should().BeTrue();

        this.ExistsTemporaryTableInDb(temporaryTables[1].Name)
            .Should().BeTrue();

        (await this.Connection.QueryAsync<Int64>($"SELECT {Q("Value")} FROM {QT(temporaryTables[0].Name)}")
                .ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(entityIds);

        (await this.Connection.QueryAsync<Entity>($"SELECT * FROM {QT(temporaryTables[1].Name)}")
                .ToListAsync(TestContext.Current.CancellationToken))
            .Should().BeEquivalentTo(entities);
    }

    [Fact]
    public async Task BuildDbCommandAsync_ShouldReturnDisposerForCommandWhichDisposesTemporaryTables()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids();
        var entities = Generate.Multiple<Entity>();

        InterpolatedSqlStatement statement = $"""
                                              SELECT     Value
                                              FROM       {TemporaryTable(entityIds)} AS Ids
                                              INNER JOIN {TemporaryTable(entities)} AS Entities
                                              ON         Entities.Id = Ids.Value
                                              """;
        var (_, commandDisposer) =
            await DbCommandBuilder.BuildDbCommandAsync(statement, this.DatabaseAdapter, this.Connection);

        var temporaryTables = statement.TemporaryTables;

        var table1Name = temporaryTables[0].Name;
        var table2Name = temporaryTables[1].Name;

        this.ExistsTemporaryTableInDb(table1Name)
            .Should().BeTrue();

        this.ExistsTemporaryTableInDb(table2Name)
            .Should().BeTrue();

        await commandDisposer.DisposeAsync();

        this.ExistsTemporaryTableInDb(table1Name)
            .Should().BeFalse();

        this.ExistsTemporaryTableInDb(table2Name)
            .Should().BeFalse();
    }

    [Fact]
    public async Task BuildDbCommandAsync_ShouldSetCommandTimeout()
    {
        var timeout = Generate.Single<TimeSpan>();

        var (command, _) = await DbCommandBuilder.BuildDbCommandAsync(
            "SELECT 1",
            this.DatabaseAdapter,
            this.Connection,
            null,
            timeout
        );

        command.CommandTimeout
            .Should().Be((Int32)timeout.TotalSeconds);
    }

    [Fact]
    public async Task BuildDbCommandAsync_ShouldSetCommandType()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProcedures, "");

        var (command, _) = await DbCommandBuilder.BuildDbCommandAsync(
            "GetEntities",
            this.DatabaseAdapter,
            this.Connection,
            commandType: CommandType.StoredProcedure
        );

        command.CommandType
            .Should().Be(CommandType.StoredProcedure);
    }

    [Fact]
    public async Task BuildDbCommandAsync_ShouldSetConnection()
    {
        var (command, _) =
            await DbCommandBuilder.BuildDbCommandAsync("SELECT 1", this.DatabaseAdapter, this.Connection);

        command.Connection
            .Should().BeSameAs(this.Connection);
    }

    [Fact]
    public async Task BuildDbCommandAsync_ShouldSetParameters()
    {
        var entityId = Generate.Id();
        var dateTimeValue = DateTime.UtcNow;
        var stringValue = Generate.Single<String>();

        var (command, _) = await DbCommandBuilder.BuildDbCommandAsync(
            $"""
             SELECT *
             FROM   Entity
             WHERE  Id = {Parameter(entityId)} AND
                    DateTimeValue = {Parameter(dateTimeValue)} AND
                    StringValue = {Parameter(stringValue)}
             """,
            this.DatabaseAdapter,
            this.Connection
        );

        command.CommandText
            .Should().Be(
                $"""
                 SELECT *
                 FROM   Entity
                 WHERE  Id = {P("EntityId")} AND
                        DateTimeValue = {P("DateTimeValue")} AND
                        StringValue = {P("StringValue")}
                 """
            );

        command.Parameters.Count
            .Should().Be(3);

        command.Parameters["EntityId"].Value
            .Should().Be(entityId);

        command.Parameters["DateTimeValue"].Value
            .Should().Be(dateTimeValue);

        command.Parameters["StringValue"].Value
            .Should().Be(stringValue);
    }

    [Fact]
    public async Task BuildDbCommandAsync_ShouldSetTransaction()
    {
        await using var transaction = await this.Connection.BeginTransactionAsync();

        var (command, _) = await DbCommandBuilder.BuildDbCommandAsync(
            "SELECT 1",
            this.DatabaseAdapter,
            this.Connection,
            transaction
        );

        command.Transaction
            .Should().BeSameAs(transaction);
    }

    [Fact]
    public async Task BuildDbCommandAsync_ShouldUseCancellationToken()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        var (command, _) = await DbCommandBuilder.BuildDbCommandAsync(
            this.TestDatabaseProvider.DelayTwoSecondsStatement,
            this.DatabaseAdapter,
            this.Connection,
            null,
            null,
            CommandType.Text,
            cancellationToken
        );

        var exception = (await Invoking(() => command.ExecuteNonQueryAsync(cancellationToken))
            .Should().ThrowAsync<Exception>()).Subject.First();

        this.DatabaseAdapter.WasSqlStatementCancelledByCancellationToken(exception, cancellationToken)
            .Should().BeTrue();
    }
}
