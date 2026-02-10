// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using LinkDotNet.StringBuilder;
using RentADeveloper.DbConnectionPlus.SqlStatements;

namespace RentADeveloper.DbConnectionPlus.DbCommands;

/// <summary>
/// Builds <see cref="DbCommand" /> instances from instances of <see cref="InterpolatedSqlStatement" />.
/// </summary>
internal static class DbCommandBuilder
{
    /// <summary>
    /// Builds a new <see cref="DbCommand" /> for the specified SQL statement.
    /// Adds the parameters of the specified SQL statement to the command, creates the temporary tables for the
    /// specified SQL statement and registers a callback to cancel the command if <paramref name="cancellationToken " />
    /// is triggered.
    /// </summary>
    /// <param name="statement">The SQL statement for which to create the command.</param>
    /// <param name="databaseAdapter">The database adapter to use to create the command.</param>
    /// <param name="connection">The database connection to use to create the command.</param>
    /// <param name="transaction">The database transaction to assign to the command.</param>
    /// <param name="commandTimeout">The command timeout to assign to the command.</param>
    /// <param name="commandType">The <see cref="CommandType" /> to assign to the command.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the command.</param>
    /// <returns>
    /// A tuple containing the created <see cref="DbCommand" /> and a disposer for the command and its associated
    /// resources.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="databaseAdapter" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="connection" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// <paramref name="statement" /> contains temporary tables and <paramref name="databaseAdapter" /> does not support
    /// (local / session-scoped) temporary tables.
    /// </exception>
    internal static (DbCommand, DbCommandDisposer) BuildDbCommand(
        InterpolatedSqlStatement statement,
        IDatabaseAdapter databaseAdapter,
        DbConnection connection,
        DbTransaction? transaction = null,
        TimeSpan? commandTimeout = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(databaseAdapter);
        ArgumentNullException.ThrowIfNull(connection);

        var (command, cancellationTokenRegistration) = BuildDbCommandCore(
            statement,
            databaseAdapter,
            connection,
            transaction,
            commandTimeout,
            commandType,
            cancellationToken
        );

        TemporaryTableDisposer[] temporaryTableDisposers = [];

        if (statement.TemporaryTables.Count > 0)
        {
            temporaryTableDisposers = BuildTemporaryTables(
                statement.TemporaryTables,
                databaseAdapter,
                connection,
                transaction,
                cancellationToken
            );
        }

        return (command, new(command, temporaryTableDisposers, cancellationTokenRegistration));
    }

    /// <summary>
    /// Asynchronously builds a new <see cref="DbCommand" /> for the specified SQL statement.
    /// Adds the parameters of the specified SQL statement to the command, creates the temporary tables for the
    /// specified SQL statement and registers a callback to cancel the command if <paramref name="cancellationToken" />
    /// is triggered.
    /// </summary>
    /// <param name="statement">The SQL statement for which to create the command.</param>
    /// <param name="databaseAdapter">The database adapter to use to create the command.</param>
    /// <param name="connection">The database connection to use to create the command.</param>
    /// <param name="transaction">The database transaction to assign to the command.</param>
    /// <param name="commandTimeout">The command timeout to assign to the command.</param>
    /// <param name="commandType">The <see cref="CommandType" /> to assign to the command.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the command.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// <see cref="Task{TResult}.Result" /> will contain a tuple containing the created <see cref="DbCommand" /> and
    /// a disposer for the command and its associated resources.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="connection" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="databaseAdapter" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// <paramref name="statement" /> contains temporary tables and <paramref name="databaseAdapter" /> does not support
    /// (local / session-scoped) temporary tables.
    /// </exception>
    internal static async Task<(DbCommand, DbCommandDisposer)> BuildDbCommandAsync(
        InterpolatedSqlStatement statement,
        IDatabaseAdapter databaseAdapter,
        DbConnection connection,
        DbTransaction? transaction = null,
        TimeSpan? commandTimeout = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(databaseAdapter);

        var (command, cancellationTokenRegistration) = BuildDbCommandCore(
            statement,
            databaseAdapter,
            connection,
            transaction,
            commandTimeout,
            commandType,
            cancellationToken
        );

        TemporaryTableDisposer[] temporaryTableDisposers = [];

        if (statement.TemporaryTables.Count > 0)
        {
            temporaryTableDisposers = await BuildTemporaryTablesAsync(
                statement.TemporaryTables,
                databaseAdapter,
                connection,
                transaction,
                cancellationToken
            ).ConfigureAwait(false);
        }

        return (command, new(command, temporaryTableDisposers, cancellationTokenRegistration));
    }

