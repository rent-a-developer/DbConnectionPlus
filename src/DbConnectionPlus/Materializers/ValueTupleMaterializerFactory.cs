// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using System.Linq.Expressions;
using System.Reflection;
using Humanizer;
using RentADeveloper.DbConnectionPlus.Converters;
using RentADeveloper.DbConnectionPlus.Extensions;

namespace RentADeveloper.DbConnectionPlus.Materializers;

/// <summary>
/// Creates functions to materialize the data in a <see cref="DbDataReader" /> to instances of
/// <see cref="ValueTuple" />.
/// </summary>
internal static class ValueTupleMaterializerFactory
{
    /// <summary>
    /// Gets a materializer function that materializes the data in a <see cref="DbDataReader" /> to an instance of the
    /// value tuple type <typeparamref name="TValueTuple" />.
    /// </summary>
    /// <typeparam name="TValueTuple">
    /// The type of value tuple the materializer function should materialize.
    /// </typeparam>
    /// <param name="dataReader">The <see cref="DbDataReader" /> for which to create the materializer.</param>
    /// <returns>
    /// A function that materializes the data in a <see cref="DbDataReader" /> to an instance of
    /// <typeparamref name="TValueTuple" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="dataReader" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 The type <typeparamref name="TValueTuple" /> is not a <see cref="ValueTuple" /> type.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="dataReader" /> has no fields.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 The value tuple type <typeparamref name="TValueTuple" /> does not have the same number of
    ///                 fields as <paramref name="dataReader" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 A field of <paramref name="dataReader" /> has a field type which is not compatible with the
    ///                 field type of the corresponding field of the value tuple type
    /// <typeparamref name="TValueTuple" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="dataReader" /> contains a field having an unsupported field type.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <remarks>
    /// <para>The order of the fields in the value tuple must match the order of the fields in <paramref name="dataReader" />.</para>
    /// <para>
    /// The field types of the fields in <paramref name="dataReader" /> must be compatible with the field types of the
    /// fields in <paramref name="dataReader" />.
    /// The compatibility is determined using <see cref="ValueConverter.CanConvert(Type, Type)" />.
    /// </para>
    /// </remarks>
    internal static Func<DbDataReader, TValueTuple> GetMaterializer<TValueTuple>(DbDataReader dataReader)
    {
        ArgumentNullException.ThrowIfNull(dataReader);

        var valueTupleType = typeof(TValueTuple);

        if (!valueTupleType.IsValueTupleType())
        {
            throw new ArgumentException(
                $"The specified type {typeof(TValueTuple)} is not a {typeof(ValueTuple)} type."
            );
        }

        var valueTupleFieldTypes = GetValueTupleFieldTypes(valueTupleType);
        var dataReaderFieldNames = dataReader.GetFieldNames();
        var dataReaderFieldTypes = dataReader.GetFieldTypes();

        ValidateDataReader(
            valueTupleType,
            valueTupleFieldTypes,
            dataReader,
            dataReaderFieldNames,
            dataReaderFieldTypes
        );

        // We can only re-use a cached materializer if the value tuple field types, the data reader field names
        // and the data reader field types are the same:
        var cacheKey = new MaterializerCacheKey(valueTupleFieldTypes, dataReaderFieldNames, dataReaderFieldTypes);

        return (Func<DbDataReader, TValueTuple>)materializerCache.GetOrAdd(
            cacheKey,
            static (_, args) =>
                CreateMaterializer(
                    args.valueTupleType,
                    args.valueTupleFieldTypes,
                    args.dataReader,
                    args.dataReaderFieldNames,
                    args.dataReaderFieldTypes
                ),
            (valueTupleType, valueTupleFieldTypes, dataReader, dataReaderFieldNames, dataReaderFieldTypes)
        );
    }

