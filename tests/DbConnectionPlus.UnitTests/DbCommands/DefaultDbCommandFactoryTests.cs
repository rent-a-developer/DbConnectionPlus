using RentADeveloper.DbConnectionPlus.DbCommands;

namespace RentADeveloper.DbConnectionPlus.UnitTests.DbCommands;

public class DefaultDbCommandFactoryTests : UnitTestsBase
{
    [Fact]
    public void CreateDbCommand_NoTimeout_ShouldUseDefaultTimeout()
    {
        var command = this.factory.CreateDbCommand(this.MockDbConnection, "SELECT 1");

        command.CommandTimeout
            .Should().Be(0);
    }

    [Fact]
    public void CreateDbCommand_ShouldCreateDbCommandWithSpecifiedSettings()
    {
        using var transaction = this.MockDbConnection.BeginTransaction();

        var timeout = Generate.Single<TimeSpan>();

        var command = this.factory.CreateDbCommand(
            this.MockDbConnection,
            "SELECT 1",
            transaction,
            timeout,
            CommandType.StoredProcedure
        );

        command.Connection
            .Should().BeSameAs(this.MockDbConnection);

        command.CommandText
            .Should().Be("SELECT 1");

        command.Transaction
            .Should().BeSameAs(transaction);

        command.CommandType
            .Should().Be(CommandType.StoredProcedure);

        command.CommandTimeout
            .Should().Be((Int32)timeout.TotalSeconds);
    }

    [Fact]
    public void ShouldGuardAgainstNullArguments() =>
        ArgumentNullGuardVerifier.Verify(() =>
            this.factory.CreateDbCommand(this.MockDbConnection, "SELECT 1")
        );

    private readonly DefaultDbCommandFactory factory = new();
}
