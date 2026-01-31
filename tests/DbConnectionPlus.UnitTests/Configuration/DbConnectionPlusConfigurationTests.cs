using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using NSubstitute.DbConnection;
using Oracle.ManagedDataAccess.Client;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.MySql;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.Oracle;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.PostgreSql;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.Sqlite;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.SqlServer;
using RentADeveloper.DbConnectionPlus.SqlStatements;

namespace RentADeveloper.DbConnectionPlus.UnitTests.Configuration;

public class DbConnectionPlusConfigurationTests : UnitTestsBase
{
    [Fact]
    public void EnumSerializationMode_Integers_ShouldSerializeEnumAsInteger()
    {
        var enumValue = Generate.Single<TestEnum>();

        this.MockDbConnection.SetupQuery(_ => true).Returns(new { Id = 1 });

        DbParameter? interceptedDbParameter = null;

        DbConnectionPlusConfiguration.Instance.InterceptDbCommand =
            (command, _) => interceptedDbParameter = command.Parameters[0];

        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Integers;

        this.MockDbConnection.ExecuteNonQuery($"SELECT {Parameter(enumValue)}");

        interceptedDbParameter
            .Should().NotBeNull();

        interceptedDbParameter.Value
            .Should().Be((Int32)enumValue);
    }

    [Fact]
    public void EnumSerializationMode_Strings_ShouldSerializeEnumAsString()
    {
        var enumValue = Generate.Single<TestEnum>();

        this.MockDbConnection.SetupQuery(_ => true).Returns(new { Id = 1 });

        DbParameter? interceptedDbParameter = null;

        DbConnectionPlusConfiguration.Instance.InterceptDbCommand =
            (command, _) => interceptedDbParameter = command.Parameters[0];

        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        this.MockDbConnection.ExecuteNonQuery($"SELECT {Parameter(enumValue)}");

        interceptedDbParameter
            .Should().NotBeNull();

        interceptedDbParameter.Value
            .Should().Be(enumValue.ToString());
    }

    [Fact]
    public void Freeze_ShouldFreezeConfigurationAndEntityTypeBuilders()
    {
        var configuration = new DbConnectionPlusConfiguration();

        var entityTypeBuilder = configuration.Entity<Entity>();

        entityTypeBuilder.ToTable("Entities");

        var entityPropertyBuilder = entityTypeBuilder.Property(a => a.Id);

        entityPropertyBuilder.IsKey();

        ((IFreezable)configuration).Freeze();

        Invoking(() => configuration.EnumSerializationMode = EnumSerializationMode.Integers)
            .Should().Throw<InvalidOperationException>()
            .WithMessage("The configuration of DbConnectionPlus is frozen and can no longer be modified.");

        Invoking(() => configuration.InterceptDbCommand = null)
            .Should().Throw<InvalidOperationException>()
            .WithMessage("The configuration of DbConnectionPlus is frozen and can no longer be modified.");

        Invoking(() => configuration.Entity<Entity>())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("The configuration of DbConnectionPlus is frozen and can no longer be modified.");

        Invoking(() => entityTypeBuilder.ToTable("Entities"))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("The configuration of DbConnectionPlus is frozen and can no longer be modified.");

        Invoking(() => entityTypeBuilder.Property(a => a.Id))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("The configuration of DbConnectionPlus is frozen and can no longer be modified.");

        Invoking(() => entityPropertyBuilder.IsKey())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("The configuration of DbConnectionPlus is frozen and can no longer be modified.");
    }

    [Fact]
    public void GetDatabaseAdapter_NoAdapterRegisteredForConnectionType_ShouldThrow() =>
        Invoking(() => DbConnectionPlusConfiguration.Instance.GetDatabaseAdapter(typeof(FakeConnectionC)))
            .Should().Throw<InvalidOperationException>()
            .WithMessage(
                "No database adapter is registered for the database connection of the type " +
                $"{typeof(FakeConnectionC)}. Please call {nameof(DbConnectionExtensions)}." +
                $"{nameof(DbConnectionExtensions.Configure)} to register an adapter for that connection type."
            );

