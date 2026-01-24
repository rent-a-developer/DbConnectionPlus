using RentADeveloper.DbConnectionPlus.DatabaseAdapters.MySql;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.DatabaseAdapters.MySql;

public class MySqlDatabaseAdapterTests : IntegrationTestsBase<MySqlTestDatabaseProvider>
{
    [Fact]
    public void SupportsTemporaryTables_ShouldReturnTrue()
    {
        var adapter = new MySqlDatabaseAdapter();

        adapter.SupportsTemporaryTables(this.Connection)
            .Should().BeTrue();
    }
}
