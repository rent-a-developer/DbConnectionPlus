using System.Data.Common;
using Npgsql;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.PostgreSql;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.TestDatabase;

/// <summary>
/// Provides the test database for PostgreSQL tests.
/// </summary>
public class PostgreSqlTestDatabaseProvider : ITestDatabaseProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlTestDatabaseProvider" /> class.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The environment variable 'ConnectionString_PostgreSQL' is not set.
    /// </exception>
    static PostgreSqlTestDatabaseProvider()
    {
        connectionString = Environment.GetEnvironmentVariable(ConnectionStringKey)?.Trim()!;

        if (String.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"The environment variable '{ConnectionStringKey}' is not set!");
        }

        Console.Out.WriteLine("Using the following connection string for PostgreSQL:");
        Console.Out.WriteLine(connectionString);
    }

    /// <inheritdoc />
    public Boolean CanRetrieveStructureOfTemporaryTables => true;

    /// <inheritdoc />
    public IDatabaseAdapter DatabaseAdapter => new PostgreSqlDatabaseAdapter();

    /// <inheritdoc />
    public String DatabaseCollation => throw new NotImplementedException();

    /// <inheritdoc />
    public String DelayTwoSecondsStatement => "SELECT pg_sleep(2);";

    /// <inheritdoc />
    public Boolean HasUnsupportedDataType => true;

    /// <inheritdoc />
    public Boolean SupportsCommandExecutionWhileDataReaderIsOpen => false;

    /// <inheritdoc />
    public Boolean SupportsDateTimeOffset => false;

    /// <inheritdoc />
    public Boolean SupportsProperCommandCancellation => true;

    /// <inheritdoc />
    public Boolean SupportsStoredProcedures => true;

    /// <inheritdoc />
    public Boolean SupportsStoredProceduresReturningResultSet => false;

    /// <inheritdoc />
    public Boolean TemporaryTableTextColumnInheritsCollationFromDatabase => true;

    /// <inheritdoc />
    public DbConnection CreateConnection()
    {
        var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        connection.ChangeDatabase(DatabaseName);
        return connection;
    }

    /// <inheritdoc />
    public Boolean ExistsTemporaryTable(String tableName, DbConnection connection, DbTransaction? transaction = null) =>
        connection.Exists(
            $"""
             SELECT 1
             FROM   information_schema.tables
             WHERE  table_type = 'LOCAL TEMPORARY' AND
                    table_name = '{tableName}'
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
        connection.QuerySingle<String>(
            $"""
             SELECT data_type
             FROM   information_schema.columns
             WHERE  table_schema LIKE 'pg_temp%' AND
                    table_name = '{temporaryTableName}' AND
                    column_name = '{columnName}'
             """,
            cancellationToken: TestContext.Current.CancellationToken
        );

    /// <inheritdoc />
    public String GetUnsupportedDataTypeLiteral() =>
        "(1, 2)";

    public void ResetDatabase()
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        if (!isDatabasePrepared)
        {
            connection.ExecuteNonQuery($"DROP DATABASE IF EXISTS \"{DatabaseName}\" WITH (FORCE)");
            connection.ExecuteNonQuery($"CREATE DATABASE \"{DatabaseName}\"");

            connection.ChangeDatabase(DatabaseName);

            connection.ExecuteNonQuery(CreateDatabaseObjectsSql);

            isDatabasePrepared = true;
        }

        connection.ChangeDatabase(DatabaseName);
        connection.ExecuteNonQuery(PurgeTablesSql);
    }

    private const String ConnectionStringKey = "ConnectionString_PostgreSQL";

    private const String CreateDatabaseObjectsSql =
        """
        CREATE TABLE "Entity"
        (
            "Id" bigint NOT NULL PRIMARY KEY,
            "BooleanValue" boolean,
            "ByteValue" smallint,
            "CharValue" char(1),
            "DateOnlyValue" date,
            "DateTimeValue" timestamp without time zone,
            "DecimalValue" decimal,
            "DoubleValue" double precision,
            "EnumValue" character varying(200),
            "GuidValue" uuid,
            "Int16Value" smallint,
            "Int32Value" integer,
            "Int64Value" bigint,
            "SingleValue" real,
            "StringValue" text,
            "TimeOnlyValue" time,
            "TimeSpanValue" interval
        );

        CREATE TABLE "EntityWithEnumStoredAsString"
        (
            "Id" bigint NOT NULL PRIMARY KEY,
            "Enum" character varying(200) NULL
        );

        CREATE TABLE "EntityWithEnumStoredAsInteger"
        (
            "Id" bigint NOT NULL PRIMARY KEY,
            "Enum" integer NULL
        );

        CREATE TABLE "EntityWithNonNullableProperty"
        (
            "Id" bigint NOT NULL PRIMARY KEY,
            "Value" bigint NULL
        );

        CREATE TABLE "EntityWithNullableProperty"
        (
            "Id" bigint NOT NULL PRIMARY KEY,
            "Value" bigint NULL
        );

        CREATE TABLE "EntityWithIdentityAndComputedProperties"
        (
            "Id" bigint NOT NULL PRIMARY KEY,
            "IdentityValue" bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
            "ComputedValue" bigint GENERATED ALWAYS AS ("BaseValue"+(999)),
            "BaseValue" bigint NOT NULL
        );

        CREATE TABLE "EntityWithCompositeKey"
        (
            "Key1" bigint NOT NULL,
            "Key2" bigint NOT NULL,
            "StringValue" text NOT NULL,
            PRIMARY KEY ("Key1", "Key2")
        );

        CREATE TABLE "EntityWithNotMappedProperty"
        (
            "Id" bigint NOT NULL PRIMARY KEY,
            "MappedValue" text NOT NULL,
            "NotMappedValue" text NULL
        );

        CREATE PROCEDURE "GetEntities" ()
        LANGUAGE SQL
        AS $$
        	SELECT * FROM "Entity"
        $$;

        CREATE PROCEDURE "GetEntityIds" ()
        LANGUAGE SQL
        AS $$
        	SELECT "Id" FROM "Entity"
        $$;

        CREATE PROCEDURE "GetEntityIdsAndStringValues" ()
        LANGUAGE SQL
        AS $$
        	SELECT "Id", "StringValue" FROM "Entity"
        $$;

        CREATE PROCEDURE "GetFirstEntity" ()
        LANGUAGE SQL
        AS $$
        	SELECT * FROM "Entity" LIMIT 1
        $$;

        CREATE PROCEDURE "GetFirstEntityId" ()
        LANGUAGE SQL
        AS $$
        	SELECT "Id" FROM "Entity" LIMIT 1
        $$;

        CREATE PROCEDURE "DeleteAllEntities" ()
        LANGUAGE SQL
        AS $$
        	DELETE FROM "Entity"
        $$;
        """;

    private const String DatabaseName = "DbConnectionPlusTests";

    private const String PurgeTablesSql =
        """
        TRUNCATE TABLE "Entity";
        TRUNCATE TABLE "EntityWithEnumStoredAsString";
        TRUNCATE TABLE "EntityWithEnumStoredAsInteger";
        TRUNCATE TABLE "EntityWithNonNullableProperty";
        TRUNCATE TABLE "EntityWithNullableProperty";
        TRUNCATE TABLE "EntityWithIdentityAndComputedProperties";
        TRUNCATE TABLE "EntityWithCompositeKey";
        TRUNCATE TABLE "EntityWithNotMappedProperty";
        """;

    private static readonly String connectionString;

    private static Boolean isDatabasePrepared;
}
