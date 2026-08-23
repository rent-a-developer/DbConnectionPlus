// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus.PackageConsumption.Aot;

// =====================================================================================================
// The types the smoke test materializes.
//
// They are plain classes, never records: a positional record's properties are init-only, so the
// property-setter strategy this file exists to exercise would not apply to them at all.
//
// Reading a property to assert its value is fine. Under Native AOT, reflection metadata is a SEPARATE
// OUTPUT from compiled code: ILC emits a PropertyInfo only for members it decides are reflectable, and a
// direct call is deliberately not evidence of that - if it were, every method in the closure would carry a
// name and a signature in the binary. So a statically rooted member runs correctly and is still invisible
// to reflection, and GetProperties() returns an empty array rather than throwing. Measured on this
// toolchain (net10.0, PublishAot): with 1 of 6 properties rooted by a direct call, reflection found 0
// members; with a positional record whose ToString and Equals were both invoked - so every getter
// demonstrably ran - reflection still found 0 members.
//
// Rooting therefore does not substitute for a preservation mechanism, and the mechanism need not be
// [DynamicallyAccessedMembers]: ILLink.Descriptors.xml, [DynamicDependency], and a literal
// typeof(X).GetProperties() that the trimmer recognises intrinsically all work too.
//
// This is an AOT property, not a trimming property. IL trimming alone edits ordinary assemblies in place,
// so a kept member keeps its metadata row and reflection does find it - measured, the same two cases
// returned 1 and 6 members under PublishTrimmed without PublishAot. This consumer never publishes that
// way; see README.md.
// =====================================================================================================

/// <summary>The status of a <see cref="SmokeEntity" />. Stored as an integer.</summary>
public enum SmokeStatus
{
    /// <summary>The entity has not been classified yet.</summary>
    Unknown = 0,

    /// <summary>The entity is in use.</summary>
    Active = 1,

    /// <summary>The entity is retained for reference only.</summary>
    Archived = 2,
}

/// <summary>
/// Materialized through the property-setter strategy: a parameterless constructor plus writable properties.
/// This is the strategy the zero-binding guard protects.
/// </summary>
public sealed class SmokeEntity
{
    /// <summary>The primary key. Not database-generated, so it takes part in the INSERT.</summary>
    [Key]
    public Int64 Id { get; set; }

    /// <summary>A plain string column.</summary>
    public String Name { get; set; } = String.Empty;

    /// <summary>Stored as TEXT by SQLite, so materializing it exercises a String to Decimal conversion.</summary>
    public Decimal Balance { get; set; }

    /// <summary>Stored as INTEGER by SQLite, so materializing it exercises an Int64 to Boolean conversion.</summary>
    public Boolean IsActive { get; set; }

    /// <summary>Stored as TEXT by SQLite, so materializing it exercises a String to DateTime conversion.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Stored as TEXT by SQLite, so materializing it exercises a String to Guid conversion.</summary>
    public Guid ExternalId { get; set; }

    /// <summary>An enum property, materialized from the integer SQLite returns.</summary>
    public SmokeStatus Status { get; set; }

    /// <summary>SQLite reports INTEGER columns as <see cref="Int64" />, so this narrows on materialization.</summary>
    public Int32 Quantity { get; set; }
}

/// <summary>
/// Materialized through constructor injection: no writable properties, so the materializer has to find and
/// invoke the constructor whose parameters match the result set.
/// </summary>
public sealed class ImmutableSmokeEntity
{
    /// <summary>Initializes a new instance of the <see cref="ImmutableSmokeEntity" /> class.</summary>
    /// <param name="id">The primary key.</param>
    /// <param name="name">The name.</param>
    /// <param name="balance">The balance.</param>
    public ImmutableSmokeEntity(Int64 id, String name, Decimal balance)
    {
        this.Id = id;
        this.Name = name;
        this.Balance = balance;
    }

    /// <summary>The primary key.</summary>
    public Int64 Id { get; }

    /// <summary>The name.</summary>
    public String Name { get; }

