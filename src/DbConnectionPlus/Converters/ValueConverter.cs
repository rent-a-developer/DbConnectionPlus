// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using RentADeveloper.DbConnectionPlus.Extensions;

namespace RentADeveloper.DbConnectionPlus.Converters;

/// <summary>
/// Converts values to different types.
/// </summary>
internal static class ValueConverter
{
    /// <summary>
    /// Determines whether this converter can convert a value of the type <paramref name="sourceType" /> to the type
    /// <paramref name="targetType" />.
    /// </summary>
    /// <param name="sourceType">The type to convert from.</param>
    /// <param name="targetType">The type to convert to.</param>
    /// <returns>
    /// <see langword="true" /> if this converter can convert a value of the type <paramref name="sourceType" /> to the
    /// type <paramref name="targetType" />; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="sourceType" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="targetType" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    internal static Boolean CanConvert(Type sourceType, Type targetType)
    {
        ArgumentNullException.ThrowIfNull(sourceType);
        ArgumentNullException.ThrowIfNull(targetType);

        var effectiveSourceType = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
        var effectiveTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (
            effectiveSourceType == effectiveTargetType ||
            effectiveTargetType == typeof(Object)
        )
        {
            // Conversion to same type or to Object is always possible.
            return true;
        }

        if (effectiveSourceType.IsEnum)
        {
            return IsSupportedEnumConversionType(effectiveTargetType);
        }

        if (effectiveTargetType.IsEnum)
        {
            return IsSupportedEnumConversionType(effectiveSourceType);
        }

        return supportedConversions.Contains((effectiveSourceType, effectiveTargetType));
    }

