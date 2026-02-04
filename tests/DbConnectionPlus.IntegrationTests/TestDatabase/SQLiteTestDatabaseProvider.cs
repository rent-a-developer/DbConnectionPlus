// ReSharper disable ParameterHidesMember

using System.Data.Common;
using Microsoft.Data.Sqlite;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.Sqlite;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.TestDatabase;

/// <summary>
/// Provides the test database for SQLite tests.
/// </summary>
public class SqliteTestDatabaseProvider : ITestDatabaseProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteTestDatabaseProvider" /> class.
    /// </summary>
    public SqliteTestDatabaseProvider()
    {
        this.connection = new("Data Source=:memory:");
        this.connection.Open();
    }

    /// <inheritdoc />
    public Boolean CanRetrieveStructureOfTemporaryTables => true;

    /// <inheritdoc />
    public IDatabaseAdapter DatabaseAdapter => new SqliteDatabaseAdapter();

    /// <inheritdoc />
    public String DatabaseCollation => throw new NotImplementedException();

    /// <inheritdoc />
    public String DelayTwoSecondsStatement =>
        """
        WITH RECURSIVE delay(x) AS (
          SELECT 1
          UNION ALL
          SELECT x + 1 FROM delay WHERE x < 5000000
        )
        SELECT x FROM delay WHERE x = 5000000;
        """;

    /// <inheritdoc />
    public Boolean HasUnsupportedDataType => false;

    /// <inheritdoc />
    public Boolean SupportsCommandExecutionWhileDataReaderIsOpen => true;

    /// <inheritdoc />
    public Boolean SupportsDateTimeOffset => true;

    /// <inheritdoc />
    public Boolean SupportsProperCommandCancellation => false;

    /// <inheritdoc />
    public Boolean SupportsStoredProcedures => false;

    /// <inheritdoc />
    public Boolean SupportsStoredProceduresReturningResultSet => false;

    /// <inheritdoc />
    public Boolean TemporaryTableTextColumnInheritsCollationFromDatabase => true;

    /// <inheritdoc />
    public DbConnection CreateConnection() =>
        this.connection;

    /// <inheritdoc />
    public Boolean ExistsTemporaryTable(String tableName, DbConnection connection, DbTransaction? transaction = null) =>
        this.connection.Exists(
            $"""
             SELECT 1
             FROM sqlite_temp_master
             WHERE type = 'table'
             AND name = '{tableName}'
             """,
            transaction,
            cancellationToken: TestContext.Current.CancellationToken
        );

    /// <inheritdoc />
    public String GetCollationOfTemporaryTableColumn(
        String temporaryTableName,
        String columnName,
        DbConnection connection
    ) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public String GetDataTypeOfTemporaryTableColumn(
        String temporaryTableName,
        String columnName,
        DbConnection connection
    ) =>
        this.connection
            .Query<(Int32 cid, String name, String Type, Boolean notnull, Object dflt_value, Int32 pk)>(
                $"""
                 PRAGMA table_info("{temporaryTableName}");
                 """,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Where(a => a.name == columnName)
            .Select(a => a.Type)
            .Single();

    /// <inheritdoc />
    public String GetUnsupportedDataTypeLiteral() =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public void ResetDatabase()
    {
        if (!this.isDatabasePrepared)
        {
            this.connection.ExecuteNonQuery(CreateDatabaseObjectsSql);

            this.isDatabasePrepared = true;
        }
    }

    private readonly SqliteConnection connection;

    private Boolean isDatabasePrepared;

    private const String CreateDatabaseObjectsSql =
        """
        CREATE TABLE Entity
        (
            Id INTEGER,
            BytesValue BLOB,
            BooleanValue INTEGER,
            ByteValue INTEGER,
            CharValue TEXT,
            DateOnlyValue TEXT,
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
            TimeOnlyValue TEXT,
            TimeSpanValue TEXT
        );

        CREATE TABLE EntityWithDateTimeOffset
        (
            Id INTEGER,
            DateTimeOffsetValue TEXT
        );

        CREATE TABLE EntityWithEnumStoredAsString
        (
            Id INTEGER,
            Enum TEXT
        );

        CREATE TABLE EntityWithEnumStoredAsInteger
        (
            Id INTEGER,
            Enum INTEGER
        );

        CREATE TABLE EntityWithNullableProperty
        (
            Id INTEGER NOT NULL,
            Value INTEGER NULL
        );

        CREATE TABLE MappingTestEntity
        (
            Computed INTEGER GENERATED ALWAYS AS (Value+999) VIRTUAL,
            ConcurrencyToken BLOB,
            Identity INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
            Key1 INTEGER NOT NULL,
            Key2 INTEGER NOT NULL,
            Value INTEGER NOT NULL,
            RowVersion BLOB DEFAULT (randomblob(8)),
            NotMapped TEXT NULL
        );

        CREATE TRIGGER TriggerMappingTestEntity
        BEFORE UPDATE ON MappingTestEntity
        FOR EACH ROW
        BEGIN
        	UPDATE MappingTestEntity SET RowVersion = randomblob(8) WHERE Key1 = OLD.Key1 AND Key2 = OLD.Key2;
        END;
        """;
}