    [Fact]
    public void GetDatabaseAdapter_ShouldGetAdapter()
    {
        var adapterA = Substitute.For<IDatabaseAdapter>();
        var adapterB = Substitute.For<IDatabaseAdapter>();

        DbConnectionPlusConfiguration.Instance.RegisterDatabaseAdapter<FakeConnectionA>(adapterA);
        DbConnectionPlusConfiguration.Instance.RegisterDatabaseAdapter<FakeConnectionB>(adapterB);

        DbConnectionPlusConfiguration.Instance.GetDatabaseAdapter(typeof(FakeConnectionA))
            .Should().BeSameAs(adapterA);

        DbConnectionPlusConfiguration.Instance.GetDatabaseAdapter(typeof(FakeConnectionB))
            .Should().BeSameAs(adapterB);
    }

    [Fact]
    public void GetDatabaseAdapter_ShouldGetDefaultAdapters()
    {
        DbConnectionPlusConfiguration.Instance.GetDatabaseAdapter(typeof(MySqlConnection))
            .Should().BeOfType<MySqlDatabaseAdapter>();

        DbConnectionPlusConfiguration.Instance.GetDatabaseAdapter(typeof(OracleConnection))
            .Should().BeOfType<OracleDatabaseAdapter>();

        DbConnectionPlusConfiguration.Instance.GetDatabaseAdapter(typeof(NpgsqlConnection))
            .Should().BeOfType<PostgreSqlDatabaseAdapter>();

        DbConnectionPlusConfiguration.Instance.GetDatabaseAdapter(typeof(SqliteConnection))
            .Should().BeOfType<SqliteDatabaseAdapter>();

        DbConnectionPlusConfiguration.Instance.GetDatabaseAdapter(typeof(SqlConnection))
            .Should().BeOfType<SqlServerDatabaseAdapter>();
    }

    [Fact]
    public void GetEntityTypeBuilders_ShouldGetConfiguredBuilders()
    {
        var configuration = new DbConnectionPlusConfiguration();

        var entityBuilder = configuration.Entity<Entity>();
        var mappingTestEntityFluentApiBuilder = configuration.Entity<MappingTestEntityFluentApi>();

        var entityTypeBuilders = configuration.GetEntityTypeBuilders();

        entityTypeBuilders
            .Should().ContainKeys(
                typeof(Entity),
                typeof(MappingTestEntityFluentApi)
            );

        entityTypeBuilders[typeof(Entity)]
            .Should().BeSameAs(entityBuilder);

        entityTypeBuilders[typeof(MappingTestEntityFluentApi)]
            .Should().BeSameAs(mappingTestEntityFluentApiBuilder);
    }

