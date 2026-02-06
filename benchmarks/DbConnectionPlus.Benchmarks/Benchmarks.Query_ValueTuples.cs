// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    [GlobalCleanup(
        Targets =
        [
            nameof(Query_ValueTuples_DbCommand),
            nameof(Query_ValueTuples_Dapper),
            nameof(Query_ValueTuples_DbConnectionPlus)
        ]
    )]
    public void Query_ValueTuples__Cleanup() =>
        this.connection.Dispose();

    [GlobalSetup(
        Targets =
        [
            nameof(Query_ValueTuples_DbCommand),
            nameof(Query_ValueTuples_Dapper),
            nameof(Query_ValueTuples_DbConnectionPlus)
        ]
    )]
    public void Query_ValueTuples__Setup() =>
        this.SetupDatabase(Query_ValueTuples_EntitiesPerOperation);

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(Query_ValueTuples_Category)]
    public List<(Int64 Id, DateTime DateTimeValue, TestEnum EnumValue, String StringValue)>
        Query_ValueTuples_Dapper() =>
        SqlMapper
            .Query<(Int64 Id, DateTime DateTimeValue, TestEnum EnumValue, String StringValue)>(
                this.connection,
                "SELECT Id, DateTimeValue, EnumValue, StringValue FROM Entity"
            )
            .ToList();

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(Query_ValueTuples_Category)]
    public List<(Int64 Id, DateTime DateTimeValue, TestEnum EnumValue, String StringValue)>
        Query_ValueTuples_DbCommand()
    {
        var tuples = new List<(Int64 Id, DateTime DateTimeValue, TestEnum EnumValue, String StringValue)>();

        using var command = this.connection.CreateCommand();
        command.CommandText = "SELECT Id, DateTimeValue, EnumValue, StringValue FROM Entity";

        using var dataReader = command.ExecuteReader();

        while (dataReader.Read())
        {
            tuples.Add(
                (
                    dataReader.GetInt64(0),
                    DateTime.Parse(dataReader.GetString(1), CultureInfo.InvariantCulture),
                    Enum.Parse<TestEnum>(dataReader.GetString(2)),
                    dataReader.GetString(3)
                )
            );
        }

        return tuples;
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(Query_ValueTuples_Category)]
    public List<(Int64 Id, DateTime DateTimeValue, TestEnum EnumValue, String StringValue)>
        Query_ValueTuples_DbConnectionPlus() =>
        this.connection
            .Query<(Int64 Id, DateTime DateTimeValue, TestEnum EnumValue, String StringValue)>(
                "SELECT Id, DateTimeValue, EnumValue, StringValue FROM Entity"
            )
            .ToList();

    private const String Query_ValueTuples_Category = "Query_ValueTuples";
    private const Int32 Query_ValueTuples_EntitiesPerOperation = 150;
}
