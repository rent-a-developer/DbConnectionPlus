// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus.SqlStatements;

/// <summary>
/// A fragment of an interpolated SQL statement that represents a literal string.
/// </summary>
/// <param name="Value">The literal string.</param>
internal record Literal(String Value) : IInterpolatedSqlStatementFragment;
