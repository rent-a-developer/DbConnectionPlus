// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    [GlobalCleanup(
        Targets =
        [
            nameof(UpdateEntity_DbCommand),
            nameof(UpdateEntity_Dapper),
            nameof(UpdateEntity_DbConnectionPlus)
        ]
    )]
    public void UpdateEntity__Cleanup() =>
        this.connection.Dispose();

    [GlobalSetup(
        Targets =
        [
            nameof(UpdateEntity_DbCommand),
            nameof(UpdateEntity_Dapper),
            nameof(UpdateEntity_DbConnectionPlus)
        ]
    )]
    public void UpdateEntity__Setup() =>
        this.SetupDatabase(1);

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(UpdateEntity_Category)]
    public void UpdateEntity_Dapper()
    {
        var entity = this.entitiesInDb[0];

        var updatedEntity = Generate.UpdateFor(entity);

        SqlMapperExtensions.Update(this.connection, updatedEntity);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(UpdateEntity_Category)]
    public void UpdateEntity_DbCommand()
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
        command.Parameters.Add(new("@Id", updatedEntity.Id));
        command.Parameters.Add(new("@BooleanValue", updatedEntity.BooleanValue ? 1 : 0));
        command.Parameters.Add(new("@BytesValue", updatedEntity.BytesValue));
        command.Parameters.Add(new("@ByteValue", updatedEntity.ByteValue));
        command.Parameters.Add(new("@CharValue", updatedEntity.CharValue));
        command.Parameters.Add(
            new("@DateTimeValue", updatedEntity.DateTimeValue.ToString(CultureInfo.InvariantCulture))
        );
        command.Parameters.Add(new("@DecimalValue", updatedEntity.DecimalValue.ToString(CultureInfo.InvariantCulture)));
        command.Parameters.Add(new("@DoubleValue", updatedEntity.DoubleValue));
        command.Parameters.Add(new("@EnumValue", updatedEntity.EnumValue.ToString()));
        command.Parameters.Add(new("@GuidValue", updatedEntity.GuidValue.ToString()));
        command.Parameters.Add(new("@Int16Value", updatedEntity.Int16Value));
        command.Parameters.Add(new("@Int32Value", updatedEntity.Int32Value));
        command.Parameters.Add(new("@Int64Value", updatedEntity.Int64Value));
        command.Parameters.Add(new("@SingleValue", updatedEntity.SingleValue));
        command.Parameters.Add(new("@StringValue", updatedEntity.StringValue));
        command.Parameters.Add(new("@TimeSpanValue", updatedEntity.TimeSpanValue.ToString()));

        command.ExecuteNonQuery();
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
