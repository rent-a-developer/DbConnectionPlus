// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    [GlobalCleanup(
        Targets =
        [
            nameof(Parameter_DbCommand),
            nameof(Parameter_Dapper),
            nameof(Parameter_DbConnectionPlus)
        ]
    )]
    public void Parameter__Cleanup() =>
        this.connection.Dispose();

    [GlobalSetup(
        Targets =
        [
            nameof(Parameter_DbCommand),
            nameof(Parameter_Dapper),
            nameof(Parameter_DbConnectionPlus)
        ]
    )]
    public void Parameter__Setup() =>
        this.SetupDatabase(0);

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(Parameter_Category)]
    public Object Parameter_Dapper()
    {
        var result = new List<Object>();

        using var dataReader = SqlMapper.ExecuteReader(
            this.connection,
            "SELECT @P1, @P2, @P3, @P4, @P5",
            new
            {
                P1 = 1,
                P2 = "Test",
                P3 = DateTime.UtcNow,
                P4 = Guid.NewGuid(),
                P5 = true
            }
        );

        dataReader.Read();

        result.Add((Int32)dataReader.GetInt64(0));
        result.Add(dataReader.GetString(1));
        result.Add(dataReader.GetDateTime(2));
        result.Add(dataReader.GetGuid(3));
        result.Add(dataReader.GetBoolean(4));

        return result;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(Parameter_Category)]
    public Object Parameter_DbCommand()
    {
        var result = new List<Object>();

        using var command = this.connection.CreateCommand();
        command.CommandText = "SELECT @P1, @P2, @P3, @P4, @P5";
        command.Parameters.Add(new("@P1", 1));
        command.Parameters.Add(new("@P2", "Test"));
        command.Parameters.Add(new("@P3", DateTime.UtcNow));
        command.Parameters.Add(new("@P4", Guid.NewGuid()));
        command.Parameters.Add(new("@P5", true));

        using var dataReader = command.ExecuteReader();

        dataReader.Read();

        result.Add((Int32)dataReader.GetInt64(0));
        result.Add(dataReader.GetString(1));
        result.Add(dataReader.GetDateTime(2));
        result.Add(dataReader.GetGuid(3));
        result.Add(dataReader.GetBoolean(4));

        return result;
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(Parameter_Category)]
    public Object Parameter_DbConnectionPlus()
    {
        var result = new List<Object>();

        using var dataReader = this.connection.ExecuteReader(
            $"""
             SELECT {Parameter(1)},
                 {Parameter("Test")},
                 {Parameter(DateTime.UtcNow)},
                 {Parameter(Guid.NewGuid())},
                 {Parameter(true)}
             """
        );

        dataReader.Read();

        result.Add((Int32)dataReader.GetInt64(0));
        result.Add(dataReader.GetString(1));
        result.Add(dataReader.GetDateTime(2));
        result.Add(dataReader.GetGuid(3));
        result.Add(dataReader.GetBoolean(4));

        return result;
    }

    private const String Parameter_Category = "Parameter";
}
