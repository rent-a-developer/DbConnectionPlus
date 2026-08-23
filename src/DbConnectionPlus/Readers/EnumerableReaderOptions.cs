// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus.Readers;

/// <summary>
/// The behaviours an <see cref="EnumerableReader" /> applies to the values it reads from an entity.
/// </summary>
/// <remarks>
/// These flags exist because the temporary-table builders disagree deliberately: MySQL serializes
/// <see cref="Enum" /> and <see cref="char" /> values while it reads them, and SQL Server, SQLite, PostgreSQL and
/// Oracle hand the raw values to their bulk-copy APIs. That asymmetry is intentional and predates the AOT work —
/// do not level it out here.
/// </remarks>
[Flags]
internal enum EnumerableReaderOptions
{
    /// <summary>
    /// Report and return every value exactly as the property holds it.
    /// </summary>
    None = 0,

    /// <summary>
    /// Report <see cref="Enum" /> columns as the type they serialize to, and serialize <see cref="Enum" /> values
    /// according to <see cref="DbConnectionPlusConfiguration.EnumSerializationMode" /> while reading them.
    /// </summary>
    SerializeEnums = 1,

    /// <summary>
    /// Report <see cref="char" /> columns as <see cref="string" /> and return their values as
    /// <see cref="string" />, mirroring what the data readers of the major database systems do for CHAR columns.
    /// </summary>
    ReadCharsAsStrings = 2,
}
