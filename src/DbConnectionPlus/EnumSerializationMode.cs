// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus;

/// <summary>
/// Specifies how <see cref="Enum" /> values are serialized when sent to a database.
/// </summary>
public enum EnumSerializationMode
{
    /// <summary>
    /// <see cref="Enum" /> values are serialized as integers.
    /// </summary>
    Integers = 0,

    /// <summary>
    /// <see cref="Enum" /> values are serialized as strings.
    /// </summary>
    Strings = 1
}
