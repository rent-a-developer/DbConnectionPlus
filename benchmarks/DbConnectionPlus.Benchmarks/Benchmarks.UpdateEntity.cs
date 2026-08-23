// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    private const string UpdateEntity_Category = "UpdateEntity";
    private const int UpdateEntity_UpdatedEntityPoolSize = 64;

    private List<BenchmarkEntity> updateEntity_ModifiedEntitiesPool = null!;
    private int updateEntity_ModifiedEntitiesPoolIndex;

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(UpdateEntity_Category)]
    public void UpdateEntity_Command()
    {
        var updatedEntity = this.UpdateEntity_GetNextModifiedEntity();

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
            { "StringValue", new("StringValue", null) },
        };

        command.Parameters.AddRange(parameters.Values);

        PopulateEntityParameters(updatedEntity, parameters);

        command.ExecuteNonQuery();
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(UpdateEntity_Category)]
    public void UpdateEntity_Dapper() =>
        SqlMapperExtensions.Update(this.connection, this.UpdateEntity_GetNextModifiedEntity());

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(UpdateEntity_Category)]
    public void UpdateEntity_DbConnectionPlus() =>
        this.connection.UpdateEntity(this.UpdateEntity_GetNextModifiedEntity());

    [GlobalCleanup(
        Targets = [nameof(UpdateEntity_Command), nameof(UpdateEntity_Dapper), nameof(UpdateEntity_DbConnectionPlus)]
    )]
    public void UpdateEntity__Cleanup() => this.connection.Dispose();

    [GlobalSetup(
        Targets = [nameof(UpdateEntity_Command), nameof(UpdateEntity_Dapper), nameof(UpdateEntity_DbConnectionPlus)]
    )]
    public void UpdateEntity__Setup()
    {
        this.SetupDatabase(1);

        // Building the updated entity inside the benchmark charged its generation to all three implementations -
        // including the DbCommand baseline and Dapper - which both dominated the measurement and hid changes in
        // DbConnectionPlus behind a baseline that moved with them.
        //
        // A pool rather than a single entity, so that consecutive invocations write different values. Reusing one
        // entity would mean every invocation after the first writes the values that are already stored, which is not
        // what an update does in practice.
        this.updateEntity_ModifiedEntitiesPool =
        [
            .. Enumerable
                .Range(0, UpdateEntity_UpdatedEntityPoolSize)
                .Select(_ => Generate.UpdateFor(this.entitiesInDb[0])),
        ];
    }

    private BenchmarkEntity UpdateEntity_GetNextModifiedEntity()
    {
        this.updateEntity_ModifiedEntitiesPoolIndex =
            (this.updateEntity_ModifiedEntitiesPoolIndex + 1) % UpdateEntity_UpdatedEntityPoolSize;

        return this.updateEntity_ModifiedEntitiesPool[this.updateEntity_ModifiedEntitiesPoolIndex];
    }
}