    /// <summary>The balance.</summary>
    public Decimal Balance { get; }
}

// =====================================================================================================
// The enum types the value tuple cases materialize.
//
// One distinct enum per case. Whatever preserves - or fails to preserve - the members of one of them must
// not be able to make another case pass, so no two cases share a type.
//
// No member of any of these enums is referenced anywhere in this project. The rows are written with SQL
// literals, and the assertions compare the underlying integer rather than a named member. A static
// reference to a member could preserve it and make the case pass for the wrong reason. The member names
// appear in this project only as string literals, which preserve nothing.
//
// Nothing in the library annotates these types: [DynamicallyAccessedMembers] on the query method's type
// parameter reaches the value tuple, and ILLink.Descriptors.xml reaches System.ValueTuple`1-`8 - neither
// reaches a type used as a tuple field. Their members survive because the trimmer preserves the members of
// an enum it keeps (dotnet/runtime#100814, dotnet/runtime#105351). These cases are the regression guard for
// that behaviour, on the two code paths that depend on it: Enum.IsDefined/Enum.ToObject for a numeric
// column, and Enum.TryParse by name for a text one.
//
// The values are deliberately not 0, 1 or 2, so that a default-valued or off-by-one bind is
// distinguishable from a correct one.
// =====================================================================================================

/// <summary>
/// Materialized from an <c>INTEGER</c> column into a field of a value tuple with seven fields or fewer.
/// </summary>
public enum FlatTupleNumericEnum
{
    /// <summary>Never stored. Present so that a failed bind has a member to land on.</summary>
    Unset = 0,

    /// <summary>The member the smoke test stores and asserts.</summary>
    FlatNumericChosen = 71,

    /// <summary>Never stored. Present so that an off-by-one bind is visible.</summary>
    FlatNumericOther = 72,
}

/// <summary>
/// Materialized from an <c>INTEGER</c> column into the field of the nested value tuple that a tuple with
/// more than seven fields keeps in its <c>Rest</c> field.
/// </summary>
public enum NestedTupleNumericEnum
{
    /// <summary>Never stored. Present so that a failed bind has a member to land on.</summary>
    Unset = 0,

    /// <summary>The member the smoke test stores and asserts.</summary>
    NestedNumericChosen = 81,

    /// <summary>Never stored. Present so that an off-by-one bind is visible.</summary>
    NestedNumericOther = 82,
}

/// <summary>
/// Parsed by name from a <c>TEXT</c> column into a field of a value tuple with seven fields or fewer. This
/// is the path that needs the enum's member <em>names</em>, not just its values.
/// </summary>
public enum FlatTupleNamedEnum
{
    /// <summary>Never stored. Present so that a failed bind has a member to land on.</summary>
    Unset = 0,

    /// <summary>The member the smoke test stores, by name, and asserts.</summary>
    FlatNamedChosen = 91,

    /// <summary>Never stored. Present so that a parse landing on the wrong member is visible.</summary>
    FlatNamedOther = 92,
}

/// <summary>
/// Parsed by name from a <c>TEXT</c> column into the field of the nested value tuple that a tuple with more
/// than seven fields keeps in its <c>Rest</c> field. This is the narrowest case in the whole smoke test: the
/// type is reached through the generic arguments of another type at run time, and the conversion needs its
/// member names.
/// </summary>
public enum NestedTupleNamedEnum
{
    /// <summary>Never stored. Present so that a failed bind has a member to land on.</summary>
    Unset = 0,

    /// <summary>The member the smoke test stores, by name, and asserts.</summary>
    NestedNamedChosen = 101,

    /// <summary>Never stored. Present so that a parse landing on the wrong member is visible.</summary>
    NestedNamedOther = 102,
}

/// <summary>
/// Streamed into a multi-column temporary table, which is the path <c>EnumerableReader</c> serves.
/// </summary>
public sealed class SmokeItem
{
    /// <summary>The primary key.</summary>
    [Key]
    public Int64 Id { get; set; }

    /// <summary>The label of the item.</summary>
    public String Label { get; set; } = String.Empty;
}
