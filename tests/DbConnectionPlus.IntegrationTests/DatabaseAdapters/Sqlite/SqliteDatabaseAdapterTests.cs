using RentADeveloper.DbConnectionPlus.DatabaseAdapters.Sqlite;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.DatabaseAdapters.Sqlite;

public class SqliteDatabaseAdapterTests : IntegrationTestsBase<SqliteTestDatabaseProvider>
{
    [Fact]
    public void SupportsTemporaryTables_ShouldReturnTrue()
    {
        var adapter = new SqliteDatabaseAdapter();

        adapter.SupportsTemporaryTables(this.Connection)
            .Should().BeTrue();
    }
}
