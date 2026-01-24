using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.MySql;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.Oracle;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.PostgreSql;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.Sqlite;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.SqlServer;

namespace RentADeveloper.DbConnectionPlus.UnitTests.DatabaseAdapters;

public class DatabaseAdapterRegistryTests
{
    [Fact]
    public void GetAdapter_NoAdapterRegisteredForConnectionType_ShouldThrow() =>
        Invoking(() => DatabaseAdapterRegistry.GetAdapter(typeof(FakeConnectionC)))
            .Should().Throw<InvalidOperationException>()
            .WithMessage(
                $"No database adapter is registered for the database connection of the type " +
                $"{typeof(FakeConnectionC)}. Please call " +
                $"{nameof(DatabaseAdapterRegistry)}.{nameof(DatabaseAdapterRegistry.RegisterAdapter)} to register an " +
                $"adapter for that connection type."
            );

    [Fact]
    public void GetAdapter_ShouldGetAdapter()
    {
        var adapterA = Substitute.For<IDatabaseAdapter>();
        var adapterB = Substitute.For<IDatabaseAdapter>();

        DatabaseAdapterRegistry.RegisterAdapter<FakeConnectionA>(adapterA);
        DatabaseAdapterRegistry.RegisterAdapter<FakeConnectionB>(adapterB);

        DatabaseAdapterRegistry.GetAdapter(typeof(FakeConnectionA))
            .Should().BeSameAs(adapterA);

        DatabaseAdapterRegistry.GetAdapter(typeof(FakeConnectionB))
            .Should().BeSameAs(adapterB);
    }

    [Fact]
    public void GetAdapter_ShouldGetDefaultAdapters()
    {
        DatabaseAdapterRegistry.GetAdapter(typeof(MySqlConnection))
            .Should().BeOfType<MySqlDatabaseAdapter>();

        DatabaseAdapterRegistry.GetAdapter(typeof(OracleConnection))
            .Should().BeOfType<OracleDatabaseAdapter>();

        DatabaseAdapterRegistry.GetAdapter(typeof(NpgsqlConnection))
            .Should().BeOfType<PostgreSqlDatabaseAdapter>();

        DatabaseAdapterRegistry.GetAdapter(typeof(SqliteConnection))
            .Should().BeOfType<SqliteDatabaseAdapter>();

        DatabaseAdapterRegistry.GetAdapter(typeof(SqlConnection))
            .Should().BeOfType<SqlServerDatabaseAdapter>();
    }

    [Fact]
    public void RegisterAdapter_ShouldRegisterAdapter()
    {
        var adapterA = Substitute.For<IDatabaseAdapter>();

        DatabaseAdapterRegistry.RegisterAdapter<FakeConnectionA>(adapterA);

        DatabaseAdapterRegistry.GetAdapter(typeof(FakeConnectionA))
            .Should().BeSameAs(adapterA);

        var adapterB = Substitute.For<IDatabaseAdapter>();

        DatabaseAdapterRegistry.RegisterAdapter<FakeConnectionB>(adapterB);

        DatabaseAdapterRegistry.GetAdapter(typeof(FakeConnectionB))
            .Should().BeSameAs(adapterB);
    }

    [Fact]
    public void RegisterAdapter_ShouldReplaceRegisteredAdapter()
    {
        var adapterA = Substitute.For<IDatabaseAdapter>();

        DatabaseAdapterRegistry.RegisterAdapter<FakeConnectionA>(adapterA);

        DatabaseAdapterRegistry.GetAdapter(typeof(FakeConnectionA))
            .Should().BeSameAs(adapterA);

        var adapterB = Substitute.For<IDatabaseAdapter>();

        DatabaseAdapterRegistry.RegisterAdapter<FakeConnectionA>(adapterB);

        DatabaseAdapterRegistry.GetAdapter(typeof(FakeConnectionA))
            .Should().BeSameAs(adapterB);
    }

    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        ArgumentNullGuardVerifier.Verify(() =>
            DatabaseAdapterRegistry.GetAdapter(typeof(SqlConnection))
        );

        ArgumentNullGuardVerifier.Verify(() =>
            DatabaseAdapterRegistry.RegisterAdapter<SqlConnection>(new SqlServerDatabaseAdapter())
        );
    }
}
