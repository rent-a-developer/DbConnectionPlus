// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    private const string InsertEntity_Category = "InsertEntity";

    private readonly BenchmarkEntity insertEntity_entityToInsert = Generate.Single();

    // A fresh key per invocation, because Id is the primary key and the benchmarks insert the same entity over
    // and over into a table that starts out empty.
    private long insertEntity_nextId;

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(InsertEntity_Category)]
    public void InsertEntity_Command()
    {
        this.AssignNextInsertEntityId();

        using var command = this.connection.CreateCommand();

        command.CommandText = InsertEntitySql;

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

        PopulateEntityParameters(this.insertEntity_entityToInsert, parameters);

        command.ExecuteNonQuery();
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(InsertEntity_Category)]
    public void InsertEntity_Dapper()
    {
        this.AssignNextInsertEntityId();

        SqlMapperExtensions.Insert(this.connection, this.insertEntity_entityToInsert);
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(InsertEntity_Category)]
    public void InsertEntity_DbConnectionPlus()
    {
        this.AssignNextInsertEntityId();

        this.connection.InsertEntity(this.insertEntity_entityToInsert);
    }

    [GlobalCleanup(
        Targets = [nameof(InsertEntity_Command), nameof(InsertEntity_Dapper), nameof(InsertEntity_DbConnectionPlus)]
    )]
    public void InsertEntity__Cleanup() => this.connection.Dispose();

    [GlobalSetup(
        Targets = [nameof(InsertEntity_Command), nameof(InsertEntity_Dapper), nameof(InsertEntity_DbConnectionPlus)]
    )]
    public void InsertEntity__Setup() => this.SetupDatabase(0);

    private void AssignNextInsertEntityId() => this.insertEntity_entityToInsert.Id = ++this.insertEntity_nextId;
}
