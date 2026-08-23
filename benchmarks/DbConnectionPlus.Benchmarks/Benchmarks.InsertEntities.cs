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
        this.AssignNextInsertEntitiesIds();

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
            { "StringValue", new("StringValue", null) }
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
    public void InsertEntities_Dapper()
    {
        this.AssignNextInsertEntitiesIds();

        SqlMapperExtensions.Insert(this.connection, this.insertEntities_entitiesToInsert);
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(InsertEntities_Category)]
    public void InsertEntities_DbConnectionPlus()
    {
        this.AssignNextInsertEntitiesIds();

        this.connection.InsertEntities(this.insertEntities_entitiesToInsert);
    }

    // A fresh key per entity, because Id is the primary key and the benchmarks insert the same set of entities
    // over and over into a table that starts out empty. This runs inside the measured region, but it is a few
    // hundred nanoseconds of field writes against an operation of several milliseconds, and all three
    // implementations pay it.
    private void AssignNextInsertEntitiesIds()
    {
        foreach (var entity in this.insertEntities_entitiesToInsert)
        {
            entity.Id = ++this.insertEntities_nextId;
        }
    }

    private readonly List<BenchmarkEntity> insertEntities_entitiesToInsert =
        Generate.Multiple(InsertEntities_EntitiesPerOperation);

    private long insertEntities_nextId;

    private const string InsertEntities_Category = "InsertEntities";
    private const int InsertEntities_EntitiesPerOperation = 200;

    private const string InsertEntitySql = """
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
                                             Int16Value,
                                             Int32Value,
                                             Int64Value,
                                             SingleValue,
                                             StringValue
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
                                             @Int16Value,
                                             @Int32Value,
                                             @Int64Value,
                                             @SingleValue,
                                             @StringValue
                                           )
                                           """;
}