    [Fact]
    public void InterceptDbCommand_ShouldInterceptDbCommands()
    {
        var interceptor = Substitute.For<InterceptDbCommand>();

        DbCommand? interceptedDbCommand = null;
        IReadOnlyCollection<InterpolatedTemporaryTable>? interceptedTemporaryTables = null;

        interceptor
            .WhenForAnyArgs(interceptor2 =>
                interceptor2.Invoke(
                    Arg.Any<DbCommand>(),
                    Arg.Any<IReadOnlyList<InterpolatedTemporaryTable>>()
                )
            )
            .Do(info =>
                {
                    interceptedDbCommand = info.Arg<DbCommand>();
                    interceptedTemporaryTables = info.Arg<IReadOnlyList<InterpolatedTemporaryTable>>();
                }
            );

        DbConnectionPlusConfiguration.Instance.InterceptDbCommand = interceptor;

        this.MockDbConnection.SetupQuery(_ => true).Returns(new { Id = 1 });

        var entities = Generate.Multiple<Entity>();
        var entityIds = Generate.Ids();
        var stringValue = entities[0].StringValue;

        InterpolatedSqlStatement statement =
            $"""
             SELECT Id, StringValue
             FROM   {TemporaryTable(entities)} TEntity
             WHERE  TEntity.Id IN ({TemporaryTable(entityIds)}) OR StringValue = {Parameter(stringValue)}
             """;

        var temporaryTables = statement.TemporaryTables;

        var transaction = this.MockDbConnection.BeginTransaction();
        var timeout = Generate.Single<TimeSpan>();
        var cancellationToken = Generate.Single<CancellationToken>();

        _ = this.MockDbConnection.Query<Int32>(
            statement,
            transaction,
            timeout,
            CommandType.StoredProcedure,
            cancellationToken
        ).ToList();

        interceptor.Received().Invoke(
            Arg.Any<DbCommand>(),
            Arg.Any<IReadOnlyList<InterpolatedTemporaryTable>>()
        );

        interceptedDbCommand
            .Should().NotBeNull();

        interceptedDbCommand.CommandText
            .Should().Be(
                $"""
                 SELECT Id, StringValue
                 FROM   [#{temporaryTables[0].Name}] TEntity
                 WHERE  TEntity.Id IN ([#{temporaryTables[1].Name}]) OR StringValue = @StringValue
                 """
            );

        interceptedDbCommand.Transaction
            .Should().Be(transaction);

        interceptedDbCommand.CommandType
            .Should().Be(CommandType.StoredProcedure);

        interceptedDbCommand.CommandTimeout
            .Should().Be((Int32)timeout.TotalSeconds);

        interceptedDbCommand.Parameters.Count
            .Should().Be(1);

        interceptedDbCommand.Parameters[0].ParameterName
            .Should().Be("StringValue");

        interceptedDbCommand.Parameters[0].Value
            .Should().Be(stringValue);

        interceptedTemporaryTables
            .Should().NotBeNull();

        interceptedTemporaryTables
            .Should().BeEquivalentTo(temporaryTables);
    }

    [Fact]
    public void RegisterDatabaseAdapter_ShouldRegisterAdapter()
    {
        var adapterA = Substitute.For<IDatabaseAdapter>();

        DbConnectionPlusConfiguration.Instance.RegisterDatabaseAdapter<FakeConnectionA>(adapterA);

        DbConnectionPlusConfiguration.Instance.GetDatabaseAdapter(typeof(FakeConnectionA))
            .Should().BeSameAs(adapterA);

        var adapterB = Substitute.For<IDatabaseAdapter>();

        DbConnectionPlusConfiguration.Instance.RegisterDatabaseAdapter<FakeConnectionB>(adapterB);

        DbConnectionPlusConfiguration.Instance.GetDatabaseAdapter(typeof(FakeConnectionB))
            .Should().BeSameAs(adapterB);
    }

    [Fact]
    public void RegisterDatabaseAdapter_ShouldReplaceRegisteredAdapter()
    {
        var adapterA = Substitute.For<IDatabaseAdapter>();

        DbConnectionPlusConfiguration.Instance.RegisterDatabaseAdapter<FakeConnectionA>(adapterA);

        DbConnectionPlusConfiguration.Instance.GetDatabaseAdapter(typeof(FakeConnectionA))
            .Should().BeSameAs(adapterA);

        var adapterB = Substitute.For<IDatabaseAdapter>();

        DbConnectionPlusConfiguration.Instance.RegisterDatabaseAdapter<FakeConnectionA>(adapterB);

        DbConnectionPlusConfiguration.Instance.GetDatabaseAdapter(typeof(FakeConnectionA))
            .Should().BeSameAs(adapterB);
    }

    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        ArgumentNullGuardVerifier.Verify(() =>
            DbConnectionPlusConfiguration.Instance.GetDatabaseAdapter(typeof(SqlConnection))
        );

        ArgumentNullGuardVerifier.Verify(() =>
            DbConnectionPlusConfiguration.Instance.RegisterDatabaseAdapter<SqlConnection>(
                new SqlServerDatabaseAdapter()
            )
        );
    }
}
