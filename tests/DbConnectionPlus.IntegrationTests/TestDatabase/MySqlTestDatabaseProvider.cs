using System.Data.Common;
using MySqlConnector;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.MySql;
using RentADeveloper.DbConnectionPlus.IntegrationTests.TestDatabase.Containers;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.TestDatabase;

/// <summary>
/// Provides the test database for MySQL tests.
/// </summary>
public class MySqlTestDatabaseProvider : ITestDatabaseProvider
{
    /// <inheritdoc />
    public bool CanRetrieveStructureOfTemporaryTables => true;

    /// <inheritdoc />
    public IDatabaseAdapter DatabaseAdapter => new MySqlDatabaseAdapter();

    /// <inheritdoc />
    public string DatabaseCollation => throw new NotImplementedException();

    /// <inheritdoc />
    public string DelayTwoSecondsStatement => "SELECT SLEEP(2);";

    /// <inheritdoc />
    public bool HasUnsupportedDataType => false;

    /// <inheritdoc />
    public bool SupportsCommandExecutionWhileDataReaderIsOpen => false;

    /// <inheritdoc />
    public bool SupportsDateTimeOffset => false;

    /// <inheritdoc />
    public bool SupportsProperCommandCancellation => false;

    /// <inheritdoc />
    public bool SupportsStoredProcedures => true;

    /// <inheritdoc />
    public bool SupportsStoredProceduresReturningResultSet => true;

    /// <inheritdoc />
    public bool TemporaryTableTextColumnInheritsCollationFromDatabase => true;

    /// <inheritdoc />
    public DbConnection CreateConnection()
    {
        var connection = new MySqlConnection(ConnectionString);
        connection.Open();

        // Needed for MySqlBulkCopy to work.
        connection.ExecuteNonQuery("SET GLOBAL local_infile=1");

        connection.ChangeDatabase(DatabaseName);

        return connection;
    }

    /// <inheritdoc />
    public bool ExistsTemporaryTable(string tableName, DbConnection connection, DbTransaction? transaction = null)
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
        connection.Query<(string Field, string Type, string Null, string Key, object Default, object Extra)>(
            $"SHOW COLUMNS FROM `{temporaryTableName}` WHERE Field = '{columnName}'",
            cancellationToken: TestContext.Current.CancellationToken
        ).Select(a => a.Type.ToUpper()).First();

    /// <inheritdoc />
    public string GetUnsupportedDataTypeLiteral() =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public void ResetDatabase()
    {
        using var connection = new MySqlConnection(ConnectionString);
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

    /// <inheritdoc />
    public static ValueTask StartDatabaseAsync() =>
        TestDatabaseContainers.StartMySqlAsync();

    /// <summary>
    /// The connection string that connects to the MySQL server running in the test container.
    /// </summary>
    private static string ConnectionString =>
        TestDatabaseContainers.MySql.ConnectionString;

    private static void ExecuteScript(MySqlConnection connection, string script)
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

    private const string DatabaseName = "DbConnectionPlusTests";

    private const string PurgeTablesSql =
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

    private static bool isDatabasePrepared;
}
