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
    public void UpdateEntities__Setup()
    {
        this.SetupDatabase(UpdateEntities_EntitiesPerOperation);

        // See the note on UpdateEntity__Setup: generating the updated entities inside the benchmark charged their
        // generation to all three implementations, and a single pre-generated set would make every invocation after
        // the first write the values that are already stored.
        this.updateEntities_ModifiedEntitiesPool =
        [
            .. Enumerable
                .Range(0, UpdateEntities_UpdatedEntitiesPoolSize)
                .Select(_ => Generate.UpdatesFor(this.entitiesInDb))
        ];
    }

    private List<BenchmarkEntity> UpdateEntities_GetNextModifiedEntities()
    {
        this.updateEntities_ModifiedEntitiesPoolIndex = (this.updateEntities_ModifiedEntitiesPoolIndex + 1) % UpdateEntities_UpdatedEntitiesPoolSize;

        return this.updateEntities_ModifiedEntitiesPool[this.updateEntities_ModifiedEntitiesPoolIndex];
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(UpdateEntities_Category)]
    public void UpdateEntities_Command()
    {
        var updatedEntities = this.UpdateEntities_GetNextModifiedEntities();

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
                                        Int16Value = @Int16Value,
                                        Int32Value = @Int32Value,
                                        Int64Value = @Int64Value,
                                        SingleValue = @SingleValue,
                                        StringValue = @StringValue
                              WHERE     Id = @Id
                              """;

        var parameters = new Dictionary<string, SqliteParameter>
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
            { "Int16Value", new("Int16Value", null) },
            { "Int32Value", new("Int32Value", null) },
            { "Int64Value", new("Int64Value", null) },
            { "SingleValue", new("SingleValue", null) },
            { "StringValue", new("StringValue", null) }
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
    public void UpdateEntities_Dapper() =>
        SqlMapperExtensions.Update(this.connection, this.UpdateEntities_GetNextModifiedEntities());

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(UpdateEntities_Category)]
    public void UpdateEntities_DbConnectionPlus() =>
        this.connection.UpdateEntities(this.UpdateEntities_GetNextModifiedEntities());

    private List<List<BenchmarkEntity>> updateEntities_ModifiedEntitiesPool = null!;
    private int updateEntities_ModifiedEntitiesPoolIndex;

    private const string UpdateEntities_Category = "UpdateEntities";
    private const int UpdateEntities_EntitiesPerOperation = 100;
    private const int UpdateEntities_UpdatedEntitiesPoolSize = 8;
}
