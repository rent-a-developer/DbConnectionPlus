// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    [GlobalCleanup(
        Targets =
        [
            nameof(InsertEntities_Command),
            nameof(InsertEntities_Dapper),
            nameof(InsertEntities_DbConnectionPlus)
        ]
    )]
    public void InsertEntities__Cleanup() =>
        this.connection.Dispose();

    [GlobalSetup(
        Targets =
        [
            nameof(InsertEntities_Command),
            nameof(InsertEntities_Dapper),
            nameof(InsertEntities_DbConnectionPlus)
        ]
    )]
    public void InsertEntities__Setup() =>
        this.SetupDatabase(0);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(InsertEntities_Category)]
    public void InsertEntities_Command()
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

        foreach (var entity in this.insertEntities_entitiesToInsert)
        {
            PopulateEntityParameters(entity, parameters);

            command.ExecuteNonQuery();
        }
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(InsertEntities_Category)]
    public void InsertEntities_Dapper() =>
        SqlMapperExtensions.Insert(this.connection, this.insertEntities_entitiesToInsert);

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(InsertEntities_Category)]
    public void InsertEntities_DbConnectionPlus() =>
        this.connection.InsertEntities(this.insertEntities_entitiesToInsert);

    private readonly List<BenchmarkEntity> insertEntities_entitiesToInsert =
        Generate.Multiple<BenchmarkEntity>(InsertEntities_EntitiesPerOperation);

    private const String InsertEntities_Category = "InsertEntities";
    private const Int32 InsertEntities_EntitiesPerOperation = 200;

    private const String InsertEntitySql = """
                                           INSERT INTO Entity
                                           (
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
                                           )
                                           VALUES
                                           (
                                             @Id,
                                             @BooleanValue,
                                             @BytesValue,
                                             @ByteValue,
                                             @CharValue,
                                             @DateTimeValue,
                                             @DecimalValue,
                                             @DoubleValue,
                                             @EnumValue,
                                             @GuidValue,
                                             @Int16Value,
                                             @Int32Value,
                                             @Int64Value,
                                             @SingleValue,
                                             @StringValue,
                                             @TimeSpanValue
                                           )
                                           """;
}
