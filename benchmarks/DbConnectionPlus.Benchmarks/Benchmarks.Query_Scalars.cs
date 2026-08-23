// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    private const string Query_Scalars_Category = "Query_Scalars";
    private const int Query_Scalars_EntitiesPerOperation = 600;

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(Query_Scalars_Category)]
    public List<long> Query_Scalars_Command()
    {
        var result = new List<long>();

        using var command = this.connection.CreateCommand();

        command.CommandText = "SELECT Id FROM Entity";

        using var dataReader = command.ExecuteReader();

        while (dataReader.Read())
        {
            result.Add(dataReader.GetInt64(0));
        }

        return result;
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(Query_Scalars_Category)]
    public List<long> Query_Scalars_Dapper() => [.. SqlMapper.Query<long>(this.connection, "SELECT Id FROM Entity")];

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(Query_Scalars_Category)]
    public List<long> Query_Scalars_DbConnectionPlus() => [.. this.connection.Query<long>("SELECT Id FROM Entity")];

    [GlobalCleanup(
        Targets = [nameof(Query_Scalars_Command), nameof(Query_Scalars_Dapper), nameof(Query_Scalars_DbConnectionPlus)]
    )]
    public void Query_Scalars__Cleanup() => this.connection.Dispose();

    [GlobalSetup(
        Targets = [nameof(Query_Scalars_Command), nameof(Query_Scalars_Dapper), nameof(Query_Scalars_DbConnectionPlus)]
    )]
    public void Query_Scalars__Setup() => this.SetupDatabase(Query_Scalars_EntitiesPerOperation);
}