    /// <summary>
    /// Creates a materializer function that materializes the data in a <see cref="DbDataReader" /> to an instance of
    /// the value tuple type <paramref name="valueTupleType" />.
    /// </summary>
    /// <param name="valueTupleType">
    /// The type of value tuple to materialize.
    /// </param>
    /// <param name="valueTupleFieldTypes">
    /// The field types of the value tuple type <paramref name="valueTupleType" />.
    /// </param>
    /// <param name="dataReader">The <see cref="DbDataReader" /> for which to create the materializer function.</param>
    /// <param name="dataReaderFieldNames">
    /// The names of the fields in <paramref name="dataReader" />.
    /// The order of the names must match the order of the fields in <paramref name="dataReader" />.
    /// </param>
    /// <param name="dataReaderFieldTypes">
    /// The field types of the fields in <paramref name="dataReader" />.
    /// The order of the types must match the order of the fields in <paramref name="dataReader" />.
    /// </param>
    /// <returns>
    /// A function that materializes the data in a <see cref="DbDataReader" /> to an instance of the value tuple type
    /// <paramref name="valueTupleType" />.
    /// </returns>
    private static Delegate CreateMaterializer(
        Type valueTupleType,
        Type[] valueTupleFieldTypes,
        DbDataReader dataReader,
        String[] dataReaderFieldNames,
        Type[] dataReaderFieldTypes
    )
    {
        /*
         * This method creates an expression tree to generate a materializer function instead of using reflection for
         * the materialization, because using reflection would be significantly slower.
         * Using expression trees also allows us to use the typed GetXXX methods of DbDataReader, which avoids boxing
         * in many cases.
         */

        var dataReaderParameterExpression = Expression.Parameter(typeof(DbDataReader), "dataReader");
        var dataReaderFieldValueExpressions = new Expression[dataReader.FieldCount];

        for (var fieldOrdinal = 0; fieldOrdinal < dataReader.FieldCount; fieldOrdinal++)
        {
            var fieldOrdinalExpression = Expression.Constant(fieldOrdinal);

            var dataReaderFieldName = dataReaderFieldNames[fieldOrdinal];
            var columnNameOrPosition = !String.IsNullOrWhiteSpace(dataReaderFieldName)
                ? $"column '{dataReaderFieldName}'"
                : $"{(fieldOrdinal + 1).OrdinalizeEnglish()} column";

            var dataReaderFieldType = dataReaderFieldTypes[fieldOrdinal];
            var targetType = valueTupleFieldTypes[fieldOrdinal];

            // Basically:
            // dataReader.GetXXX(fieldOrdinal)
            var getFieldValueCallExpression = MaterializerFactoryHelper.CreateGetDbDataReaderFieldValueExpression(
                dataReaderParameterExpression,
                fieldOrdinalExpression,
                fieldOrdinal,
                dataReaderFieldName,
                dataReaderFieldType
            );

            /*
             * Basically:
             *
             * if (dataReader.IsDBNull(fieldOrdinal))
             * {
             *      if (targetType.IsReferenceTypeOrNullableType())
             *      {
             *          default(targetType);
             *      }
             *      else
             *      {
             *          throw new InvalidCastException(...);
             *      }
             * }
             * else
             * {
             *      if (dataReaderFieldType != targetType)
             *      {
             *          try
             *          {
             *              ValueConverter.ConvertValueToType<targetType>((Object) dataReader.GetXXX(fieldOrdinal));
             *          }
             *          catch (Exception ex)
             *          {
             *              throw new InvalidCastException(..., ex);
             *          }
             *      }
             *      else
             *      {
             *          dataReader.GetXXX(fieldOrdinal);
             *      }
             * }
             */
            var exceptionParameterExpression = Expression.Parameter(typeof(Exception));

            Expression isDbNullBranchExpression = targetType.IsReferenceTypeOrNullableType()
                ? Expression.Default(targetType)
                : Expression.Throw(
                    Expression.New(
                        typeof(InvalidCastException).GetConstructor([typeof(String)])!,
                        Expression.Constant(
                            $"The {columnNameOrPosition} returned by the SQL statement contains a NULL " +
                            $"value, but the corresponding field of the value tuple type {valueTupleType} " +
                            "is non-nullable."
                        )
                    ),
                    targetType
                );

            var throwInvalidCastExceptionExpression = Expression.Throw(
                Expression.New(
                    typeof(InvalidCastException).GetConstructor(
                        [typeof(String), typeof(Exception)]
                    )!,
                    Expression.Constant(
                        $"The {columnNameOrPosition} returned by the SQL statement contains a " +
                        $"value that could not be converted to the type {targetType} " +
                        $"of the corresponding field of the value tuple type {valueTupleType}. " +
                        "See inner exception for details."
                    ),
                    exceptionParameterExpression
                ),
                targetType
            );

            var convertFieldValueExpression = Expression.TryCatch(
                Expression.Convert(
                    Expression.Call(
                        null,
                        MaterializerFactoryHelper.ValueConverterConvertValueToTypeMethod
                            .MakeGenericMethod(targetType),
                        Expression.Convert(getFieldValueCallExpression, typeof(Object))
                    ),
                    targetType
                ),
                Expression.Catch(
                    exceptionParameterExpression,
                    throwInvalidCastExceptionExpression
                )
            );

            var isNotDbNullBranchExpression = dataReaderFieldType != targetType
                ? convertFieldValueExpression
                : getFieldValueCallExpression;

            dataReaderFieldValueExpressions[fieldOrdinal] =
                Expression.Condition(
                    Expression.Call(
                        dataReaderParameterExpression,
                        MaterializerFactoryHelper.DbDataReaderIsDBNullMethod,
                        fieldOrdinalExpression
                    ),
                    isDbNullBranchExpression,
                    isNotDbNullBranchExpression
                );
        }

        // In C# value tuples with more than 7 fields are represented as nested value tuples.
        // E.g. a ValueTuple with 15 fields is represented as:
        // ValueTuple<T1, ..., T7, ValueTuple<T8, ..., T14, ValueTuple<T15>>>
        // In this case we need to create the nested value tuples from the inside out.

        // First we chunk the field value expressions into groups of 7.
        // We use a stack to reverse the order, so we start with the expressions for the most inner value tuple.
        var fieldValueExpressionChunks = new Stack<Expression[]>(dataReaderFieldValueExpressions.Chunk(7));

        // Then we get the constructors for the value tuple types.
        // Again, we use a stack to reverse the order, so we start with the constructor for the most inner value tuple.
        var valueTupleConstructors = new Stack<ConstructorInfo>();

        var currentValueTupleType = valueTupleType;

        // We traverse the value tuple types from the outermost to the innermost to get the constructors.
        while (true)
        {
            var valueTupleFields = currentValueTupleType.GetFields(BindingFlags.Public | BindingFlags.Instance);

            valueTupleConstructors.Push(
                currentValueTupleType.GetConstructor(
                    BindingFlags.Public | BindingFlags.Instance,
                    valueTupleFields.Select(a => a.FieldType).ToArray()
                )!
            );

            var lastValueTupleField = valueTupleFields[^1];

            // If the last field is not "Rest", we have reached the inner most value tuple type and we are done.
            if (lastValueTupleField.Name != "Rest")
            {
                break;
            }

            // Continue with the next inner value tuple type.
            currentValueTupleType = lastValueTupleField.FieldType;
        }

        Expression? newExpression = null;

        // Now we create the nested value tuples from the inside out.
        // When we are done newExpression will contain the expression for the outermost value tuple.
        while (valueTupleConstructors.Count > 0)
        {
            var constructor = valueTupleConstructors.Pop();
            var chunk = fieldValueExpressionChunks.Pop();

            // If newExpression is null, it means we are at the innermost value tuple,
            // so we only need to use the current chunk of field value expressions as arguments.
            //
            // Otherwise, if newExpression is not null, it means we are not at the innermost value tuple,
            // and we need to add newExpression (which contains the last created inner value tuple) as the argument for
            // the "Rest" parameter.
            var arguments = newExpression is not null ? [..chunk, newExpression] : chunk;

            newExpression = Expression.New(
                constructor,
                arguments
            );
        }

        return Expression.Lambda(newExpression!, dataReaderParameterExpression).Compile();
    }

