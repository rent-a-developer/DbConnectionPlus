// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus.SqlStatements;

/// <summary>
/// A sequence of values, created from an expression in an interpolated string, to be passed to an SQL statement as a
/// temporary table.
/// </summary>
/// <param name="Name">The name for the table.</param>
/// <param name="Values">The values with which to populate the table.</param>
/// <param name="ValuesType">The type of values in <paramref name="Values" />.</param>
/// <remarks>
/// The annotation on <paramref name="ValuesType" /> is applied to both the parameter and the generated property:
/// the type is read back off the property when the temporary table is built, and without the annotation on the
/// property the trimmer would not see that requirement.
/// </remarks>
public record InterpolatedTemporaryTable(
    String Name,
    IEnumerable Values,
    [property: DynamicallyAccessedMembers(EntityHelper.TemporaryTableValueMemberTypes)]
    [param: DynamicallyAccessedMembers(EntityHelper.TemporaryTableValueMemberTypes)]
    Type ValuesType
)
    : IInterpolatedSqlStatementFragment;
