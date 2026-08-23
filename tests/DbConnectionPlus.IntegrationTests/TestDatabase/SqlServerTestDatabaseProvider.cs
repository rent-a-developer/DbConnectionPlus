using System.Data.Common;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.SqlServer;
using RentADeveloper.DbConnectionPlus.IntegrationTests.TestDatabase.Containers;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.TestDatabase;

/// <summary>
/// Provides the test database for SQL Server tests.
/// </summary>
public class SqlServerTestDatabaseProvider : ITestDatabaseProvider
{
    private const string CreateDatabaseObjectsSql = """
        CREATE TABLE Entity
        (
            Id BIGINT NOT NULL PRIMARY KEY,
            BooleanValue BIT,
            BytesValue VARBINARY(MAX),
            ByteValue TINYINT,
            CharValue CHAR(1),
            DateOnlyValue DATE,
            DateTimeValue DATETIME2,
            DecimalValue DECIMAL(28,10),
            DoubleValue FLOAT,
            EnumValue NVARCHAR(200),
            GuidValue UNIQUEIDENTIFIER,
            Int16Value SMALLINT,
            Int32Value INT,
            Int64Value BIGINT,
            NullableBooleanValue BIT NULL,
            SingleValue REAL,
            StringValue NVARCHAR(MAX),
            TimeOnlyValue TIME,
            TimeSpanValue TIME
        );
        GO

        CREATE TABLE EntityWithDateTimeOffset
        (
            Id BIGINT NOT NULL PRIMARY KEY,
            DateTimeOffsetValue DATETIMEOFFSET NULL
        );
        GO

        CREATE TABLE EntityWithEnumStoredAsString
        (
            Id BIGINT NOT NULL PRIMARY KEY,
            Enum NVARCHAR(200) NULL
        );
        GO

        CREATE TABLE EntityWithEnumStoredAsInteger
        (
            Id BIGINT NOT NULL PRIMARY KEY,
            Enum INT NULL
        );
        GO

        CREATE TABLE MappingTestEntity
        (
            Computed AS ([Value]+(999)),
            ConcurrencyToken VARBINARY(max),
            [Identity] INT IDENTITY(1,1) NOT NULL,
            Key1 BIGINT NOT NULL,
            Key2 BIGINT NOT NULL,
            Value INT NOT NULL,
            NotMapped VARCHAR(200) NULL,
            RowVersion ROWVERSION,
            PRIMARY KEY (Key1, Key2)
        );
        GO

        CREATE PROCEDURE GetEntities
        AS
        BEGIN
        	SELECT * FROM Entity
        END;
        GO

        CREATE PROCEDURE GetEntityIds
        AS
        BEGIN
        	SELECT Id FROM Entity
        END;
        GO

        CREATE PROCEDURE GetEntityIdsAndStringValues
        AS
        BEGIN
        	SELECT Id, StringValue FROM Entity
        END;
        GO

        CREATE PROCEDURE GetFirstEntity
        AS
        BEGIN
        	SELECT TOP 1 * FROM Entity
        END;
        GO

        CREATE PROCEDURE GetFirstEntityId
        AS
        BEGIN
        	SELECT TOP 1 Id FROM Entity
        END;
        GO

        CREATE PROCEDURE DeleteAllEntities
        AS
        BEGIN
        	DELETE FROM Entity
        END;
        GO
        """;

    private const string DatabaseName = "DbConnectionPlusTests";

    private const string PurgeTablesSql = """
        TRUNCATE TABLE Entity;
        GO

        TRUNCATE TABLE EntityWithDateTimeOffset;
        GO

        TRUNCATE TABLE EntityWithEnumStoredAsString;
        GO

        TRUNCATE TABLE EntityWithEnumStoredAsInteger;
        GO

        TRUNCATE TABLE MappingTestEntity;
        GO
        """;

    private static bool isDatabasePrepared;

    /// <inheritdoc />
    public bool CanRetrieveStructureOfTemporaryTables => true;

    /// <inheritdoc />
    public IDatabaseAdapter DatabaseAdapter => new SqlServerDatabaseAdapter();

    /// <inheritdoc />
    public string DatabaseCollation => "Latin1_General_CI_AS";

