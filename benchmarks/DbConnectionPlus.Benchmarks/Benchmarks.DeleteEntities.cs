// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    [IterationCleanup(
        Targets =
        [
            nameof(DeleteEntities_Command),
            nameof(DeleteEntities_Dapper),
            nameof(DeleteEntities_DbConnectionPlus)
        ]
    )]
    public void DeleteEntities__Cleanup() =>
        this.connection.Dispose();

    [IterationSetup(
        Targets =
        [
            nameof(DeleteEntities_Command),
            nameof(DeleteEntities_Dapper),
            nameof(DeleteEntities_DbConnectionPlus)
        ]
    )]
    public void DeleteEntities__Setup() =>
        this.SetupDatabase(DeleteEntities_EntitiesPerOperation * DeleteEntities_OperationsPerInvoke);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(DeleteEntities_Category)]
    public void DeleteEntities_Command()
    {
        for (int i = 0; i < DeleteEntities_OperationsPerInvoke; i++)
        {
            using var command = this.connection.CreateCommand();
            command.CommandText = "DELETE FROM Entity WHERE Id = @Id";

            var idParameter = command.CreateParameter();
            idParameter.ParameterName = "@Id";
            command.Parameters.Add(idParameter);

            var entities = this.entitiesInDb.Take(DeleteEntities_EntitiesPerOperation).ToList();

            foreach (var entity in entities)
            {
                idParameter.Value = entity.Id;

                command.ExecuteNonQuery();
            }

            this.entitiesInDb.RemoveRange(0, DeleteEntities_EntitiesPerOperation);
        }
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(DeleteEntities_Category)]
    public void DeleteEntities_Dapper()
    {
        for (int i = 0; i < DeleteEntities_OperationsPerInvoke; i++)
        {
            var entities = this.entitiesInDb.Take(DeleteEntities_EntitiesPerOperation).ToList();

            SqlMapperExtensions.Delete(this.connection, entities);

            this.entitiesInDb.RemoveRange(0, DeleteEntities_EntitiesPerOperation);
        }
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(DeleteEntities_Category)]
    public void DeleteEntities_DbConnectionPlus()
    {
        for (int i = 0; i < DeleteEntities_OperationsPerInvoke; i++)
        {
            var entities = this.entitiesInDb.Take(DeleteEntities_EntitiesPerOperation).ToList();

            this.connection.DeleteEntities(entities);

            this.entitiesInDb.RemoveRange(0, DeleteEntities_EntitiesPerOperation);
        }
    }

    private const String DeleteEntities_Category = "DeleteEntities";
    private const Int32 DeleteEntities_EntitiesPerOperation = 250;
    private const Int32 DeleteEntities_OperationsPerInvoke = 20;
}