    /// <summary>
    /// <para>
    /// Gets the types of the fields of the value tuple type <paramref name="valueTupleType" /> including the fields
    /// of all nested value tuple types.
    /// </para>
    /// <para>
    /// For example, for the value tuple type
    /// <c><![CDATA[ ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9> ]]></c> this method returns the types
    /// T1, T2, T3, T4, T5, T6, T7, T8, T9.
    /// </para>
    /// </summary>
    /// <param name="valueTupleType">The value tuple type of which to get the field types.</param>
    /// <returns>
    /// An array containing the types of the fields of the value tuple type <paramref name="valueTupleType" />
    /// including the fields of all nested value tuple types.
    /// </returns>
    private static Type[] GetValueTupleFieldTypes(Type valueTupleType)
    {
        var fieldTypes = new List<Type>();
        var valueTupleTypes = new Stack<Type>();

        valueTupleTypes.Push(valueTupleType);

        while (valueTupleTypes.Count > 0)
        {
            var currentValueTupleType = valueTupleTypes.Pop();

            foreach (var field in currentValueTupleType.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.Name == "Rest")
                {
                    // Push the nested value tuple type.
                    valueTupleTypes.Push(field.FieldType);
                }
                else
                {
                    fieldTypes.Add(field.FieldType);
                }
            }
        }

