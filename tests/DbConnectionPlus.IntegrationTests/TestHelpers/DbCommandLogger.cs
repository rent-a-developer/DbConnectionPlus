using System.Data.Common;
using LinkDotNet.StringBuilder;
using RentADeveloper.DbConnectionPlus.Extensions;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.TestHelpers;

/// <summary>
/// Logs database commands executed during tests for debugging purposes.
/// </summary>
public static class DbCommandLogger
{
    /// <summary>
    /// Determines whether database commands should be logged.
    /// </summary>
    public static Boolean LogCommands { get; set; } = true;

    /// <summary>
    /// Logs the specified database command and the temporary tables used in the command.
    /// </summary>
    /// <param name="command">The <see cref="DbCommand" /> to log.</param>
    /// <param name="temporaryTables">The temporary tables used in the command.</param>
    public static void LogDbCommand(DbCommand command, IReadOnlyList<InterpolatedTemporaryTable> temporaryTables)
    {
        if (!LogCommands)
        {
            return;
        }

        using var logMessageBuilder = new ValueStringBuilder(stackalloc Char[500]);

        logMessageBuilder.AppendLine();
        logMessageBuilder.AppendLine("-----------------");
        logMessageBuilder.AppendLine("Executing Command");
        logMessageBuilder.AppendLine("-----------------");
        logMessageBuilder.AppendLine(command.CommandText.Trim());

        if (command.Parameters.Count > 0)
        {
            logMessageBuilder.AppendLine();
            logMessageBuilder.AppendLine("----------");
            logMessageBuilder.AppendLine("Parameters");
            logMessageBuilder.AppendLine("----------");

            foreach (DbParameter parameter in command.Parameters)
            {
                logMessageBuilder.Append(" - ");
                logMessageBuilder.Append(parameter.ParameterName);

                logMessageBuilder.Append(" (");
                logMessageBuilder.Append(parameter.Direction.ToString());
                logMessageBuilder.Append(")");

                logMessageBuilder.Append(" = ");
                logMessageBuilder.Append(parameter.Value.ToDebugString());
                logMessageBuilder.AppendLine();
            }
        }

        if (temporaryTables.Count > 0)
        {
            logMessageBuilder.AppendLine();
            logMessageBuilder.AppendLine("----------------");
            logMessageBuilder.AppendLine("Temporary Tables");
            logMessageBuilder.AppendLine("----------------");

            foreach (var temporaryTable in temporaryTables)
            {
                logMessageBuilder.AppendLine();
                logMessageBuilder.AppendLine(temporaryTable.Name);
                logMessageBuilder.AppendLine(new String('-', temporaryTable.Name.Length));

                foreach (var value in temporaryTable.Values)
                {
                    logMessageBuilder.AppendLine(value.ToDebugString());
                }
            }
        }

        Console.WriteLine(logMessageBuilder.ToString());
    }
}
