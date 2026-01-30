// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using RentADeveloper.DbConnectionPlus.Readers;
using RentADeveloper.DbConnectionPlus.SqlStatements;
using DbCommandBuilder = RentADeveloper.DbConnectionPlus.DbCommands.DbCommandBuilder;

namespace RentADeveloper.DbConnectionPlus;

/// <summary>
/// Provides extension members for the type <see cref="DbConnection" />.
/// </summary>
public static partial class DbConnectionExtensions
{
    /// <summary>
    /// Executes the specified SQL statement and returns a <see cref="DbDataReader" /> to read the statement result
    /// set.
    /// </summary>
    /// <param name="connection">The database connection to use to execute the statement.</param>
    /// <param name="statement">The SQL statement to execute.</param>
    /// <param name="transaction">The database transaction within to execute the statement.</param>
    /// <param name="commandTimeout">The timeout to use for the execution of the statement.</param>
    /// <param name="commandBehavior">
    /// The command behavior to be passed to <see cref="DbCommand.ExecuteReader(System.Data.CommandBehavior)" />.
    /// </param>
    /// <param name="commandType">A value indicating how <paramref name="statement" /> is to be interpreted.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// An instance of <see cref="DbDataReader" /> that can be used to read the statement result set.
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
    /// using var lowStockProductsReader = connection.ExecuteReader(
    ///    $"SELECT * FROM Product WHERE UnitsInStock < {Parameter(lowStockThreshold)}"
    /// );
    /// ]]>
    /// </code>
    /// </example>
    public static DbDataReader ExecuteReader(
        this DbConnection connection,
        InterpolatedSqlStatement statement,
        DbTransaction? transaction = null,
        TimeSpan? commandTimeout = null,
        CommandBehavior commandBehavior = CommandBehavior.Default,
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

        DbDataReader? dataReader = null;

        try
        {
            OnBeforeExecutingCommand(command, statement.TemporaryTables);
            dataReader = command.ExecuteReader(commandBehavior);

            var disposeSignalingDecorator = new DisposeSignalingDataReaderDecorator(
                dataReader,
                databaseAdapter,
                cancellationToken
            );

            // ReSharper disable AccessToDisposedClosure
            disposeSignalingDecorator.OnDisposing = () => commandDisposer.Dispose();
            disposeSignalingDecorator.OnDisposingAsync = () => commandDisposer.DisposeAsync();
            // ReSharper restore AccessToDisposedClosure

            return disposeSignalingDecorator;
        }
        catch (Exception exception) when (
            databaseAdapter.WasSqlStatementCancelledByCancellationToken(exception, cancellationToken)
        )
        {
            dataReader?.Dispose();
            commandDisposer.Dispose();

            throw new OperationCanceledException(cancellationToken);
        }
    }

    /// <summary>
    /// Asynchronously executes the specified SQL statement and returns a <see cref="DbDataReader" /> to read the
    /// statement result set.
    /// </summary>
    /// <param name="connection">The database connection to use to execute the statement.</param>
    /// <param name="statement">The SQL statement to execute.</param>
    /// <param name="transaction">The database transaction within to execute the statement.</param>
    /// <param name="commandTimeout">The timeout to use for the execution of the statement.</param>
    /// <param name="commandBehavior">
    /// The command behavior to be passed to
    /// <see cref="DbCommand.ExecuteReaderAsync(System.Data.CommandBehavior,System.Threading.CancellationToken)" />.
    /// </param>
    /// <param name="commandType">A value indicating how <paramref name="statement" /> is to be interpreted.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// <see cref="Task{TResult}.Result" /> will contain an instance of <see cref="DbDataReader" /> that can be used
    /// to read the statement result set.
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
    /// await using var lowStockProductsReader = await connection.ExecuteReaderAsync(
    ///    $"SELECT * FROM Product WHERE UnitsInStock < {Parameter(lowStockThreshold)}"
    /// );
    /// ]]>
    /// </code>
    /// </example>
    public static async Task<DbDataReader> ExecuteReaderAsync(
        this DbConnection connection,
        InterpolatedSqlStatement statement,
        DbTransaction? transaction = null,
        TimeSpan? commandTimeout = null,
        CommandBehavior commandBehavior = CommandBehavior.Default,
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

        DbDataReader? dataReader = null;

        try
        {
            OnBeforeExecutingCommand(command, statement.TemporaryTables);
            dataReader = await command.ExecuteReaderAsync(commandBehavior, cancellationToken).ConfigureAwait(false);

            var disposeSignalingDecorator = new DisposeSignalingDataReaderDecorator(
                dataReader,
                databaseAdapter,
                cancellationToken
            );

            disposeSignalingDecorator.OnDisposing = () => commandDisposer.Dispose();
            disposeSignalingDecorator.OnDisposingAsync = () => commandDisposer.DisposeAsync();

            return disposeSignalingDecorator;
        }
        catch (Exception exception) when (
            databaseAdapter.WasSqlStatementCancelledByCancellationToken(exception, cancellationToken)
        )
        {
            if (dataReader is not null)
            {
                await dataReader.DisposeAsync().ConfigureAwait(false);
            }

            await commandDisposer.DisposeAsync().ConfigureAwait(false);

            throw new OperationCanceledException(cancellationToken);
        }
    }
}