    /// <summary>
    /// Builds a new <see cref="DbCommand" /> for the specified SQL statement.
    /// Adds the parameters of the specified SQL statement to the command and registers a callback to cancel the
    /// command if <paramref name="cancellationToken" /> is triggered.
    /// </summary>
    /// <param name="statement">The SQL statement for which to create the command.</param>
    /// <param name="databaseAdapter">The database adapter to use to create the command.</param>
    /// <param name="connection">The database connection to use to create the command.</param>
    /// <param name="transaction">The database transaction to assign to the command.</param>
    /// <param name="commandTimeout">The command timeout to assign to the command.</param>
    /// <param name="commandType">The <see cref="CommandType" /> to assign to the command.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the command.</param>
    /// <returns>
    /// A tuple containing the created <see cref="DbCommand" /> and the cancellation token registration for the command.
    /// </returns>
    private static (DbCommand, CancellationTokenRegistration) BuildDbCommandCore(
        InterpolatedSqlStatement statement,
        IDatabaseAdapter databaseAdapter,
        DbConnection connection,
        DbTransaction? transaction = null,
        TimeSpan? commandTimeout = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default
    )
    {
        using var codeBuilder = new ValueStringBuilder(stackalloc char[2048]);
        var parameterNameOccurrences = new Dictionary<String, Int32>(StringComparer.OrdinalIgnoreCase);
        var parameterCount = 0;

        var command = connection.CreateCommand();

        command.Transaction = transaction;
        command.CommandType = commandType;

        if (commandTimeout is not null)
        {
            command.CommandTimeout = (Int32)commandTimeout.Value.TotalSeconds;
        }

        var dbParameters = command.Parameters;

        foreach (var fragment in statement.Fragments)
        {
            switch (fragment)
            {
                case Literal literal:
                    codeBuilder.Append(literal.Value);
                    break;

                case InterpolatedParameter interpolatedParameter:
                {
                    var parameterName = interpolatedParameter.InferredName ??
                                        "Parameter_" + (parameterCount + 1);

                    if (parameterNameOccurrences.TryGetValue(parameterName, out var count))
                    {
                        // Parameter name is already used, so we append a suffix to make it unique.
                        count++;
                        parameterNameOccurrences[parameterName] = count;
                        parameterName += count;
                    }
                    else
                    {
                        parameterNameOccurrences[parameterName] = 1;
                    }

                    var dbParameter = command.CreateParameter();
                    dbParameter.ParameterName = parameterName;
                    databaseAdapter.BindParameterValue(dbParameter, interpolatedParameter.Value);
                    dbParameters.Add(dbParameter);

                    codeBuilder.Append(databaseAdapter.FormatParameterName(parameterName));

                    parameterCount++;
                    break;
                }

                case Parameter parameter:
                {
                    var dbParameter = command.CreateParameter();
                    dbParameter.ParameterName = parameter.Name;
                    databaseAdapter.BindParameterValue(dbParameter, parameter.Value);
                    dbParameters.Add(dbParameter);

                    parameterNameOccurrences[parameter.Name] = 1;
                    parameterCount++;
                    break;
                }

                case InterpolatedTemporaryTable interpolatedTemporaryTable:
                    codeBuilder.Append(
                        databaseAdapter.QuoteTemporaryTableName(interpolatedTemporaryTable.Name, connection)
                    );
                    break;
            }
        }

        command.CommandText = codeBuilder.ToString();

        var cancellationTokenRegistration = DbCommandHelper.RegisterDbCommandCancellation(command, cancellationToken);

        return (command, cancellationTokenRegistration);
    }

