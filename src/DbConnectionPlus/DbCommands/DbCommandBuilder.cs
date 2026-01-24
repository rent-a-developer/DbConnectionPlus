// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using LinkDotNet.StringBuilder;
using RentADeveloper.DbConnectionPlus.Converters;
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

        var (command, temporaryTables, cancellationTokenRegistration) = BuildDbCommandCore(
            statement,
            databaseAdapter,
            connection,
            transaction,
            commandTimeout,
            commandType,
            cancellationToken
        );

        TemporaryTableDisposer[] temporaryTableDisposers = [];

        if (temporaryTables.Length > 0)
        {
            temporaryTableDisposers = BuildTemporaryTables(
                temporaryTables,
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

        var (command, temporaryTables, cancellationTokenRegistration) = BuildDbCommandCore(
            statement,
            databaseAdapter,
            connection,
            transaction,
            commandTimeout,
            commandType,
            cancellationToken
        );

        TemporaryTableDisposer[] temporaryTableDisposers = [];

        if (temporaryTables.Length > 0)
        {
            temporaryTableDisposers = await BuildTemporaryTablesAsync(
                temporaryTables,
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
    /// A tuple containing the created <see cref="DbCommand" />, the temporary tables for the statement, and the
    /// cancellation token registration for the command.
    /// </returns>
    private static (DbCommand, InterpolatedTemporaryTable[], CancellationTokenRegistration) BuildDbCommandCore(
        InterpolatedSqlStatement statement,
        IDatabaseAdapter databaseAdapter,
        DbConnection connection,
        DbTransaction? transaction = null,
        TimeSpan? commandTimeout = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default
    )
    {
        using var codeBuilder = new ValueStringBuilder(stackalloc Char[500]);
        var parameters = new Dictionary<String, Object?>(StringComparer.InvariantCultureIgnoreCase);
        var temporaryTables = new List<InterpolatedTemporaryTable>();

        foreach (var fragment in statement.Fragments)
        {
            switch (fragment)
            {
                case Parameter parameter:
                {
                    var parameterValue = parameter.Value;

                    if (parameterValue is Enum enumValue)
                    {
                        parameterValue = EnumSerializer.SerializeEnum(
                            enumValue,
                            DbConnectionExtensions.EnumSerializationMode
                        );
                    }

                    parameters.Add(parameter.Name, parameterValue);
                    break;
                }

                case InterpolatedParameter interpolatedParameter:
                {
                    var parameterName = interpolatedParameter.InferredName;

                    if (String.IsNullOrWhiteSpace(parameterName))
                    {
                        parameterName = "Parameter_" + (parameters.Count + 1).ToString(CultureInfo.InvariantCulture);
                    }

                    if (parameters.ContainsKey(parameterName))
                    {
                        var suffix = 2;

                        var newParameterName = parameterName + suffix.ToString(CultureInfo.InvariantCulture);

                        while (parameters.ContainsKey(newParameterName))
                        {
                            suffix++;
                            newParameterName = parameterName + suffix.ToString(CultureInfo.InvariantCulture);
                        }

                        parameterName = newParameterName;
                    }

                    var parameterValue = interpolatedParameter.Value;

                    if (parameterValue is Enum enumValue)
                    {
                        parameterValue = EnumSerializer.SerializeEnum(
                            enumValue,
                            DbConnectionExtensions.EnumSerializationMode
                        );
                    }

                    parameters.Add(parameterName, parameterValue);
                    codeBuilder.Append(databaseAdapter.FormatParameterName(parameterName));
                    break;
                }

                case InterpolatedTemporaryTable interpolatedTemporaryTable:
                    temporaryTables.Add(interpolatedTemporaryTable);
                    codeBuilder.Append(
                        databaseAdapter.QuoteTemporaryTableName(interpolatedTemporaryTable.Name, connection)
                    );
                    break;

                case Literal literal:
                    codeBuilder.Append(literal.Value);
                    break;
            }
        }

        var command = DbConnectionExtensions.DbCommandFactory.CreateDbCommand(
            connection,
            codeBuilder.ToString(),
            transaction,
            commandTimeout,
            commandType
        );

        var cancellationTokenRegistration = DbCommandHelper.RegisterDbCommandCancellation(
            command,
            cancellationToken
        );

        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();

            parameter.ParameterName = name;

            databaseAdapter.BindParameterValue(parameter, value);

            command.Parameters.Add(parameter);
        }

        return (command, temporaryTables.ToArray(), cancellationTokenRegistration);
    }

    /// <summary>
    /// Builds the specified temporary tables.
    /// </summary>
    /// <param name="temporaryTables">The temporary tables to build.</param>
    /// <param name="databaseAdapter">The database adapter to use to build the temporary tables.</param>
    /// <param name="connection">The database connection to use to build the temporary tables.</param>
    /// <param name="transaction">The database transaction within to build the temporary tables.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// An array of <see cref="TemporaryTableDisposer" /> instances that can be used to dispose the built
    /// temporary tables.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// <paramref name="databaseAdapter" /> does not support (local / session-scoped) temporary tables.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was cancelled via <paramref name="cancellationToken" />.
    /// </exception>
    private static TemporaryTableDisposer[] BuildTemporaryTables(
        InterpolatedTemporaryTable[] temporaryTables,
        IDatabaseAdapter databaseAdapter,
        DbConnection connection,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    )
    {
        if (!databaseAdapter.SupportsTemporaryTables(connection))
        {
            throw new NotSupportedException(
                $"The database adapter {databaseAdapter.GetType()} does not support (local / session-scoped) " +
                $"temporary tables. Therefore the temporary tables feature of DbConnectionPlus can not be used with " +
                $"this database."
            );
        }

        var temporaryTableDisposers = new TemporaryTableDisposer?[temporaryTables.Length];

        try
        {
            for (var i = 0; i < temporaryTables.Length; i++)
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
    /// <param name="temporaryTables">The temporary tables to build.</param>
    /// <param name="databaseAdapter">The database adapter to use to build the temporary tables.</param>
    /// <param name="connection">The database connection to use to build the temporary tables.</param>
    /// <param name="transaction">The database transaction within to build the temporary tables.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// <see cref="Task{TResult}.Result" /> will contain an array of <see cref="TemporaryTableDisposer" />
    /// instances that can be used to dispose the built temporary tables.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// <paramref name="databaseAdapter" /> does not support (local / session-scoped) temporary tables.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was cancelled via <paramref name="cancellationToken" />.
    /// </exception>
    private static async Task<TemporaryTableDisposer[]> BuildTemporaryTablesAsync(
        InterpolatedTemporaryTable[] temporaryTables,
        IDatabaseAdapter databaseAdapter,
        DbConnection connection,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    )
    {
        if (!databaseAdapter.SupportsTemporaryTables(connection))
        {
            throw new NotSupportedException(
                $"The database adapter {databaseAdapter.GetType()} does not support (local / session-scoped) " +
                $"temporary tables. Therefore the temporary tables feature of DbConnectionPlus can not be used with " +
                $"this database."
            );
        }

        var temporaryTableDisposers = new TemporaryTableDisposer?[temporaryTables.Length];

        try
        {
            for (var i = 0; i < temporaryTables.Length; i++)
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
