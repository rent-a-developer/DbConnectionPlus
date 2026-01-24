// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using RentADeveloper.DbConnectionPlus.SqlStatements;
using DbCommandBuilder = RentADeveloper.DbConnectionPlus.DbCommands.DbCommandBuilder;

namespace RentADeveloper.DbConnectionPlus;

/// <summary>
/// Provides extension members for the type <see cref="DbConnection" />.
/// </summary>
public static partial class DbConnectionExtensions
{
    /// <summary>
    /// Executes the specified SQL statement and returns the number of rows affected by the statement.
    /// </summary>
    /// <param name="connection">The database connection to use to execute the statement.</param>
    /// <param name="statement">The SQL statement to execute.</param>
    /// <param name="transaction">The database transaction within to execute the statement.</param>
    /// <param name="commandTimeout">The timeout to use for the execution of the statement.</param>
    /// <param name="commandType">A value indicating how <paramref name="statement" /> is to be interpreted.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The number of rows affected by the statement.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">
    /// The statement was cancelled via <paramref name="cancellationToken" />.
    /// </exception>
    /// <remarks>
    /// See <see cref="DbCommand.ExecuteNonQuery" /> for additional exceptions this method may throw.
    /// </remarks>
    /// <example>
    /// <code>
    /// using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;
    /// 
    /// if (supplier.IsRetired)
    /// {
    ///     var numberOfDeletedProducts = connection.ExecuteNonQuery(
    ///        $"DELETE FROM Product WHERE SupplierId = {Parameter(supplier.Id)}"
    ///     );
    /// }
    /// </code>
    /// </example>
    public static Int32 ExecuteNonQuery(
        this DbConnection connection,
        InterpolatedSqlStatement statement,
        DbTransaction? transaction = null,
        TimeSpan? commandTimeout = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(connection);

        var databaseAdapter = DatabaseAdapterRegistry.GetAdapter(connection.GetType());

        var (command, commandDisposer) = DbCommandBuilder.BuildDbCommand(
            statement,
            databaseAdapter,
            connection,
            transaction,
            commandTimeout,
            commandType,
            cancellationToken
        );

        using (commandDisposer)
        {
            try
            {
                OnBeforeExecutingCommand(command, statement.TemporaryTables);
                return command.ExecuteNonQuery();
            }
            catch (Exception exception) when (
                databaseAdapter.WasSqlStatementCancelledByCancellationToken(exception, cancellationToken)
            )
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }
    }

    /// <summary>
    /// Asynchronously executes the specified SQL statement and returns the number of rows affected by the statement.
    /// </summary>
    /// <param name="connection">The database connection to use to execute the statement.</param>
    /// <param name="statement">The SQL statement to execute.</param>
    /// <param name="transaction">The database transaction within to execute the statement.</param>
    /// <param name="commandTimeout">The timeout to use for the execution of the statement.</param>
    /// <param name="commandType">A value indicating how <paramref name="statement" /> is to be interpreted.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// <see cref="Task{TResult}.Result" /> will contain the number of rows affected by the statement.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">
    /// The statement was cancelled via <paramref name="cancellationToken" />.
    /// </exception>
    /// <remarks>
    /// See <see cref="DbCommand.ExecuteNonQueryAsync(System.Threading.CancellationToken)" /> for additional exceptions
    /// this method may throw.
    /// </remarks>
    /// <example>
    /// <code>
    /// using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;
    /// 
    /// if (supplier.IsRetired)
    /// {
    ///     var numberOfDeletedProducts = await connection.ExecuteNonQueryAsync(
    ///        $"DELETE FROM Product WHERE SupplierId = {Parameter(supplier.Id)}"
    ///     );
    /// }
    /// </code>
    /// </example>
    public static async Task<Int32> ExecuteNonQueryAsync(
        this DbConnection connection,
        InterpolatedSqlStatement statement,
        DbTransaction? transaction = null,
        TimeSpan? commandTimeout = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(connection);

        var databaseAdapter = DatabaseAdapterRegistry.GetAdapter(connection.GetType());

        var (command, commandDisposer) = await DbCommandBuilder.BuildDbCommandAsync(
            statement,
            databaseAdapter,
            connection,
            transaction,
            commandTimeout,
            commandType,
            cancellationToken
        ).ConfigureAwait(false);

        await using (commandDisposer)
        {
            try
            {
                OnBeforeExecutingCommand(command, statement.TemporaryTables);
                return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (
                databaseAdapter.WasSqlStatementCancelledByCancellationToken(exception, cancellationToken)
            )
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }
    }
}
