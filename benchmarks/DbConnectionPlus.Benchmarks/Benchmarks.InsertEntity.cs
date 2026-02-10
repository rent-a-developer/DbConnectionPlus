// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    [GlobalCleanup(
        Targets =
        [
            nameof(InsertEntity_Command),
            nameof(InsertEntity_Dapper),
            nameof(InsertEntity_DbConnectionPlus)
        ]
    )]
    public void InsertEntity__Cleanup() =>
        this.connection.Dispose();

    [GlobalSetup(
        Targets =
        [
            nameof(InsertEntity_Command),
            nameof(InsertEntity_Dapper),
            nameof(InsertEntity_DbConnectionPlus)
        ]
    )]
    public void InsertEntity__Setup() =>
        this.SetupDatabase(0);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(InsertEntity_Category)]
    public void InsertEntity_Command()
    {
        using var command = this.connection.CreateCommand();

        command.CommandText = InsertEntitySql;

        var parameters = new Dictionary<String, SqliteParameter>
        {
            { "Id", new("Id", null) },
            { "BooleanValue", new("BooleanValue", null) },
            { "BytesValue", new("BytesValue", null) },
            { "ByteValue", new("ByteValue", null) },
            { "CharValue", new("CharValue", null) },
            { "DateTimeValue", new("DateTimeValue", null) },
            { "DecimalValue", new("DecimalValue", null) },
            { "DoubleValue", new("DoubleValue", null) },
            { "EnumValue", new("EnumValue", null) },
            { "GuidValue", new("GuidValue", null) },
            { "Int16Value", new("Int16Value", null) },
            { "Int32Value", new("Int32Value", null) },
            { "Int64Value", new("Int64Value", null) },
            { "SingleValue", new("SingleValue", null) },
            { "StringValue", new("StringValue", null) },
            { "TimeSpanValue", new("TimeSpanValue", null) }
        };

        command.Parameters.AddRange(parameters.Values);

        PopulateEntityParameters(this.insertEntity_entityToInsert, parameters);

        command.ExecuteNonQuery();
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(InsertEntity_Category)]
    public void InsertEntity_Dapper() =>
        SqlMapperExtensions.Insert(this.connection, this.insertEntity_entityToInsert);

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(InsertEntity_Category)]
    public void InsertEntity_DbConnectionPlus() =>
        this.connection.InsertEntity(this.insertEntity_entityToInsert);

    private readonly BenchmarkEntity insertEntity_entityToInsert = Generate.Single<BenchmarkEntity>();
    private const String InsertEntity_Category = "InsertEntity";
}
