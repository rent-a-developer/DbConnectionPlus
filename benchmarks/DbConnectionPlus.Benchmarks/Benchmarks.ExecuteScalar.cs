// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    [GlobalCleanup(
        Targets =
        [
            nameof(ExecuteScalar_DbCommand),
            nameof(ExecuteScalar_Dapper),
            nameof(ExecuteScalar_DbConnectionPlus)
        ]
    )]
    public void ExecuteScalar__Cleanup() =>
        this.connection.Dispose();

    [GlobalSetup(
        Targets =
        [
            nameof(ExecuteScalar_DbCommand),
            nameof(ExecuteScalar_Dapper),
            nameof(ExecuteScalar_DbConnectionPlus)
        ]
    )]
    public void ExecuteScalar__Setup() =>
        this.SetupDatabase(1);

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(ExecuteScalar_Category)]
    public String ExecuteScalar_Dapper()
    {
        var entity = this.entitiesInDb[0];

        return SqlMapper.ExecuteScalar<String>(
            this.connection,
            "SELECT StringValue FROM Entity WHERE Id = @Id",
            new { entity.Id }
        )!;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(ExecuteScalar_Category)]
    public String ExecuteScalar_DbCommand()
    {
        var entity = this.entitiesInDb[0];

        using var command = this.connection.CreateCommand();

        command.CommandText = "SELECT StringValue FROM Entity WHERE Id = @Id";
        command.Parameters.Add(new("@Id", entity.Id));

        return (String)command.ExecuteScalar()!;
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(ExecuteScalar_Category)]
    public String ExecuteScalar_DbConnectionPlus()
    {
        var entity = this.entitiesInDb[0];

        return this.connection.ExecuteScalar<String>(
            $"SELECT StringValue FROM Entity WHERE Id = {Parameter(entity.Id)}"
        );
    }

    private const String ExecuteScalar_Category = "ExecuteScalar";
}
