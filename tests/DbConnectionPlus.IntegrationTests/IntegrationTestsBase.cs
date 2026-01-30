// ReSharper disable StaticMemberInGenericType
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1158

using System.Data.Common;
using System.Globalization;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.Oracle;
using RentADeveloper.DbConnectionPlus.Entities;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests;

/// <summary>
/// Base class for integration tests that need a database connection.
/// </summary>
/// <typeparam name="TTestDatabaseProvider">The type of the test database provider to use for the tests.</typeparam>
public abstract class IntegrationTestsBase<TTestDatabaseProvider> : IDisposable, IAsyncDisposable
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    protected IntegrationTestsBase()
    {
        // Ensure consistent culture for tests.
        CultureInfo.CurrentCulture =
            CultureInfo.CurrentUICulture =
                Thread.CurrentThread.CurrentCulture =
                    Thread.CurrentThread.CurrentUICulture = new("en-US");

        DbCommandLogger.LogCommands = false;

        this.TestDatabaseProvider = new();
        this.TestDatabaseProvider.ResetDatabase();

        currentTestDatabaseProvider.Value = this.TestDatabaseProvider;

        this.Connection = this.TestDatabaseProvider.CreateConnection();

        currentTestDatabaseConnection.Value = this.Connection;

        this.DbCommandFactory = new(this.TestDatabaseProvider);
        DbConnectionExtensions.DbCommandFactory = this.DbCommandFactory;

        DbCommandLogger.LogCommands = true;

        OracleDatabaseAdapter.AllowTemporaryTables = true;

        // Reset all settings to defaults before each test.
        DbConnectionPlusConfiguration.Instance = new()
        {
            EnumSerializationMode = EnumSerializationMode.Strings,
            InterceptDbCommand = DbCommandLogger.LogDbCommand
        };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        this.Connection.Close();
        this.Connection.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        await this.Connection.CloseAsync();
        await this.Connection.DisposeAsync();
    }

    /// <summary>
    /// Returns the specified parameter name with the appropriate prefix (e.g. "@" for SQL Server or ":" for Oracle)
    /// according to the current test database type.
    /// </summary>
    /// <param name="parameterName">The parameter name to format.</param>
    /// <returns>
    /// The formatted parameter name, including the appropriate prefix, suitable for inclusion in SQL statements.
    /// </returns>
    /// <remarks>The name of this method is intentionally kept very short, so test code doesn't get bloated.</remarks>
    public static String P(String parameterName) =>
        currentTestDatabaseProvider.Value!.DatabaseAdapter.FormatParameterName(parameterName);

    /// <summary>
    /// Returns the specified database identifier properly quoted for use in SQL statements according to the current
    /// test database type.
    /// </summary>
    /// <param name="identifier">The identifier to quote.</param>
    /// <returns>The quoted identifier, suitable for inclusion in SQL statements.</returns>
    /// <remarks>The name of this method is intentionally kept very short, so test code doesn't get bloated.</remarks>
    public static String Q(String identifier) =>
        currentTestDatabaseProvider.Value!.DatabaseAdapter.QuoteIdentifier(identifier);

    /// <summary>
    /// Returns the specified temporary table name properly quoted for use in SQL statements according to the current
    /// test database type.
    /// </summary>
    /// <param name="tableName">The name of the temporary table to quote.</param>
    /// <returns>The quoted temporary table name, suitable for inclusion in SQL statements.</returns>
    /// <remarks>The name of this method is intentionally kept very short, so test code doesn't get bloated.</remarks>
    public static String QT(String tableName) =>
        currentTestDatabaseProvider.Value!.DatabaseAdapter.QuoteTemporaryTableName(
            tableName,
            currentTestDatabaseConnection.Value!
        );

    /// <summary>
    /// The connection to the test database.
    /// </summary>
    protected DbConnection Connection { get; }

    /// <summary>
    /// The DbConnectionPlus database adapter for the test database.
    /// </summary>
    protected IDatabaseAdapter DatabaseAdapter => this.TestDatabaseProvider.DatabaseAdapter;

    /// <summary>
    /// The provider for the test database.
    /// </summary>
    protected TTestDatabaseProvider TestDatabaseProvider { get; }

    /// <summary>
    /// Creates the specified number of entities of the type <typeparamref name="T" /> and inserts them into the test
    /// database.
    /// </summary>
    /// <typeparam name="T">The type of entities to create and insert.</typeparam>
    /// <param name="numberOfEntities">
    /// The number of entities to create and insert.
    /// If omitted a small random number (<see cref="Generate.SmallNumber" />) will be used.
    /// </param>
    /// <param name="transaction">The database transaction within to perform the operation.</param>
    /// <returns>The entities that were created and inserted.</returns>
    protected List<T> CreateEntitiesInDb<T>(Int32? numberOfEntities = null, DbTransaction? transaction = null)
        where T : class =>
        ExecuteWithoutDbCommandLogging(() =>
            {
                var entities = Generate.Multiple<T>(numberOfEntities);

                this.Connection.InsertEntities(
                    entities,
                    transaction,
                    TestContext.Current.CancellationToken
                );

                foreach (var entity in entities)
                {
                    // Verify that the entity has been inserted:
                    this.ExistsEntityInDb(entity, transaction)
                        .Should().BeTrue();
                }

                return entities;
            }
        );

    /// <summary>
    /// Creates an entity of the type <typeparamref name="T" /> and inserts it into the test database.
    /// </summary>
    /// <typeparam name="T">The type of entity to create and insert.</typeparam>
    /// <param name="transaction">The database transaction within to perform the operation.</param>
    /// <returns>The entity that was created and inserted.</returns>
    protected T CreateEntityInDb<T>(DbTransaction? transaction = null)
        where T : class
        =>
            ExecuteWithoutDbCommandLogging(() =>
                {
                    var entity = Generate.Single<T>();

                    this.Connection.InsertEntity(
                        entity,
                        transaction,
                        TestContext.Current.CancellationToken
                    );

                    // Verify that the entity has been inserted:
                    this.ExistsEntityInDb(entity, transaction)
                        .Should().BeTrue();

                    return entity;
                }
            );

    /// <summary>
    /// Determines whether an entity having the key(s) of the specified entity exists in the test database.
    /// </summary>
    /// <typeparam name="T">The type of entity to check.</typeparam>
    /// <param name="entity">The entity to check for existence.</param>
    /// <param name="transaction">The transaction within to perform the operation.</param>
    /// <returns>
    /// <see langword="true" /> if the specified entity exists in the test database; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="InvalidOperationException"></exception>
    protected Boolean ExistsEntityInDb<T>(T entity, DbTransaction? transaction = null)
        where T : class
    {
        var metadata = EntityHelper.GetEntityTypeMetadata(typeof(T));
        var keyProperties = metadata.KeyProperties;

        if (keyProperties.Count == 0)
        {
            throw new InvalidOperationException($"The type {typeof(T)} has no key property / properties.");
        }

        var statement = new InterpolatedSqlStatement(
            $"""
             SELECT 1
             FROM   {Q(metadata.TableName)}
             WHERE  {
                 String.Join(
                     " AND ",
                     [.. keyProperties.Select(p => $"{Q(p.ColumnName)} = {P(p.PropertyName)}")]
                 )
             }
             """,
            keyProperties.Select(p => (p.PropertyName, p.PropertyGetter!(entity))).ToArray()!
        );

        return ExecuteWithoutDbCommandLogging(() => this.Connection.Exists(
                statement,
                transaction,
                cancellationToken: TestContext.Current.CancellationToken
            )
        );
    }

    /// <summary>
    /// Determines whether a temporary table with the specified name exists in the test database.
    /// </summary>
    /// <param name="tableName">The name of the temporary table to check.</param>
    /// <param name="transaction">The database transaction within to perform the operation.</param>
    /// <returns>
    /// <see langword="true" /> if a temporary table with the specified name exists in the test database;
    /// otherwise, <see langword="false" />.
    /// </returns>
    protected Boolean ExistsTemporaryTableInDb(String tableName, DbTransaction? transaction = null) =>
        ExecuteWithoutDbCommandLogging(() =>
            this.TestDatabaseProvider.ExistsTemporaryTable(
                tableName,
                this.Connection,
                transaction
            )
        );

    /// <summary>
    /// Gets the collation of the specified column of the specified temporary table.
    /// </summary>
    /// <param name="temporaryTableName">The name of the temporary table that contains the specified column.</param>
    /// <param name="columnName">The name of the column of which to get the collation.</param>
    /// <returns>The collation of the specified column of the specified temporary table.</returns>
    protected String GetCollationOfTemporaryTableColumn(String temporaryTableName, String columnName) =>
        ExecuteWithoutDbCommandLogging(() =>
            this.TestDatabaseProvider.GetCollationOfTemporaryTableColumn(
                temporaryTableName,
                columnName,
                this.Connection
            )
        );

    /// <summary>
    /// Gets the data type of the specified column of the specified temporary table.
    /// </summary>
    /// <param name="temporaryTableName">The name of the temporary table that contains the specified column.</param>
    /// <param name="columnName">The name of the column of which to get the data type.</param>
    /// <returns>The data type of the specified column of the specified temporary table.</returns>
    protected String GetDataTypeOfTemporaryTableColumn(
        String temporaryTableName,
        String columnName
    ) =>
        ExecuteWithoutDbCommandLogging(() =>
            this.TestDatabaseProvider.GetDataTypeOfTemporaryTableColumn(
                temporaryTableName,
                columnName,
                this.Connection
            )
        );

    /// <summary>
    /// Creates a <see cref="CancellationToken" /> that will be cancelled after 100 milliseconds.
    /// </summary>
    /// <returns>A <see cref="CancellationToken" /> that will be cancelled after 100 milliseconds.</returns>
    protected static CancellationToken CreateCancellationTokenThatIsCancelledAfter100Milliseconds()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(100);
        return cancellationTokenSource.Token;
    }

    /// <summary>
    /// Executes <paramref name="func" /> while disabling database command logging during the execution.
    /// </summary>
    /// <typeparam name="T">The type of the return value of <paramref name="func" />.</typeparam>
    /// <param name="func">The function to execute.</param>
    /// <returns>The return value of <paramref name="func" />.</returns>
    private static T ExecuteWithoutDbCommandLogging<T>(Func<T> func)
    {
        DbCommandLogger.LogCommands = false;
        var result = func();
        DbCommandLogger.LogCommands = true;
        return result;
    }

    /// <summary>
    /// The connection to the test database for the currently running integration test.
    /// </summary>
    private static readonly AsyncLocal<DbConnection> currentTestDatabaseConnection = new();

    /// <summary>
    /// The test database provider for the currently running integration test.
    /// </summary>
    private static readonly AsyncLocal<ITestDatabaseProvider> currentTestDatabaseProvider = new();

    /// <summary>
    /// The database command factory used for testing cancellation of SQL statements.
    /// </summary>
    protected readonly DelayDbCommandFactory DbCommandFactory;
}
