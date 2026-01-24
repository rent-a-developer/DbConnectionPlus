namespace RentADeveloper.DbConnectionPlus.IntegrationTests.DbCommands;

public sealed class
    DbCommandHelperTests_MySql :
    DbCommandHelperTests<MySqlTestDatabaseProvider>;

public sealed class
    DbCommandHelperTests_Oracle :
    DbCommandHelperTests<OracleTestDatabaseProvider>;

public sealed class
    DbCommandHelperTests_PostgreSql :
    DbCommandHelperTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    DbCommandHelperTests_Sqlite :
    DbCommandHelperTests<SqliteTestDatabaseProvider>;

public sealed class
    DbCommandHelperTests_SqlServer :
    DbCommandHelperTests<SqlServerTestDatabaseProvider>;

public abstract class DbCommandHelperTests<TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    [Fact]
    public void RegisterDbCommandCancellation_CancellationToken_ShouldRegister()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var entity = this.CreateEntityInDb<Entity>();

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        var command = this.Connection.CreateCommand();

        command.CommandText =
            this.TestDatabaseProvider.DelayTwoSecondsStatement + $"DELETE FROM {Q("Entity")}";

        using var cancellationTokenRegistration =
            DbCommandHelper.RegisterDbCommandCancellation(command, cancellationToken);

        Invoking(() => command.ExecuteNonQuery())
            .Should().Throw<Exception>();

        this.ExistsEntityInDb(entity)
            .Should().BeTrue();
    }

    [Fact]
    public void RegisterDbCommandCancellation_NoneCancellationToken_ShouldNotRegister()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var entity = this.CreateEntityInDb<Entity>();

        var command = this.Connection.CreateCommand();

        command.CommandText =
            this.TestDatabaseProvider.DelayTwoSecondsStatement + $"DELETE FROM {Q("Entity")}";

        using var cancellationTokenRegistration =
            DbCommandHelper.RegisterDbCommandCancellation(command, CancellationToken.None);

        command.ExecuteNonQuery()
            .Should().Be(1);

        this.ExistsEntityInDb(entity)
            .Should().BeFalse();
    }
}
