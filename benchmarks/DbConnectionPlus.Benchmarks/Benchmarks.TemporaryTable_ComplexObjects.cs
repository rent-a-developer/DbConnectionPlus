// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    [GlobalCleanup(
        Targets =
        [
            nameof(TemporaryTable_ComplexObjects_DbCommand),
            nameof(TemporaryTable_ComplexObjects_DbConnectionPlus)
        ]
    )]
    public void TemporaryTable_ComplexObjects__Cleanup() =>
        this.connection.Dispose();

    [GlobalSetup(
        Targets =
        [
            nameof(TemporaryTable_ComplexObjects_DbCommand),
            nameof(TemporaryTable_ComplexObjects_DbConnectionPlus)
        ]
    )]
    public void TemporaryTable_ComplexObjects__Setup() =>
        this.SetupDatabase(TemporaryTable_ComplexObjects_EntitiesPerOperation);

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(TemporaryTable_ComplexObjects_Category)]
    public List<BenchmarkEntity> TemporaryTable_ComplexObjects_Dapper()
    {
        var entities = Generate.Multiple<BenchmarkEntity>(TemporaryTable_ComplexObjects_EntitiesPerOperation);

        SqlMapper.Execute(this.connection, CreateTempEntitiesTableSql);

        using var insertCommand = this.connection.CreateCommand();
        insertCommand.CommandText = InsertIntoTempEntities;

        var idParameter = new SqliteParameter
        {
            ParameterName = "@Id"
        };

        var booleanValueParameter = new SqliteParameter
        {
            ParameterName = "@BooleanValue"
        };

        var bytesValueParameter = new SqliteParameter
        {
            ParameterName = "@BytesValue"
        };

        var byteValueParameter = new SqliteParameter
        {
            ParameterName = "@ByteValue"
        };

        var charValueParameter = new SqliteParameter
        {
            ParameterName = "@CharValue"
        };

        var dateTimeValueParameter = new SqliteParameter
        {
            ParameterName = "@DateTimeValue"
        };

        var decimalValueParameter = new SqliteParameter
        {
            ParameterName = "@DecimalValue"
        };

        var doubleValueParameter = new SqliteParameter
        {
            ParameterName = "@DoubleValue"
        };

        var enumValueParameter = new SqliteParameter
        {
            ParameterName = "@EnumValue"
        };

        var guidValueParameter = new SqliteParameter
        {
            ParameterName = "@GuidValue"
        };

        var int16ValueParameter = new SqliteParameter
        {
            ParameterName = "@Int16Value"
        };

        var int32ValueParameter = new SqliteParameter
        {
            ParameterName = "@Int32Value"
        };

        var int64ValueParameter = new SqliteParameter
        {
            ParameterName = "@Int64Value"
        };

        var singleValueParameter = new SqliteParameter
        {
            ParameterName = "@SingleValue"
        };

        var stringValueParameter = new SqliteParameter
        {
            ParameterName = "@StringValue"
        };

        var timeSpanValueParameter = new SqliteParameter
        {
            ParameterName = "@TimeSpanValue"
        };

        insertCommand.Parameters.Add(idParameter);
        insertCommand.Parameters.Add(booleanValueParameter);
        insertCommand.Parameters.Add(bytesValueParameter);
        insertCommand.Parameters.Add(byteValueParameter);
        insertCommand.Parameters.Add(charValueParameter);
        insertCommand.Parameters.Add(dateTimeValueParameter);
        insertCommand.Parameters.Add(decimalValueParameter);
        insertCommand.Parameters.Add(doubleValueParameter);
        insertCommand.Parameters.Add(enumValueParameter);
        insertCommand.Parameters.Add(guidValueParameter);
        insertCommand.Parameters.Add(int16ValueParameter);
        insertCommand.Parameters.Add(int32ValueParameter);
        insertCommand.Parameters.Add(int64ValueParameter);
        insertCommand.Parameters.Add(singleValueParameter);
        insertCommand.Parameters.Add(stringValueParameter);
        insertCommand.Parameters.Add(timeSpanValueParameter);

        foreach (var entity in entities)
        {
            idParameter.Value = entity.Id;
            booleanValueParameter.Value = entity.BooleanValue ? 1 : 0;
            bytesValueParameter.Value = entity.BytesValue;
            byteValueParameter.Value = entity.ByteValue;
            charValueParameter.Value = entity.CharValue;
            dateTimeValueParameter.Value = entity.DateTimeValue.ToString(CultureInfo.InvariantCulture);
            decimalValueParameter.Value = entity.DecimalValue.ToString(CultureInfo.InvariantCulture);
            doubleValueParameter.Value = entity.DoubleValue;
            enumValueParameter.Value = entity.EnumValue.ToString();
            guidValueParameter.Value = entity.GuidValue.ToString();
            int16ValueParameter.Value = entity.Int16Value;
            int32ValueParameter.Value = entity.Int32Value;
            int64ValueParameter.Value = entity.Int64Value;
            singleValueParameter.Value = entity.SingleValue;
            stringValueParameter.Value = entity.StringValue;
            timeSpanValueParameter.Value = entity.TimeSpanValue.ToString();

            insertCommand.ExecuteNonQuery();
        }

        var result = SqlMapper.Query<BenchmarkEntity>(this.connection, SelectTempEntitiesSql).ToList();

        SqlMapper.Execute(this.connection, "DROP TABLE temp.Entities");

        return result;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(TemporaryTable_ComplexObjects_Category)]
    public List<BenchmarkEntity> TemporaryTable_ComplexObjects_DbCommand()
    {
        var entities = Generate.Multiple<BenchmarkEntity>(TemporaryTable_ComplexObjects_EntitiesPerOperation);

        var result = new List<BenchmarkEntity>();

        using var createTableCommand = this.connection.CreateCommand();
        createTableCommand.CommandText = CreateTempEntitiesTableSql;
        createTableCommand.ExecuteNonQuery();

        using var insertCommand = this.connection.CreateCommand();
        insertCommand.CommandText = InsertIntoTempEntities;

        var idParameter = new SqliteParameter
        {
            ParameterName = "@Id"
        };

        var booleanValueParameter = new SqliteParameter
        {
            ParameterName = "@BooleanValue"
        };

        var bytesValueParameter = new SqliteParameter
        {
            ParameterName = "@BytesValue"
        };

        var byteValueParameter = new SqliteParameter
        {
            ParameterName = "@ByteValue"
        };

        var charValueParameter = new SqliteParameter
        {
            ParameterName = "@CharValue"
        };

        var dateTimeValueParameter = new SqliteParameter
        {
            ParameterName = "@DateTimeValue"
        };

        var decimalValueParameter = new SqliteParameter
        {
            ParameterName = "@DecimalValue"
        };

        var doubleValueParameter = new SqliteParameter
        {
            ParameterName = "@DoubleValue"
        };

        var enumValueParameter = new SqliteParameter
        {
            ParameterName = "@EnumValue"
        };

        var guidValueParameter = new SqliteParameter
        {
            ParameterName = "@GuidValue"
        };

        var int16ValueParameter = new SqliteParameter
        {
            ParameterName = "@Int16Value"
        };

        var int32ValueParameter = new SqliteParameter
        {
            ParameterName = "@Int32Value"
        };

        var int64ValueParameter = new SqliteParameter
        {
            ParameterName = "@Int64Value"
        };

        var singleValueParameter = new SqliteParameter
        {
            ParameterName = "@SingleValue"
        };

        var stringValueParameter = new SqliteParameter
        {
            ParameterName = "@StringValue"
        };

        var timeSpanValueParameter = new SqliteParameter
        {
            ParameterName = "@TimeSpanValue"
        };

        insertCommand.Parameters.Add(idParameter);
        insertCommand.Parameters.Add(booleanValueParameter);
        insertCommand.Parameters.Add(bytesValueParameter);
        insertCommand.Parameters.Add(byteValueParameter);
        insertCommand.Parameters.Add(charValueParameter);
        insertCommand.Parameters.Add(dateTimeValueParameter);
        insertCommand.Parameters.Add(decimalValueParameter);
        insertCommand.Parameters.Add(doubleValueParameter);
        insertCommand.Parameters.Add(enumValueParameter);
        insertCommand.Parameters.Add(guidValueParameter);
        insertCommand.Parameters.Add(int16ValueParameter);
        insertCommand.Parameters.Add(int32ValueParameter);
        insertCommand.Parameters.Add(int64ValueParameter);
        insertCommand.Parameters.Add(singleValueParameter);
        insertCommand.Parameters.Add(stringValueParameter);
        insertCommand.Parameters.Add(timeSpanValueParameter);

        foreach (var entity in entities)
        {
            idParameter.Value = entity.Id;
            booleanValueParameter.Value = entity.BooleanValue ? 1 : 0;
            bytesValueParameter.Value = entity.BytesValue;
            byteValueParameter.Value = entity.ByteValue;
            charValueParameter.Value = entity.CharValue;
            dateTimeValueParameter.Value = entity.DateTimeValue.ToString(CultureInfo.InvariantCulture);
            decimalValueParameter.Value = entity.DecimalValue.ToString(CultureInfo.InvariantCulture);
            doubleValueParameter.Value = entity.DoubleValue;
            enumValueParameter.Value = entity.EnumValue.ToString();
            guidValueParameter.Value = entity.GuidValue.ToString();
            int16ValueParameter.Value = entity.Int16Value;
            int32ValueParameter.Value = entity.Int32Value;
            int64ValueParameter.Value = entity.Int64Value;
            singleValueParameter.Value = entity.SingleValue;
            stringValueParameter.Value = entity.StringValue;
            timeSpanValueParameter.Value = entity.TimeSpanValue.ToString();

            insertCommand.ExecuteNonQuery();
        }

        using var selectCommand = this.connection.CreateCommand();
        selectCommand.CommandText = SelectTempEntitiesSql;

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
    public List<BenchmarkEntity> TemporaryTable_ComplexObjects_DbConnectionPlus()
    {
        var entities = Generate.Multiple<BenchmarkEntity>(TemporaryTable_ComplexObjects_EntitiesPerOperation);

        return this.connection.Query<BenchmarkEntity>($"SELECT * FROM {TemporaryTable(entities)}").ToList();
    }

    private const String CreateTempEntitiesTableSql = """
                                                      CREATE TEMP TABLE Entities (
                                                          Id INTEGER,
                                                          BytesValue BLOB,
                                                          BooleanValue INTEGER,
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

    private const String SelectTempEntitiesSql = """
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
                                                     temp.Entities
                                                 """;

    private const String TemporaryTable_ComplexObjects_Category = "TemporaryTable_ComplexObjects";
    private const Int32 TemporaryTable_ComplexObjects_EntitiesPerOperation = 250;
}
