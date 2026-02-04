using System.Data.Common;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.SqlServer;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.TestDatabase;

/// <summary>
/// Provides the test database for SQL Server tests.
/// </summary>
public class SqlServerTestDatabaseProvider : ITestDatabaseProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerTestDatabaseProvider" /> class.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The environment variable 'ConnectionString_SqlServer' is not set.
    /// </exception>
    static SqlServerTestDatabaseProvider()
    {
        connectionString = Environment.GetEnvironmentVariable(ConnectionStringKey)?.Trim()!;

        if (String.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"The environment variable '{ConnectionStringKey}' is not set!");
        }

        Console.Out.WriteLine("Using the following connection string for SQL Server:");
        Console.Out.WriteLine(connectionString);
    }

    /// <inheritdoc />
    public Boolean CanRetrieveStructureOfTemporaryTables => true;

    /// <inheritdoc />
    public IDatabaseAdapter DatabaseAdapter => new SqlServerDatabaseAdapter();

    /// <inheritdoc />
    public String DatabaseCollation => "Latin1_General_CI_AS";

    /// <inheritdoc />
    public String DelayTwoSecondsStatement => "WAITFOR DELAY '00:00:02';";

    /// <inheritdoc />
    public Boolean HasUnsupportedDataType => true;

    /// <inheritdoc />
    public Boolean SupportsCommandExecutionWhileDataReaderIsOpen => true;

    /// <inheritdoc />
    public Boolean SupportsDateTimeOffset => true;

    /// <inheritdoc />
    public Boolean SupportsProperCommandCancellation => true;

    /// <inheritdoc />
    public Boolean SupportsStoredProcedures => true;

    /// <inheritdoc />
    public Boolean SupportsStoredProceduresReturningResultSet => true;

    /// <inheritdoc />
    public Boolean TemporaryTableTextColumnInheritsCollationFromDatabase => false;

    /// <inheritdoc />
    public DbConnection CreateConnection()
    {
        var connection = new SqlConnection(connectionString);
        connection.Open();

        connection.ChangeDatabase(DatabaseName);

        return connection;
    }

    /// <inheritdoc />
    public Boolean ExistsTemporaryTable(String tableName, DbConnection connection, DbTransaction? transaction = null) =>
        connection.ExecuteScalar<Boolean>(
            $"IF OBJECT_ID('tempdb..#{tableName}', 'U') IS NOT NULL SELECT 1 ELSE SELECT 0",
            transaction,
            cancellationToken: TestContext.Current.CancellationToken
        );

    /// <inheritdoc />
    public String GetCollationOfTemporaryTableColumn(
        String temporaryTableName,
        String columnName,
        DbConnection connection
    ) =>
        connection.ExecuteScalar<String>(
            $"""
             SELECT	C.collation_name AS CollationName
             FROM	tempdb.sys.columns C
             WHERE	c.object_id = OBJECT_ID('tempdb..#{temporaryTableName}') AND C.name = '{columnName}'
             """,
            cancellationToken: TestContext.Current.CancellationToken
        );

    /// <inheritdoc />
    public String GetDataTypeOfTemporaryTableColumn(
        String temporaryTableName,
        String columnName,
        DbConnection connection
    ) =>
        connection.QuerySingle<String>(
            $"""
             SELECT  t.name AS DataType
             FROM    tempdb.sys.columns c
             JOIN    tempdb.sys.types t ON c.user_type_id = t.user_type_id
             WHERE   c.object_id = OBJECT_ID('tempdb..#{temporaryTableName}') AND c.name = '{columnName}'
             """,
            cancellationToken: TestContext.Current.CancellationToken
        );

    /// <inheritdoc />
    public String GetUnsupportedDataTypeLiteral() =>
        "CONVERT(SQL_VARIANT, 123)";

    /// <inheritdoc />
    public void ResetDatabase()
    {
        using var connection = new SqlConnection(connectionString);
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

    private static void ExecuteScript(SqlConnection connection, String script)
    {
        var statements = script
            .Split("GO", StringSplitOptions.RemoveEmptyEntries)
            .Where(a => !String.IsNullOrWhiteSpace(a.Trim()));

        foreach (var statement in statements)
        {
            connection.ExecuteNonQuery(statement);
        }
    }

    private const String ConnectionStringKey = "ConnectionString_SqlServer";

    private const String CreateDatabaseObjectsSql =
        """
        CREATE TABLE Entity
        (
            Id BIGINT NOT NULL PRIMARY KEY,
            BytesValue VARBINARY(MAX),
            BooleanValue BIT,
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

        CREATE TABLE EntityWithNullableProperty
        (
            Id BIGINT NOT NULL PRIMARY KEY,
            Value BIGINT NULL
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

    private const String DatabaseName = "DbConnectionPlusTests";

    private const String PurgeTablesSql =
        """
        TRUNCATE TABLE Entity;
        GO

        TRUNCATE TABLE EntityWithDateTimeOffset;
        GO

        TRUNCATE TABLE EntityWithEnumStoredAsString;
        GO

        TRUNCATE TABLE EntityWithEnumStoredAsInteger;
        GO

        TRUNCATE TABLE EntityWithNullableProperty;
        GO

        TRUNCATE TABLE MappingTestEntity;
        GO
        """;

    private static readonly String connectionString;

    private static Boolean isDatabasePrepared;
}