    /// <summary>
    /// Converts <paramref name="value" /> to the type <typeparamref name="TTarget" />.
    /// </summary>
    /// <typeparam name="TTarget">The type to convert <paramref name="value" /> to.</typeparam>
    /// <param name="value">The value to convert to the type <typeparamref name="TTarget" />.</param>
    /// <returns><paramref name="value" /> converted to the type <typeparamref name="TTarget" />.</returns>
    /// <exception cref="InvalidCastException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="value" /> is <see langword="null" /> or a <see cref="DBNull" /> value, but
    ///                 the type <typeparamref name="TTarget" /> is non-nullable.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="value" /> could not be converted to the type <typeparamref name="TTarget" />,
    ///                 because that conversion is not supported.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <typeparamref name="TTarget" /> is <see cref="Char" /> or <see cref="Nullable{Char}" /> and
    /// <paramref name="value" /> is a string that has a length other than 1.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static TTarget? ConvertValueToType<TTarget>(Object? value)
    {
        var targetType = typeof(TTarget);

        // Unwrap Nullable<T> types:
        var effectiveTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        switch (value)
        {
            // Cases are ordered by frequency of use:

            case null or DBNull when default(TTarget) is null:
                return default;

            case null or DBNull when default(TTarget) is not null:
                ThrowCouldNotConvertNullOrDbNullToNonNullableTargetTypeException(value, targetType);
                return default; // Just to satisfy the compiler.

            case TTarget alreadyTargetTypeValue:
                return alreadyTargetTypeValue;

            case String stringValue when effectiveTargetType == typeof(Guid):
                if (!Guid.TryParse(stringValue, out var guidResult))
                {
                    ThrowCouldNotConvertValueToTargetTypeException(stringValue, targetType);
                }

                return (TTarget)(Object)guidResult;

            case String stringValue when effectiveTargetType == typeof(TimeSpan):
                if (!TimeSpan.TryParse(stringValue, out var timeSpanResult))
                {
                    ThrowCouldNotConvertValueToTargetTypeException(stringValue, targetType);
                }

                return (TTarget)(Object)timeSpanResult;

            case String stringValue when effectiveTargetType == typeof(Char):
                if (stringValue.Length != 1)
                {
                    ThrowCouldNotConvertNonSingleCharStringToCharException(stringValue, targetType);
                }

                return (TTarget)(Object)stringValue[0];

            case String stringValue when effectiveTargetType == typeof(DateTimeOffset):
                if (!DateTimeOffset.TryParse(stringValue, out var dateTimeOffsetResult))
                {
                    ThrowCouldNotConvertValueToTargetTypeException(stringValue, targetType);
                }

                return (TTarget)(Object)dateTimeOffsetResult;

            case String stringValue when effectiveTargetType == typeof(DateOnly):
                if (!DateOnly.TryParse(stringValue, out var dateOnlyResult))
                {
                    ThrowCouldNotConvertValueToTargetTypeException(stringValue, targetType);
                }

                return (TTarget)(Object)dateOnlyResult;

            case String stringValue when effectiveTargetType == typeof(TimeOnly):
                if (!TimeOnly.TryParse(stringValue, out var timeOnlyResult))
                {
                    ThrowCouldNotConvertValueToTargetTypeException(stringValue, targetType);
                }

                return (TTarget)(Object)timeOnlyResult;

            case Guid guid when targetType == typeof(String):
                return (TTarget)(Object)guid.ToString("D");

            case Guid guid when targetType == typeof(Byte[]):
                return (TTarget)(Object)guid.ToByteArray();

            case DateTime dateTime when targetType == typeof(String):
                return (TTarget)(Object)dateTime.ToString("O", CultureInfo.InvariantCulture);

            case DateTime dateTime when effectiveTargetType == typeof(DateOnly):
                return (TTarget)(Object)DateOnly.FromDateTime(dateTime);

            case TimeSpan timeSpan when targetType == typeof(String):
                return (TTarget)(Object)timeSpan.ToString("g", CultureInfo.InvariantCulture);

            case TimeSpan timeSpan when effectiveTargetType == typeof(TimeOnly):
                return (TTarget)(Object)TimeOnly.FromTimeSpan(timeSpan);

            case Byte[] bytes when effectiveTargetType == typeof(Guid):
                return (TTarget)(Object)new Guid(bytes);

            case DateTimeOffset dateTimeOffset when targetType == typeof(String):
                return (TTarget)(Object)dateTimeOffset.ToString("O", CultureInfo.InvariantCulture);

            case DateOnly dateOnly when targetType == typeof(String):
                return (TTarget)(Object)dateOnly.ToString("O", CultureInfo.InvariantCulture);

            case TimeOnly timeOnly when targetType == typeof(String):
                return (TTarget)(Object)timeOnly.ToString("O", CultureInfo.InvariantCulture);

            default:
                if (effectiveTargetType.IsEnum)
                {
                    return EnumConverter.ConvertValueToEnumMember<TTarget>(value);
                }

                try
                {
                    return (TTarget?)Convert.ChangeType(value, effectiveTargetType, CultureInfo.InvariantCulture);
                }
                catch (Exception exception) when (
                    exception is ArgumentException or InvalidCastException or FormatException or OverflowException
                )
                {
                    ThrowCouldNotConvertValueToTargetTypeException(value, targetType, exception);
                    return default; // Just to satisfy the compiler
                }
        }
    }

