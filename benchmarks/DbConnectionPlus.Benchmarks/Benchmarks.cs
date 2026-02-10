namespace RentADeveloper.DbConnectionPlus.Benchmarks;

// Note: All benchmark settings (i.e. *_EntitiesPerOperation and *_OperationsPerInvoke) are chosen so that each invoke
// takes at least 100 milliseconds to complete on a reasonably fast machine.

[MemoryDiagnoser]
[Config(typeof(BenchmarksConfig))]
public partial class Benchmarks
{
    static Benchmarks()
    {
        SqlMapper.AddTypeHandler(new GuidTypeHandler());
        SqlMapper.AddTypeHandler(new TimeSpanTypeHandler());
    }

    public Benchmarks() =>
        SqlMapperExtensions.TableNameMapper = null;

    private void SetupDatabase(Int32 numberOfEntities)
    {
        this.connection = new("Data Source=:memory:");
        this.connection.Open();

        using var createEntityTableCommand = this.connection.CreateCommand();
        createEntityTableCommand.CommandText = CreateEntityTableSql;
        createEntityTableCommand.ExecuteNonQuery();

        using var transaction = this.connection.BeginTransaction();

        this.entitiesInDb = Generate.Multiple<BenchmarkEntity>(numberOfEntities);
        this.connection.InsertEntities(this.entitiesInDb, transaction);

        transaction.Commit();
    }

    private static void PopulateEntityParameters(BenchmarkEntity entity, Dictionary<String, SqliteParameter> parameters)
    {
        parameters["Id"].Value = entity.Id;
        parameters["BooleanValue"].Value = entity.BooleanValue ? 1 : 0;
        parameters["BytesValue"].Value = entity.BytesValue;
        parameters["ByteValue"].Value = entity.ByteValue;
        parameters["CharValue"].Value = entity.CharValue;
        parameters["DateTimeValue"].Value = entity.DateTimeValue.ToString(CultureInfo.InvariantCulture);
        parameters["DecimalValue"].Value = entity.DecimalValue.ToString(CultureInfo.InvariantCulture);
        parameters["DoubleValue"].Value = entity.DoubleValue;
        parameters["EnumValue"].Value = entity.EnumValue.ToString();
        parameters["GuidValue"].Value = entity.GuidValue.ToString();
        parameters["Int16Value"].Value = entity.Int16Value;
        parameters["Int32Value"].Value = entity.Int32Value;
        parameters["Int64Value"].Value = entity.Int64Value;
        parameters["SingleValue"].Value = entity.SingleValue;
        parameters["StringValue"].Value = entity.StringValue;
        parameters["TimeSpanValue"].Value = entity.TimeSpanValue.ToString();
    }

    private static BenchmarkEntity ReadEntity(IDataReader dataReader)
    {
        var charBuffer = new Char[1];

        var ordinal = 0;

        return new()
        {
            Id = dataReader.GetInt64(ordinal++),
            BooleanValue = dataReader.GetInt64(ordinal++) == 1,
            BytesValue = (Byte[])dataReader.GetValue(ordinal++),
            ByteValue = dataReader.GetByte(ordinal++),
            CharValue = dataReader.GetChars(ordinal++, 0, charBuffer, 0, 1) == 1 ? charBuffer[0] : throw new(),
            DateTimeValue = DateTime.Parse(dataReader.GetString(ordinal++), CultureInfo.InvariantCulture),
            DecimalValue = Decimal.Parse(dataReader.GetString(ordinal++), CultureInfo.InvariantCulture),
            DoubleValue = dataReader.GetDouble(ordinal++),
            EnumValue = Enum.Parse<TestEnum>(dataReader.GetString(ordinal++)),
            GuidValue = Guid.Parse(dataReader.GetString(ordinal++)),
            Int16Value = (Int16)dataReader.GetInt64(ordinal++),
            Int32Value = (Int32)dataReader.GetInt64(ordinal++),
            Int64Value = dataReader.GetInt64(ordinal++),
            SingleValue = dataReader.GetFloat(ordinal++),
            StringValue = dataReader.GetString(ordinal++),
            TimeSpanValue = TimeSpan.Parse(dataReader.GetString(ordinal), CultureInfo.InvariantCulture)
        };
    }

    private SqliteConnection connection = null!;
    private List<BenchmarkEntity> entitiesInDb = null!;

    private const String CreateEntityTableSql =
        """
        CREATE TABLE Entity
        (
            Id INTEGER,
            BooleanValue INTEGER,
            BytesValue BLOB,
            ByteValue INTEGER,
            CharValue TEXT,
            DateTimeValue TEXT,
            DecimalValue TEXT,
            DoubleValue REAL,
            EnumValue TEXT,
            GuidValue TEXT,
            Int16Value INTEGER,
            Int32Value INTEGER,
            Int64Value INTEGER,
            SingleValue REAL,
            StringValue TEXT,
            TimeSpanValue TEXT
        );
        """;
}
