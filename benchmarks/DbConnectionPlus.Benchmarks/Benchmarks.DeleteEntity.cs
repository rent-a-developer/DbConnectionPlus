// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    [IterationCleanup(
        Targets =
        [
            nameof(DeleteEntity_Command),
            nameof(DeleteEntity_Dapper),
            nameof(DeleteEntity_DbConnectionPlus)
        ]
    )]
    public void DeleteEntity__Cleanup() =>
        this.SetupDatabase(DeleteEntity_OperationsPerInvoke);

    [IterationSetup(
        Targets =
        [
            nameof(DeleteEntity_Command),
            nameof(DeleteEntity_Dapper),
            nameof(DeleteEntity_DbConnectionPlus)
        ]
    )]
    public void DeleteEntity__Setup() =>
        this.SetupDatabase(DeleteEntity_OperationsPerInvoke);

    [Benchmark(Baseline = true, OperationsPerInvoke = DeleteEntity_OperationsPerInvoke)]
    [BenchmarkCategory(DeleteEntity_Category)]
    public void DeleteEntity_Command()
    {
        for (var i = 0; i < DeleteEntity_OperationsPerInvoke; i++)
        {
            var entityToDelete = this.entitiesInDb[0];

            using var command = this.connection.CreateCommand();

            command.CommandText = "DELETE FROM Entity WHERE Id = @Id";

            var idParameter = command.CreateParameter();

            idParameter.ParameterName = "@Id";
            idParameter.Value = entityToDelete.Id;

            command.Parameters.Add(idParameter);

            command.ExecuteNonQuery();

            this.entitiesInDb.Remove(entityToDelete);
        }
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = DeleteEntity_OperationsPerInvoke)]
    [BenchmarkCategory(DeleteEntity_Category)]
    public void DeleteEntity_Dapper()
    {
        for (var i = 0; i < DeleteEntity_OperationsPerInvoke; i++)
        {
            var entityToDelete = this.entitiesInDb[0];

            SqlMapperExtensions.Delete(this.connection, entityToDelete);

            this.entitiesInDb.Remove(entityToDelete);
        }
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = DeleteEntity_OperationsPerInvoke)]
    [BenchmarkCategory(DeleteEntity_Category)]
    public void DeleteEntity_DbConnectionPlus()
    {
        for (var i = 0; i < DeleteEntity_OperationsPerInvoke; i++)
        {
            var entityToDelete = this.entitiesInDb[0];

            this.connection.DeleteEntity(entityToDelete);

            this.entitiesInDb.Remove(entityToDelete);
        }
    }

    private const String DeleteEntity_Category = "DeleteEntity";
    private const Int32 DeleteEntity_OperationsPerInvoke = 8000;
}
