// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

using DataRow = RentADeveloper.DbConnectionPlus.Dynamic.DataRow;

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    [GlobalCleanup(
        Targets =
        [
            nameof(Query_Dynamic_Command),
            nameof(Query_Dynamic_Dapper),
            nameof(Query_Dynamic_DbConnectionPlus)
        ]
    )]
    public void Query_Dynamic__Cleanup() =>
        this.connection.Dispose();

    [GlobalSetup(
        Targets =
        [
            nameof(Query_Dynamic_Command),
            nameof(Query_Dynamic_Dapper),
            nameof(Query_Dynamic_DbConnectionPlus)
        ]
    )]
    public void Query_Dynamic__Setup() =>
        this.SetupDatabase(Query_Dynamic_EntitiesPerOperation);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(Query_Dynamic_Category)]
    public List<dynamic> Query_Dynamic_Command()
    {
        var entities = new List<dynamic>();

        using var dataReader = this.connection.ExecuteReader("SELECT * FROM Entity");

        while (dataReader.Read())
        {
            var charBuffer = new Char[1];

            var ordinal = 0;

            var dictionary = new Dictionary<String, Object?>
            {
                ["Id"] = dataReader.GetInt64(ordinal++),
                ["BooleanValue"] = dataReader.GetInt64(ordinal++) == 1,
                ["BytesValue"] = (Byte[])dataReader.GetValue(ordinal++),
                ["ByteValue"] = dataReader.GetByte(ordinal++),
                ["CharValue"] = dataReader.GetChars(ordinal++, 0, charBuffer, 0, 1) == 1
                    ? charBuffer[0]
                    : throw new(),
                ["DateTimeValue"] = DateTime.Parse(dataReader.GetString(ordinal++), CultureInfo.InvariantCulture),
                ["DecimalValue"] = Decimal.Parse(dataReader.GetString(ordinal++), CultureInfo.InvariantCulture),
                ["DoubleValue"] = dataReader.GetDouble(ordinal++),
                ["EnumValue"] = Enum.Parse<TestEnum>(dataReader.GetString(ordinal++)),
                ["GuidValue"] = Guid.Parse(dataReader.GetString(ordinal++)),
                ["Int16Value"] = (Int16)dataReader.GetInt64(ordinal++),
                ["Int32Value"] = (Int32)dataReader.GetInt64(ordinal++),
                ["Int64Value"] = dataReader.GetInt64(ordinal++),
                ["SingleValue"] = dataReader.GetFloat(ordinal++),
                ["StringValue"] = dataReader.GetString(ordinal++),
                ["TimeSpanValue"] = TimeSpan.Parse(dataReader.GetString(ordinal), CultureInfo.InvariantCulture)
            };

            entities.Add(new DataRow(dictionary));
        }

        return entities;
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(Query_Dynamic_Category)]
    public List<dynamic> Query_Dynamic_Dapper() =>
        SqlMapper.Query(this.connection, "SELECT * FROM Entity").ToList();

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(Query_Dynamic_Category)]
    public List<dynamic> Query_Dynamic_DbConnectionPlus() =>
        this.connection.Query("SELECT * FROM Entity").ToList();

    private const String Query_Dynamic_Category = "Query_Dynamic";
    private const Int32 Query_Dynamic_EntitiesPerOperation = 100;
}
