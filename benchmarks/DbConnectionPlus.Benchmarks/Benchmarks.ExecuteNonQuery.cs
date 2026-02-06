// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    [IterationCleanup(
        Targets =
        [
            nameof(ExecuteNonQuery_DbCommand),
            nameof(ExecuteNonQuery_Dapper),
            nameof(ExecuteNonQuery_DbConnectionPlus)
        ]
    )]
    public void ExecuteNonQuery__Cleanup() =>
        this.connection.Dispose();

    [IterationSetup(
        Targets =
        [
            nameof(ExecuteNonQuery_DbCommand),
            nameof(ExecuteNonQuery_Dapper),
            nameof(ExecuteNonQuery_DbConnectionPlus)
        ]
    )]
    public void ExecuteNonQuery__Setup() =>
        this.SetupDatabase(ExecuteNonQuery_OperationsPerInvoke);

    [Benchmark(Baseline = false, OperationsPerInvoke = ExecuteNonQuery_OperationsPerInvoke)]
    [BenchmarkCategory(ExecuteNonQuery_Category)]
    public void ExecuteNonQuery_Dapper()
    {
        for (var i = 0; i < ExecuteNonQuery_OperationsPerInvoke; i++)
        {
            var entity = this.entitiesInDb[0];

            SqlMapper.Execute(this.connection, "DELETE FROM Entity WHERE Id = @Id", new { entity.Id });

            this.entitiesInDb.Remove(entity);
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = ExecuteNonQuery_OperationsPerInvoke)]
    [BenchmarkCategory(ExecuteNonQuery_Category)]
    public void ExecuteNonQuery_DbCommand()
    {
        for (var i = 0; i < ExecuteNonQuery_OperationsPerInvoke; i++)
        {
            var entity = this.entitiesInDb[0];

            using var command = this.connection.CreateCommand();

            command.CommandText = "DELETE FROM Entity WHERE Id = @Id";
            command.Parameters.Add(new("@Id", entity.Id));

            command.ExecuteNonQuery();

            this.entitiesInDb.Remove(entity);
        }
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = ExecuteNonQuery_OperationsPerInvoke)]
    [BenchmarkCategory(ExecuteNonQuery_Category)]
    public void ExecuteNonQuery_DbConnectionPlus()
    {
        for (var i = 0; i < ExecuteNonQuery_OperationsPerInvoke; i++)
        {
            var entity = this.entitiesInDb[0];

            this.connection.ExecuteNonQuery($"DELETE FROM Entity WHERE Id = {Parameter(entity.Id)}");

            this.entitiesInDb.Remove(entity);
        }
    }

    private const String ExecuteNonQuery_Category = "ExecuteNonQuery";
    private const Int32 ExecuteNonQuery_OperationsPerInvoke = 7700;
}
