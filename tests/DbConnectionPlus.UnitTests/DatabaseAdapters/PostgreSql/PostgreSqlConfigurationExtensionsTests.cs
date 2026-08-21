using Npgsql;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.PostgreSql;

namespace RentADeveloper.DbConnectionPlus.UnitTests.DatabaseAdapters.PostgreSql;

public class PostgreSqlConfigurationExtensionsTests : UnitTestsBase
{
    [Fact]
    public void ShouldGuardAgainstNullArguments() =>
        ArgumentNullGuardVerifier.Verify(() => PostgreSqlConfigurationExtensions.UsePostgreSql(new()));

    [Fact]
    public void UsePostgreSql_ShouldRegisterPostgreSqlAdapter()
    {
        var configuration = new DbConnectionPlusConfiguration();

        var result = configuration.UsePostgreSql();

        result.Should().BeSameAs(configuration);

        var adapter = configuration.GetDatabaseAdapter(typeof(NpgsqlConnection));
        adapter.Should().NotBeNull();
        adapter.Should().BeOfType<PostgreSqlDatabaseAdapter>();
    }
}