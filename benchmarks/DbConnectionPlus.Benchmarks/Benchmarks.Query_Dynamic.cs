// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

using System.Dynamic;

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    [GlobalCleanup(
        Targets =
        [
            nameof(Query_Dynamic_DbCommand),
            nameof(Query_Dynamic_Dapper),
            nameof(Query_Dynamic_DbConnectionPlus)
        ]
    )]
    public void Query_Dynamic__Cleanup() =>
        this.connection.Dispose();

    [GlobalSetup(
        Targets =
        [
            nameof(Query_Dynamic_DbCommand),
            nameof(Query_Dynamic_Dapper),
            nameof(Query_Dynamic_DbConnectionPlus)
        ]
    )]
    public void Query_Dynamic__Setup() =>
        this.SetupDatabase(Query_Dynamic_EntitiesPerOperation);

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(Query_Dynamic_Category)]
    public List<dynamic> Query_Dynamic_Dapper() =>
        SqlMapper.Query(this.connection, "SELECT * FROM Entity").ToList();

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(Query_Dynamic_Category)]
    public List<dynamic> Query_Dynamic_DbCommand()
    {
        var entities = new List<dynamic>();

        using var dataReader = this.connection.ExecuteReader("SELECT * FROM Entity");

        while (dataReader.Read())
        {
            var charBuffer = new Char[1];

            var ordinal = 0;
            dynamic entity = new ExpandoObject();

            entity.Id = dataReader.GetInt64(ordinal++);
            entity.BooleanValue = dataReader.GetInt64(ordinal++) == 1;
            entity.BytesValue = (Byte[])dataReader.GetValue(ordinal++);
            entity.ByteValue = dataReader.GetByte(ordinal++);
            entity.CharValue = dataReader.GetChars(ordinal++, 0, charBuffer, 0, 1) == 1
                ? charBuffer[0]
                : throw new();
            entity.DateTimeValue = DateTime.Parse(dataReader.GetString(ordinal++), CultureInfo.InvariantCulture);
            entity.DecimalValue = Decimal.Parse(dataReader.GetString(ordinal++), CultureInfo.InvariantCulture);
            entity.DoubleValue = dataReader.GetDouble(ordinal++);
            entity.EnumValue = Enum.Parse<TestEnum>(dataReader.GetString(ordinal++));
            entity.GuidValue = Guid.Parse(dataReader.GetString(ordinal++));
            entity.Int16Value = (Int16)dataReader.GetInt64(ordinal++);
            entity.Int32Value = (Int32)dataReader.GetInt64(ordinal++);
            entity.Int64Value = dataReader.GetInt64(ordinal++);
            entity.SingleValue = dataReader.GetFloat(ordinal++);
            entity.StringValue = dataReader.GetString(ordinal++);
            entity.TimeSpanValue = TimeSpan.Parse(dataReader.GetString(ordinal), CultureInfo.InvariantCulture);

            entities.Add(entity);
        }

        return entities;
    }

    [Benchmark(Baseline = false)]
    [BenchmarkCategory(Query_Dynamic_Category)]
    public List<dynamic> Query_Dynamic_DbConnectionPlus() =>
        this.connection.Query("SELECT * FROM Entity").ToList();

    private const String Query_Dynamic_Category = "Query_Dynamic";
    private const Int32 Query_Dynamic_EntitiesPerOperation = 100;
}
