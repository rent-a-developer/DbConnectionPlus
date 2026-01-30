using NSubstitute.DbConnection;
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

        ((IFreezable)configuration).Freeze();

        Invoking(() => configuration.EnumSerializationMode = EnumSerializationMode.Integers)
            .Should().Throw<InvalidOperationException>()
            .WithMessage("This configuration is frozen and can no longer be modified.");

        Invoking(() => configuration.InterceptDbCommand = null)
            .Should().Throw<InvalidOperationException>()
            .WithMessage("This configuration is frozen and can no longer be modified.");

        Invoking(() => configuration.Entity<Entity>())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("This configuration is frozen and can no longer be modified.");

        Invoking(() => entityTypeBuilder.ToTable("Entities"))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("This builder is frozen and can no longer be modified.");
    }

    [Fact]
    public void GetEntityTypeBuilders_ShouldGetConfiguredBuilders()
    {
        var configuration = new DbConnectionPlusConfiguration();

        var entityBuilder = configuration.Entity<Entity>();
        var entityWithIdentityAndComputedPropertiesBuilder =
            configuration.Entity<EntityWithIdentityAndComputedProperties>();
        var entityWithNotMappedPropertyBuilder = configuration.Entity<EntityWithNotMappedProperty>();

        var entityTypeBuilders = configuration.GetEntityTypeBuilders();

        entityTypeBuilders
            .Should().ContainKeys(
                typeof(Entity),
                typeof(EntityWithIdentityAndComputedProperties),
                typeof(EntityWithNotMappedProperty)
            );

        entityTypeBuilders[typeof(Entity)]
            .Should().BeSameAs(entityBuilder);

        entityTypeBuilders[typeof(EntityWithIdentityAndComputedProperties)]
            .Should().BeSameAs(entityWithIdentityAndComputedPropertiesBuilder);

        entityTypeBuilders[typeof(EntityWithNotMappedProperty)]
            .Should().BeSameAs(entityWithNotMappedPropertyBuilder);
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
}
