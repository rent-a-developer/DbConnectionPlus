// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using RentADeveloper.DbConnectionPlus.Extensions;

namespace RentADeveloper.DbConnectionPlus.Converters;

/// <summary>
/// Converts strings and numeric values to enum members.
/// </summary>
internal static class EnumConverter
{
    /// <summary>
    /// Converts <paramref name="value" /> to an enum member of the type <typeparamref name="TTarget" />.
    /// </summary>
    /// <typeparam name="TTarget">The type to convert <paramref name="value" /> to.</typeparam>
    /// <param name="value">
    /// The value to convert to an enum member of the type <typeparamref name="TTarget" />.
    /// </param>
    /// <returns>
    /// <paramref name="value" /> converted to an enum member of the type <typeparamref name="TTarget" />.
    /// </returns>
    /// <remarks>
    /// <paramref name="value" /> must either be a string representing the name of an enum member (case-insensitive)
    /// of the type <typeparamref name="TTarget" /> or a numeric value representing the value of an enum member of
    /// the type <typeparamref name="TTarget" />.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// <typeparamref name="TTarget" /> is not an enum type nor a nullable enum type.
    /// </exception>
    /// <exception cref="InvalidCastException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="value" /> could not be converted to the type
    /// <typeparamref name="TTarget" />, because <paramref name="value" /> is <see langword="null" /> and the type
    /// <typeparamref name="TTarget" /> is non-nullable.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="value" /> could not be converted to an enum member of the type
    /// <typeparamref name="TTarget" />, because <paramref name="value" /> is an empty string.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="value" /> could not be converted to an enum member of the type
    /// <typeparamref name="TTarget" />, because <paramref name="value" /> is a string that consists only of white-space
    ///                 characters.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="value" /> could not be converted to an enum member of the type
    /// <typeparamref name="TTarget" />, because <paramref name="value" /> is a string that does not match
    ///                 the name of any enum member of the type <typeparamref name="TTarget" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="value" /> could not be converted to an enum member of the type
    /// <typeparamref name="TTarget" />, because <paramref name="value" /> is of a type that could not be converted to
    ///                 the underlying type of the type <typeparamref name="TTarget" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="value" /> could not be converted to an enum member of the type
    /// <typeparamref name="TTarget" />, because <paramref name="value" /> is a numeric value that does not match the
    ///                 value of any enum member of the type <typeparamref name="TTarget" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="value" /> could not be converted to an enum member of the type
    /// <typeparamref name="TTarget" />, because <paramref name="value" /> is neither an enum member of that type
    ///                 nor a string nor a numeric value.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static TTarget? ConvertValueToEnumMember<TTarget>(Object? value)
    {
        var targetType = typeof(TTarget);

        // Unwrap Nullable<T> types:
        var effectiveTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (!effectiveTargetType.IsEnum)
        {
            ThrowTypeIsNeitherEnumNorNullableEnumTypeException(value, targetType);
        }

        switch (value)
        {
            case null or DBNull when default(TTarget) is null:
                return default;

            case null or DBNull when default(TTarget) is not null:
                ThrowCouldNotConvertNullToNonNullableEnumTypeException(targetType);
                return default; // Just to satisfy the compiler.

            case TTarget alreadyTargetTypeValue:
                return alreadyTargetTypeValue;

            case String stringValue when String.IsNullOrWhiteSpace(stringValue):
                ThrowCouldNotConvertEmptyOrWhitespaceStringToEnumTypeException(targetType);
                return default; // Just to satisfy the compiler.

            case String stringValue:
                if (!Enum.TryParse(effectiveTargetType, stringValue, true, out var result))
                {
                    ThrowCouldNotConvertStringToEnumTypeException(stringValue, targetType);
                }

                return (TTarget?)result;

            case Byte or SByte or Int16 or UInt16 or Int32 or UInt32 or Int64 or UInt64 or Double or Single or Decimal:
                var enumUnderlyingType = Enum.GetUnderlyingType(effectiveTargetType);

                var valueConvertedToEnumUnderlyingType = Convert.ChangeType(
                    value,
                    enumUnderlyingType,
                    CultureInfo.InvariantCulture
                );

                if (!Enum.IsDefined(effectiveTargetType, valueConvertedToEnumUnderlyingType))
                {
                    ThrowCouldNotConvertNumericValueToEnumType(value, targetType);
                }

                return (TTarget?)Enum.ToObject(effectiveTargetType, valueConvertedToEnumUnderlyingType);

            default:
                ThrowValueIsNeitherEnumValueNorStringNorNumericValueException(value, targetType);
                return default; // Just to satisfy the compiler.
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    private static void ThrowCouldNotConvertEmptyOrWhitespaceStringToEnumTypeException(Type enumType) =>
        throw new InvalidCastException(
            $"Could not convert an empty string or a string that consists only of white-space characters to an enum " +
            $"member of the type {enumType}."
        );

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    private static void ThrowCouldNotConvertNullToNonNullableEnumTypeException(Type enumType) =>
        throw new InvalidCastException(
            $"Could not convert {{null}} to an enum member of the type {enumType}."
        );

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    private static void
        ThrowCouldNotConvertNumericValueToEnumType(Object value, Type enumType) =>
        throw new InvalidCastException(
            $"Could not convert the value {value.ToDebugString()} to an enum member of the type {enumType}. That " +
            $"value does not match any of the values of the enum's members."
        );

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    private static void ThrowCouldNotConvertStringToEnumTypeException(
        String value,
        Type enumType
    ) =>
        throw new InvalidCastException(
            $"Could not convert the string '{value}' to an enum member of the type {enumType}. That string does " +
            $"not match any of the names of the enum's members."
        );

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    private static void ThrowTypeIsNeitherEnumNorNullableEnumTypeException(Object? value, Type enumType) =>
        throw new ArgumentException(
            $"Could not convert the value {value.ToDebugString()} to an enum member of the type {enumType}, because " +
            $"the type {enumType} is not an enum type.",
            nameof(enumType)
        );

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    private static void ThrowValueIsNeitherEnumValueNorStringNorNumericValueException(
        Object? value,
        Type originalEnumType
    ) =>
        throw new InvalidCastException(
            $"Could not convert the value {value.ToDebugString()} to an enum member of the type {originalEnumType}. " +
            $"The value must either be an enum value of that type or a string or a numeric value."
        );
}
