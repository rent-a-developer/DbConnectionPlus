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
    private static readonly Dictionary<Type, MethodInfo> dbDataReaderTypedGetMethods = new()
    {
        { typeof(bool), typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetBoolean))! },
        { typeof(byte), typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetByte))! },
        { typeof(DateTime), typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetDateTime))! },
        { typeof(decimal), typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetDecimal))! },
        { typeof(double), typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetDouble))! },
        { typeof(float), typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetFloat))! },
        { typeof(Guid), typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetGuid))! },
        { typeof(short), typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetInt16))! },
        { typeof(int), typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetInt32))! },
        { typeof(long), typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetInt64))! },
        { typeof(string), typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetString))! },
    };

    /// <summary>
    /// The functions that read a field value using the same typed <see cref="DbDataReader" />.GetXXX method as
    /// <see cref="dbDataReaderTypedGetMethods" />, for the materializer path that cannot compile an expression tree.
    /// </summary>
    /// <remarks>
    /// The key set must stay identical to the key set of <see cref="dbDataReaderTypedGetMethods" />, otherwise the
    /// two materializer paths disagree on which field types are supported.
    /// </remarks>
    private static readonly Dictionary<Type, Func<DbDataReader, int, object?>> dbDataReaderTypedGetValueFunctions =
        new()
        {
            { typeof(bool), static (dataReader, fieldOrdinal) => dataReader.GetBoolean(fieldOrdinal) },
            { typeof(byte), static (dataReader, fieldOrdinal) => dataReader.GetByte(fieldOrdinal) },
            { typeof(DateTime), static (dataReader, fieldOrdinal) => dataReader.GetDateTime(fieldOrdinal) },
            { typeof(decimal), static (dataReader, fieldOrdinal) => dataReader.GetDecimal(fieldOrdinal) },
            { typeof(double), static (dataReader, fieldOrdinal) => dataReader.GetDouble(fieldOrdinal) },
            { typeof(float), static (dataReader, fieldOrdinal) => dataReader.GetFloat(fieldOrdinal) },
            { typeof(Guid), static (dataReader, fieldOrdinal) => dataReader.GetGuid(fieldOrdinal) },
            { typeof(short), static (dataReader, fieldOrdinal) => dataReader.GetInt16(fieldOrdinal) },
            { typeof(int), static (dataReader, fieldOrdinal) => dataReader.GetInt32(fieldOrdinal) },
            { typeof(long), static (dataReader, fieldOrdinal) => dataReader.GetInt64(fieldOrdinal) },
            { typeof(string), static (dataReader, fieldOrdinal) => dataReader.GetString(fieldOrdinal) },
        };

    /// <summary>
    /// The field types <see cref="DbDataReader" /> has no typed GetXXX method for, and which
    /// <see cref="CreateGetDbDataReaderFieldValueExpression" /> therefore reads through
    /// <see cref="DbDataReader.GetValue" /> instead.
    /// </summary>
    private static readonly HashSet<Type> dbDataReaderUntypedFieldTypes =
    [
        typeof(byte[]),
        typeof(DateOnly),
        typeof(DateTimeOffset),
        typeof(TimeOnly),
        typeof(TimeSpan),
    ];

    /// <summary>
    /// The generic method definition of the <see cref="ValueConverter.ConvertValueToType{TTarget}" /> method, cached
    /// for <see cref="MakeValueConverterConvertValueToTypeMethod" />.
    /// </summary>
    private static readonly MethodInfo valueConverterConvertValueToTypeMethod = typeof(ValueConverter)
        .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
        .First(m => m is { Name: nameof(ValueConverter.ConvertValueToType), IsGenericMethod: true });

    /// <summary>
    /// The <see cref="DbDataReader.GetValue(int)" /> method.
    /// </summary>
    internal static MethodInfo DbDataReaderGetValueMethod { get; } =
        typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetValue))!;

    /// <summary>
    /// The <see cref="DbDataReader.IsDBNull(int)" /> method.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    internal static MethodInfo DbDataReaderIsDBNullMethod { get; } =
        typeof(DbDataReader).GetMethod(nameof(DbDataReader.IsDBNull))!;

    /// <summary>
    /// The 'Chars' property of the <see cref="string" /> type.
    /// </summary>
    internal static PropertyInfo StringCharsProperty { get; } =
        typeof(string).GetProperty("Chars", BindingFlags.Instance | BindingFlags.Public)!;

    /// <summary>
    /// The <see cref="string.Concat(string, string, string)" /> method.
    /// </summary>
    internal static MethodInfo StringConcatMethod { get; } =
        typeof(string).GetMethod(nameof(string.Concat), [typeof(string), typeof(string), typeof(string)])!;

    /// <summary>
    /// The <see cref="string.Length" /> property.
    /// </summary>
    internal static PropertyInfo StringLengthProperty { get; } =
        typeof(string).GetProperty(nameof(string.Length), BindingFlags.Instance | BindingFlags.Public)!;

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
        int fieldOrdinal,
        string? fieldName,
        Type fieldType
    )
    {
        ArgumentNullException.ThrowIfNull(dataReaderExpression);
        ArgumentNullException.ThrowIfNull(fieldOrdinalExpression);
        ArgumentNullException.ThrowIfNull(fieldType);

        if (fieldType == typeof(byte[]))
        {
            // Special handling for byte arrays since DbDataReader does not have a GetBytes method that returns
            // a byte array directly.
            return Expression.Convert(
                Expression.Call(dataReaderExpression, DbDataReaderGetValueMethod, fieldOrdinalExpression),
                typeof(byte[])
            );
        }

        if (fieldType == typeof(TimeSpan))
        {
            // Special handling for the type TimeSpan since DbDataReader does not have a GetTimeSpan method that
            // returns a TimeSpan directly.
            return Expression.Convert(
                Expression.Call(dataReaderExpression, DbDataReaderGetValueMethod, fieldOrdinalExpression),
                typeof(TimeSpan)
            );
        }

        if (fieldType == typeof(TimeOnly))
        {
            // Special handling for the type TimeOnly since DbDataReader does not have a GetTimeOnly method that
            // returns a TimeOnly directly.
            return Expression.Convert(
                Expression.Call(dataReaderExpression, DbDataReaderGetValueMethod, fieldOrdinalExpression),
                typeof(TimeOnly)
            );
        }

        if (fieldType == typeof(DateOnly))
        {
            // Special handling for the type DateOnly since DbDataReader does not have a GetDateOnly method
            // that returns a DateOnly directly.
            return Expression.Convert(
                Expression.Call(dataReaderExpression, DbDataReaderGetValueMethod, fieldOrdinalExpression),
                typeof(DateOnly)
            );
        }

        if (fieldType == typeof(DateTimeOffset))
        {
            // Special handling for the type DateTimeOffset since DbDataReader does not have a GetDateTimeOffset method
            // that returns a DateTimeOffset directly.
            return Expression.Convert(
                Expression.Call(dataReaderExpression, DbDataReaderGetValueMethod, fieldOrdinalExpression),
                typeof(DateTimeOffset)
            );
        }

        if (!dbDataReaderTypedGetMethods.TryGetValue(fieldType, out var dbDataReaderGetMethod))
        {
            if (!string.IsNullOrWhiteSpace(fieldName))
            {
                throw new ArgumentException(
                    $"The data type {fieldType} of the column '{fieldName}' returned by the SQL statement is not "
                        + "supported.",
                    nameof(fieldType)
                );
            }

            throw new ArgumentException(
                $"The data type {fieldType} of the {(fieldOrdinal + 1).OrdinalizeEnglish()} column returned by the "
                    + "SQL statement is not supported.",
                nameof(fieldType)
            );
        }

        return Expression.Call(dataReaderExpression, dbDataReaderGetMethod, fieldOrdinalExpression);
    }

    /// <summary>
    /// Creates a function that gets the value of the field with the ordinal <paramref name="fieldOrdinal" /> from a
    /// <see cref="DbDataReader" />, using the same <see cref="DbDataReader" />.GetXXX method that
    /// <see cref="CreateGetDbDataReaderFieldValueExpression" /> emits for the field type
    /// <paramref name="fieldType" />.
    /// </summary>
    /// <param name="fieldOrdinal">The field ordinal of the field to get the value from.</param>
    /// <param name="fieldName">The field name of the field to get the value from.</param>
    /// <param name="fieldType">The field type of the field to get the value from.</param>
    /// <returns>The created function. It returns the field value boxed in an <see cref="object" />.</returns>
    /// <exception cref="ArgumentException">
    /// The specified type <paramref name="fieldType" /> is not supported.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="fieldType" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// This is the counterpart of <see cref="CreateGetDbDataReaderFieldValueExpression" /> for the materializer path
    /// that runs when the application is published with Native AOT and no expression tree can be compiled.
    /// </para>
    /// <para>
    /// Both must pick the same <see cref="DbDataReader" />.GetXXX method for a given field type. A database provider
    /// is free to return a different value from <see cref="DbDataReader.GetValue" /> than from its typed
    /// counterpart, so picking a different method here would make the two materializer paths disagree.
    /// </para>
    /// </remarks>
    internal static Func<DbDataReader, object?> CreateGetDbDataReaderFieldValueFunction(
        int fieldOrdinal,
        string? fieldName,
        Type fieldType
    )
    {
        ArgumentNullException.ThrowIfNull(fieldType);

        if (dbDataReaderUntypedFieldTypes.Contains(fieldType))
        {
            // Special handling for the field types DbDataReader has no typed GetXXX method for. The expression
            // counterpart casts the DbDataReader.GetValue result to the field type; here the value stays boxed.
            return dataReader => dataReader.GetValue(fieldOrdinal);
        }

        if (!dbDataReaderTypedGetValueFunctions.TryGetValue(fieldType, out var dbDataReaderGetValueFunction))
        {
            if (!string.IsNullOrWhiteSpace(fieldName))
            {
                throw new ArgumentException(
                    $"The data type {fieldType} of the column '{fieldName}' returned by the SQL statement is not "
                        + "supported.",
                    nameof(fieldType)
                );
            }

            throw new ArgumentException(
                $"The data type {fieldType} of the {(fieldOrdinal + 1).OrdinalizeEnglish()} column returned by the "
                    + "SQL statement is not supported.",
                nameof(fieldType)
            );
        }

        return dataReader => dbDataReaderGetValueFunction(dataReader, fieldOrdinal);
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
    internal static bool IsDbDataReaderTypedGetMethodAvailable(Type fieldType)
    {
        ArgumentNullException.ThrowIfNull(fieldType);

        return dbDataReaderTypedGetMethods.ContainsKey(fieldType) || dbDataReaderUntypedFieldTypes.Contains(fieldType);
    }

    /// <summary>
    /// Specializes <see cref="valueConverterConvertValueToTypeMethod" /> over <paramref name="targetType" />, so that
    /// a compiled expression tree can call the generic
    /// <see cref="ValueConverter.ConvertValueToType{TTarget}" /> directly.
    /// </summary>
    /// <param name="targetType">The type to specialize the method over.</param>
    /// <returns>
    /// The <see cref="ValueConverter.ConvertValueToType{TTarget}" /> method, specialized over
    /// <paramref name="targetType" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This exists as its own method purely so that the <c>IL2060</c> suppression below covers one line of code
    /// instead of the whole expression-building method it is called from. Both materializer factories call it.
    /// </para>
    /// <para>
    /// The suppression is sound rather than convenient, and provably so:
    /// <see cref="ValueConverter.ConvertValueToType{TTarget}" /> declares <b>no</b>
    /// <see cref="DynamicallyAccessedMembersAttribute" /> on its type parameter. <c>IL2060</c> reports that the
    /// requirements of a runtime-specialized generic method cannot be guaranteed; here there are no requirements to
    /// guarantee, so there is nothing the trimmer could remove and nothing for a consumer to act on. Whether the
    /// conversion reflects over the target type at all is a question about the converters, which are annotation-free
    /// and verified so: the trim analyzers report nothing for <c>ValueConverter</c> or <c>EnumConverter</c>.
    /// </para>
    /// <para>
    /// <see cref="RequiresDynamicCodeAttribute" /> is a different matter and is kept: specializing a generic method
    /// over a value type at run time genuinely needs code generation. Only the callers that are guarded by
    /// <see cref="RuntimeFeature.IsDynamicCodeSupported" /> may reach this.
    /// </para>
    /// </remarks>
    [RequiresDynamicCode(
        "Specializing a generic method over a value type at run time is not supported when the application is "
            + "published with Native AOT. Call this only from a RuntimeFeature.IsDynamicCodeSupported branch."
    )]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2060:MakeGenericMethod call cannot be statically analyzed",
        Justification = "ValueConverter.ConvertValueToType<TTarget> declares no DynamicallyAccessedMembers on TTarget, so the "
            + "specialized instantiation has no requirements that trimming could fail to preserve. Reaching this "
            + "method at all requires a RuntimeFeature.IsDynamicCodeSupported branch."
    )]
    internal static MethodInfo MakeValueConverterConvertValueToTypeMethod(Type targetType) =>
        valueConverterConvertValueToTypeMethod.MakeGenericMethod(targetType);
}
