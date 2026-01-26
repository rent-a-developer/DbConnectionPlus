// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using System.Linq.Expressions;
using System.Reflection;
using RentADeveloper.DbConnectionPlus.Converters;
using RentADeveloper.DbConnectionPlus.Extensions;

namespace RentADeveloper.DbConnectionPlus.Materializers;

/// <summary>
/// Provides helper functions for materializer factories.
/// </summary>
internal static class MaterializerFactoryHelper
{
    /// <summary>
    /// The <see cref="DbDataReader.GetValue(Int32)" /> method.
    /// </summary>
    internal static MethodInfo DbDataReaderGetValueMethod { get; } = typeof(DbDataReader)
        .GetMethod(nameof(DbDataReader.GetValue))!;

    /// <summary>
    /// The <see cref="DbDataReader.IsDBNull(Int32)" /> method.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    internal static MethodInfo DbDataReaderIsDBNullMethod { get; } = typeof(DbDataReader)
        .GetMethod(nameof(DbDataReader.IsDBNull))!;

    /// <summary>
    /// The <see cref="EnumConverter.ConvertValueToEnumMember{TTarget}" /> method.
    /// </summary>
    internal static MethodInfo EnumConverterConvertValueToEnumMemberMethod { get; } = typeof(EnumConverter)
        .GetMethod(nameof(EnumConverter.ConvertValueToEnumMember), BindingFlags.Static | BindingFlags.NonPublic)!;

    /// <summary>
    /// The 'Chars' property of the <see cref="String" /> type.
    /// </summary>
    internal static PropertyInfo StringCharsProperty { get; } = typeof(String)
        .GetProperty("Chars", BindingFlags.Instance | BindingFlags.Public)!;

    /// <summary>
    /// The <see cref="String.Concat(String, String, String)" /> method.
    /// </summary>
    internal static MethodInfo StringConcatMethod { get; } = typeof(String)
        .GetMethod(nameof(String.Concat), [typeof(String), typeof(String), typeof(String)])!;

    /// <summary>
    /// The <see cref="String.Length" /> property.
    /// </summary>
    internal static PropertyInfo StringLengthProperty { get; } = typeof(String)
        .GetProperty(nameof(String.Length), BindingFlags.Instance | BindingFlags.Public)!;

    /// <summary>
    /// The <see cref="ValueConverter.ConvertValueToType{TTarget}" /> method.
    /// </summary>
    internal static MethodInfo ValueConverterConvertValueToTypeMethod { get; } = typeof(ValueConverter)
        .GetMethod(nameof(ValueConverter.ConvertValueToType), BindingFlags.Static | BindingFlags.NonPublic)!;

