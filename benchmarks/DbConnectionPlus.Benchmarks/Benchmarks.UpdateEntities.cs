// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    [GlobalCleanup(
        Targets =
        [
            nameof(UpdateEntities_DbCommand),
            nameof(UpdateEntities_Dapper),
            nameof(UpdateEntities_DbConnectionPlus)
        ]
    )]
    public void UpdateEntities__Cleanup() =>
        this.connection.Dispose();

    [GlobalSetup(
        Targets =
        [
            nameof(UpdateEntities_DbCommand),
            nameof(UpdateEntities_Dapper),
            nameof(UpdateEntities_DbConnectionPlus)
        ]
    )]
    public void UpdateEntities__Setup() =>
        this.SetupDatabase(UpdateEntities_EntitiesPerOperation);

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(UpdateEntities_Category)]
    public void UpdateEntities_Dapper()
    {
        var updatesEntities = Generate.UpdateFor(this.entitiesInDb);

        SqlMapperExtensions.Update(this.connection, updatesEntities);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(UpdateEntities_Category)]
    public void UpdateEntities_DbCommand()
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

        foreach (var updatedEntity in updatedEntities)
        {
            idParameter.Value = updatedEntity.Id;
            booleanValueParameter.Value = updatedEntity.BooleanValue ? 1 : 0;
            bytesValueParameter.Value = updatedEntity.BytesValue;
            byteValueParameter.Value = updatedEntity.ByteValue;
            charValueParameter.Value = updatedEntity.CharValue;
            dateTimeValueParameter.Value = updatedEntity.DateTimeValue.ToString(CultureInfo.InvariantCulture);
            decimalValueParameter.Value = updatedEntity.DecimalValue.ToString(CultureInfo.InvariantCulture);
            doubleValueParameter.Value = updatedEntity.DoubleValue;
            enumValueParameter.Value = updatedEntity.EnumValue.ToString();
            guidValueParameter.Value = updatedEntity.GuidValue.ToString();
            int16ValueParameter.Value = updatedEntity.Int16Value;
            int32ValueParameter.Value = updatedEntity.Int32Value;
            int64ValueParameter.Value = updatedEntity.Int64Value;
            singleValueParameter.Value = updatedEntity.SingleValue;
            stringValueParameter.Value = updatedEntity.StringValue;
            timeSpanValueParameter.Value = updatedEntity.TimeSpanValue.ToString();

            command.ExecuteNonQuery();
        }
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
