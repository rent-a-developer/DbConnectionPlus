// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    [GlobalCleanup(
        Targets =
        [
            nameof(InsertEntity_DbCommand),
            nameof(InsertEntity_Dapper),
            nameof(InsertEntity_DbConnectionPlus)
        ]
    )]
    public void InsertEntity__Cleanup() =>
        this.connection.Dispose();

    [GlobalSetup(
        Targets =
        [
            nameof(InsertEntity_DbCommand),
            nameof(InsertEntity_Dapper),
            nameof(InsertEntity_DbConnectionPlus)
        ]
    )]
    public void InsertEntity__Setup() =>
        this.SetupDatabase(0);

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(InsertEntity_Category)]
    public void InsertEntity_Dapper()
    {
        var entity = Generate.Single<BenchmarkEntity>();

        SqlMapperExtensions.Insert(this.connection, entity);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(InsertEntity_Category)]
    public void InsertEntity_DbCommand()
    {
        var entity = Generate.Single<BenchmarkEntity>();

        using var command = this.connection.CreateCommand();

        command.CommandText = InsertEntitySql;

        command.Parameters.Add(new("@Id", entity.Id));
        command.Parameters.Add(new("@BooleanValue", entity.BooleanValue ? 1 : 0));
        command.Parameters.Add(new("@BytesValue", entity.BytesValue));
        command.Parameters.Add(new("@ByteValue", entity.ByteValue));
        command.Parameters.Add(new("@CharValue", entity.CharValue));
        command.Parameters.Add(new("@DateTimeValue", entity.DateTimeValue.ToString(CultureInfo.InvariantCulture)));
        command.Parameters.Add(new("@DecimalValue", entity.DecimalValue));
        command.Parameters.Add(new("@DoubleValue", entity.DoubleValue));
        command.Parameters.Add(new("@EnumValue", entity.EnumValue.ToString()));
        command.Parameters.Add(new("@GuidValue", entity.GuidValue.ToString()));
        command.Parameters.Add(new("@Int16Value", entity.Int16Value));
        command.Parameters.Add(new("@Int32Value", entity.Int32Value));
        command.Parameters.Add(new("@Int64Value", entity.Int64Value));
        command.Parameters.Add(new("@SingleValue", entity.SingleValue));
        command.Parameters.Add(new("@StringValue", entity.StringValue));
        command.Parameters.Add(new("@TimeSpanValue", entity.TimeSpanValue.ToString()));

        command.ExecuteNonQuery();
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(InsertEntity_Category)]
    public void InsertEntity_DbConnectionPlus()
    {
        var entity = Generate.Single<BenchmarkEntity>();

        this.connection.InsertEntity(entity);
    }

    private const String InsertEntity_Category = "InsertEntity";
}
