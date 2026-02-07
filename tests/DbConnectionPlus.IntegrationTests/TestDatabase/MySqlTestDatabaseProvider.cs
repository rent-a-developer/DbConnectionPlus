using System.Data.Common;
using MySqlConnector;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.MySql;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.TestDatabase;

/// <summary>
/// Provides the test database for MySQL tests.
/// </summary>
public class MySqlTestDatabaseProvider : ITestDatabaseProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlTestDatabaseProvider" /> class.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The environment variable 'ConnectionString_MySql' is not set.
    /// </exception>
    static MySqlTestDatabaseProvider()
    {
        connectionString = Environment.GetEnvironmentVariable(ConnectionStringKey)?.Trim()!;

        if (String.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"The environment variable '{ConnectionStringKey}' is not set!");
        }

        Console.Out.WriteLine("Using the following connection string for MySQL:");
        Console.Out.WriteLine(connectionString);
    }

    /// <inheritdoc />
    public Boolean CanRetrieveStructureOfTemporaryTables => true;

    /// <inheritdoc />
    public IDatabaseAdapter DatabaseAdapter => new MySqlDatabaseAdapter();

    /// <inheritdoc />
    public String DatabaseCollation => throw new NotImplementedException();

    /// <inheritdoc />
    public String DelayTwoSecondsStatement => "SELECT SLEEP(2);";

    /// <inheritdoc />
    public Boolean HasUnsupportedDataType => false;

    /// <inheritdoc />
    public Boolean SupportsCommandExecutionWhileDataReaderIsOpen => false;

    /// <inheritdoc />
    public Boolean SupportsDateTimeOffset => false;

    /// <inheritdoc />
    public Boolean SupportsProperCommandCancellation => false;

    /// <inheritdoc />
    public Boolean SupportsStoredProcedures => true;

    /// <inheritdoc />
    public Boolean SupportsStoredProceduresReturningResultSet => true;

    /// <inheritdoc />
    public Boolean TemporaryTableTextColumnInheritsCollationFromDatabase => true;

    /// <inheritdoc />
    public DbConnection CreateConnection()
    {
        var connection = new MySqlConnection(connectionString);
        connection.Open();

        // Needed for MySqlBulkCopy to work.
        connection.ExecuteNonQuery("SET GLOBAL local_infile=1");

        connection.ChangeDatabase(DatabaseName);

        return connection;
    }

    /// <inheritdoc />
    public Boolean ExistsTemporaryTable(String tableName, DbConnection connection, DbTransaction? transaction = null)
    {
        try
        {
            // Only way to check for temporary table existence in MySQL is to try to query it.
            connection.ExecuteNonQuery(
                $"SELECT * FROM `{tableName}`",
                transaction,
                cancellationToken: TestContext.Current.CancellationToken
            );
            return true;
        }
        catch
        {
#pragma warning disable ERP022
            return false;
#pragma warning restore ERP022
        }
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
        connection.Query<(String Field, String Type, String Null, String Key, Object Default, Object Extra)>(
            $"SHOW COLUMNS FROM `{temporaryTableName}` WHERE Field = '{columnName}'",
            cancellationToken: TestContext.Current.CancellationToken
        ).Select(a => a.Type.ToUpper()).First();

    /// <inheritdoc />
    public String GetUnsupportedDataTypeLiteral() =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public void ResetDatabase()
    {
        using var connection = new MySqlConnection(connectionString);
        connection.Open();

        if (!isDatabasePrepared)
        {
            connection.ExecuteNonQuery($"DROP DATABASE IF EXISTS `{DatabaseName}`");
            connection.ExecuteNonQuery($"CREATE DATABASE `{DatabaseName}`");

            connection.ChangeDatabase(DatabaseName);

            ExecuteScript(connection, CreateDatabaseObjectsSql);

            isDatabasePrepared = true;
        }

        connection.ChangeDatabase(DatabaseName);
        ExecuteScript(connection, PurgeTablesSql);
    }

    private static void ExecuteScript(MySqlConnection connection, String script)
    {
        var statements = script
            .Split("GO", StringSplitOptions.RemoveEmptyEntries)
            .Where(a => !String.IsNullOrWhiteSpace(a.Trim()));

        foreach (var statement in statements)
        {
            connection.ExecuteNonQuery(statement);
        }
    }

    private const String ConnectionStringKey = "ConnectionString_MySql";

    private const String CreateDatabaseObjectsSql =
        """
        CREATE TABLE `Entity`
        (
            `Id` BIGINT,
            `BooleanValue` TINYINT(1),
            `BytesValue` BLOB,
            `ByteValue` TINYINT UNSIGNED,
            `CharValue` CHAR(1),
            `DateOnlyValue` DATE,
            `DateTimeValue` DATETIME,
            `DecimalValue` DECIMAL(65,30),
            `DoubleValue` DOUBLE,
            `EnumValue` VARCHAR(200),
            `GuidValue` CHAR(36),
            `Int16Value` SMALLINT,
            `Int32Value` INT,
            `Int64Value` BIGINT,
            `NullableBooleanValue` TINYINT(1) NULL,
            `SingleValue` FLOAT,
            `StringValue` TEXT,
            `TimeOnlyValue` TIME,
            `TimeSpanValue` TIME
        );
        GO

        CREATE TABLE `EntityWithEnumStoredAsString`
        (
            `Id` BIGINT,
            `Enum` VARCHAR(200) NULL
        );
        GO

        CREATE TABLE `EntityWithEnumStoredAsInteger`
        (
            `Id` BIGINT,
            `Enum` INT NULL
        );
        GO

        CREATE TABLE `MappingTestEntity`
        (
            `Computed` INT AS (`Value`+999),
            `ConcurrencyToken` BLOB,
            `Identity` INT AUTO_INCREMENT PRIMARY KEY NOT NULL,
            `Key1` BIGINT NOT NULL,
            `Key2` BIGINT NOT NULL,
            `Value` INT NOT NULL,
            `NotMapped` TEXT NULL,
            `RowVersion` BLOB
        );
        GO

        CREATE TRIGGER Trigger_BeforeInsert_MappingTestEntity
        BEFORE INSERT ON MappingTestEntity
        FOR EACH ROW
        BEGIN
          SET NEW.RowVersion = UNHEX(REPLACE(UUID(), '-', ''));
        END;
        GO

        CREATE TRIGGER Trigger_BeforeUpdate_MappingTestEntity
        BEFORE UPDATE ON MappingTestEntity
        FOR EACH ROW
        BEGIN
          SET NEW.RowVersion = UNHEX(REPLACE(UUID(), '-', ''));
        END;
        GO

        CREATE PROCEDURE `GetEntities` ()
        BEGIN
        	SELECT * FROM `Entity`;
        END;
        GO

        CREATE PROCEDURE `GetEntityIds` ()
        BEGIN
        	SELECT `Id` FROM `Entity`;
        END;
        GO

        CREATE PROCEDURE `GetEntityIdsAndStringValues` ()
        BEGIN
        	SELECT `Id`, `StringValue` FROM `Entity`;
        END;
        GO

        CREATE PROCEDURE `GetFirstEntity` ()
        BEGIN
        	SELECT * FROM `Entity` LIMIT 1;
        END;
        GO

        CREATE PROCEDURE `GetFirstEntityId` ()
        BEGIN
        	SELECT `Id` FROM `Entity` LIMIT 1;
        END;
        GO

        CREATE PROCEDURE `DeleteAllEntities` ()
        BEGIN
        	DELETE FROM `Entity`;
        END;
        GO
        """;

    private const String DatabaseName = "DbConnectionPlusTests";

    private const String PurgeTablesSql =
        """
        TRUNCATE TABLE `Entity`;
        GO

        TRUNCATE TABLE `EntityWithEnumStoredAsString`;
        GO

        TRUNCATE TABLE `EntityWithEnumStoredAsInteger`;
        GO

        TRUNCATE TABLE `MappingTestEntity`;
        GO
        """;

    private static readonly String connectionString;

    private static Boolean isDatabasePrepared;
}
