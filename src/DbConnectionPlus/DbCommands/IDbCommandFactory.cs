// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus.DbCommands;

/// <summary>
/// Represents a factory that creates instances of <see cref="DbCommand" />.
/// </summary>
public interface IDbCommandFactory
{
    /// <summary>
    /// Creates an instance of <see cref="DbCommand" /> with the specified settings.
    /// </summary>
    /// <param name="connection">The connection to use to create the <see cref="DbCommand" />.</param>
    /// <param name="commandText">The command text to assign to the <see cref="DbCommand" />.</param>
    /// <param name="transaction">The transaction to assign to the <see cref="DbCommand" />.</param>
    /// <param name="commandTimeout">The command timeout to assign to the <see cref="DbCommand" />.</param>
    /// <param name="commandType">The <see cref="CommandType" /> to assign to the <see cref="DbCommand" />.</param>
    /// <returns>An instance of <see cref="DbCommand" /> with the specified settings.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="connection" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="commandText" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    public DbCommand CreateDbCommand(
        DbConnection connection,
        String commandText,
        DbTransaction? transaction = null,
        TimeSpan? commandTimeout = null,
        CommandType commandType = CommandType.Text
    );
}
