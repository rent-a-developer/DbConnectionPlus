using MySqlConnector;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.MySql;


namespace RentADeveloper.DbConnectionPlus.UnitTests.DatabaseAdapters.MySql;

public class MySqlConfigurationExtensionsTests : UnitTestsBase
{
    [Fact]
    public void ShouldGuardAgainstNullArguments() =>
        ArgumentNullGuardVerifier.Verify(() => MySqlConfigurationExtensions.UseMySql(new()));

    [Fact]
    public void UseMySql_ShouldRegisterMySqlAdapter()
    {
        var configuration = new DbConnectionPlusConfiguration();

        var result = configuration.UseMySql();

        result.Should().BeSameAs(configuration);

        var adapter = configuration.GetDatabaseAdapter(typeof(MySqlConnection));
        adapter.Should().NotBeNull();
        adapter.Should().BeOfType<MySqlDatabaseAdapter>();
    }
}