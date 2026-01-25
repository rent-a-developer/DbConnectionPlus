using System.Diagnostics.CodeAnalysis;
using RentADeveloper.DbConnectionPlus.Extensions;

namespace RentADeveloper.DbConnectionPlus;

/// <summary>
/// Provides methods to throw common exceptions.
/// </summary>
public static class ThrowHelper
{
    /// <summary>
    /// Throws an <see cref="ArgumentException" /> indicating that the specified entity type has no key property.
    /// </summary>
    /// <param name="entityType">The entity type that lacks a key property.</param>
    /// <exception cref="ArgumentException">Always thrown.</exception>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    public static void ThrowEntityTypeHasNoKeyPropertyException(Type entityType) =>
        throw new ArgumentException(
            $"Could not get the key property / properties of the type {entityType}. Make sure that at least one " +
            $"instance property of that type is denoted with a {typeof(KeyAttribute)}."
        );

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> indicating that the specified
    /// <see cref="EnumSerializationMode" /> is invalid.
    /// </summary>
    /// <typeparam name="T">
    /// The type parameter for the return value of this method.
    /// This just exists to satisfy the compiler when this method is used in an expression or when we must trick the
    /// compiler to believe a value is returned.
    /// </typeparam>
    /// <param name="enumSerializationMode">The <see cref="EnumSerializationMode" /> value that is invalid.</param>
    /// <exception cref="ArgumentOutOfRangeException">Always thrown.</exception>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    public static T ThrowInvalidEnumSerializationModeException<T>(EnumSerializationMode enumSerializationMode) =>
        throw new ArgumentOutOfRangeException(
            nameof(enumSerializationMode),
            enumSerializationMode,
            $"The {nameof(EnumSerializationMode)} {enumSerializationMode.ToDebugString()} is not supported."
        );

    /// <summary>
    /// Throws an <see cref="InvalidOperationException" /> indicating that an SQL statement did return more than one row
    /// when it was expected to return just one.
    /// </summary>
    /// <exception cref="InvalidOperationException">Always thrown.</exception>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    public static void ThrowSqlStatementReturnedMoreThanOneRowException() =>
        throw new InvalidOperationException("The SQL statement did return more than one row.");

    /// <summary>
    /// Throws an <see cref="InvalidOperationException" /> indicating that an SQL statement did not return any rows
    /// when it was expected to.
    /// </summary>
    /// <exception cref="InvalidOperationException">Always thrown.</exception>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    public static void ThrowSqlStatementReturnedNoRowsException() =>
        throw new InvalidOperationException("The SQL statement did not return any rows.");

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> indicating that the specified connection is not of the
    /// expected type.
    /// </summary>
    /// <typeparam name="T">
    /// The type parameter for the return value of this method.
    /// This just exists to satisfy the compiler when this method is used in an expression or when we must trick the
    /// compiler to believe a value is returned.
    /// </typeparam>
    /// <typeparam name="TExpectedConnectionType">The type of connection that was expected.</typeparam>
    /// <exception cref="ArgumentOutOfRangeException">Always thrown.</exception>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    public static T ThrowWrongConnectionTypeException<TExpectedConnectionType, T>()
        where TExpectedConnectionType : DbConnection
        =>
            throw new ArgumentOutOfRangeException(
                // ReSharper disable once NotResolvedInText
                "connection",
                $"The provided connection is not of the type {typeof(TExpectedConnectionType)}."
            );

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> indicating that the specified transaction is not of the
    /// expected type.
    /// </summary>
    /// <typeparam name="T">
    /// The type parameter for the return value of this method.
    /// This just exists to satisfy the compiler when this method is used in an expression or when we must trick the
    /// compiler to believe a value is returned.
    /// </typeparam>
    /// <typeparam name="TExpectedTransactionType">The type of transaction that was expected.</typeparam>
    /// <exception cref="ArgumentOutOfRangeException">Always thrown.</exception>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    public static T ThrowWrongTransactionTypeException<TExpectedTransactionType, T>()
        where TExpectedTransactionType : DbTransaction
        =>
            throw new ArgumentOutOfRangeException(
                // ReSharper disable once NotResolvedInText
                "transaction",
                $"The provided transaction is not of the type {typeof(TExpectedTransactionType)}."
            );
}
