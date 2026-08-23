using System.Data.Common;
using Oracle.ManagedDataAccess.Client;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.Oracle;
using RentADeveloper.DbConnectionPlus.IntegrationTests.TestDatabase.Containers;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.TestDatabase;

/// <summary>
/// Provides the test database for Oracle tests.
/// </summary>
public class OracleTestDatabaseProvider : ITestDatabaseProvider
{
    /// <inheritdoc />
    public bool CanRetrieveStructureOfTemporaryTables => false;

    /// <inheritdoc />
    public IDatabaseAdapter DatabaseAdapter => new OracleDatabaseAdapter();

    /// <inheritdoc />
    public string DatabaseCollation => throw new NotImplementedException();

    /// <inheritdoc />
    public string DelayTwoSecondsStatement => "BEGIN DBMS_LOCK.SLEEP(2); END;";

    /// <inheritdoc />
    public bool HasUnsupportedDataType => false;

    /// <inheritdoc />
    public bool SupportsCommandExecutionWhileDataReaderIsOpen => true;

    /// <inheritdoc />
    public bool SupportsDateTimeOffset => true;

    /// <inheritdoc />
    public bool SupportsProperCommandCancellation => false;

    /// <inheritdoc />
    public bool SupportsStoredProcedures => true;

    /// <inheritdoc />
    public bool SupportsStoredProceduresReturningResultSet => false;

    /// <inheritdoc />
    public bool TemporaryTableTextColumnInheritsCollationFromDatabase => true;

    /// <inheritdoc />
    public DbConnection CreateConnection()
    {
        var connection = new OracleConnection(ConnectionString);

        // Clear the connection we got from the pool, so that its session actually ends.
        // Otherwise, Oracle will keep temporary tables alive for that session and we will eventually run out of them.
        OracleConnection.ClearPool(connection);

        connection.Open();
        return connection;
    }

    /// <inheritdoc />
    public bool ExistsTemporaryTable(string tableName, DbConnection connection, DbTransaction? transaction = null)
    {
        var quoteTemporaryTableName = this.DatabaseAdapter.QuoteTemporaryTableName(tableName, connection);
        var unquotedTemporaryTableName = quoteTemporaryTableName[1..^1]; // Strip the quotes (").

        return connection.Exists(
            $"SELECT * FROM USER_PRIVATE_TEMP_TABLES WHERE TABLE_NAME = {Parameter(unquotedTemporaryTableName)}"
        );
    }

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
        throw new NotImplementedException();

    /// <inheritdoc />
    public string GetUnsupportedDataTypeLiteral() =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public void ResetDatabase()
    {
        using var connection = new OracleConnection(ConnectionString);
        connection.Open();

        if (!isDatabasePrepared)
        {
            ExecuteScript(connection, DropDatabaseObjectsSql);
            ExecuteScript(connection, CreateDatabaseObjectsSql);

            isDatabasePrepared = true;
        }

        ExecuteScript(connection, PurgeTablesSql);
    }

    /// <inheritdoc />
    public static ValueTask StartDatabaseAsync() =>
        TestDatabaseContainers.StartOracleAsync();

    /// <summary>
    /// The connection string that connects to the Oracle server running in the test container.
    /// </summary>
    private static string ConnectionString =>
        TestDatabaseContainers.Oracle.ConnectionString;

    private static void ExecuteScript(OracleConnection connection, string script)
    {
        var statements = script
            .Split("GO", StringSplitOptions.RemoveEmptyEntries)
            .Where(a => !string.IsNullOrWhiteSpace(a.Trim()));

        foreach (var statement in statements)
        {
            connection.ExecuteNonQuery(statement);
        }
    }

    private const string CreateDatabaseObjectsSql =
        """
        CREATE TABLE "Entity"
        (
            "Id" NUMBER(19) NOT NULL PRIMARY KEY,
            "BooleanValue" NUMBER(1),
            "BytesValue" RAW(2000),
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
            "NullableBooleanValue" NUMBER(1) NULL,
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

    private const string DropDatabaseObjectsSql =
        """
        DROP TABLE IF EXISTS "Entity" PURGE;
        GO

        DROP TABLE IF EXISTS "EntityWithDateTimeOffset" PURGE;
        GO

        DROP TABLE IF EXISTS "EntityWithEnumStoredAsString" PURGE;
        GO

        DROP TABLE IF EXISTS "EntityWithEnumStoredAsInteger" PURGE;
        GO

        DROP TABLE IF EXISTS "MappingTestEntity" PURGE;
        GO

        DROP PROCEDURE IF EXISTS "DeleteAllEntities";
        GO
        """;

    private const string PurgeTablesSql =
        """
        TRUNCATE TABLE "Entity";
        GO

        TRUNCATE TABLE "EntityWithDateTimeOffset";
        GO

        TRUNCATE TABLE "EntityWithEnumStoredAsString";
        GO

        TRUNCATE TABLE "EntityWithEnumStoredAsInteger";
        GO

        TRUNCATE TABLE "MappingTestEntity";
        GO
        """;

    private static bool isDatabasePrepared;
}
