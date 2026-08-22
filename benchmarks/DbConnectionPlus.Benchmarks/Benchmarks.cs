using System.Runtime.CompilerServices;
using RentADeveloper.DbConnectionPlus.Configuration;

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

// Note: the *_EntitiesPerOperation and *_OperationsPerInvoke settings size the work done per invocation. They are
// not sized to reach a particular iteration time - BenchmarkDotNet's pilot stage tunes the invocation count for
// that, on whatever machine the suite runs on. They are chosen so that a single invocation is large enough for the
// per-invocation overhead (opening a transaction, in the delete benchmarks) not to dilute the ratios, and no larger.
// See the README next to this file.

[MemoryDiagnoser]
[Config(typeof(BenchmarksConfig))]
public partial class Benchmarks
{
    static Benchmarks() =>
        DbConnectionPlusConfiguration.Instance.UseSqlite();

    public Benchmarks()
    {
        // Dapper.Contrib only ever runs in the JIT job, and touching SqlMapperExtensions at all triggers its static
        // initialization. The guard keeps that out of the Native AOT job, where this constructor still runs for
        // every benchmark - anything that throws in it would take down the DbCommand baseline and DbConnectionPlus
        // too, which have nothing to do with Dapper.
        if (RuntimeFeature.IsDynamicCodeSupported)
        {
            SqlMapperExtensions.TableNameMapper = null;
        }
    }

    private void SetupDatabase(Int32 numberOfEntities)
    {
        this.connection?.Dispose();

        this.connection = new("Data Source=:memory:");
        this.connection.Open();

        using var createEntityTableCommand = this.connection.CreateCommand();
        createEntityTableCommand.CommandText = CreateEntityTableSql;
        createEntityTableCommand.ExecuteNonQuery();

        using var transaction = this.connection.BeginTransaction();

        this.entitiesInDb = Generate.Multiple(numberOfEntities);
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
        parameters["Int16Value"].Value = entity.Int16Value;
        parameters["Int32Value"].Value = entity.Int32Value;
        parameters["Int64Value"].Value = entity.Int64Value;
        parameters["SingleValue"].Value = entity.SingleValue;
        parameters["StringValue"].Value = entity.StringValue;
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
            CharValue = dataReader.GetChars(ordinal++, 0, charBuffer, 0, 1) == 1 ? charBuffer[0] : throw new InvalidOperationException(),
            DateTimeValue = DateTime.Parse(dataReader.GetString(ordinal++), CultureInfo.InvariantCulture),
            DecimalValue = Decimal.Parse(dataReader.GetString(ordinal++), CultureInfo.InvariantCulture),
            DoubleValue = dataReader.GetDouble(ordinal++),
            EnumValue = Enum.Parse<TestEnum>(dataReader.GetString(ordinal++)),
            Int16Value = (Int16)dataReader.GetInt64(ordinal++),
            Int32Value = (Int32)dataReader.GetInt64(ordinal++),
            Int64Value = dataReader.GetInt64(ordinal++),
            SingleValue = dataReader.GetFloat(ordinal++),
            StringValue = dataReader.GetString(ordinal)
        };
    }

    private SqliteConnection connection = null!;
    private List<BenchmarkEntity> entitiesInDb = null!;

    /*
     * INTEGER PRIMARY KEY makes Id an alias for the rowid, so lookups by Id are b-tree descents instead of
     * full table scans. Without it every "WHERE Id = ?" scanned the whole table, and that scan dominated the delete,
     * update, exists and scalar benchmarks and made their results a function of the seeded row count rather than of
     * the code under test.
     */
    private const String CreateEntityTableSql =
        """
        CREATE TABLE Entity
        (
            Id INTEGER PRIMARY KEY,
            BooleanValue INTEGER,
            BytesValue BLOB,
            ByteValue INTEGER,
            CharValue TEXT,
            DateTimeValue TEXT,
            DecimalValue TEXT,
            DoubleValue REAL,
            EnumValue TEXT,
            Int16Value INTEGER,
            Int32Value INTEGER,
            Int64Value INTEGER,
            SingleValue REAL,
            StringValue TEXT
        );
        """;
}
