// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    [GlobalCleanup(
        Targets = [
            nameof(Query_ValueTuples_Command),
            nameof(Query_ValueTuples_Dapper),
            nameof(Query_ValueTuples_DbConnectionPlus),
        ]
    )]
    public void Query_ValueTuples__Cleanup() => this.connection.Dispose();

    [GlobalSetup(
        Targets = [
            nameof(Query_ValueTuples_Command),
            nameof(Query_ValueTuples_Dapper),
            nameof(Query_ValueTuples_DbConnectionPlus),
        ]
    )]
    public void Query_ValueTuples__Setup() => this.SetupDatabase(Query_ValueTuples_EntitiesPerOperation);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(Query_ValueTuples_Category)]
    public List<(long Id, DateTime DateTimeValue, TestEnum EnumValue, string StringValue)> Query_ValueTuples_Command()
    {
        var result = new List<(long Id, DateTime DateTimeValue, TestEnum EnumValue, string StringValue)>();

        using var command = this.connection.CreateCommand();

        command.CommandText = "SELECT Id, DateTimeValue, EnumValue, StringValue FROM Entity";

        using var dataReader = command.ExecuteReader();

        while (dataReader.Read())
        {
            result.Add(
                (
                    dataReader.GetInt64(0),
                    DateTime.Parse(dataReader.GetString(1), CultureInfo.InvariantCulture),
                    Enum.Parse<TestEnum>(dataReader.GetString(2)),
                    dataReader.GetString(3)
                )
            );
        }

        return result;
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(Query_ValueTuples_Category)]
    public List<(long Id, DateTime DateTimeValue, TestEnum EnumValue, string StringValue)> Query_ValueTuples_Dapper() =>
        [
            .. SqlMapper.Query<(long Id, DateTime DateTimeValue, TestEnum EnumValue, string StringValue)>(
                this.connection,
                "SELECT Id, DateTimeValue, EnumValue, StringValue FROM Entity"
            ),
        ];

    // There is no Query_ValueTuples_Dapper_Aot benchmark, so this category's Native AOT group compares
    // DbConnectionPlus against the raw DbCommand baseline alone. Dapper.AOT's generator does not materialize value
    // tuples: annotating the call site produced no interceptor and no diagnostic either, so it would silently have
    // run as ordinary Dapper and thrown in the Native AOT job. Adding [BindTupleByName] only silences the DAP012
    // advisory, it does not enable generation. See the README next to this file.

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(Query_ValueTuples_Category)]
    public List<(
        long Id,
        DateTime DateTimeValue,
        TestEnum EnumValue,
        string StringValue
    )> Query_ValueTuples_DbConnectionPlus() =>
        [
            .. this.connection.Query<(long Id, DateTime DateTimeValue, TestEnum EnumValue, string StringValue)>(
                "SELECT Id, DateTimeValue, EnumValue, StringValue FROM Entity"
            ),
        ];

    private const string Query_ValueTuples_Category = "Query_ValueTuples";
    private const int Query_ValueTuples_EntitiesPerOperation = 150;
}
