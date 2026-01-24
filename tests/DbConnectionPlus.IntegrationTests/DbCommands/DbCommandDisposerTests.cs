namespace RentADeveloper.DbConnectionPlus.IntegrationTests.DbCommands;

public sealed class
    DbCommandDisposerTests_MySql :
    DbCommandDisposerTests<MySqlTestDatabaseProvider>;

public sealed class
    DbCommandDisposerTests_Oracle :
    DbCommandDisposerTests<OracleTestDatabaseProvider>;

public sealed class
    DbCommandDisposerTests_PostgreSql :
    DbCommandDisposerTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    DbCommandDisposerTests_Sqlite :
    DbCommandDisposerTests<SqliteTestDatabaseProvider>;

public sealed class
    DbCommandDisposerTests_SqlServer :
    DbCommandDisposerTests<SqlServerTestDatabaseProvider>;

public abstract class DbCommandDisposerTests<TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    [Fact]
    public void Dispose_AlreadyDisposed_ShouldNotAttemptToDropTemporaryTablesAgain()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids();

        InterpolatedSqlStatement statement = $"SELECT {Q("Value")} FROM {TemporaryTable(entityIds)}";

        var temporaryTables = statement.TemporaryTables;

        var (_, commandDisposer) = DbCommandBuilder.BuildDbCommand(statement, this.DatabaseAdapter, this.Connection);

        this.ExistsTemporaryTableInDb(temporaryTables[0].Name)
            .Should().BeTrue();

        commandDisposer.Dispose();

        this.ExistsTemporaryTableInDb(temporaryTables[0].Name)
            .Should().BeFalse();

        Invoking(() => commandDisposer.Dispose())
            .Should().NotThrow();

        Invoking(() => commandDisposer.Dispose())
            .Should().NotThrow();

        Invoking(() => commandDisposer.Dispose())
            .Should().NotThrow();
    }

    [Fact]
    public void Dispose_ShouldDropTemporaryTables()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds1 = Generate.Ids();
        var entityIds2 = Generate.Ids();

        InterpolatedSqlStatement statement =
            $"""
             SELECT {Q("Value")} FROM {TemporaryTable(entityIds1)}
             UNION
             SELECT {Q("Value")} FROM {TemporaryTable(entityIds2)}
             """;

        var temporaryTables = statement.TemporaryTables;

        var (_, commandDisposer) = DbCommandBuilder.BuildDbCommand(statement, this.DatabaseAdapter, this.Connection);

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
    public async Task DisposeAsync_AlreadyDisposed_ShouldNotAttemptToDropTemporaryTablesAgain()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids();
        InterpolatedSqlStatement statement = $"SELECT {Q("Value")} FROM {TemporaryTable(entityIds)}";

        var temporaryTables = statement.TemporaryTables;

        var (_, commandDisposer) =
            await DbCommandBuilder.BuildDbCommandAsync(statement, this.DatabaseAdapter, this.Connection);

        this.ExistsTemporaryTableInDb(temporaryTables[0].Name)
            .Should().BeTrue();

        await commandDisposer.DisposeAsync();

        this.ExistsTemporaryTableInDb(temporaryTables[0].Name)
            .Should().BeFalse();

        await Invoking(() => commandDisposer.DisposeAsync().AsTask())
            .Should().NotThrowAsync();

        await Invoking(() => commandDisposer.DisposeAsync().AsTask())
            .Should().NotThrowAsync();

        await Invoking(() => commandDisposer.DisposeAsync().AsTask())
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task DisposeAsync_ShouldDisposeTemporaryTables()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds1 = Generate.Ids();
        var entityIds2 = Generate.Ids();

        InterpolatedSqlStatement statement =
            $"""
             SELECT {Q("Value")} FROM {TemporaryTable(entityIds1)}
             UNION
             SELECT {Q("Value")} FROM {TemporaryTable(entityIds2)}
             """;

        var temporaryTables = statement.TemporaryTables;

        var (_, commandDisposer) =
            await DbCommandBuilder.BuildDbCommandAsync(statement, this.DatabaseAdapter, this.Connection);

        this.ExistsTemporaryTableInDb(temporaryTables[0].Name)
            .Should().BeTrue();

        await commandDisposer.DisposeAsync();

        this.ExistsTemporaryTableInDb(temporaryTables[0].Name)
            .Should().BeFalse();
    }
}
