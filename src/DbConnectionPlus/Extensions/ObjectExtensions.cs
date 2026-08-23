// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus.Extensions;

/// <summary>
/// Provides extension methods for the type <see cref="object" />.
/// </summary>
internal static class ObjectExtensions
{
    /// <summary>
    /// The deepest sequence nesting that is rendered before the representation is truncated.
    /// </summary>
    private const int MaxSequenceDepth = 10;

    /// <summary>
    /// Gets the string representation of this value suffixed by the fullname of this value's type.
    /// </summary>
    /// <param name="value">The value of which to get the string representation.</param>
    /// <returns>
    /// A string representation of <paramref name="value" /> suffixed by the fullname of the value's type.
    /// </returns>
    /// <remarks>
    /// Sequences are rendered element by element as <c>[a,b,c]</c>; a value of any other unhandled type is
    /// rendered via <see cref="object.ToString" />. Nothing here reflects over the value, so the whole path stays
    /// usable under Native AOT and trimming — which matters, because this method builds the message of every
    /// conversion failure and must not fail while doing so.
    /// </remarks>
    internal static string ToDebugString(this object? value) =>
        value switch
        {
            null => "{null}",
            DBNull => "{DBNull}",
            _ => $"'{FormatValue(value, 0)}' ({value.GetType()})",
        };

    /// <summary>
    /// Gets the string representation of a sequence, as its elements separated by commas in square brackets.
    /// </summary>
    /// <param name="values">The sequence of which to get the string representation.</param>
    /// <param name="depth">The nesting depth at which <paramref name="values" /> itself sits.</param>
    /// <returns>A string representation of <paramref name="values" />.</returns>
    private static string FormatSequence(IEnumerable values, int depth) =>
        depth >= MaxSequenceDepth
            ? "[...]"
            : "[" + string.Join(",", values.Cast<object?>().Select(item => FormatValue(item, depth + 1))) + "]";

    /// <summary>
    /// Gets the string representation of a value, without the type suffix.
    /// </summary>
    /// <param name="value">The value of which to get the string representation.</param>
    /// <param name="depth">The current nesting depth, used to bound the recursion into nested sequences.</param>
    /// <returns>A string representation of <paramref name="value" />.</returns>
    private static string FormatValue(object? value, int depth) =>
        value switch
        {
            null => "{null}",

            DBNull => "{DBNull}",

            bool booleanValue => booleanValue ? "True" : "False",

            byte byteValue => byteValue.ToString("G", CultureInfo.InvariantCulture),

            byte[] bytesValue => Convert.ToBase64String(bytesValue),

            char charValue => charValue.ToString(),

            DateTime dateTimeValue => dateTimeValue.ToString("O", CultureInfo.InvariantCulture),

            DateTimeOffset dateTimeOffsetValue => dateTimeOffsetValue.ToString("O", CultureInfo.InvariantCulture),

            decimal decimalValue => decimalValue.ToString("N", CultureInfo.InvariantCulture),

            double doubleValue => doubleValue.ToString("G17", CultureInfo.InvariantCulture),

            Enum enumValue => enumValue.ToString(),

            Guid guidValue => guidValue.ToString("D", CultureInfo.InvariantCulture),

            short int16Value => int16Value.ToString("G", CultureInfo.InvariantCulture),

            int int32Value => int32Value.ToString("G", CultureInfo.InvariantCulture),

            long int64Value => int64Value.ToString("G", CultureInfo.InvariantCulture),

            IntPtr intPtrValue => intPtrValue.ToString("G", CultureInfo.InvariantCulture),

            sbyte sbyteValue => sbyteValue.ToString("G", CultureInfo.InvariantCulture),

            float singleValue => singleValue.ToString("G9", CultureInfo.InvariantCulture),

            string stringValue => stringValue,

            TimeSpan timeSpanValue => timeSpanValue.ToString("c", CultureInfo.InvariantCulture),

            ushort uint16Value => uint16Value.ToString("G", CultureInfo.InvariantCulture),

            uint uint32Value => uint32Value.ToString("G", CultureInfo.InvariantCulture),

            ulong uint64Value => uint64Value.ToString("G", CultureInfo.InvariantCulture),

            UIntPtr uintPtrValue => uintPtrValue.ToString("G", CultureInfo.InvariantCulture),

            // Must stay below the Byte[] and String arms above, both of which are sequences that have
            // a more useful representation of their own.
            IEnumerable sequenceValue => FormatSequence(sequenceValue, depth),

            // Deliberately not JsonSerializer.Serialize: the reflection-based JsonSerializer overloads
            // are unavailable under Native AOT, so a conversion error would itself fail while building
            // its message. A type that renders as its own name here simply has no ToString override.
            _ => value.ToString() ?? string.Empty,
        };
}
