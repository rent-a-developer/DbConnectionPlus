// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus.SqlStatements;

/// <summary>
/// A value, created from an expression in an interpolated string, to be passed to an SQL statement as a parameter.
/// </summary>
/// <param name="InferredName">
/// The name for the parameter inferred from the expression from which the parameter value was obtained.
/// This is <see langword="null" /> if no name could be inferred.
/// </param>
/// <param name="Value">The value of the parameter.</param>
public record InterpolatedParameter(String? InferredName, Object? Value)
    : IInterpolatedSqlStatementFragment;
