namespace RentADeveloper.DbConnectionPlus.IntegrationTests.DbCommands;

public sealed class
    DefaultDbCommandFactoryTests_MySql :
    DefaultDbCommandFactoryTests<MySqlTestDatabaseProvider>;

public sealed class
    DefaultDbCommandFactoryTests_Oracle :
    DefaultDbCommandFactoryTests<OracleTestDatabaseProvider>;

public sealed class
    DefaultDbCommandFactoryTests_PostgreSql :
    DefaultDbCommandFactoryTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    DefaultDbCommandFactoryTests_Sqlite :
    DefaultDbCommandFactoryTests<SqliteTestDatabaseProvider>;

public sealed class
    DefaultDbCommandFactoryTests_SqlServer :
    DefaultDbCommandFactoryTests<SqlServerTestDatabaseProvider>;

public abstract class DefaultDbCommandFactoryTests<TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    [Fact]
    public void CreateDbCommand_NoTimeout_ShouldUseDefaultTimeout()
    {
        var command = this.factory.CreateDbCommand(this.Connection, "SELECT 1");

        command.CommandTimeout
            .Should().Be(this.Connection.CreateCommand().CommandTimeout);
    }

    [Fact]
    public void CreateDbCommand_ShouldCreateDbCommandWithSpecifiedSettings()
    {
        var commandType = this.TestDatabaseProvider.SupportsStoredProcedures
            ? CommandType.StoredProcedure
            : CommandType.Text;

        using var transaction = this.Connection.BeginTransaction();

        var timeout = Generate.Single<TimeSpan>();

        var command = this.factory.CreateDbCommand(
            this.Connection,
            "SELECT 1",
            transaction,
            timeout,
            commandType
        );

        command.Connection
            .Should().BeSameAs(this.Connection);

        command.CommandText
            .Should().Be("SELECT 1");

        command.Transaction
            .Should().BeSameAs(transaction);

        command.CommandTimeout
            .Should().Be((Int32)timeout.TotalSeconds);

        command.CommandType
            .Should().Be(commandType);
    }

    private readonly DefaultDbCommandFactory factory = new();
}
