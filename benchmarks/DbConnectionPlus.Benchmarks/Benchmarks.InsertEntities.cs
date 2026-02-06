// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    [GlobalCleanup(
        Targets =
        [
            nameof(InsertEntities_DbCommand),
            nameof(InsertEntities_Dapper),
            nameof(InsertEntities_DbConnectionPlus)
        ]
    )]
    public void InsertEntities__Cleanup() =>
        this.connection.Dispose();

    [GlobalSetup(
        Targets =
        [
            nameof(InsertEntities_DbCommand),
            nameof(InsertEntities_Dapper),
            nameof(InsertEntities_DbConnectionPlus)
        ]
    )]
    public void InsertEntities__Setup() =>
        this.SetupDatabase(0);

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(InsertEntities_Category)]
    public void InsertEntities_Dapper()
    {
        var entitiesToInsert = Generate.Multiple<BenchmarkEntity>(InsertEntities_EntitiesPerOperation);

        SqlMapperExtensions.Insert(this.connection, entitiesToInsert);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(InsertEntities_Category)]
    public void InsertEntities_DbCommand()
    {
        var entities = Generate.Multiple<BenchmarkEntity>(InsertEntities_EntitiesPerOperation);

        using var command = this.connection.CreateCommand();
        command.CommandText = InsertEntitySql;

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

        command.Parameters.Add(idParameter);
        command.Parameters.Add(booleanValueParameter);
        command.Parameters.Add(bytesValueParameter);
        command.Parameters.Add(byteValueParameter);
        command.Parameters.Add(charValueParameter);
        command.Parameters.Add(dateTimeValueParameter);
        command.Parameters.Add(decimalValueParameter);
        command.Parameters.Add(doubleValueParameter);
        command.Parameters.Add(enumValueParameter);
        command.Parameters.Add(guidValueParameter);
        command.Parameters.Add(int16ValueParameter);
        command.Parameters.Add(int32ValueParameter);
        command.Parameters.Add(int64ValueParameter);
        command.Parameters.Add(singleValueParameter);
        command.Parameters.Add(stringValueParameter);
        command.Parameters.Add(timeSpanValueParameter);

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

            command.ExecuteNonQuery();
        }
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(InsertEntities_Category)]
    public void InsertEntities_DbConnectionPlus()
    {
        var entitiesToInsert = Generate.Multiple<BenchmarkEntity>(InsertEntities_EntitiesPerOperation);

        this.connection.InsertEntities(entitiesToInsert);
    }

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
