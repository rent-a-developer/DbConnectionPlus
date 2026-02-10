// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    [GlobalCleanup(
        Targets =
        [
            nameof(TemporaryTable_ComplexObjects_Command),
            nameof(TemporaryTable_ComplexObjects_Dapper),
            nameof(TemporaryTable_ComplexObjects_DbConnectionPlus)
        ]
    )]
    public void TemporaryTable_ComplexObjects__Cleanup() =>
        this.connection.Dispose();

    [GlobalSetup(
        Targets =
        [
            nameof(TemporaryTable_ComplexObjects_Command),
            nameof(TemporaryTable_ComplexObjects_Dapper),
            nameof(TemporaryTable_ComplexObjects_DbConnectionPlus)
        ]
    )]
    public void TemporaryTable_ComplexObjects__Setup() =>
        this.SetupDatabase(TemporaryTable_ComplexObjects_EntitiesPerOperation);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(TemporaryTable_ComplexObjects_Category)]
    public List<BenchmarkEntity> TemporaryTable_ComplexObjects_Command()
    {
        var result = new List<BenchmarkEntity>();

        using var createTableCommand = this.connection.CreateCommand();
        createTableCommand.CommandText = CreateTempEntitiesTableSql;
        createTableCommand.ExecuteNonQuery();

        using var insertCommand = this.connection.CreateCommand();

        insertCommand.CommandText = InsertIntoTempEntities;

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

        insertCommand.Parameters.AddRange(parameters.Values);

        foreach (var entity in this.temporaryTable_ComplexObjects_Entities)
        {
            PopulateEntityParameters(entity, parameters);

            insertCommand.ExecuteNonQuery();
        }

        using var selectCommand = this.connection.CreateCommand();

        selectCommand.CommandText = "SELECT * FROM temp.Entities";

        using var dataReader = selectCommand.ExecuteReader();

        while (dataReader.Read())
        {
            result.Add(ReadEntity(dataReader));
        }

        using var dropTableCommand = this.connection.CreateCommand();
        dropTableCommand.CommandText = "DROP TABLE temp.Entities";
        dropTableCommand.ExecuteNonQuery();

        return result;
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(TemporaryTable_ComplexObjects_Category)]
    public List<BenchmarkEntity> TemporaryTable_ComplexObjects_Dapper()
    {
        SqlMapper.Execute(this.connection, CreateTempEntitiesTableSql);

        SqlMapperExtensions.TableNameMapper = _ => "temp.Entities";

        SqlMapperExtensions.Insert(this.connection, this.temporaryTable_ComplexObjects_Entities);

        var result = SqlMapper.Query<BenchmarkEntity>(this.connection, "SELECT * FROM temp.Entities").ToList();

        SqlMapper.Execute(this.connection, "DROP TABLE temp.Entities");

        return result;
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(TemporaryTable_ComplexObjects_Category)]
    public List<BenchmarkEntity> TemporaryTable_ComplexObjects_DbConnectionPlus() =>
        this.connection
            .Query<BenchmarkEntity>($"SELECT * FROM {TemporaryTable(this.temporaryTable_ComplexObjects_Entities)}")
            .ToList();

    private readonly List<BenchmarkEntity> temporaryTable_ComplexObjects_Entities =
        Generate.Multiple<BenchmarkEntity>(TemporaryTable_ComplexObjects_EntitiesPerOperation);

    private const String CreateTempEntitiesTableSql = """
                                                      CREATE TEMP TABLE Entities (
                                                          Id INTEGER,
                                                          BooleanValue INTEGER,
                                                          BytesValue BLOB,
                                                          ByteValue INTEGER,
                                                          CharValue TEXT,
                                                          DateTimeValue TEXT,
                                                          DecimalValue TEXT,
                                                          DoubleValue REAL,
                                                          EnumValue TEXT,
                                                          GuidValue TEXT,
                                                          Int16Value INTEGER,
                                                          Int32Value INTEGER,
                                                          Int64Value INTEGER,
                                                          SingleValue REAL,
                                                          StringValue TEXT,
                                                          TimeSpanValue TEXT
                                                      )
                                                      """;

    private const String InsertIntoTempEntities = """
                                                  INSERT INTO temp.Entities (
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
                                                  VALUES (
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

    private const String TemporaryTable_ComplexObjects_Category = "TemporaryTable_ComplexObjects";
    private const Int32 TemporaryTable_ComplexObjects_EntitiesPerOperation = 250;
}
