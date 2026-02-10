// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    [GlobalCleanup(
        Targets =
        [
            nameof(TemporaryTable_ScalarValues_Command),
            nameof(TemporaryTable_ScalarValues_Dapper),
            nameof(TemporaryTable_ScalarValues_DbConnectionPlus)
        ]
    )]
    public void TemporaryTable_ScalarValues__Cleanup() =>
        this.connection.Dispose();

    [GlobalSetup(
        Targets =
        [
            nameof(TemporaryTable_ScalarValues_Command),
            nameof(TemporaryTable_ScalarValues_Dapper),
            nameof(TemporaryTable_ScalarValues_DbConnectionPlus)
        ]
    )]
    public void TemporaryTable_ScalarValues__Setup() =>
        this.SetupDatabase(0);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(TemporaryTable_ScalarValues_Category)]
    public List<Int64> TemporaryTable_ScalarValues_Command()
    {
        using var createTableCommand = this.connection.CreateCommand();
        createTableCommand.CommandText = "CREATE TEMP TABLE \"Values\" (Value INTEGER)";
        createTableCommand.ExecuteNonQuery();

        using var insertCommand = this.connection.CreateCommand();
        insertCommand.CommandText = "INSERT INTO temp.\"Values\" (Value) VALUES (@Value)";

        var valueParameter = new SqliteParameter
        {
            ParameterName = "@Value"
        };

        insertCommand.Parameters.Add(valueParameter);

        foreach (var value in this.temporaryTable_ScalarValues_Values)
        {
            valueParameter.Value = value;

            insertCommand.ExecuteNonQuery();
        }

        using var selectCommand = this.connection.CreateCommand();

        selectCommand.CommandText = "SELECT Value FROM temp.\"Values\"";

        using var dataReader = selectCommand.ExecuteReader();

        var result = new List<Int64>();

        while (dataReader.Read())
        {
            result.Add(dataReader.GetInt64(0));
        }

        using var dropTableCommand = this.connection.CreateCommand();
        dropTableCommand.CommandText = "DROP TABLE temp.\"Values\"";
        dropTableCommand.ExecuteNonQuery();

        return result;
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(TemporaryTable_ScalarValues_Category)]
    public List<Int64> TemporaryTable_ScalarValues_Dapper()
    {
        SqlMapper.Execute(this.connection, "CREATE TEMP TABLE \"Values\" (Value INTEGER)");

        SqlMapperExtensions.TableNameMapper = _ => "temp.\"Values\"";

        SqlMapperExtensions.Insert(
            this.connection,
            this.temporaryTable_ScalarValues_Values.Select(a => new { Value = a })
        );

        var result = SqlMapper.Query<Int64>(this.connection, "SELECT Value FROM temp.\"Values\"").ToList();

        SqlMapper.Execute(this.connection, "DROP TABLE temp.\"Values\"");

        return result;
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(TemporaryTable_ScalarValues_Category)]
    public List<Int64> TemporaryTable_ScalarValues_DbConnectionPlus() =>
        this.connection.Query<Int64>($"SELECT Value FROM {TemporaryTable(this.temporaryTable_ScalarValues_Values)}")
            .ToList();

    private readonly List<Int64> temporaryTable_ScalarValues_Values = Enumerable
        .Range(0, TemporaryTable_ScalarValues_ValuesPerOperation)
        .Select(a => (Int64)a)
        .ToList();

    private const String TemporaryTable_ScalarValues_Category = "TemporaryTable_ScalarValues";
    private const Int32 TemporaryTable_ScalarValues_ValuesPerOperation = 5000;
}
