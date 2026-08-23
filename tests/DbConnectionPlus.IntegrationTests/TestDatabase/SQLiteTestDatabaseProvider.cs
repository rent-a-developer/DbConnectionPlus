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
    public bool CanRetrieveStructureOfTemporaryTables => true;

    /// <inheritdoc />
    public IDatabaseAdapter DatabaseAdapter => new SqliteDatabaseAdapter();

    /// <inheritdoc />
    public string DatabaseCollation => throw new NotImplementedException();

    /// <inheritdoc />
    public string DelayTwoSecondsStatement =>
        """
        WITH RECURSIVE delay(x) AS (
          SELECT 1
          UNION ALL
          SELECT x + 1 FROM delay WHERE x < 5000000
        )
        SELECT x FROM delay WHERE x = 5000000;
        """;

    /// <inheritdoc />
    public bool HasUnsupportedDataType => false;

    /// <inheritdoc />
    public bool SupportsCommandExecutionWhileDataReaderIsOpen => true;

    /// <inheritdoc />
    public bool SupportsDateTimeOffset => true;

    /// <inheritdoc />
    public bool SupportsProperCommandCancellation => false;

    /// <inheritdoc />
    public bool SupportsStoredProcedures => false;

    /// <inheritdoc />
    public bool SupportsStoredProceduresReturningResultSet => false;

    /// <inheritdoc />
    public bool TemporaryTableTextColumnInheritsCollationFromDatabase => true;

    /// <inheritdoc />
    public DbConnection CreateConnection() =>
        this.connection;

    /// <inheritdoc />
    public bool ExistsTemporaryTable(string tableName, DbConnection connection, DbTransaction? transaction = null) =>
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
    public string GetCollationOfTemporaryTableColumn(
        string temporaryTableName,
        string columnName,
        DbConnection connection
    ) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public string GetDataTypeOfTemporaryTableColumn(
        string temporaryTableName,
        string columnName,
        DbConnection connection
    ) =>
        this.connection
            .Query<(int cid, string name, string Type, bool notnull, object dflt_value, int pk)>(
                $"""
                 PRAGMA table_info("{temporaryTableName}");
                 """,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Where(a => a.name == columnName)
            .Select(a => a.Type)
            .Single();

    /// <inheritdoc />
    public string GetUnsupportedDataTypeLiteral() =>
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

    /// <inheritdoc />
    /// <remarks>SQLite runs in-process, in memory, so there is no server and nothing to start.</remarks>
    public static ValueTask StartDatabaseAsync() =>
        default;

    private readonly SqliteConnection connection;

    private bool isDatabasePrepared;

    private const string CreateDatabaseObjectsSql =
        """
        CREATE TABLE Entity
        (
            Id INTEGER,
            BooleanValue INTEGER,
            BytesValue BLOB,
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
            NullableBooleanValue INTEGER NULL,
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
