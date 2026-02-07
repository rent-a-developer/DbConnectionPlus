// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    [GlobalCleanup(
        Targets =
        [
            nameof(Exists_DbCommand),
            nameof(Exists_Dapper),
            nameof(Exists_DbConnectionPlus)
        ]
    )]
    public void Exists__Cleanup() =>
        this.connection.Dispose();

    [GlobalSetup(
        Targets =
        [
            nameof(Exists_DbCommand),
            nameof(Exists_Dapper),
            nameof(Exists_DbConnectionPlus)
        ]
    )]
    public void Exists__Setup() =>
        this.SetupDatabase(1);

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(Exists_Category)]
    public Boolean Exists_Dapper()
    {
        var entityId = this.entitiesInDb[0].Id;

        using var dataReader = SqlMapper.ExecuteReader(
            this.connection,
            "SELECT 1 FROM Entity WHERE Id = @Id",
            new { Id = entityId }
        );

        return dataReader.Read();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(Exists_Category)]
    public Boolean Exists_DbCommand()
    {
        var entityId = this.entitiesInDb[0].Id;

        using var command = this.connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM Entity WHERE Id = @Id";
        command.Parameters.Add(new("@Id", entityId));

        using var dataReader = command.ExecuteReader();

        return dataReader.HasRows;
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(Exists_Category)]
    public Boolean Exists_DbConnectionPlus()
    {
        var entityId = this.entitiesInDb[0].Id;

        return this.connection.Exists($"SELECT 1 FROM Entity WHERE Id = {Parameter(entityId)}");
    }

    private const String Exists_Category = "Exists";
}
