// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    [GlobalCleanup(
        Targets =
        [
            nameof(ExecuteReader_DbCommand),
            nameof(ExecuteReader_Dapper),
            nameof(ExecuteReader_DbConnectionPlus)
        ]
    )]
    public void ExecuteReader__Cleanup() =>
        this.connection.Dispose();

    [GlobalSetup(
        Targets =
        [
            nameof(ExecuteReader_DbCommand),
            nameof(ExecuteReader_Dapper),
            nameof(ExecuteReader_DbConnectionPlus)
        ]
    )]
    public void ExecuteReader__Setup() =>
        this.SetupDatabase(100);

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(ExecuteReader_Category)]
    public List<BenchmarkEntity> ExecuteReader_Dapper()
    {
        var result = new List<BenchmarkEntity>();

        using var dataReader = SqlMapper.ExecuteReader(this.connection, ExecuteReaderSql);

        while (dataReader.Read())
        {
            result.Add(ReadEntity(dataReader));
        }

        return result;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(ExecuteReader_Category)]
    public List<BenchmarkEntity> ExecuteReader_DbCommand()
    {
        var result = new List<BenchmarkEntity>();

        using var command = this.connection.CreateCommand();
        command.CommandText = ExecuteReaderSql;

        using var dataReader = command.ExecuteReader();

        while (dataReader.Read())
        {
            result.Add(ReadEntity(dataReader));
        }

        return result;
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(ExecuteReader_Category)]
    public List<BenchmarkEntity> ExecuteReader_DbConnectionPlus()
    {
        var result = new List<BenchmarkEntity>();

        using var dataReader = this.connection.ExecuteReader(ExecuteReaderSql);

        while (dataReader.Read())
        {
            result.Add(ReadEntity(dataReader));
        }

        return result;
    }

    private const String ExecuteReader_Category = "ExecuteReader";

    private const String ExecuteReaderSql = """
                                            SELECT
                                                Id,
                                                BooleanValue,
                                                BytesValue,
                                                ByteValue,
                                                CharValue,
                                                DateTimeValue,
                                                DecimalValue,
                                                DoubleValue,
                                                EnumValue,
                                                GuidValue,
                                                Int16Value,
                                                Int32Value,
                                                Int64Value,
                                                SingleValue,
                                                StringValue,
                                                TimeSpanValue
                                            FROM
                                                Entity
                                            """;
}