    /// <summary>
    /// Determines whether <paramref name="type" /> is a type that can be converted to an enum type or a type that an
    /// enum can be converted to.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="type" /> is a type that can be converted to an enum type or a type
    /// that an enum can be converted to; otherwise, <see langword="false" />.
    /// </returns>
    private static Boolean IsSupportedEnumConversionType(Type type) =>
        Type.GetTypeCode(type) is
            // Ordered by frequency of use:
            TypeCode.String or
            TypeCode.Int32 or
            TypeCode.Int16 or
            TypeCode.Int64 or
            TypeCode.Double or
            TypeCode.Single or
            TypeCode.Decimal or
            TypeCode.Byte or
            TypeCode.SByte or
            TypeCode.UInt16 or
            TypeCode.UInt32 or
            TypeCode.UInt64;

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    private static void ThrowCouldNotConvertNonSingleCharStringToCharException(String stringValue, Type targetType) =>
        throw new InvalidCastException(
            $"Could not convert the string '{stringValue}' to the type {targetType}. The string must be exactly one " +
            "character long."
        );

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    private static void ThrowCouldNotConvertNullOrDbNullToNonNullableTargetTypeException(
        Object? value,
        Type targetType
    ) =>
        throw new InvalidCastException(
            $"Could not convert the value {value.ToDebugString()} to the type {targetType}, because the type is " +
            "non-nullable."
        );

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    private static void ThrowCouldNotConvertValueToTargetTypeException(
        Object? value,
        Type targetType,
        Exception innerException
    ) =>
        throw new InvalidCastException(
            $"Could not convert the value {value.ToDebugString()} to the type {targetType}. See inner exception " +
            "for details.",
            innerException
        );

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    private static void ThrowCouldNotConvertValueToTargetTypeException(
        Object? value,
        Type targetType
    ) =>
        throw new InvalidCastException(
            $"Could not convert the value {value.ToDebugString()} to the type {targetType}. "
        );

