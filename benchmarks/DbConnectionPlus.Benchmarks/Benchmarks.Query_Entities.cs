// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    [GlobalCleanup(
        Targets =
        [
            nameof(Query_Entities_Command),
            nameof(Query_Entities_Dapper),
            nameof(Query_Entities_DbConnectionPlus)
        ]
    )]
    public void Query_Entities__Cleanup() =>
        this.connection.Dispose();

    [GlobalSetup(
        Targets =
        [
            nameof(Query_Entities_Command),
            nameof(Query_Entities_Dapper),
            nameof(Query_Entities_DbConnectionPlus)
        ]
    )]
    public void Query_Entities__Setup() =>
        this.SetupDatabase(Query_Entities_EntitiesPerOperation);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(Query_Entities_Category)]
    public List<BenchmarkEntity> Query_Entities_Command()
    {
        var result = new List<BenchmarkEntity>();

        using var command = this.connection.CreateCommand();

        command.CommandText = "SELECT * FROM Entity";

        using var dataReader = command.ExecuteReader();

        while (dataReader.Read())
        {
            result.Add(ReadEntity(dataReader));
        }

        return result;
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(Query_Entities_Category)]
    public List<BenchmarkEntity> Query_Entities_Dapper() =>
        SqlMapper.Query<BenchmarkEntity>(this.connection, "SELECT * FROM Entity").ToList();

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(Query_Entities_Category)]
    public List<BenchmarkEntity> Query_Entities_DbConnectionPlus() =>
        this.connection.Query<BenchmarkEntity>("SELECT * FROM Entity").ToList();

    private const String Query_Entities_Category = "Query_Entities";
    private const Int32 Query_Entities_EntitiesPerOperation = 100;
}
