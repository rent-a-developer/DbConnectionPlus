// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus.Extensions;

/// <summary>
/// Provides extension members for the type <see cref="Type" />.
/// </summary>
internal static class TypeExtensions
{
    /// <summary>
    /// Determines whether this type is a built-in .NET type
    /// (e.g. <see cref="Boolean" />, <see cref="String" />, <see cref="Decimal" />, ...).
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>
    /// <see langword="true" /> if this type is a built-in .NET type; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="type" /> is <see langword="null" />.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Boolean IsBuiltInTypeOrNullableBuiltInType(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return builtInTypes.Contains(Nullable.GetUnderlyingType(type) ?? type);
    }

    /// <summary>
    /// Determines whether this type is <see cref="Char" /> or <see cref="Nullable{Char}" />.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>
    /// <see langword="true" /> if this type is <see cref="Char" /> or <see cref="Nullable{Char}" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="type" /> is <see langword="null" />.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Boolean IsCharOrNullableCharType(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type == typeof(Char) || type == typeof(Char?);
    }

    /// <summary>
    /// Determines whether this type is an <see cref="Enum" /> type or a nullable <see cref="Enum" /> type.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>
    /// <see langword="true" /> if this type is an <see cref="Enum" /> type or a nullable <see cref="Enum" /> type;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="type" /> is <see langword="null" />.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Boolean IsEnumOrNullableEnumType(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.IsEnum || Nullable.GetUnderlyingType(type) is { IsEnum: true };
    }

    /// <summary>
    /// Determines whether this type is a reference type or a nullable type (<see cref="Nullable{T}" />).
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>
    /// <see langword="true" /> if this type is a reference type or a nullable type (<see cref="Nullable{T}" />);
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="type" /> is <see langword="null" />.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Boolean IsReferenceTypeOrNullableType(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
    }

    /// <summary>
    /// Determines whether this type is a <see cref="ValueTuple" /> type (e.g.
    /// <see cref="ValueTuple{T1}" />, <see cref="ValueTuple{T1, T2}" />, <see cref="ValueTuple{T1, T2, T3}" />, ...).
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>
    /// <see langword="true" /> if this type is a <see cref="ValueTuple" /> type; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="type" /> is <see langword="null" />.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Boolean IsValueTupleType(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.IsGenericType && valueTupleTypes.Contains(type.GetGenericTypeDefinition());
    }

    private static readonly HashSet<Type> builtInTypes =
    [
        typeof(Boolean),
        typeof(Byte),
        typeof(SByte),
        typeof(Char),
        typeof(Decimal),
        typeof(Double),
        typeof(Single),
        typeof(Int16),
        typeof(UInt16),
        typeof(Int32),
        typeof(UInt32),
        typeof(Int64),
        typeof(UInt64),
        typeof(IntPtr),
        typeof(UIntPtr),
        typeof(String),
        typeof(DateTime),
        typeof(DateOnly),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(TimeOnly),
        typeof(Guid)
    ];

    private static readonly HashSet<Type> valueTupleTypes =
    [
        typeof(ValueTuple<>),
        typeof(ValueTuple<,>),
        typeof(ValueTuple<,,>),
        typeof(ValueTuple<,,,>),
        typeof(ValueTuple<,,,,>),
        typeof(ValueTuple<,,,,,>),
        typeof(ValueTuple<,,,,,,>),
        typeof(ValueTuple<,,,,,,,>)
    ];
}
