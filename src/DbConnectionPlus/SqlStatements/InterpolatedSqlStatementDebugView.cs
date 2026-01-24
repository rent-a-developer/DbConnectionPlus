// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus.SqlStatements;

/// <summary>
/// A debug view proxy for the type <see cref="InterpolatedSqlStatement" />.
/// </summary>
/// <param name="statement">The SQL statement to view in the debugger.</param>
internal sealed class InterpolatedSqlStatementDebugView(InterpolatedSqlStatement statement)
{
    /// <summary>
    /// The debug view of the SQL statement.
    /// </summary>
    public String DebugView =>
        statement.ToString();

    /// <summary>
    /// The fragments that make up the SQL statement.
    /// </summary>
    public IReadOnlyList<IInterpolatedSqlStatementFragment> Fragments =>
        statement.Fragments;
}
