// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    [GlobalCleanup(
        Targets =
        [
            nameof(UpdateEntity_Command),
            nameof(UpdateEntity_Dapper),
            nameof(UpdateEntity_DbConnectionPlus)
        ]
    )]
    public void UpdateEntity__Cleanup() =>
        this.connection.Dispose();

    [GlobalSetup(
        Targets =
        [
            nameof(UpdateEntity_Command),
            nameof(UpdateEntity_Dapper),
            nameof(UpdateEntity_DbConnectionPlus)
        ]
    )]
    public void UpdateEntity__Setup() =>
        this.SetupDatabase(1);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(UpdateEntity_Category)]
    public void UpdateEntity_Command()
    {
        var entity = this.entitiesInDb[0];

        var updatedEntity = Generate.UpdateFor(entity);

        using var command = this.connection.CreateCommand();

        command.CommandText = """
                              UPDATE    Entity
                              SET       BooleanValue = @BooleanValue,
                                        BytesValue = @BytesValue,
                                        ByteValue = @ByteValue,
                                        CharValue = @CharValue,
                                        DateTimeValue = @DateTimeValue,
                                        DecimalValue = @DecimalValue,
                                        DoubleValue = @DoubleValue,
                                        EnumValue = @EnumValue,
                                        GuidValue = @GuidValue,
                                        Int16Value = @Int16Value,
                                        Int32Value = @Int32Value,
                                        Int64Value = @Int64Value,
                                        SingleValue = @SingleValue,
                                        StringValue = @StringValue,
                                        TimeSpanValue = @TimeSpanValue
                              WHERE     Id = @Id
                              """;

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

        PopulateEntityParameters(updatedEntity, parameters);

        command.ExecuteNonQuery();
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(UpdateEntity_Category)]
    public void UpdateEntity_Dapper()
    {
        var entity = this.entitiesInDb[0];

        var updatedEntity = Generate.UpdateFor(entity);

        SqlMapperExtensions.Update(this.connection, updatedEntity);
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(UpdateEntity_Category)]
    public void UpdateEntity_DbConnectionPlus()
    {
        var entity = this.entitiesInDb[0];

        var updatedEntity = Generate.UpdateFor(entity);

        this.connection.UpdateEntity(updatedEntity);
    }

    private const String UpdateEntity_Category = "UpdateEntity";
}
