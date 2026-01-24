using RentADeveloper.DbConnectionPlus.DbCommands;

namespace RentADeveloper.DbConnectionPlus.UnitTests.DbCommands;

public class DbCommandHelperTests : UnitTestsBase
{
    [Fact]
    public void RegisterDbCommandCancellation_CancellationToken_ShouldRegister()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var registration = DbCommandHelper.RegisterDbCommandCancellation(this.MockDbCommand, cancellationToken);

        registration
            .Should().NotBe(default(CancellationTokenRegistration));

        registration.Token
            .Should().Be(cancellationToken);
    }

    [Fact]
    public void RegisterDbCommandCancellation_NoneCancellationToken_ShouldNotRegister()
    {
        var registration = DbCommandHelper.RegisterDbCommandCancellation(this.MockDbCommand, CancellationToken.None);

        registration
            .Should().Be(default(CancellationTokenRegistration));

        registration.Token
            .Should().Be(CancellationToken.None);
    }

    [Fact]
    public void ShouldGuardAgainstNullArguments() =>
        ArgumentNullGuardVerifier.Verify(() =>
            DbCommandHelper.RegisterDbCommandCancellation(this.MockDbCommand, CancellationToken.None)
        );
}
