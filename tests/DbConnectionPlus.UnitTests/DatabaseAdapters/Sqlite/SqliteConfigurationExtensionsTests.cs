using Microsoft.Data.Sqlite;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.Sqlite;

namespace RentADeveloper.DbConnectionPlus.UnitTests.DatabaseAdapters.Sqlite;

public class SqliteConfigurationExtensionsTests : UnitTestsBase
{
    [Fact]
    public void ShouldGuardAgainstNullArguments() =>
        ArgumentNullGuardVerifier.Verify(() => SqliteConfigurationExtensions.UseSqlite(new()));

    [Fact]
    public void UseSqlite_ShouldRegisterSqliteAdapter()
    {
        var configuration = new DbConnectionPlusConfiguration();

        var result = configuration.UseSqlite();

        result.Should().BeSameAs(configuration);

        var adapter = configuration.GetDatabaseAdapter(typeof(SqliteConnection));
        adapter.Should().NotBeNull();
        adapter.Should().BeOfType<SqliteDatabaseAdapter>();
    }
}
