using RentADeveloper.DbConnectionPlus.DatabaseAdapters;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.MySql;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.Oracle;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.PostgreSql;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.Sqlite;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.SqlServer;

namespace RentADeveloper.DbConnectionPlus.UnitTests.DatabaseAdapters;

public class EntityManipulatorTests : UnitTestsBase
{
    [Theory]
    [MemberData(nameof(GetManipulators))]
    public void ShouldGuardAgainstNullArguments(IEntityManipulator manipulator)
    {
        var entities = Generate.Multiple<Entity>();
        var entity = Generate.Single<Entity>();

        ArgumentNullGuardVerifier.Verify(() =>
            manipulator.DeleteEntities(this.MockDbConnection, entities, null, CancellationToken.None)
        );

        ArgumentNullGuardVerifier.Verify(() =>
            manipulator.DeleteEntitiesAsync(this.MockDbConnection, entities, null, CancellationToken.None)
        );

        ArgumentNullGuardVerifier.Verify(() =>
            manipulator.DeleteEntity(this.MockDbConnection, entity, null, CancellationToken.None)
        );

        ArgumentNullGuardVerifier.Verify(() =>
            manipulator.DeleteEntityAsync(this.MockDbConnection, entity, null, CancellationToken.None)
        );

        ArgumentNullGuardVerifier.Verify(() =>
            manipulator.InsertEntities(this.MockDbConnection, entities, null, CancellationToken.None)
        );

        ArgumentNullGuardVerifier.Verify(() =>
            manipulator.InsertEntitiesAsync(this.MockDbConnection, entities, null, CancellationToken.None)
        );

        ArgumentNullGuardVerifier.Verify(() =>
            manipulator.InsertEntity(this.MockDbConnection, entity, null, CancellationToken.None)
        );

        ArgumentNullGuardVerifier.Verify(() =>
            manipulator.InsertEntityAsync(this.MockDbConnection, entity, null, CancellationToken.None)
        );

        ArgumentNullGuardVerifier.Verify(() =>
            manipulator.UpdateEntities(this.MockDbConnection, entities, null, CancellationToken.None)
        );

        ArgumentNullGuardVerifier.Verify(() =>
            manipulator.UpdateEntitiesAsync(this.MockDbConnection, entities, null, CancellationToken.None)
        );

        ArgumentNullGuardVerifier.Verify(() =>
            manipulator.UpdateEntity(this.MockDbConnection, entity, null, CancellationToken.None)
        );

        ArgumentNullGuardVerifier.Verify(() =>
            manipulator.UpdateEntityAsync(this.MockDbConnection, entity, null, CancellationToken.None)
        );
    }

    public static IEnumerable<ValueTuple<IEntityManipulator>> GetManipulators()
    {
        yield return new(new MySqlEntityManipulator(new()));
        yield return new(new OracleEntityManipulator(new()));
        yield return new(new PostgreSqlEntityManipulator(new()));
        yield return new(new SqliteEntityManipulator(new()));
        yield return new(new SqlServerEntityManipulator(new()));
    }
}