        return fieldTypes.ToArray();
    }

    /// <summary>
    /// Validates that instances of the value tuple type <paramref name="valueTupleType" /> can be materialized from
    /// the data in <paramref name="dataReader" />.
    /// </summary>
    /// <param name="valueTupleType">The type of value tuple to materialize.</param>
    /// <param name="valueTupleFieldTypes">
    /// The field types of fields of the value tuple type <paramref name="valueTupleType" />.
    /// </param>
    /// <param name="dataReader">The <see cref="DbDataReader" /> to validate.</param>
    /// <param name="dataReaderFieldNames">
    /// The names of the fields in <paramref name="dataReader" />.
    /// The order of the names must match the order of the fields in <paramref name="dataReader" />.
    /// </param>
    /// <param name="dataReaderFieldTypes">
    /// The field types of the fields in <paramref name="dataReader" />.
    /// The order of the types must match the order of the fields in <paramref name="dataReader" />.
    /// </param>
    /// <exception cref="ArgumentException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="dataReader" /> has no fields.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 The value tuple type <paramref name="valueTupleType" /> does not have the same number of
    ///                 fields as <paramref name="dataReader" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 A field of <paramref name="dataReader" /> has a field type which is not compatible with the
    ///                 field type of the corresponding field of the value tuple type
    /// <paramref name="valueTupleType" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="dataReader" /> contains a field having an unsupported field type.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    private static void ValidateDataReader(
        Type valueTupleType,
        Type[] valueTupleFieldTypes,
        DbDataReader dataReader,
        String[] dataReaderFieldNames,
        Type[] dataReaderFieldTypes
    )
    {
        if (dataReader.FieldCount == 0)
        {
            throw new ArgumentException("The SQL statement did not return any columns.", nameof(dataReader));
        }

        if (dataReader.FieldCount != valueTupleFieldTypes.Length)
        {
            throw new ArgumentException(
                $"The SQL statement returned {"column".ToQuantity(dataReader.FieldCount)}, but the value tuple type " +
                $"{valueTupleType} has {"field".ToQuantity(valueTupleFieldTypes.Length)}. Make sure that the SQL " +
                "statement returns the same number of columns as the number of fields in the value tuple type.",
                nameof(dataReader)
            );
        }

        for (var fieldOrdinal = 0; fieldOrdinal < dataReader.FieldCount; fieldOrdinal++)
        {
            var dataReaderFieldName = dataReaderFieldNames[fieldOrdinal];

            var columnNameOrPosition = !String.IsNullOrWhiteSpace(dataReaderFieldName)
                ? $"column '{dataReaderFieldName}'"
                : $"{(fieldOrdinal + 1).OrdinalizeEnglish()} column";

            var valueTupleFieldType = valueTupleFieldTypes[fieldOrdinal];
            var dataReaderFieldType = dataReaderFieldTypes[fieldOrdinal];

            if (!ValueConverter.CanConvert(dataReaderFieldType, valueTupleFieldType))
            {
                throw new ArgumentException(
                    $"The data type {dataReaderFieldType} of the {columnNameOrPosition} returned by the SQL " +
                    $"statement is not compatible with the field type {valueTupleFieldType} of the corresponding " +
                    $"field of the value tuple type {valueTupleType}.",
                    nameof(dataReader)
                );
            }

            if (!MaterializerFactoryHelper.IsDbDataReaderTypedGetMethodAvailable(dataReaderFieldType))
            {
                throw new ArgumentException(
                    $"The data type {dataReaderFieldType} of the {columnNameOrPosition} returned by the SQL " +
                    "statement is not supported.",
                    nameof(dataReader)
                );
            }
        }
    }

    private static readonly ConcurrentDictionary<MaterializerCacheKey, Delegate> materializerCache = [];

    /// <summary>
    /// A cache key used to uniquely identify a value tuple materializer.
    /// </summary>
    /// <param name="valueTupleFieldTypes">The field types of value tuple the materializer materializes.</param>
    /// <param name="dataReaderFieldNames">
    /// The field names of the <see cref="DbDataReader" /> from which to materialize.
    /// The order of the names must match the order of the fields in the data reader.
    /// </param>
    /// <param name="dataReaderFieldTypes">
    /// The field types of the <see cref="DbDataReader" /> from which to materialize.
    /// The order of the types must match the order of the fields in the data reader.
    /// </param>
    private readonly struct MaterializerCacheKey(
        Type[] valueTupleFieldTypes,
        String[] dataReaderFieldNames,
        Type[] dataReaderFieldTypes
    )
        : IEquatable<MaterializerCacheKey>
    {
        /// <inheritdoc />
        public Boolean Equals(MaterializerCacheKey other) =>
            this.ValueTupleFieldTypes.SequenceEqual(other.ValueTupleFieldTypes) &&
            this.DataReaderFieldNames.SequenceEqual(other.DataReaderFieldNames) &&
            this.DataReaderFieldTypes.SequenceEqual(other.DataReaderFieldTypes);

        /// <inheritdoc />
        public override Boolean Equals(Object? obj) =>
            obj is MaterializerCacheKey other && this.Equals(other);

        /// <inheritdoc />
        public override Int32 GetHashCode()
        {
            var hashCode = new HashCode();


            foreach (var fieldType in this.ValueTupleFieldTypes)
            {
                hashCode.Add(fieldType);
            }

            foreach (var fieldName in this.DataReaderFieldNames)
            {
                hashCode.Add(fieldName);
            }

            foreach (var fieldType in this.DataReaderFieldTypes)
            {
                hashCode.Add(fieldType);
            }

            return hashCode.ToHashCode();
        }

        private String[] DataReaderFieldNames { get; } = dataReaderFieldNames;
        private Type[] DataReaderFieldTypes { get; } = dataReaderFieldTypes;
        private Type[] ValueTupleFieldTypes { get; } = valueTupleFieldTypes;
    }
}