    /// <summary>
    /// Builds the specified temporary tables.
    /// </summary>
    /// <param name="temporaryTables">The tables to build.</param>
    /// <param name="databaseAdapter">The database adapter to use to build the tables.</param>
    /// <param name="connection">The database connection to use to build the tables.</param>
    /// <param name="transaction">The database transaction within to build the tables.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// An array of <see cref="TemporaryTableDisposer" /> instances that can be used to dispose the built
    /// tables.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// <paramref name="databaseAdapter" /> does not support (local / session-scoped) temporary tables.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was cancelled via <paramref name="cancellationToken" />.
    /// </exception>
    private static TemporaryTableDisposer[] BuildTemporaryTables(
        IReadOnlyList<InterpolatedTemporaryTable> temporaryTables,
        IDatabaseAdapter databaseAdapter,
        DbConnection connection,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    )
    {
        if (!databaseAdapter.SupportsTemporaryTables(connection))
        {
            ThrowHelper.ThrowDatabaseAdapterDoesNotSupportTemporaryTablesException(databaseAdapter);
        }

        var temporaryTableDisposers = new TemporaryTableDisposer?[temporaryTables.Count];

        try
        {
            for (var i = 0; i < temporaryTables.Count; i++)
            {
                var interpolatedTemporaryTable = temporaryTables[i];

                temporaryTableDisposers[i] = databaseAdapter.TemporaryTableBuilder.BuildTemporaryTable(
                    connection,
                    transaction,
                    interpolatedTemporaryTable.Name,
                    interpolatedTemporaryTable.Values,
                    interpolatedTemporaryTable.ValuesType,
                    cancellationToken
                );
            }
        }
        catch (Exception exception) when (
            databaseAdapter.WasSqlStatementCancelledByCancellationToken(exception, cancellationToken)
        )
        {
            foreach (var temporaryTableDisposer in temporaryTableDisposers)
            {
                temporaryTableDisposer?.Dispose();
            }

            throw new OperationCanceledException(cancellationToken);
        }

        return temporaryTableDisposers!;
    }

    /// <summary>
    /// Asynchronously builds the specified temporary tables.
    /// </summary>
    /// <param name="temporaryTables">The tables to build.</param>
    /// <param name="databaseAdapter">The database adapter to use to build the tables.</param>
    /// <param name="connection">The database connection to use to build the tables.</param>
    /// <param name="transaction">The database transaction within to build the tables.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// <see cref="Task{TResult}.Result" /> will contain an array of <see cref="TemporaryTableDisposer" />
    /// instances that can be used to dispose the built tables.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// <paramref name="databaseAdapter" /> does not support (local / session-scoped) temporary tables.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was cancelled via <paramref name="cancellationToken" />.
    /// </exception>
    private static async Task<TemporaryTableDisposer[]> BuildTemporaryTablesAsync(
        IReadOnlyList<InterpolatedTemporaryTable> temporaryTables,
        IDatabaseAdapter databaseAdapter,
        DbConnection connection,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    )
    {
        if (!databaseAdapter.SupportsTemporaryTables(connection))
        {
            ThrowHelper.ThrowDatabaseAdapterDoesNotSupportTemporaryTablesException(databaseAdapter);
        }

        var temporaryTableDisposers = new TemporaryTableDisposer?[temporaryTables.Count];

        try
        {
            for (var i = 0; i < temporaryTables.Count; i++)
            {
                var interpolatedTemporaryTable = temporaryTables[i];

                temporaryTableDisposers[i] = await databaseAdapter.TemporaryTableBuilder.BuildTemporaryTableAsync(
                    connection,
                    transaction,
                    interpolatedTemporaryTable.Name,
                    interpolatedTemporaryTable.Values,
                    interpolatedTemporaryTable.ValuesType,
                    cancellationToken
                ).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (
            databaseAdapter.WasSqlStatementCancelledByCancellationToken(exception, cancellationToken)
        )
        {
            foreach (var temporaryTableDisposer in temporaryTableDisposers)
            {
                if (temporaryTableDisposer is not null)
                {
                    await temporaryTableDisposer.DisposeAsync().ConfigureAwait(false);
                }
            }

            throw new OperationCanceledException(cancellationToken);
        }

        return temporaryTableDisposers!;
    }
}
