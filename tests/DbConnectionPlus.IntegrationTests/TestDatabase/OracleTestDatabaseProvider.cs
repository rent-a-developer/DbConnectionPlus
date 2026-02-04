using System.Data.Common;
using Oracle.ManagedDataAccess.Client;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.Oracle;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.TestDatabase;

/// <summary>
/// Provides the test database for Oracle tests.
/// </summary>
public class OracleTestDatabaseProvider : ITestDatabaseProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OracleTestDatabaseProvider" /> class.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The environment variable 'ConnectionString_Oracle' is not set.
    /// </exception>
    static OracleTestDatabaseProvider()
    {
        connectionString = Environment.GetEnvironmentVariable(ConnectionStringKey)?.Trim()!;

        if (String.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"The environment variable '{ConnectionStringKey}' is not set!");
        }

        Console.Out.WriteLine("Using the following connection string for Oracle:");
        Console.Out.WriteLine(connectionString);
    }

    /// <inheritdoc />
    public Boolean CanRetrieveStructureOfTemporaryTables => false;

    /// <inheritdoc />
    public IDatabaseAdapter DatabaseAdapter => new OracleDatabaseAdapter();

    /// <inheritdoc />
    public String DatabaseCollation => throw new NotImplementedException();

    /// <inheritdoc />
    public String DelayTwoSecondsStatement => "BEGIN DBMS_LOCK.SLEEP(2); END;";

    /// <inheritdoc />
    public Boolean HasUnsupportedDataType => false;

    /// <inheritdoc />
    public Boolean SupportsCommandExecutionWhileDataReaderIsOpen => true;

    /// <inheritdoc />
    public Boolean SupportsDateTimeOffset => true;

    /// <inheritdoc />
    public Boolean SupportsProperCommandCancellation => false;

    /// <inheritdoc />
    public Boolean SupportsStoredProcedures => true;

    /// <inheritdoc />
    public Boolean SupportsStoredProceduresReturningResultSet => false;

    /// <inheritdoc />
    public Boolean TemporaryTableTextColumnInheritsCollationFromDatabase => true;

    /// <inheritdoc />
    public DbConnection CreateConnection()
    {
        var connection = new OracleConnection(connectionString);

        // Clear the connection we got from the pool, so that its session actually ends.
        // Otherwise, Oracle will keep temporary tables alive for that session and we will eventually run out of them.
        OracleConnection.ClearPool(connection);

        connection.Open();
        return connection;
    }

    /// <inheritdoc />
    public Boolean ExistsTemporaryTable(String tableName, DbConnection connection, DbTransaction? transaction = null)
    {
        var quoteTemporaryTableName = this.DatabaseAdapter.QuoteTemporaryTableName(tableName, connection);
        var unquotedTemporaryTableName = quoteTemporaryTableName[1..^1]; // Strip the quotes (").

        return connection.Exists(
            $"SELECT * FROM USER_PRIVATE_TEMP_TABLES WHERE TABLE_NAME = {Parameter(unquotedTemporaryTableName)}"
        );
    }

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
        throw new NotImplementedException();

    /// <inheritdoc />
    public String GetUnsupportedDataTypeLiteral() =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public void ResetDatabase()
    {
        using var connection = new OracleConnection(connectionString);
        connection.Open();

        if (!isDatabasePrepared)
        {
            ExecuteScript(connection, DropDatabaseObjectsSql);
            ExecuteScript(connection, CreateDatabaseObjectsSql);

            isDatabasePrepared = true;
        }

        ExecuteScript(connection, PurgeTablesSql);
    }

    private static void ExecuteScript(OracleConnection connection, String script)
    {
        var statements = script
            .Split("GO", StringSplitOptions.RemoveEmptyEntries)
            .Where(a => !String.IsNullOrWhiteSpace(a.Trim()));

        foreach (var statement in statements)
        {
            connection.ExecuteNonQuery(statement);
        }
    }

    private const String ConnectionStringKey = "ConnectionString_Oracle";

    private const String CreateDatabaseObjectsSql =
        """
        CREATE TABLE "Entity"
        (
            "Id" NUMBER(19) NOT NULL PRIMARY KEY,
            "BytesValue" RAW(2000),
            "BooleanValue" NUMBER(1),
            "ByteValue" NUMBER(3),
            "CharValue" CHAR(1),
            "DateOnlyValue" DATE,
            "DateTimeValue" TIMESTAMP,
            "DecimalValue" NUMBER,
            "DoubleValue" BINARY_DOUBLE,
            "EnumValue" NVARCHAR2(200),
            "GuidValue" RAW(16),
            "Int16Value" NUMBER(5),
            "Int32Value" NUMBER(10),
            "Int64Value" NUMBER(19),
            "SingleValue" BINARY_FLOAT,
            "StringValue" NVARCHAR2(2000),
            "TimeOnlyValue" INTERVAL DAY TO SECOND,
            "TimeSpanValue" INTERVAL DAY TO SECOND
        );
        GO

        CREATE TABLE "EntityWithDateTimeOffset"
        (
            "Id" NUMBER(19) NOT NULL PRIMARY KEY,
            "DateTimeOffsetValue" TIMESTAMP WITH TIME ZONE NULL
        );
        GO

        CREATE TABLE "EntityWithEnumStoredAsString"
        (
            "Id" NUMBER(19) NOT NULL PRIMARY KEY,
            "Enum" NVARCHAR2(200) NULL
        );
        GO

        CREATE TABLE "EntityWithEnumStoredAsInteger"
        (
            "Id" NUMBER(19) NOT NULL PRIMARY KEY,
            "Enum" INT NULL
        );
        GO

        CREATE TABLE "EntityWithNullableProperty"
        (
            "Id" NUMBER(19) NOT NULL PRIMARY KEY,
            "Value" NUMBER(19) NULL
        );
        GO

        CREATE TABLE "MappingTestEntity"
        (
            "Computed" GENERATED ALWAYS AS (("Value"+999)),
            "ConcurrencyToken" RAW(2000),
            "Identity" NUMBER(10) GENERATED ALWAYS AS IDENTITY(START with 1 INCREMENT by 1),
            "Key1" NUMBER(19) NOT NULL,
            "Key2" NUMBER(19) NOT NULL,
            "Value" NUMBER(10) NOT NULL,
            "NotMapped" CLOB NULL,
            "RowVersion" RAW(16),
            PRIMARY KEY ("Key1", "Key2")
        );
        GO

        CREATE OR REPLACE TRIGGER "TriggerMappingTestEntity"
        BEFORE INSERT OR UPDATE ON "MappingTestEntity"
        FOR EACH ROW
        BEGIN
          :NEW."RowVersion" := SYS_GUID();
        END;
        GO

        CREATE OR REPLACE NONEDITIONABLE PROCEDURE "DeleteAllEntities" AS
        BEGIN
            DELETE FROM "Entity";
        END;
        GO

        """;

    private const String DropDatabaseObjectsSql =
        """
        DROP TABLE IF EXISTS "Entity" PURGE;
        GO

        DROP TABLE IF EXISTS "EntityWithDateTimeOffset" PURGE;
        GO

        DROP TABLE IF EXISTS "EntityWithEnumStoredAsString" PURGE;
        GO

        DROP TABLE IF EXISTS "EntityWithEnumStoredAsInteger" PURGE;
        GO

        DROP TABLE IF EXISTS "EntityWithNullableProperty" PURGE;
        GO

        DROP TABLE IF EXISTS "MappingTestEntity" PURGE;
        GO

        DROP PROCEDURE IF EXISTS "DeleteAllEntities";
        GO
        """;

    private const String PurgeTablesSql =
        """
        TRUNCATE TABLE "Entity";
        GO

        TRUNCATE TABLE "EntityWithDateTimeOffset";
        GO

        TRUNCATE TABLE "EntityWithEnumStoredAsString";
        GO

        TRUNCATE TABLE "EntityWithEnumStoredAsInteger";
        GO

        TRUNCATE TABLE "EntityWithNullableProperty";
        GO

        TRUNCATE TABLE "MappingTestEntity";
        GO
        """;

    private static readonly String connectionString;

    private static Boolean isDatabasePrepared;
}
