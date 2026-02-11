// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    [GlobalCleanup(
        Targets =
        [
            nameof(Parameter_Command),
            nameof(Parameter_Dapper),
            nameof(Parameter_DbConnectionPlus)
        ]
    )]
    public void Parameter__Cleanup() =>
        this.connection.Dispose();

    [GlobalSetup(
        Targets =
        [
            nameof(Parameter_Command),
            nameof(Parameter_Dapper),
            nameof(Parameter_DbConnectionPlus)
        ]
    )]
    public void Parameter__Setup() =>
        this.SetupDatabase(0);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(Parameter_Category)]
    public Object? Parameter_Command()
    {
        using var command = this.connection.CreateCommand();

        command.CommandText = "SELECT @P1 + @P2 + @P3 + @P4 + @P5 + @P6 + @P7 + @P8 + @P9 + @P10";

        command.Parameters.Add(new("@P1", 1));
        command.Parameters.Add(new("@P2", 2));
        command.Parameters.Add(new("@P3", 3));
        command.Parameters.Add(new("@P4", 4));
        command.Parameters.Add(new("@P5", 5));
        command.Parameters.Add(new("@P6", 6));
        command.Parameters.Add(new("@P7", 7));
        command.Parameters.Add(new("@P8", 8));
        command.Parameters.Add(new("@P9", 9));
        command.Parameters.Add(new("@P10", 10));

        return command.ExecuteScalar();
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(Parameter_Category)]
    public Object? Parameter_Dapper() =>
        SqlMapper.ExecuteScalar(
            this.connection,
            "SELECT @P1 + @P2 + @P3 + @P4 + @P5 + @P6 + @P7 + @P8 + @P9 + @P10",
            new { P1 = 1, P2 = 2, P3 = 3, P4 = 4, P5 = 5, P6 = 6, P7 = 7, P8 = 8, P9 = 9, P10 = 10 }
        );

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(Parameter_Category)]
    public Object Parameter_DbConnectionPlus() =>
        this.connection.ExecuteScalar<Int64>(
            $"""
             SELECT {Parameter(1)} + {Parameter(2)} + {Parameter(3)} + {Parameter(4)} + {Parameter(5)} + 
                    {Parameter(6)} + {Parameter(7)} + {Parameter(8)} + {Parameter(9)} + {Parameter(10)}
             """
        );

    private const String Parameter_Category = "Parameter";
}
