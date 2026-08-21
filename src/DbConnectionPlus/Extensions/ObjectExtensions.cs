// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

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
    /// <remarks>
    /// Sequences are rendered element by element as <c>[a,b,c]</c>; a value of any other unhandled type is
    /// rendered via <see cref="Object.ToString" />. Nothing here reflects over the value, so the whole path stays
    /// usable under Native AOT and trimming — which matters, because this method builds the message of every
    /// conversion failure and must not fail while doing so.
    /// </remarks>
    internal static String ToDebugString(this Object? value) =>
        value switch
        {
            null => "{null}",
            DBNull => "{DBNull}",
            _ => $"'{FormatValue(value, 0)}' ({value.GetType()})"
        };

    /// <summary>
    /// Gets the string representation of a value, without the type suffix.
    /// </summary>
    /// <param name="value">The value of which to get the string representation.</param>
    /// <param name="depth">The current nesting depth, used to bound the recursion into nested sequences.</param>
    /// <returns>A string representation of <paramref name="value" />.</returns>
    private static String FormatValue(Object? value, Int32 depth) =>
        value switch
        {
            null =>
                "{null}",

            DBNull =>
                "{DBNull}",

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

            // Must stay below the Byte[] and String arms above, both of which are sequences that have
            // a more useful representation of their own.
            IEnumerable sequenceValue =>
                FormatSequence(sequenceValue, depth),

            // Deliberately not JsonSerializer.Serialize: the reflection-based JsonSerializer overloads
            // are unavailable under Native AOT, so a conversion error would itself fail while building
            // its message. A type that renders as its own name here simply has no ToString override.
            _ =>
                value.ToString() ?? String.Empty
        };

    /// <summary>
    /// Gets the string representation of a sequence, as its elements separated by commas in square brackets.
    /// </summary>
    /// <param name="values">The sequence of which to get the string representation.</param>
    /// <param name="depth">The nesting depth at which <paramref name="values" /> itself sits.</param>
    /// <returns>A string representation of <paramref name="values" />.</returns>
    private static String FormatSequence(IEnumerable values, Int32 depth) =>
        depth >= MaxSequenceDepth
            ? "[...]"
            : "[" + String.Join(",", values.Cast<Object?>().Select(item => FormatValue(item, depth + 1))) + "]";

    /// <summary>
    /// The deepest sequence nesting that is rendered before the representation is truncated.
    /// </summary>
    private const Int32 MaxSequenceDepth = 10;
}
