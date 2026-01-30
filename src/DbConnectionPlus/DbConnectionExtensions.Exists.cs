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
    /// Executes the specified SQL statement and returns a <see cref="Boolean" /> value indicating whether the result
    /// set returned by the statement contains at least one row.
    /// This method is intended to check for the existence of rows matching certain criteria, e.g. checking whether a
    /// Product with a specific Id exists.
    /// </summary>
    /// <param name="connection">The database connection to use to execute the statement.</param>
    /// <param name="statement">The SQL statement to execute.</param>
    /// <param name="transaction">The database transaction within to execute the statement.</param>
    /// <param name="commandTimeout">The timeout to use for the execution of the statement.</param>
    /// <param name="commandType">A value indicating how <paramref name="statement" /> is to be interpreted.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// <see langword="true" /> if the result set returned by the statement contains at least one row; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">
    /// The statement was cancelled via <paramref name="cancellationToken" />.
    /// </exception>
    /// <remarks>
    /// See <see cref="DbCommand.ExecuteReader()" /> for additional exceptions this method may throw.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;
    /// 
    /// var lowStockThreshold = configuration.Thresholds.LowStock;
    /// 
    /// var existLowStockProducts = connection.Exists(
    ///    $"SELECT 1 FROM Product WHERE UnitsInStock < {Parameter(lowStockThreshold)}"
    /// );
    /// ]]>
    /// </code>
    /// </example>
    public static Boolean Exists(
        this DbConnection connection,
        InterpolatedSqlStatement statement,
        DbTransaction? transaction = null,
        TimeSpan? commandTimeout = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(connection);

        var databaseAdapter = DbConnectionPlusConfiguration.Instance.GetDatabaseAdapter(connection.GetType());

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
                using var reader = command.ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.SingleResult);
                return reader.Read();
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
    /// Asynchronously executes the specified SQL statement and returns a <see cref="Boolean" /> value indicating
    /// whether the result set returned by the statement contains at least one row.
    /// This method is intended to check for the existence of rows matching certain criteria, e.g. checking whether a
    /// Product with a specific Id exists.
    /// </summary>
    /// <param name="connection">The database connection to use to execute the statement.</param>
    /// <param name="statement">The SQL statement to execute.</param>
    /// <param name="transaction">The database transaction within to execute the statement.</param>
    /// <param name="commandTimeout">The timeout to use for the execution of the statement.</param>
    /// <param name="commandType">A value indicating how <paramref name="statement" /> is to be interpreted.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// <see cref="Task{TResult}.Result" /> will contain <see langword="true" /> if the result set returned by the
    /// statement contains at least one row; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">
    /// The statement was cancelled via <paramref name="cancellationToken" />.
    /// </exception>
    /// <remarks>
    /// See <see cref="DbCommand.ExecuteReaderAsync(System.Threading.CancellationToken)" /> for additional exceptions
    /// this method may throw.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;
    /// 
    /// var lowStockThreshold = configuration.Thresholds.LowStock;
    /// 
    /// var existLowStockProducts = await connection.ExistsAsync(
    ///    $"SELECT 1 FROM Product WHERE UnitsInStock < {Parameter(lowStockThreshold)}"
    /// );
    /// ]]>
    /// </code>
    /// </example>
    public static async Task<Boolean> ExistsAsync(
        this DbConnection connection,
        InterpolatedSqlStatement statement,
        DbTransaction? transaction = null,
        TimeSpan? commandTimeout = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(connection);

        var databaseAdapter = DbConnectionPlusConfiguration.Instance.GetDatabaseAdapter(connection.GetType());

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
#pragma warning disable CA2007
                await using var reader = await command.ExecuteReaderAsync(
                    CommandBehavior.SingleRow | CommandBehavior.SingleResult,
                    cancellationToken
                ).ConfigureAwait(false);
#pragma warning restore CA2007

                return await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
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
