// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    [GlobalCleanup(
        Targets =
        [
            nameof(TemporaryTable_ScalarValues_DbCommand),
            nameof(TemporaryTable_ScalarValues_DbConnectionPlus)
        ]
    )]
    public void TemporaryTable_ScalarValues__Cleanup() =>
        this.connection.Dispose();

    [GlobalSetup(
        Targets =
        [
            nameof(TemporaryTable_ScalarValues_DbCommand),
            nameof(TemporaryTable_ScalarValues_DbConnectionPlus)
        ]
    )]
    public void TemporaryTable_ScalarValues__Setup() =>
        this.SetupDatabase(0);

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(TemporaryTable_ScalarValues_Category)]
    public List<String> TemporaryTable_ScalarValues_Dapper()
    {
        var scalarValues = Enumerable
            .Range(0, TemporaryTable_ScalarValues_ValuesPerOperation)
            .Select(a => a.ToString(CultureInfo.InvariantCulture))
            .ToList();

        SqlMapper.Execute(this.connection, "CREATE TEMP TABLE \"Values\" (Value TEXT)");

        using var insertCommand = this.connection.CreateCommand();
        insertCommand.CommandText = "INSERT INTO temp.\"Values\" (Value) VALUES (@Value)";

        var valueParameter = new SqliteParameter
        {
            ParameterName = "@Value"
        };

        insertCommand.Parameters.Add(valueParameter);

        foreach (var value in scalarValues)
        {
            valueParameter.Value = value;

            insertCommand.ExecuteNonQuery();
        }

        var result = SqlMapper.Query<String>(this.connection, "SELECT Value FROM temp.\"Values\"").ToList();

        SqlMapper.Execute(this.connection, "DROP TABLE temp.\"Values\"");

        return result;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(TemporaryTable_ScalarValues_Category)]
    public List<String> TemporaryTable_ScalarValues_DbCommand()
    {
        var scalarValues = Enumerable
            .Range(0, TemporaryTable_ScalarValues_ValuesPerOperation)
            .Select(a => a.ToString(CultureInfo.InvariantCulture))
            .ToList();

        var result = new List<String>();

        using var createTableCommand = this.connection.CreateCommand();
        createTableCommand.CommandText = "CREATE TEMP TABLE \"Values\" (Value TEXT)";
        createTableCommand.ExecuteNonQuery();

        using var insertCommand = this.connection.CreateCommand();
        insertCommand.CommandText = "INSERT INTO temp.\"Values\" (Value) VALUES (@Value)";

        var valueParameter = new SqliteParameter
        {
            ParameterName = "@Value"
        };

        insertCommand.Parameters.Add(valueParameter);

        foreach (var value in scalarValues)
        {
            valueParameter.Value = value;

            insertCommand.ExecuteNonQuery();
        }

        using var selectCommand = this.connection.CreateCommand();
        selectCommand.CommandText = "SELECT Value FROM temp.\"Values\"";

        using var dataReader = selectCommand.ExecuteReader();

        while (dataReader.Read())
        {
            result.Add(dataReader.GetString(0));
        }

        using var dropTableCommand = this.connection.CreateCommand();
        dropTableCommand.CommandText = "DROP TABLE temp.\"Values\"";
        dropTableCommand.ExecuteNonQuery();

        return result;
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(TemporaryTable_ScalarValues_Category)]
    public List<String> TemporaryTable_ScalarValues_DbConnectionPlus()
    {
        var scalarValues = Enumerable
            .Range(0, TemporaryTable_ScalarValues_ValuesPerOperation)
            .Select(a => a.ToString(CultureInfo.InvariantCulture))
            .ToList();

        return this.connection.Query<String>($"SELECT Value FROM {TemporaryTable(scalarValues)}").ToList();
    }

    private const String TemporaryTable_ScalarValues_Category = "TemporaryTable_ScalarValues";
    private const Int32 TemporaryTable_ScalarValues_ValuesPerOperation = 5000;
}
