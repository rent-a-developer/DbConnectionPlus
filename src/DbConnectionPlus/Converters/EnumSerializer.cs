// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus.Converters;

/// <summary>
/// Serializes enum values according to a specified serialization mode.
/// </summary>
internal class EnumSerializer
{
    /// <summary>
    /// Serializes <paramref name="enumValue" /> according to <paramref name="serializationMode" />.
    /// </summary>
    /// <param name="enumValue">The enum value to serialize.</param>
    /// <param name="serializationMode">The mode to use to serialize <paramref name="enumValue" />.</param>
    /// <returns>
    /// <paramref name="enumValue" /> serialized according to <paramref name="serializationMode" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="enumValue" /> is null.</exception>
    /// <exception cref="NotSupportedException">
    /// <paramref name="serializationMode" /> is not a valid <see cref="EnumSerializationMode" /> value.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Object SerializeEnum(Enum enumValue, EnumSerializationMode serializationMode)
    {
        ArgumentNullException.ThrowIfNull(enumValue);

        return serializationMode switch
        {
            EnumSerializationMode.Strings =>
                enumValue.ToString(),

            EnumSerializationMode.Integers =>
                Convert.ToInt32(enumValue, CultureInfo.InvariantCulture),

            _ =>
                ThrowHelper.ThrowInvalidEnumSerializationModeException<Object>(serializationMode)
        };
    }
}
