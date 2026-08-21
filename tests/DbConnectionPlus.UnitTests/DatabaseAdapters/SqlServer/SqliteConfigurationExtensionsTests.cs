using RentADeveloper.DbConnectionPlus.DatabaseAdapters.SqlServer;

namespace RentADeveloper.DbConnectionPlus.UnitTests.DatabaseAdapters.SqlServer;

public class SqlServerConfigurationExtensionsTests : UnitTestsBase
{
    [Fact]
    public void ShouldGuardAgainstNullArguments() =>
        ArgumentNullGuardVerifier.Verify(() => SqlServerConfigurationExtensions.UseSqlServer(new()));

    [Fact]
    public void UseSqlServer_ShouldRegisterSqlServerAdapter()
    {
        var configuration = new DbConnectionPlusConfiguration();

        var result = configuration.UseSqlServer();

        result.Should().BeSameAs(configuration);

        var adapter = configuration.GetDatabaseAdapter(typeof(SqlConnection));
        adapter.Should().NotBeNull();
        adapter.Should().BeOfType<SqlServerDatabaseAdapter>();
    }
}