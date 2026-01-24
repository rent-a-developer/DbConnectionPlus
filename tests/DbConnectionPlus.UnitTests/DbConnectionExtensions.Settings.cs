using NSubstitute.DbConnection;
using RentADeveloper.DbConnectionPlus.SqlStatements;

namespace RentADeveloper.DbConnectionPlus.UnitTests;

public class DbConnectionExtensions_SettingsTests : UnitTestsBase
{
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

        DbConnectionExtensions.InterceptDbCommand = interceptor;

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
