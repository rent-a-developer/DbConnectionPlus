using System.Data.Common;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.TestHelpers;

/// <summary>
/// An implementation of <see cref="IDbCommandFactory" /> that supports delaying created commands.
/// </summary>
public class DelayDbCommandFactory(ITestDatabaseProvider testDatabaseProvider) : IDbCommandFactory
{
    /// <summary>
    /// Determines whether the next database command created throw this instance will be delayed by 2 seconds.
    /// If set to <see langword="true" />, a 2 second delay will be injected into the next database command created by
    /// this factory. Subsequent commands will not be delayed unless this property is set to <see langword="true" />
    /// again.
    /// </summary>
    public Boolean DelayNextDbCommand { get; set; }

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
        if (this.DelayNextDbCommand)
        {
            command.CommandText = testDatabaseProvider.DelayTwoSecondsStatement + commandText;
            this.DelayNextDbCommand = false;
        }
        else
        {
            command.CommandText = commandText;
        }
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