    private static readonly HashSet<(Type SourceType, Type TargetType)> supportedConversions =
    [
        (typeof(Boolean), typeof(Boolean)),
        (typeof(Boolean), typeof(Byte)),
        (typeof(Boolean), typeof(Decimal)),
        (typeof(Boolean), typeof(Double)),
        (typeof(Boolean), typeof(Int16)),
        (typeof(Boolean), typeof(Int32)),
        (typeof(Boolean), typeof(Int64)),
        (typeof(Boolean), typeof(SByte)),
        (typeof(Boolean), typeof(Single)),
        (typeof(Boolean), typeof(String)),
        (typeof(Boolean), typeof(UInt16)),
        (typeof(Boolean), typeof(UInt32)),
        (typeof(Boolean), typeof(UInt64)),

        (typeof(Byte), typeof(Boolean)),
        (typeof(Byte), typeof(Byte)),
        (typeof(Byte), typeof(Char)),
        (typeof(Byte), typeof(Decimal)),
        (typeof(Byte), typeof(Double)),
        (typeof(Byte), typeof(Int16)),
        (typeof(Byte), typeof(Int32)),
        (typeof(Byte), typeof(Int64)),
        (typeof(Byte), typeof(SByte)),
        (typeof(Byte), typeof(Single)),
        (typeof(Byte), typeof(String)),
        (typeof(Byte), typeof(UInt16)),
        (typeof(Byte), typeof(UInt32)),
        (typeof(Byte), typeof(UInt64)),

        (typeof(Byte[]), typeof(Guid)),

        (typeof(Char), typeof(Byte)),
        (typeof(Char), typeof(Char)),
        (typeof(Char), typeof(Int16)),
        (typeof(Char), typeof(Int32)),
        (typeof(Char), typeof(Int64)),
        (typeof(Char), typeof(SByte)),
        (typeof(Char), typeof(String)),
        (typeof(Char), typeof(UInt16)),
        (typeof(Char), typeof(UInt32)),
        (typeof(Char), typeof(UInt64)),

        (typeof(DateOnly), typeof(DateOnly)),
        (typeof(DateOnly), typeof(String)),

        (typeof(DateTime), typeof(DateTime)),
        (typeof(DateTime), typeof(DateOnly)),
        (typeof(DateTime), typeof(String)),

        (typeof(DateTimeOffset), typeof(DateTimeOffset)),
        (typeof(DateTimeOffset), typeof(String)),

        (typeof(Decimal), typeof(Boolean)),
        (typeof(Decimal), typeof(Byte)),
        (typeof(Decimal), typeof(Decimal)),
        (typeof(Decimal), typeof(Double)),
        (typeof(Decimal), typeof(Int16)),
        (typeof(Decimal), typeof(Int32)),
        (typeof(Decimal), typeof(Int64)),
        (typeof(Decimal), typeof(SByte)),
        (typeof(Decimal), typeof(Single)),
        (typeof(Decimal), typeof(String)),
        (typeof(Decimal), typeof(UInt16)),
        (typeof(Decimal), typeof(UInt32)),
        (typeof(Decimal), typeof(UInt64)),

        (typeof(Double), typeof(Boolean)),
        (typeof(Double), typeof(Byte)),
        (typeof(Double), typeof(Decimal)),
        (typeof(Double), typeof(Double)),
        (typeof(Double), typeof(Int16)),
        (typeof(Double), typeof(Int32)),
        (typeof(Double), typeof(Int64)),
        (typeof(Double), typeof(SByte)),
        (typeof(Double), typeof(Single)),
        (typeof(Double), typeof(String)),
        (typeof(Double), typeof(UInt16)),
        (typeof(Double), typeof(UInt32)),
        (typeof(Double), typeof(UInt64)),

        (typeof(Guid), typeof(Byte[])),
        (typeof(Guid), typeof(Guid)),
        (typeof(Guid), typeof(String)),

        (typeof(Int16), typeof(Boolean)),
        (typeof(Int16), typeof(Byte)),
        (typeof(Int16), typeof(Char)),
        (typeof(Int16), typeof(Decimal)),
        (typeof(Int16), typeof(Double)),
        (typeof(Int16), typeof(Int16)),
        (typeof(Int16), typeof(Int32)),
        (typeof(Int16), typeof(Int64)),
        (typeof(Int16), typeof(SByte)),
        (typeof(Int16), typeof(Single)),
        (typeof(Int16), typeof(String)),
        (typeof(Int16), typeof(UInt16)),
        (typeof(Int16), typeof(UInt32)),
        (typeof(Int16), typeof(UInt64)),

        (typeof(Int32), typeof(Boolean)),
        (typeof(Int32), typeof(Byte)),
        (typeof(Int32), typeof(Char)),
        (typeof(Int32), typeof(Decimal)),
        (typeof(Int32), typeof(Double)),
        (typeof(Int32), typeof(Int16)),
        (typeof(Int32), typeof(Int32)),
        (typeof(Int32), typeof(Int64)),
        (typeof(Int32), typeof(SByte)),
        (typeof(Int32), typeof(Single)),
        (typeof(Int32), typeof(String)),
        (typeof(Int32), typeof(UInt16)),
        (typeof(Int32), typeof(UInt32)),
        (typeof(Int32), typeof(UInt64)),

        (typeof(Int64), typeof(Boolean)),
        (typeof(Int64), typeof(Byte)),
        (typeof(Int64), typeof(Char)),
        (typeof(Int64), typeof(Decimal)),
        (typeof(Int64), typeof(Double)),
        (typeof(Int64), typeof(Int16)),
        (typeof(Int64), typeof(Int32)),
        (typeof(Int64), typeof(Int64)),
        (typeof(Int64), typeof(SByte)),
        (typeof(Int64), typeof(Single)),
        (typeof(Int64), typeof(String)),
        (typeof(Int64), typeof(UInt16)),
        (typeof(Int64), typeof(UInt32)),
        (typeof(Int64), typeof(UInt64)),

        (typeof(IntPtr), typeof(IntPtr)),

        (typeof(SByte), typeof(Boolean)),
        (typeof(SByte), typeof(Byte)),
        (typeof(SByte), typeof(Char)),
        (typeof(SByte), typeof(Decimal)),
        (typeof(SByte), typeof(Double)),
        (typeof(SByte), typeof(Int16)),
        (typeof(SByte), typeof(Int32)),
        (typeof(SByte), typeof(Int64)),
        (typeof(SByte), typeof(SByte)),
        (typeof(SByte), typeof(Single)),
        (typeof(SByte), typeof(String)),
        (typeof(SByte), typeof(UInt16)),
        (typeof(SByte), typeof(UInt32)),
        (typeof(SByte), typeof(UInt64)),

        (typeof(Single), typeof(Boolean)),
        (typeof(Single), typeof(Byte)),
        (typeof(Single), typeof(Decimal)),
        (typeof(Single), typeof(Double)),
        (typeof(Single), typeof(Int16)),
        (typeof(Single), typeof(Int32)),
        (typeof(Single), typeof(Int64)),
        (typeof(Single), typeof(SByte)),
        (typeof(Single), typeof(Single)),
        (typeof(Single), typeof(String)),
        (typeof(Single), typeof(UInt16)),
        (typeof(Single), typeof(UInt32)),
        (typeof(Single), typeof(UInt64)),

        (typeof(String), typeof(Boolean)),
        (typeof(String), typeof(Byte)),
        (typeof(String), typeof(Char)),
        (typeof(String), typeof(DateTime)),
        (typeof(String), typeof(DateTimeOffset)),
        (typeof(String), typeof(DateOnly)),
        (typeof(String), typeof(Decimal)),
        (typeof(String), typeof(Double)),
        (typeof(String), typeof(Guid)),
        (typeof(String), typeof(Int16)),
        (typeof(String), typeof(Int32)),
        (typeof(String), typeof(Int64)),
        (typeof(String), typeof(SByte)),
        (typeof(String), typeof(Single)),
        (typeof(String), typeof(String)),
        (typeof(String), typeof(UInt16)),
        (typeof(String), typeof(UInt32)),
        (typeof(String), typeof(UInt64)),
        (typeof(String), typeof(TimeSpan)),
        (typeof(String), typeof(TimeOnly)),

        (typeof(TimeOnly), typeof(TimeOnly)),
        (typeof(TimeOnly), typeof(String)),

        (typeof(TimeSpan), typeof(TimeOnly)),
        (typeof(TimeSpan), typeof(TimeSpan)),
        (typeof(TimeSpan), typeof(String)),

        (typeof(UInt16), typeof(Boolean)),
        (typeof(UInt16), typeof(Byte)),
        (typeof(UInt16), typeof(Char)),
        (typeof(UInt16), typeof(Decimal)),
        (typeof(UInt16), typeof(Double)),
        (typeof(UInt16), typeof(Int16)),
        (typeof(UInt16), typeof(Int32)),
        (typeof(UInt16), typeof(Int64)),
        (typeof(UInt16), typeof(SByte)),
        (typeof(UInt16), typeof(Single)),
        (typeof(UInt16), typeof(String)),
        (typeof(UInt16), typeof(UInt16)),
        (typeof(UInt16), typeof(UInt32)),
        (typeof(UInt16), typeof(UInt64)),

        (typeof(UInt32), typeof(Boolean)),
        (typeof(UInt32), typeof(Byte)),
        (typeof(UInt32), typeof(Char)),
        (typeof(UInt32), typeof(Decimal)),
        (typeof(UInt32), typeof(Double)),
        (typeof(UInt32), typeof(Int16)),
        (typeof(UInt32), typeof(Int32)),
        (typeof(UInt32), typeof(Int64)),
        (typeof(UInt32), typeof(SByte)),
        (typeof(UInt32), typeof(Single)),
        (typeof(UInt32), typeof(String)),
        (typeof(UInt32), typeof(UInt16)),
        (typeof(UInt32), typeof(UInt32)),
        (typeof(UInt32), typeof(UInt64)),

        (typeof(UInt64), typeof(Boolean)),
        (typeof(UInt64), typeof(Byte)),
        (typeof(UInt64), typeof(Char)),
        (typeof(UInt64), typeof(Decimal)),
        (typeof(UInt64), typeof(Double)),
        (typeof(UInt64), typeof(Int16)),
        (typeof(UInt64), typeof(Int32)),
        (typeof(UInt64), typeof(Int64)),
        (typeof(UInt64), typeof(SByte)),
        (typeof(UInt64), typeof(Single)),
        (typeof(UInt64), typeof(String)),
        (typeof(UInt64), typeof(UInt16)),
        (typeof(UInt64), typeof(UInt32)),
        (typeof(UInt64), typeof(UInt64)),

        (typeof(UIntPtr), typeof(UIntPtr))
    ];
}
