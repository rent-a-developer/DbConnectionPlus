// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus.DbCommands;

/// <summary>
/// The default implementation of <see cref="IDbCommandFactory" />.
/// </summary>
internal sealed class DefaultDbCommandFactory : IDbCommandFactory
{
    /// <inheritdoc />
    public DbCommand CreateDbCommand(
        DbConnection connection,
        String commandText,
        DbTransaction? transaction = null,
        TimeSpan? commandTimeout = null,
        CommandType commandType = CommandType.Text
    )
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(commandText);

        var command = connection.CreateCommand();

#pragma warning disable CA2100
        command.CommandText = commandText;
#pragma warning restore CA2100

        command.Transaction = transaction;
        command.CommandType = commandType;

        if (commandTimeout is not null)
        {
            command.CommandTimeout = (Int32)commandTimeout.Value.TotalSeconds;
        }

        return command;
    }
}
