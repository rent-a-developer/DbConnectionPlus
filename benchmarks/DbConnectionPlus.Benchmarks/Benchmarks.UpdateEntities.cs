// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    [GlobalCleanup(
        Targets =
        [
            nameof(UpdateEntities_Command),
            nameof(UpdateEntities_Dapper),
            nameof(UpdateEntities_DbConnectionPlus)
        ]
    )]
    public void UpdateEntities__Cleanup() =>
        this.connection.Dispose();

    [GlobalSetup(
        Targets =
        [
            nameof(UpdateEntities_Command),
            nameof(UpdateEntities_Dapper),
            nameof(UpdateEntities_DbConnectionPlus)
        ]
    )]
    public void UpdateEntities__Setup() =>
        this.SetupDatabase(UpdateEntities_EntitiesPerOperation);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(UpdateEntities_Category)]
    public void UpdateEntities_Command()
    {
        var updatedEntities = Generate.UpdateFor(this.entitiesInDb);

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

        foreach (var updatedEntity in updatedEntities)
        {
            PopulateEntityParameters(updatedEntity, parameters);

            command.ExecuteNonQuery();
        }
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(UpdateEntities_Category)]
    public void UpdateEntities_Dapper()
    {
        var updatesEntities = Generate.UpdateFor(this.entitiesInDb);

        SqlMapperExtensions.Update(this.connection, updatesEntities);
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(UpdateEntities_Category)]
    public void UpdateEntities_DbConnectionPlus()
    {
        var updatesEntities = Generate.UpdateFor(this.entitiesInDb);

        this.connection.UpdateEntities(updatesEntities);
    }

    private const String UpdateEntities_Category = "UpdateEntities";
    private const Int32 UpdateEntities_EntitiesPerOperation = 100;
}