    /// <inheritdoc />
    public string DelayTwoSecondsStatement => "WAITFOR DELAY '00:00:02';";

    /// <inheritdoc />
    public bool HasUnsupportedDataType => true;

    /// <inheritdoc />
    public bool SupportsCommandExecutionWhileDataReaderIsOpen => true;

    /// <inheritdoc />
    public bool SupportsDateTimeOffset => true;

    /// <inheritdoc />
    public bool SupportsProperCommandCancellation => true;

    /// <inheritdoc />
    public bool SupportsStoredProcedures => true;

    /// <inheritdoc />
    public bool SupportsStoredProceduresReturningResultSet => true;

    /// <inheritdoc />
    public bool TemporaryTableTextColumnInheritsCollationFromDatabase => false;

    /// <summary>
    /// The connection string that connects to the SQL Server server running in the test container.
    /// </summary>
    private static string ConnectionString => TestDatabaseContainers.SqlServer.ConnectionString;

    /// <inheritdoc />
    public static ValueTask StartDatabaseAsync() => TestDatabaseContainers.StartSqlServerAsync();

    /// <inheritdoc />
    public DbConnection CreateConnection()
    {
        var connection = new SqlConnection(ConnectionString);
        connection.Open();

        connection.ChangeDatabase(DatabaseName);

        return connection;
    }

    /// <inheritdoc />
    public bool ExistsTemporaryTable(string tableName, DbConnection connection, DbTransaction? transaction = null) =>
        connection.ExecuteScalar<bool>(
            $"IF OBJECT_ID('tempdb..#{tableName}', 'U') IS NOT NULL SELECT 1 ELSE SELECT 0",
            transaction,
            cancellationToken: TestContext.Current.CancellationToken
        );

    /// <inheritdoc />
    public string GetCollationOfTemporaryTableColumn(
        string temporaryTableName,
        string columnName,
        DbConnection connection
    ) =>
        connection.ExecuteScalar<string>(
            $"""
            SELECT	C.collation_name AS CollationName
            FROM	tempdb.sys.columns C
            WHERE	c.object_id = OBJECT_ID('tempdb..#{temporaryTableName}') AND C.name = '{columnName}'
            """,
            cancellationToken: TestContext.Current.CancellationToken
        );

    /// <inheritdoc />
    public string GetDataTypeOfTemporaryTableColumn(
        string temporaryTableName,
        string columnName,
        DbConnection connection
    ) =>
        connection.QuerySingle<string>(
            $"""
            SELECT  t.name AS DataType
            FROM    tempdb.sys.columns c
            JOIN    tempdb.sys.types t ON c.user_type_id = t.user_type_id
            WHERE   c.object_id = OBJECT_ID('tempdb..#{temporaryTableName}') AND c.name = '{columnName}'
            """,
            cancellationToken: TestContext.Current.CancellationToken
        );

    /// <inheritdoc />
    public string GetUnsupportedDataTypeLiteral() => "CONVERT(SQL_VARIANT, 123)";

    /// <inheritdoc />
    public void ResetDatabase()
    {
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();

        if (!isDatabasePrepared)
        {
            connection.ExecuteNonQuery(
                $"""
                IF EXISTS (SELECT name FROM sys.databases WHERE name = N'{DatabaseName}')
                BEGIN
                    ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{DatabaseName}];
                END
                """
            );

            connection.ExecuteNonQuery($"CREATE DATABASE [{DatabaseName}] COLLATE {this.DatabaseCollation}");

            connection.ChangeDatabase(DatabaseName);

            ExecuteScript(connection, CreateDatabaseObjectsSql);

            isDatabasePrepared = true;
        }

        connection.ChangeDatabase(DatabaseName);

        ExecuteScript(connection, PurgeTablesSql);
    }

    private static void ExecuteScript(SqlConnection connection, string script)
    {
        var statements = script
            .Split("GO", StringSplitOptions.RemoveEmptyEntries)
            .Where(a => !string.IsNullOrWhiteSpace(a.Trim()));

        foreach (var statement in statements)
        {
            connection.ExecuteNonQuery(statement);
        }
    }
}
