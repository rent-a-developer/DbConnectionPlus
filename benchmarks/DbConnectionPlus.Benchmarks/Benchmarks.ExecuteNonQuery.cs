// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    [GlobalCleanup(
        Targets =
        [
            nameof(ExecuteNonQuery_Command),
            nameof(ExecuteNonQuery_Dapper),
            nameof(ExecuteNonQuery_DbConnectionPlus)
        ]
    )]
    public void ExecuteNonQuery__Cleanup() =>
        this.connection.Dispose();

    [GlobalSetup(
        Targets =
        [
            nameof(ExecuteNonQuery_Command),
            nameof(ExecuteNonQuery_Dapper),
            nameof(ExecuteNonQuery_DbConnectionPlus)
        ]
    )]
    public void ExecuteNonQuery__Setup() =>
        this.SetupDatabase(0);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(ExecuteNonQuery_Category)]
    public void ExecuteNonQuery_Command()
    {
        using var command = this.connection.CreateCommand();

        command.CommandText = "DELETE FROM Entity WHERE Id = @Id";

        var idParameter = command.CreateParameter();

        idParameter.ParameterName = "@Id";
        idParameter.Value = -1;

        command.Parameters.Add(idParameter);

        command.ExecuteNonQuery();
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(ExecuteNonQuery_Category)]
    public void ExecuteNonQuery_Dapper() =>
        SqlMapper.Execute(this.connection, "DELETE FROM Entity WHERE Id = @Id", new { Id = -1 });

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(ExecuteNonQuery_Category)]
    public void ExecuteNonQuery_DbConnectionPlus() =>
        this.connection.ExecuteNonQuery($"DELETE FROM Entity WHERE Id = {Parameter(-1)}");

    private const String ExecuteNonQuery_Category = "ExecuteNonQuery";
}
