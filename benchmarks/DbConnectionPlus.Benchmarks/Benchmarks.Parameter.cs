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
        var result = new Int32[5];

        using var dataReader = SqlMapper.ExecuteReader(
            this.connection,
            "SELECT @P1, @P2, @P3, @P4, @P5",
            new
            {
                P1 = 1,
                P2 = 2,
                P3 = 3,
                P4 = 4,
                P5 = 5
            }
        );

        dataReader.Read();

        result[0] = dataReader.GetInt32(0);
        result[1] = dataReader.GetInt32(1);
        result[2] = dataReader.GetInt32(2);
        result[3] = dataReader.GetInt32(3);
        result[4] = dataReader.GetInt32(4);

        return result;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(Parameter_Category)]
    public Object Parameter_DbCommand()
    {
        var result = new Int32[5];

        using var command = this.connection.CreateCommand();
        command.CommandText = "SELECT @P1, @P2, @P3, @P4, @P5";
        command.Parameters.Add(new("@P1", 1));
        command.Parameters.Add(new("@P2", 2));
        command.Parameters.Add(new("@P3", 3));
        command.Parameters.Add(new("@P4", 4));
        command.Parameters.Add(new("@P5", 5));

        using var dataReader = command.ExecuteReader();

        dataReader.Read();

        result[0] = dataReader.GetInt32(0);
        result[1] = dataReader.GetInt32(1);
        result[2] = dataReader.GetInt32(2);
        result[3] = dataReader.GetInt32(3);
        result[4] = dataReader.GetInt32(4);

        return result;
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(Parameter_Category)]
    public Object Parameter_DbConnectionPlus()
    {
        var result = new Int32[5];

        using var dataReader = this.connection.ExecuteReader(
            $"SELECT {Parameter(1)}, {Parameter(2)}, {Parameter(3)}, {Parameter(4)}, {Parameter(5)}"
        );

        dataReader.Read();

        result[0] = dataReader.GetInt32(0);
        result[1] = dataReader.GetInt32(1);
        result[2] = dataReader.GetInt32(2);
        result[3] = dataReader.GetInt32(3);
        result[4] = dataReader.GetInt32(4);

        return result;
    }

    private const String Parameter_Category = "Parameter";
}
