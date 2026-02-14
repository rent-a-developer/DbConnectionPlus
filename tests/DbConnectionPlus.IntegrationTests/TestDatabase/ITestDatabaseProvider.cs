using System.Data.Common;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.TestDatabase;

/// <summary>
/// Represents a provider that provides access to a test database.
/// </summary>
public interface ITestDatabaseProvider
{
    /// <summary>
    /// Determines whether the structure of temporary tables can be retrieved from the test database system.
    /// </summary>
    public Boolean CanRetrieveStructureOfTemporaryTables { get; }

    /// <summary>
    /// The database adapter for the test database.
    /// </summary>
    public IDatabaseAdapter DatabaseAdapter { get; }

    /// <summary>
    /// The collation of the test database.
    /// </summary>
    public String DatabaseCollation { get; }

    /// <summary>
    /// An SQL statement that delays query execution for two seconds.
    /// </summary>
    public String DelayTwoSecondsStatement { get; }

    /// <summary>
    /// Determines whether the test database system has data types not supported by DbConnectionPlus.
    /// </summary>
    public Boolean HasUnsupportedDataType { get; }

    /// <summary>
    /// Determines whether the test database system supports executing commands while a data reader is open.
    /// </summary>
    public Boolean SupportsCommandExecutionWhileDataReaderIsOpen { get; }

    /// <summary>
    /// Determines whether the test database system has a data type for the type <see cref="DateTimeOffset" />.
    /// </summary>
    public Boolean SupportsDateTimeOffset { get; }

    /// <summary>
    /// Determines whether the test database system supports proper command cancellation, meaning that cancelling a
    /// command (via <see cref="DbCommand.Cancel" />) actually stops its execution in the database and an appropriate
    /// exception is thrown.
    /// </summary>
    public Boolean SupportsProperCommandCancellation { get; }

    /// <summary>
    /// Determines whether the test database system supports stored procedures.
    /// </summary>
    public Boolean SupportsStoredProcedures { get; }

    /// <summary>
    /// Determines whether the test database system supports stored procedures which can return a result set.
    /// </summary>
    public Boolean SupportsStoredProceduresReturningResultSet { get; }

    /// <summary>
    /// Determines whether a text column of a temporary table in the test database system inherits the collation
    /// from the current database.
    /// </summary>
    public Boolean TemporaryTableTextColumnInheritsCollationFromDatabase { get; }

    /// <summary>
    /// Creates a connection to the test database.
    /// </summary>
    public DbConnection CreateConnection();

    /// <summary>
    /// Determines whether a temporary table with the specified name exists in the test database.
    /// </summary>
    /// <param name="tableName">The name of the temporary table to check for existence.</param>
    /// <param name="connection">The connection to the test database.</param>
    /// <param name="transaction">The database transaction within to perform the operation.</param>
    /// <returns>
    /// <see langword="true" /> if a temporary table with the specified name exists in the test database;
    /// otherwise, <see langword="false" />.
    /// </returns>
    public Boolean ExistsTemporaryTable(String tableName, DbConnection connection, DbTransaction? transaction = null);

    /// <summary>
    /// Gets the collation of the specified column in the specified temporary table.
    /// </summary>
    /// <param name="temporaryTableName">The name of the temporary table that contains the specified column.</param>
    /// <param name="columnName">The name of the column whose collation to retrieve.</param>
    /// <param name="connection">The connection to the test database.</param>
    /// <returns>The collation of the specified column in the specified temporary table.</returns>
    public String GetCollationOfTemporaryTableColumn(
        String temporaryTableName,
        String columnName,
        DbConnection connection
    );

    /// <summary>
    /// Gets the data type of the specified column in the specified temporary table.
    /// </summary>
    /// <param name="temporaryTableName">The name of the temporary table that contains the specified column.</param>
    /// <param name="columnName">The name of the column whose data type to retrieve.</param>
    /// <param name="connection">The connection to the test database.</param>
    /// <returns>
    /// The data type of the specified column in the specified temporary table.
    /// </returns>
    public String GetDataTypeOfTemporaryTableColumn(
        String temporaryTableName,
        String columnName,
        DbConnection connection
    );

    /// <summary>
    /// Gets a literal representing a data type in the test database system that is not supported by DbConnectionPlus.
    /// </summary>
    /// <returns>
    /// A literal representing a data type in the test database system that is not supported by DbConnectionPlus.
    /// </returns>
    public String GetUnsupportedDataTypeLiteral();

    /// <summary>
    /// Prepares the test database and resets it to a clean state.
    /// </summary>
    public void ResetDatabase();
}