    /// <summary>
    /// Creates an <see cref="Expression" /> that gets the value of a field of the specified field type from a
    /// <see cref="DbDataReader" /> using one of the typed <see cref="DbDataReader" />.GetXXX methods.
    /// </summary>
    /// <param name="dataReaderExpression">
    /// The expression of the <see cref="DbDataReader" /> to get the field value from.
    /// </param>
    /// <param name="fieldOrdinalExpression">
    /// The expression of the field ordinal of the field to get the value from.
    /// </param>
    /// <param name="fieldOrdinal">The field ordinal of the field to get the value from.</param>
    /// <param name="fieldName">The field name of the field to get the value from.</param>
    /// <param name="fieldType">The field type of the field to get the value from.</param>
    /// <returns>The created expression.</returns>
    /// <exception cref="ArgumentException">
    /// The specified type <paramref name="fieldType" /> is not supported.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="dataReaderExpression" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="fieldOrdinalExpression" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="fieldType" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    internal static Expression CreateGetDbDataReaderFieldValueExpression(
        Expression dataReaderExpression,
        Expression fieldOrdinalExpression,
        Int32 fieldOrdinal,
        String? fieldName,
        Type fieldType
    )
    {
        ArgumentNullException.ThrowIfNull(dataReaderExpression);
        ArgumentNullException.ThrowIfNull(fieldOrdinalExpression);
        ArgumentNullException.ThrowIfNull(fieldType);

        if (fieldType == typeof(Byte[]))
        {
            // Special handling for byte arrays since DbDataReader does not have a GetBytes method that returns
            // a byte array directly.
            return
                Expression.Convert(
                    Expression.Call(
                        dataReaderExpression,
                        DbDataReaderGetValueMethod,
                        fieldOrdinalExpression
                    ),
                    typeof(Byte[])
                );
        }

        if (fieldType == typeof(TimeSpan))
        {
            // Special handling for the type TimeSpan since DbDataReader does not have a GetTimeSpan method that
            // returns a TimeSpan directly.
            return
                Expression.Convert(
                    Expression.Call(
                        dataReaderExpression,
                        DbDataReaderGetValueMethod,
                        fieldOrdinalExpression
                    ),
                    typeof(TimeSpan)
                );
        }

        if (fieldType == typeof(TimeOnly))
        {
            // Special handling for the type TimeOnly since DbDataReader does not have a GetTimeOnly method that
            // returns a TimeOnly directly.
            return
                Expression.Convert(
                    Expression.Call(
                        dataReaderExpression,
                        DbDataReaderGetValueMethod,
                        fieldOrdinalExpression
                    ),
                    typeof(TimeOnly)
                );
        }

        if (fieldType == typeof(DateOnly))
        {
            // Special handling for the type DateOnly since DbDataReader does not have a GetDateOnly method
            // that returns a DateOnly directly.
            return
                Expression.Convert(
                    Expression.Call(
                        dataReaderExpression,
                        DbDataReaderGetValueMethod,
                        fieldOrdinalExpression
                    ),
                    typeof(DateOnly)
                );
        }

        if (fieldType == typeof(DateTimeOffset))
        {
            // Special handling for the type DateTimeOffset since DbDataReader does not have a GetDateTimeOffset method
            // that returns a DateTimeOffset directly.
            return
                Expression.Convert(
                    Expression.Call(
                        dataReaderExpression,
                        DbDataReaderGetValueMethod,
                        fieldOrdinalExpression
                    ),
                    typeof(DateTimeOffset)
                );
        }

        if (!dbDataReaderTypedGetMethods.TryGetValue(fieldType, out var dbDataReaderGetMethod))
        {
            if (!String.IsNullOrWhiteSpace(fieldName))
            {
                throw new ArgumentException(
                    $"The data type {fieldType} of the column '{fieldName}' returned by the SQL statement is not " +
                    "supported.",
                    nameof(fieldType)
                );
            }

            throw new ArgumentException(
                $"The data type {fieldType} of the {(fieldOrdinal + 1).OrdinalizeEnglish()} column returned by the " +
                "SQL statement is not supported.",
                nameof(fieldType)
            );
        }

        return Expression.Call(
            dataReaderExpression,
            dbDataReaderGetMethod,
            fieldOrdinalExpression
        );
    }

    /// <summary>
    /// Determines whether a typed <see cref="DbDataReader" />.GetXXX method is available for the field type
    /// <paramref name="fieldType" />.
    /// </summary>
    /// <param name="fieldType">The field type to check.</param>
    /// <returns>
    /// <see langword="true" /> if a typed <see cref="DbDataReader" />.GetXXX method is available for the field type
    /// <paramref name="fieldType" />; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="fieldType" /> is <see langword="null" />.</exception>
    internal static Boolean IsDbDataReaderTypedGetMethodAvailable(Type fieldType)
    {
        ArgumentNullException.ThrowIfNull(fieldType);

        return dbDataReaderTypedGetMethods.ContainsKey(fieldType)
               ||
               // The method CreateGetDbDataReaderFieldValueExpression has special handling for
               // Byte[], DateOnly, TimeSpan, TimeOnly and DateTimeOffset.
               fieldType == typeof(Byte[])
               ||
               fieldType == typeof(DateOnly)
               ||
               fieldType == typeof(TimeSpan)
               ||
               fieldType == typeof(TimeOnly)
               ||
               fieldType == typeof(DateTimeOffset);
    }

    private static readonly Dictionary<Type, MethodInfo> dbDataReaderTypedGetMethods = new()
    {
        { typeof(Boolean), typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetBoolean))! },
        { typeof(Byte), typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetByte))! },
        { typeof(DateTime), typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetDateTime))! },
        { typeof(Decimal), typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetDecimal))! },
        { typeof(Double), typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetDouble))! },
        { typeof(Single), typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetFloat))! },
        { typeof(Guid), typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetGuid))! },
        { typeof(Int16), typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetInt16))! },
        { typeof(Int32), typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetInt32))! },
        { typeof(Int64), typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetInt64))! },
        { typeof(String), typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetString))! }
    };
}
