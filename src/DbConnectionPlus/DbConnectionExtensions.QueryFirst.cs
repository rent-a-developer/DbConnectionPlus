// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using RentADeveloper.DbConnectionPlus.Materializers;
using RentADeveloper.DbConnectionPlus.SqlStatements;
using DbCommandBuilder = RentADeveloper.DbConnectionPlus.DbCommands.DbCommandBuilder;

namespace RentADeveloper.DbConnectionPlus;

/// <summary>
/// Provides extension members for the type <see cref="DbConnection" />.
/// </summary>
public static partial class DbConnectionExtensions
{
    /// <summary>
    /// Executes the specified SQL statement and materializes the first row of the result set returned by the statement
    /// into a dynamic object where each column is represented as a property of the dynamic object with the same name
    /// as the column.
    /// </summary>
    /// <param name="connection">The database connection to use to execute the statement.</param>
    /// <param name="statement">The SQL statement to execute.</param>
    /// <param name="transaction">The database transaction within to execute the statement.</param>
    /// <param name="commandTimeout">The timeout to use for the execution of the statement.</param>
    /// <param name="commandType">A value indicating how <paramref name="statement" /> is to be interpreted.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// A dynamic object containing the data of the first row of the result set returned by the statement.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The SQL statement did not return any rows.</exception>
    /// <exception cref="OperationCanceledException">
    /// The statement was cancelled via <paramref name="cancellationToken" />.
    /// </exception>
    /// <remarks>
    /// See <see cref="DbCommand.ExecuteReader(System.Data.CommandBehavior)" /> for additional exceptions this method
    /// may throw.
    /// </remarks>
    /// <example>
    /// <code>
    /// using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;
    /// 
    /// var product = connection.QueryFirst($"SELECT * FROM Product WHERE Id = {Parameter(id)}");
    ///
    /// var id = product.Id;
    /// var name = product.Name;
    /// ...
    /// </code>
    /// </example>
    public static dynamic QueryFirst(
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
                var reader = command.ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.SingleResult);

                using (reader)
                {
                    if (!reader.Read())
                    {
                        ThrowHelper.ThrowSqlStatementReturnedNoRowsException();
                    }

                    return DataRowMaterializer.Materialize(reader);
                }
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
    /// Asynchronously executes the specified SQL statement and materializes the first row of the result set returned
    /// by the statement into a dynamic object where each column is represented as a property of the dynamic object
    /// with the same name as the column.
    /// </summary>
    /// <param name="connection">The database connection to use to execute the statement.</param>
    /// <param name="statement">The SQL statement to execute.</param>
    /// <param name="transaction">The database transaction within to execute the statement.</param>
    /// <param name="commandTimeout">The timeout to use for the execution of the statement.</param>
    /// <param name="commandType">A value indicating how <paramref name="statement" /> is to be interpreted.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// <see cref="Task{TResult}.Result" /> will contain a dynamic object containing the data of the first row of the
    /// result set returned by the statement.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The SQL statement did not return any rows.</exception>
    /// <exception cref="OperationCanceledException">
    /// The statement was cancelled via <paramref name="cancellationToken" />.
    /// </exception>
    /// <remarks>
    /// See <see cref="DbCommand.ExecuteReaderAsync(System.Data.CommandBehavior,System.Threading.CancellationToken)" />
    /// for additional exceptions this method may throw.
    /// </remarks>
    /// <example>
    /// <code>
    /// using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;
    /// 
    /// var product = await connection.QueryFirstAsync($"SELECT * FROM Product WHERE Id = {Parameter(id)}");
    ///
    /// var id = product.Id;
    /// var name = product.Name;
    /// ...
    /// </code>
    /// </example>
    public static async Task<dynamic> QueryFirstAsync(
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

        using (commandDisposer)
        {
            try
            {
                OnBeforeExecutingCommand(command, statement.TemporaryTables);
                var reader = await command.ExecuteReaderAsync(
                        CommandBehavior.SingleRow | CommandBehavior.SingleResult,
                        cancellationToken
                    )
                    .ConfigureAwait(false);

                await using (reader)
                {
                    if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        ThrowHelper.ThrowSqlStatementReturnedNoRowsException();
                    }

                    return DataRowMaterializer.Materialize(reader);
                }
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
