using System.Data.Common;
using Npgsql;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.PostgreSql;
using RentADeveloper.DbConnectionPlus.IntegrationTests.TestDatabase.Containers;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.TestDatabase;

/// <summary>
/// Provides the test database for PostgreSQL tests.
/// </summary>
public class PostgreSqlTestDatabaseProvider : ITestDatabaseProvider
{
    private const string CreateDatabaseObjectsSql = """
        CREATE EXTENSION IF NOT EXISTS pgcrypto; -- Needed for gen_random_bytes()

        CREATE TABLE "Entity"
        (
            "Id" bigint NOT NULL PRIMARY KEY,
            "BooleanValue" boolean,
            "BytesValue" bytea,
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
            "NullableBooleanValue" boolean NULL,
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

        CREATE TABLE "MappingTestEntity"
        (
            "Computed" integer GENERATED ALWAYS AS ("Value"+(999)),
            "ConcurrencyToken" bytea,
            "Identity" integer GENERATED ALWAYS AS IDENTITY NOT NULL,
            "Key1" bigint NOT NULL,
            "Key2" bigint NOT NULL,
            "Value" integer NOT NULL,
            "NotMapped" text NULL,
            "RowVersion" bytea DEFAULT gen_random_bytes(8),
            PRIMARY KEY ("Key1", "Key2")
        );

        CREATE OR REPLACE FUNCTION "UpdateMappingTestEntityRowVersion"()
        RETURNS TRIGGER AS $$
        BEGIN
            NEW."RowVersion" = gen_random_bytes(8);
            RETURN NEW;
        END;
        $$ LANGUAGE plpgsql;

        CREATE TRIGGER "TriggerMappingTestEntityRowVersion"
        BEFORE UPDATE ON "MappingTestEntity"
        FOR EACH ROW
        EXECUTE FUNCTION "UpdateMappingTestEntityRowVersion"();

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

    private const string DatabaseName = "DbConnectionPlusTests";

    private const string PurgeTablesSql = """
        TRUNCATE TABLE "Entity";
        TRUNCATE TABLE "EntityWithEnumStoredAsString";
        TRUNCATE TABLE "EntityWithEnumStoredAsInteger";
        TRUNCATE TABLE "MappingTestEntity";
        """;

    private static bool isDatabasePrepared;

    /// <inheritdoc />
    public bool CanRetrieveStructureOfTemporaryTables => true;

    /// <inheritdoc />
    public IDatabaseAdapter DatabaseAdapter => new PostgreSqlDatabaseAdapter();

    /// <inheritdoc />
    public string DatabaseCollation => throw new NotImplementedException();

    /// <inheritdoc />
    public string DelayTwoSecondsStatement => "SELECT pg_sleep(2);";

    /// <inheritdoc />
    public bool HasUnsupportedDataType => true;

    /// <inheritdoc />
    public bool SupportsCommandExecutionWhileDataReaderIsOpen => false;

    /// <inheritdoc />
    public bool SupportsDateTimeOffset => false;

    /// <inheritdoc />
    public bool SupportsProperCommandCancellation => true;

    /// <inheritdoc />
    public bool SupportsStoredProcedures => true;

    /// <inheritdoc />
    public bool SupportsStoredProceduresReturningResultSet => false;

    /// <inheritdoc />
    public bool TemporaryTableTextColumnInheritsCollationFromDatabase => true;

    /// <summary>
    /// The connection string that connects to the PostgreSQL server running in the test container.
    /// </summary>
    private static string ConnectionString => TestDatabaseContainers.PostgreSql.ConnectionString;

    /// <inheritdoc />
    public static ValueTask StartDatabaseAsync() => TestDatabaseContainers.StartPostgreSqlAsync();

    /// <inheritdoc />
    public DbConnection CreateConnection()
    {
        var connection = new NpgsqlConnection(ConnectionString);
        connection.Open();
        connection.ChangeDatabase(DatabaseName);
        return connection;
    }

    /// <inheritdoc />
    public bool ExistsTemporaryTable(string tableName, DbConnection connection, DbTransaction? transaction = null) =>
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
    public string GetCollationOfTemporaryTableColumn(
        string temporaryTableName,
        string columnName,
        DbConnection connection
    ) => throw new NotImplementedException();

    /// <inheritdoc />
    public string GetDataTypeOfTemporaryTableColumn(
        string temporaryTableName,
        string columnName,
        DbConnection connection
    ) =>
        connection.QuerySingle<string>(
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
    public string GetUnsupportedDataTypeLiteral() => "(1, 2)";

    public void ResetDatabase()
    {
        using var connection = new NpgsqlConnection(ConnectionString);
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
}
