using Oracle.ManagedDataAccess.Client;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.Oracle;

namespace RentADeveloper.DbConnectionPlus.UnitTests.DatabaseAdapters.Oracle;

public class OracleConfigurationExtensionsTests : UnitTestsBase
{
    [Fact]
    public void ShouldGuardAgainstNullArguments() =>
        ArgumentNullGuardVerifier.Verify(() => OracleConfigurationExtensions.UseOracle(new()));

    [Fact]
    public void UseOracle_ShouldRegisterOracleAdapter()
    {
        var configuration = new DbConnectionPlusConfiguration();

        var result = configuration.UseOracle();

        result.Should().BeSameAs(configuration);

        var adapter = configuration.GetDatabaseAdapter(typeof(OracleConnection));
        adapter.Should().NotBeNull();
        adapter.Should().BeOfType<OracleDatabaseAdapter>();
    }
}