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
    internal static bool CanConvert(Type sourceType, Type targetType)
    {
        ArgumentNullException.ThrowIfNull(sourceType);
        ArgumentNullException.ThrowIfNull(targetType);

        var effectiveSourceType = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
        var effectiveTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (effectiveSourceType == effectiveTargetType || effectiveTargetType == typeof(object))
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
    ///                 <typeparamref name="TTarget" /> is <see cref="char" /> or <see cref="Nullable{Char}" /> and
    /// <paramref name="value" /> is a string that has a length other than 1.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static TTarget? ConvertValueToType<TTarget>(object? value)
    {
        var targetType = typeof(TTarget);

        // Unwrap Nullable<T> types:
        var effectiveTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        switch (value)
        {
            // Cases are ordered by frequency of use:

            case null or DBNull when default(TTarget) is null:
                return default;

            case null
            or DBNull when default(TTarget) is not null:
                ThrowCouldNotConvertNullOrDbNullToNonNullableTargetTypeException(value, targetType);
                return default; // Just to satisfy the compiler.

            case TTarget alreadyTargetTypeValue:
                return alreadyTargetTypeValue;

            case string stringValue when effectiveTargetType == typeof(Guid):
                if (!Guid.TryParse(stringValue, out var guidResult))
                {
                    ThrowCouldNotConvertValueToTargetTypeException(stringValue, targetType);
                }

                return (TTarget)(object)guidResult;

            case string stringValue when effectiveTargetType == typeof(TimeSpan):
                if (!TimeSpan.TryParse(stringValue, CultureInfo.InvariantCulture, out var timeSpanResult))
                {
                    ThrowCouldNotConvertValueToTargetTypeException(stringValue, targetType);
                }

                return (TTarget)(object)timeSpanResult;

            case string stringValue when effectiveTargetType == typeof(char):
                if (stringValue.Length != 1)
                {
                    ThrowCouldNotConvertNonSingleCharStringToCharException(stringValue, targetType);
                }

                return (TTarget)(object)stringValue[0];

            case string stringValue when effectiveTargetType == typeof(DateTimeOffset):
                if (!DateTimeOffset.TryParse(stringValue, CultureInfo.InvariantCulture, out var dateTimeOffsetResult))
                {
                    ThrowCouldNotConvertValueToTargetTypeException(stringValue, targetType);
                }

                return (TTarget)(object)dateTimeOffsetResult;

            case string stringValue when effectiveTargetType == typeof(DateOnly):
                if (!DateOnly.TryParse(stringValue, CultureInfo.InvariantCulture, out var dateOnlyResult))
                {
                    ThrowCouldNotConvertValueToTargetTypeException(stringValue, targetType);
                }

                return (TTarget)(object)dateOnlyResult;

            case string stringValue when effectiveTargetType == typeof(TimeOnly):
                if (!TimeOnly.TryParse(stringValue, CultureInfo.InvariantCulture, out var timeOnlyResult))
                {
                    ThrowCouldNotConvertValueToTargetTypeException(stringValue, targetType);
                }

                return (TTarget)(object)timeOnlyResult;

            case Guid guid when targetType == typeof(string):
                return (TTarget)(object)guid.ToString("D");

            case Guid guid when targetType == typeof(byte[]):
                return (TTarget)(object)guid.ToByteArray();

            case DateTime dateTime when targetType == typeof(string):
                return (TTarget)(object)dateTime.ToString("O", CultureInfo.InvariantCulture);

            case DateTime dateTime when effectiveTargetType == typeof(DateOnly):
                return (TTarget)(object)DateOnly.FromDateTime(dateTime);

            case TimeSpan timeSpan when targetType == typeof(string):
                return (TTarget)(object)timeSpan.ToString("g", CultureInfo.InvariantCulture);

            case TimeSpan timeSpan when effectiveTargetType == typeof(TimeOnly):
                return (TTarget)(object)TimeOnly.FromTimeSpan(timeSpan);

            case byte[] bytes when effectiveTargetType == typeof(Guid):
                return (TTarget)(object)new Guid(bytes);

            case DateTimeOffset dateTimeOffset when targetType == typeof(string):
                return (TTarget)(object)dateTimeOffset.ToString("O", CultureInfo.InvariantCulture);

            case DateOnly dateOnly when targetType == typeof(string):
                return (TTarget)(object)dateOnly.ToString("O", CultureInfo.InvariantCulture);

            case TimeOnly timeOnly when targetType == typeof(string):
                return (TTarget)(object)timeOnly.ToString("O", CultureInfo.InvariantCulture);

            default:
                if (effectiveTargetType.IsEnum)
                {
                    return EnumConverter.ConvertValueToEnumMember<TTarget>(value);
                }

                try
                {
                    return (TTarget?)Convert.ChangeType(value, effectiveTargetType, CultureInfo.InvariantCulture);
                }
                catch (Exception exception)
                    when (exception is ArgumentException or InvalidCastException or FormatException or OverflowException
                    )
                {
                    ThrowCouldNotConvertValueToTargetTypeException(value, targetType, exception);
                    return default; // Just to satisfy the compiler
                }
        }
    }

    /// <summary>
    /// Converts <paramref name="value" /> to the type <paramref name="targetType" />.
    /// </summary>
    /// <param name="value">The value to convert to the type <paramref name="targetType" />.</param>
    /// <param name="targetType">The type to convert <paramref name="value" /> to.</param>
    /// <returns><paramref name="value" /> converted to the type <paramref name="targetType" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="targetType" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidCastException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="value" /> is <see langword="null" /> or a <see cref="DBNull" /> value, but
    ///                 the type <paramref name="targetType" /> is non-nullable.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="value" /> could not be converted to the type <paramref name="targetType" />,
    ///                 because that conversion is not supported.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="targetType" /> is <see cref="char" /> or <see cref="Nullable{Char}" /> and
    /// <paramref name="value" /> is a string that has a length other than 1.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static object? ConvertValueToType(object? value, Type targetType)
    {
        ArgumentNullException.ThrowIfNull(targetType);

        // Unwrap Nullable<T> types:
        var effectiveTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        switch (value)
        {
            // Cases are ordered by frequency of use:

            case null or DBNull when targetType.IsReferenceTypeOrNullableType():
                return null;

            case null
            or DBNull when !targetType.IsReferenceTypeOrNullableType():
                ThrowCouldNotConvertNullOrDbNullToNonNullableTargetTypeException(value, targetType);
                return null; // Just to satisfy the compiler.

            case not null when value.GetType().IsAssignableTo(effectiveTargetType):
                return value;

            case string stringValue when effectiveTargetType == typeof(Guid):
                if (!Guid.TryParse(stringValue, out var guidResult))
                {
                    ThrowCouldNotConvertValueToTargetTypeException(stringValue, targetType);
                }

                return guidResult;

            case string stringValue when effectiveTargetType == typeof(TimeSpan):
                if (!TimeSpan.TryParse(stringValue, CultureInfo.InvariantCulture, out var timeSpanResult))
                {
                    ThrowCouldNotConvertValueToTargetTypeException(stringValue, targetType);
                }

                return timeSpanResult;

            case string stringValue when effectiveTargetType == typeof(char):
                if (stringValue.Length != 1)
                {
                    ThrowCouldNotConvertNonSingleCharStringToCharException(stringValue, targetType);
                }

                return stringValue[0];

            case string stringValue when effectiveTargetType == typeof(DateTimeOffset):
                if (!DateTimeOffset.TryParse(stringValue, CultureInfo.InvariantCulture, out var dateTimeOffsetResult))
                {
                    ThrowCouldNotConvertValueToTargetTypeException(stringValue, targetType);
                }

                return dateTimeOffsetResult;

            case string stringValue when effectiveTargetType == typeof(DateOnly):
                if (!DateOnly.TryParse(stringValue, CultureInfo.InvariantCulture, out var dateOnlyResult))
                {
                    ThrowCouldNotConvertValueToTargetTypeException(stringValue, targetType);
                }

                return dateOnlyResult;

            case string stringValue when effectiveTargetType == typeof(TimeOnly):
                if (!TimeOnly.TryParse(stringValue, CultureInfo.InvariantCulture, out var timeOnlyResult))
                {
                    ThrowCouldNotConvertValueToTargetTypeException(stringValue, targetType);
                }

                return timeOnlyResult;

            case Guid guid when targetType == typeof(string):
                return guid.ToString("D");

            case Guid guid when targetType == typeof(byte[]):
                return guid.ToByteArray();

            case DateTime dateTime when targetType == typeof(string):
                return dateTime.ToString("O", CultureInfo.InvariantCulture);

            case DateTime dateTime when effectiveTargetType == typeof(DateOnly):
                return DateOnly.FromDateTime(dateTime);

            case TimeSpan timeSpan when targetType == typeof(string):
                return timeSpan.ToString("g", CultureInfo.InvariantCulture);

            case TimeSpan timeSpan when effectiveTargetType == typeof(TimeOnly):
                return TimeOnly.FromTimeSpan(timeSpan);

            case byte[] bytes when effectiveTargetType == typeof(Guid):
                return new Guid(bytes);

            case DateTimeOffset dateTimeOffset when targetType == typeof(string):
                return dateTimeOffset.ToString("O", CultureInfo.InvariantCulture);

            case DateOnly dateOnly when targetType == typeof(string):
                return dateOnly.ToString("O", CultureInfo.InvariantCulture);

            case TimeOnly timeOnly when targetType == typeof(string):
                return timeOnly.ToString("O", CultureInfo.InvariantCulture);

            default:
                if (effectiveTargetType.IsEnum)
                {
                    return EnumConverter.ConvertValueToEnumMember(value, targetType);
                }

                try
                {
                    return Convert.ChangeType(value, effectiveTargetType, CultureInfo.InvariantCulture);
                }
                catch (Exception exception)
                    when (exception is ArgumentException or InvalidCastException or FormatException or OverflowException
                    )
                {
                    ThrowCouldNotConvertValueToTargetTypeException(value, targetType, exception);
                    return null; // Just to satisfy the compiler
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
    private static bool IsSupportedEnumConversionType(Type type) =>
        Type.GetTypeCode(type)
            is
                // Ordered by frequency of use:
                TypeCode.String
                or TypeCode.Int32
                or TypeCode.Int16
                or TypeCode.Int64
                or TypeCode.Double
                or TypeCode.Single
                or TypeCode.Decimal
                or TypeCode.Byte
                or TypeCode.SByte
                or TypeCode.UInt16
                or TypeCode.UInt32
                or TypeCode.UInt64;

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    private static void ThrowCouldNotConvertNonSingleCharStringToCharException(string stringValue, Type targetType) =>
        throw new InvalidCastException(
            $"Could not convert the string '{stringValue}' to the type {targetType}. The string must be exactly one "
                + "character long."
        );

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    private static void ThrowCouldNotConvertNullOrDbNullToNonNullableTargetTypeException(
        object? value,
        Type targetType
    ) =>
        throw new InvalidCastException(
            $"Could not convert the value {value.ToDebugString()} to the type {targetType}, because the type is "
                + "non-nullable."
        );

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    private static void ThrowCouldNotConvertValueToTargetTypeException(
        object? value,
        Type targetType,
        Exception innerException
    ) =>
        throw new InvalidCastException(
            $"Could not convert the value {value.ToDebugString()} to the type {targetType}. See inner exception "
                + "for details.",
            innerException
        );

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    private static void ThrowCouldNotConvertValueToTargetTypeException(object? value, Type targetType) =>
        throw new InvalidCastException(
            $"Could not convert the value {value.ToDebugString()} to the type {targetType}. "
        );

    private static readonly HashSet<(Type SourceType, Type TargetType)> supportedConversions =
    [
        (typeof(bool), typeof(bool)),
        (typeof(bool), typeof(byte)),
        (typeof(bool), typeof(decimal)),
        (typeof(bool), typeof(double)),
        (typeof(bool), typeof(short)),
        (typeof(bool), typeof(int)),
        (typeof(bool), typeof(long)),
        (typeof(bool), typeof(sbyte)),
        (typeof(bool), typeof(float)),
        (typeof(bool), typeof(string)),
        (typeof(bool), typeof(ushort)),
        (typeof(bool), typeof(uint)),
        (typeof(bool), typeof(ulong)),
        (typeof(byte), typeof(bool)),
        (typeof(byte), typeof(byte)),
        (typeof(byte), typeof(char)),
        (typeof(byte), typeof(decimal)),
        (typeof(byte), typeof(double)),
        (typeof(byte), typeof(short)),
        (typeof(byte), typeof(int)),
        (typeof(byte), typeof(long)),
        (typeof(byte), typeof(sbyte)),
        (typeof(byte), typeof(float)),
        (typeof(byte), typeof(string)),
        (typeof(byte), typeof(ushort)),
        (typeof(byte), typeof(uint)),
        (typeof(byte), typeof(ulong)),
        (typeof(byte[]), typeof(Guid)),
        (typeof(char), typeof(byte)),
        (typeof(char), typeof(char)),
        (typeof(char), typeof(short)),
        (typeof(char), typeof(int)),
        (typeof(char), typeof(long)),
        (typeof(char), typeof(sbyte)),
        (typeof(char), typeof(string)),
        (typeof(char), typeof(ushort)),
        (typeof(char), typeof(uint)),
        (typeof(char), typeof(ulong)),
        (typeof(DateOnly), typeof(DateOnly)),
        (typeof(DateOnly), typeof(string)),
        (typeof(DateTime), typeof(DateTime)),
        (typeof(DateTime), typeof(DateOnly)),
        (typeof(DateTime), typeof(string)),
        (typeof(DateTimeOffset), typeof(DateTimeOffset)),
        (typeof(DateTimeOffset), typeof(string)),
        (typeof(decimal), typeof(bool)),
        (typeof(decimal), typeof(byte)),
        (typeof(decimal), typeof(decimal)),
        (typeof(decimal), typeof(double)),
        (typeof(decimal), typeof(short)),
        (typeof(decimal), typeof(int)),
        (typeof(decimal), typeof(long)),
        (typeof(decimal), typeof(sbyte)),
        (typeof(decimal), typeof(float)),
        (typeof(decimal), typeof(string)),
        (typeof(decimal), typeof(ushort)),
        (typeof(decimal), typeof(uint)),
        (typeof(decimal), typeof(ulong)),
        (typeof(double), typeof(bool)),
        (typeof(double), typeof(byte)),
        (typeof(double), typeof(decimal)),
        (typeof(double), typeof(double)),
        (typeof(double), typeof(short)),
        (typeof(double), typeof(int)),
        (typeof(double), typeof(long)),
        (typeof(double), typeof(sbyte)),
        (typeof(double), typeof(float)),
        (typeof(double), typeof(string)),
        (typeof(double), typeof(ushort)),
        (typeof(double), typeof(uint)),
        (typeof(double), typeof(ulong)),
        (typeof(Guid), typeof(byte[])),
        (typeof(Guid), typeof(Guid)),
        (typeof(Guid), typeof(string)),
        (typeof(short), typeof(bool)),
        (typeof(short), typeof(byte)),
        (typeof(short), typeof(char)),
        (typeof(short), typeof(decimal)),
        (typeof(short), typeof(double)),
        (typeof(short), typeof(short)),
        (typeof(short), typeof(int)),
        (typeof(short), typeof(long)),
        (typeof(short), typeof(sbyte)),
        (typeof(short), typeof(float)),
        (typeof(short), typeof(string)),
        (typeof(short), typeof(ushort)),
        (typeof(short), typeof(uint)),
        (typeof(short), typeof(ulong)),
        (typeof(int), typeof(bool)),
        (typeof(int), typeof(byte)),
        (typeof(int), typeof(char)),
        (typeof(int), typeof(decimal)),
        (typeof(int), typeof(double)),
        (typeof(int), typeof(short)),
        (typeof(int), typeof(int)),
        (typeof(int), typeof(long)),
        (typeof(int), typeof(sbyte)),
        (typeof(int), typeof(float)),
        (typeof(int), typeof(string)),
        (typeof(int), typeof(ushort)),
        (typeof(int), typeof(uint)),
        (typeof(int), typeof(ulong)),
        (typeof(long), typeof(bool)),
        (typeof(long), typeof(byte)),
        (typeof(long), typeof(char)),
        (typeof(long), typeof(decimal)),
        (typeof(long), typeof(double)),
        (typeof(long), typeof(short)),
        (typeof(long), typeof(int)),
        (typeof(long), typeof(long)),
        (typeof(long), typeof(sbyte)),
        (typeof(long), typeof(float)),
        (typeof(long), typeof(string)),
        (typeof(long), typeof(ushort)),
        (typeof(long), typeof(uint)),
        (typeof(long), typeof(ulong)),
        (typeof(IntPtr), typeof(IntPtr)),
        (typeof(sbyte), typeof(bool)),
        (typeof(sbyte), typeof(byte)),
        (typeof(sbyte), typeof(char)),
        (typeof(sbyte), typeof(decimal)),
        (typeof(sbyte), typeof(double)),
        (typeof(sbyte), typeof(short)),
        (typeof(sbyte), typeof(int)),
        (typeof(sbyte), typeof(long)),
        (typeof(sbyte), typeof(sbyte)),
        (typeof(sbyte), typeof(float)),
        (typeof(sbyte), typeof(string)),
        (typeof(sbyte), typeof(ushort)),
        (typeof(sbyte), typeof(uint)),
        (typeof(sbyte), typeof(ulong)),
        (typeof(float), typeof(bool)),
        (typeof(float), typeof(byte)),
        (typeof(float), typeof(decimal)),
        (typeof(float), typeof(double)),
        (typeof(float), typeof(short)),
        (typeof(float), typeof(int)),
        (typeof(float), typeof(long)),
        (typeof(float), typeof(sbyte)),
        (typeof(float), typeof(float)),
        (typeof(float), typeof(string)),
        (typeof(float), typeof(ushort)),
        (typeof(float), typeof(uint)),
        (typeof(float), typeof(ulong)),
        (typeof(string), typeof(bool)),
        (typeof(string), typeof(byte)),
        (typeof(string), typeof(char)),
        (typeof(string), typeof(DateTime)),
        (typeof(string), typeof(DateTimeOffset)),
        (typeof(string), typeof(DateOnly)),
        (typeof(string), typeof(decimal)),
        (typeof(string), typeof(double)),
        (typeof(string), typeof(Guid)),
        (typeof(string), typeof(short)),
        (typeof(string), typeof(int)),
        (typeof(string), typeof(long)),
        (typeof(string), typeof(sbyte)),
        (typeof(string), typeof(float)),
        (typeof(string), typeof(string)),
        (typeof(string), typeof(ushort)),
        (typeof(string), typeof(uint)),
        (typeof(string), typeof(ulong)),
        (typeof(string), typeof(TimeSpan)),
        (typeof(string), typeof(TimeOnly)),
        (typeof(TimeOnly), typeof(TimeOnly)),
        (typeof(TimeOnly), typeof(string)),
        (typeof(TimeSpan), typeof(TimeOnly)),
        (typeof(TimeSpan), typeof(TimeSpan)),
        (typeof(TimeSpan), typeof(string)),
        (typeof(ushort), typeof(bool)),
        (typeof(ushort), typeof(byte)),
        (typeof(ushort), typeof(char)),
        (typeof(ushort), typeof(decimal)),
        (typeof(ushort), typeof(double)),
        (typeof(ushort), typeof(short)),
        (typeof(ushort), typeof(int)),
        (typeof(ushort), typeof(long)),
        (typeof(ushort), typeof(sbyte)),
        (typeof(ushort), typeof(float)),
        (typeof(ushort), typeof(string)),
        (typeof(ushort), typeof(ushort)),
        (typeof(ushort), typeof(uint)),
        (typeof(ushort), typeof(ulong)),
        (typeof(uint), typeof(bool)),
        (typeof(uint), typeof(byte)),
        (typeof(uint), typeof(char)),
        (typeof(uint), typeof(decimal)),
        (typeof(uint), typeof(double)),
        (typeof(uint), typeof(short)),
        (typeof(uint), typeof(int)),
        (typeof(uint), typeof(long)),
        (typeof(uint), typeof(sbyte)),
        (typeof(uint), typeof(float)),
        (typeof(uint), typeof(string)),
        (typeof(uint), typeof(ushort)),
        (typeof(uint), typeof(uint)),
        (typeof(uint), typeof(ulong)),
        (typeof(ulong), typeof(bool)),
        (typeof(ulong), typeof(byte)),
        (typeof(ulong), typeof(char)),
        (typeof(ulong), typeof(decimal)),
        (typeof(ulong), typeof(double)),
        (typeof(ulong), typeof(short)),
        (typeof(ulong), typeof(int)),
        (typeof(ulong), typeof(long)),
        (typeof(ulong), typeof(sbyte)),
        (typeof(ulong), typeof(float)),
        (typeof(ulong), typeof(string)),
        (typeof(ulong), typeof(ushort)),
        (typeof(ulong), typeof(uint)),
        (typeof(ulong), typeof(ulong)),
        (typeof(UIntPtr), typeof(UIntPtr)),
    ];
}
