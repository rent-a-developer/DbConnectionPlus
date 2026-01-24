// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus.SqlStatements;

/// <summary>
/// A sequence of values, created from an expression in an interpolated string, to be passed to an SQL statement as a
/// temporary table.
/// </summary>
/// <param name="Name">The name for the temporary table.</param>
/// <param name="Values">The values to populate the temporary table with.</param>
/// <param name="ValuesType">The type of values in <paramref name="Values" />.</param>
public readonly record struct InterpolatedTemporaryTable(String Name, IEnumerable Values, Type ValuesType)
    : IInterpolatedSqlStatementFragment;
