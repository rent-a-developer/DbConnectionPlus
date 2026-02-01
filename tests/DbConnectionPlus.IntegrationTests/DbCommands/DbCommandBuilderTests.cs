using System.Data.Common;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters;
using DbCommandBuilder = RentADeveloper.DbConnectionPlus.DbCommands.DbCommandBuilder;

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
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildDbCommand_ShouldCreateTemporaryTables(Boolean useAsyncApi)
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

        var (command, _) = await CallApi(useAsyncApi, statement, this.DatabaseAdapter, this.Connection);

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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildDbCommand_ShouldReturnDisposerForCommandWhichDisposesTemporaryTables(Boolean useAsyncApi)
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
            await CallApi(useAsyncApi, statement, this.DatabaseAdapter, this.Connection);

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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildDbCommand_ShouldSetCommandTimeout(Boolean useAsyncApi)
    {
        var timeout = Generate.Single<TimeSpan>();

        var (command, _) = await CallApi(
            useAsyncApi,
            "SELECT 1",
            this.DatabaseAdapter,
            this.Connection,
            null,
            timeout
        );

        command.CommandTimeout
            .Should().Be((Int32)timeout.TotalSeconds);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildDbCommand_ShouldSetCommandType(Boolean useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProcedures, "");

        var (command, _) = await CallApi(
            useAsyncApi,
            "GetEntities",
            this.DatabaseAdapter,
            this.Connection,
            commandType: CommandType.StoredProcedure
        );

        command.CommandType
            .Should().Be(CommandType.StoredProcedure);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildDbCommand_ShouldSetConnection(Boolean useAsyncApi)
    {
        var (command, _) =
            await CallApi(useAsyncApi, "SELECT 1", this.DatabaseAdapter, this.Connection);

        command.Connection
            .Should().BeSameAs(this.Connection);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildDbCommand_ShouldSetParameters(Boolean useAsyncApi)
    {
        var entityId = Generate.Id();
        var dateTimeValue = DateTime.UtcNow;
        var stringValue = Generate.Single<String>();

        var (command, _) = await CallApi(
            useAsyncApi,
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildDbCommand_ShouldSetTransaction(Boolean useAsyncApi)
    {
        await using var transaction = await this.Connection.BeginTransactionAsync();

        var (command, _) = await CallApi(
            useAsyncApi,
            "SELECT 1",
            this.DatabaseAdapter,
            this.Connection,
            transaction
        );

        command.Transaction
            .Should().BeSameAs(transaction);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildDbCommand_ShouldUseCancellationToken(Boolean useAsyncApi)
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        var (command, _) = await CallApi(
            useAsyncApi,
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

    private static Task<(DbCommand, DbCommandDisposer)> CallApi(
        Boolean useAsyncApi,
        InterpolatedSqlStatement statement,
        IDatabaseAdapter databaseAdapter,
        DbConnection connection,
        DbTransaction? transaction = null,
        TimeSpan? commandTimeout = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default
    )
    {
        if (useAsyncApi)
        {
            return DbCommandBuilder.BuildDbCommandAsync(
                statement,
                databaseAdapter,
                connection,
                transaction,
                commandTimeout,
                commandType,
                cancellationToken
            );
        }

        try
        {
            return Task.FromResult(
                DbCommandBuilder.BuildDbCommand(
                    statement,
                    databaseAdapter,
                    connection,
                    transaction,
                    commandTimeout,
                    commandType,
                    cancellationToken
                )
            );
        }
        catch (Exception ex)
        {
            return Task.FromException<(DbCommand, DbCommandDisposer)>(ex);
        }
    }
}
