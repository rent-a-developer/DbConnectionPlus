// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace RentADeveloper.DbConnectionPlus.Extensions;

/// <summary>
/// Provides extension methods for the type <see cref="Object" />.
/// </summary>
internal static class ObjectExtensions
{
    /// <summary>
    /// Gets the string representation of this value suffixed by the fullname of this value's type.
    /// </summary>
    /// <param name="value">The value of which to get the string representation.</param>
    /// <returns>
    /// A string representation of <paramref name="value" /> suffixed by the fullname of the value's type.
    /// </returns>
    internal static String ToDebugString(this Object? value) =>
        value switch
        {
            null => "{null}",
            DBNull => "{DBNull}",
            _ =>
                $"'{
                    value switch
                    {
                        Boolean booleanValue =>
                            booleanValue ? "True" : "False",

                        Byte byteValue =>
                            byteValue.ToString("G", CultureInfo.InvariantCulture),

                        Byte[] bytesValue =>
                            Convert.ToBase64String(bytesValue),

                        Char charValue =>
                            charValue.ToString(),

                        DateTime dateTimeValue =>
                            dateTimeValue.ToString("O", CultureInfo.InvariantCulture),

                        DateTimeOffset dateTimeOffsetValue =>
                            dateTimeOffsetValue.ToString("O", CultureInfo.InvariantCulture),

                        Decimal decimalValue =>
                            decimalValue.ToString("N", CultureInfo.InvariantCulture),

                        Double doubleValue =>
                            doubleValue.ToString("G17", CultureInfo.InvariantCulture),

                        Enum enumValue =>
                            enumValue.ToString(),

                        Guid guidValue =>
                            guidValue.ToString("D", CultureInfo.InvariantCulture),

                        Int16 int16Value =>
                            int16Value.ToString("G", CultureInfo.InvariantCulture),

                        Int32 int32Value =>
                            int32Value.ToString("G", CultureInfo.InvariantCulture),

                        Int64 int64Value =>
                            int64Value.ToString("G", CultureInfo.InvariantCulture),

                        IntPtr intPtrValue =>
                            intPtrValue.ToString("G", CultureInfo.InvariantCulture),

                        SByte sbyteValue =>
                            sbyteValue.ToString("G", CultureInfo.InvariantCulture),

                        Single singleValue =>
                            singleValue.ToString("G9", CultureInfo.InvariantCulture),

                        String stringValue =>
                            stringValue,

                        TimeSpan timeSpanValue =>
                            timeSpanValue.ToString("c", CultureInfo.InvariantCulture),

                        UInt16 uint16Value =>
                            uint16Value.ToString("G", CultureInfo.InvariantCulture),

                        UInt32 uint32Value =>
                            uint32Value.ToString("G", CultureInfo.InvariantCulture),

                        UInt64 uint64Value =>
                            uint64Value.ToString("G", CultureInfo.InvariantCulture),

                        UIntPtr uintPtrValue =>
                            uintPtrValue.ToString("G", CultureInfo.InvariantCulture),

                        _ =>
                            JsonSerializer.Serialize(value, jsonSerializerOptions)
                    }
                }' ({value.GetType()})"
        };

    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        WriteIndented = false,
        MaxDepth = 10,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };
}
