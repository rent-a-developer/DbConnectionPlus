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
    /// Executes the specified SQL statement and materializes the result set returned by the statement into a sequence
    /// of dynamic objects.
    /// Each row of the result set is mapped to a dynamic object where each column is represented as a property of the
    /// dynamic object with the same name as the column.
    /// </summary>
    /// <param name="connection">The database connection to use to execute the statement.</param>
    /// <param name="statement">The SQL statement to execute.</param>
    /// <param name="transaction">The database transaction within to execute the statement.</param>
    /// <param name="commandTimeout">The timeout to use for the execution of the statement.</param>
    /// <param name="commandType">A value indicating how <paramref name="statement" /> is to be interpreted.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// A sequence of dynamic objects containing the data of the result set returned by the statement.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection" /> is <see langword="null" />.</exception>
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
    /// var products = connection.Query($"SELECT * FROM Product WHERE CategoryId = {Parameter(categoryId)}");
    ///
    /// foreach (var product in products)
    /// {
    ///     var id = product.Id;
    ///     var name = product.Name;
    ///     ...
    /// }
    /// </code>
    /// </example>
    public static IEnumerable<dynamic> Query(
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
            DbDataReader? reader = null;

            try
            {
                OnBeforeExecutingCommand(command, statement.TemporaryTables);
                reader = command.ExecuteReader(CommandBehavior.SequentialAccess);
            }
            catch (Exception exception) when (
                databaseAdapter.WasSqlStatementCancelledByCancellationToken(exception, cancellationToken)
            )
            {
                reader?.Dispose();

                throw new OperationCanceledException(cancellationToken);
            }

            using (reader)
            {
                // We can't use "while (reader.Read())" here because, the "yield return" statement cannot be placed
                // inside a try/catch block.
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        if (!reader.Read())
                        {
                            yield break;
                        }
                    }
                    catch (Exception exception) when (
                        databaseAdapter.WasSqlStatementCancelledByCancellationToken(exception, cancellationToken)
                    )
                    {
                        throw new OperationCanceledException(cancellationToken);
                    }

                    yield return DataRowMaterializer.Materialize(reader);
                }
            }
        }
    }

    /// <summary>
    /// Asynchronously executes the specified SQL statement and materializes the result set returned by the statement
    /// into a sequence of dynamic objects.
    /// Each row of the result set is mapped to a dynamic object where each column is represented as a property of the
    /// dynamic object with the same name as the column.
    /// </summary>
    /// <param name="connection">The database connection to use to execute the statement.</param>
    /// <param name="statement">The SQL statement to execute.</param>
    /// <param name="transaction">The database transaction within to execute the statement.</param>
    /// <param name="commandTimeout">The timeout to use for the execution of the statement.</param>
    /// <param name="commandType">A value indicating how <paramref name="statement" /> is to be interpreted.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// An asynchronous sequence of dynamic objects containing the data of the result set returned by the statement.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection" /> is <see langword="null" />.</exception>
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
    /// var products = connection.QueryAsync($"SELECT * FROM Product WHERE CategoryId = {Parameter(categoryId)}");
    ///
    /// await foreach (var product in products)
    /// {
    ///     var id = product.Id;
    ///     var name = product.Name;
    ///     ...
    /// }
    /// </code>
    /// </example>
    public static async IAsyncEnumerable<dynamic> QueryAsync(
        this DbConnection connection,
        InterpolatedSqlStatement statement,
        DbTransaction? transaction = null,
        TimeSpan? commandTimeout = null,
        CommandType commandType = CommandType.Text,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
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
            DbDataReader? reader = null;

            try
            {
                OnBeforeExecutingCommand(command, statement.TemporaryTables);
                reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception) when (
                databaseAdapter.WasSqlStatementCancelledByCancellationToken(exception, cancellationToken)
            )
            {
                if (reader is not null)
                {
                    await reader.DisposeAsync().ConfigureAwait(false);
                }

                throw new OperationCanceledException(cancellationToken);
            }

            await using (reader)
            {
                // We can't use "while (await reader.ReadAsync())" here because, the "yield return" statement cannot
                // be placed inside a try/catch block.
                while (true)
                {
                    try
                    {
                        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            yield break;
                        }
                    }
                    catch (Exception exception) when (
                        databaseAdapter.WasSqlStatementCancelledByCancellationToken(exception, cancellationToken)
                    )
                    {
                        throw new OperationCanceledException(cancellationToken);
                    }

                    yield return DataRowMaterializer.Materialize(reader);
                }
            }
        }
    }
}
