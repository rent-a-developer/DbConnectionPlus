// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus.SqlStatements;

/// <summary>
/// A fragment of an interpolated SQL statement that represents a parameter.
/// </summary>
/// <param name="Name">The name of the parameter.</param>
/// <param name="Value">The value of the parameter.</param>
internal readonly record struct Parameter(String Name, Object? Value) : IInterpolatedSqlStatementFragment;
