// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using System.Linq.Expressions;
using RentADeveloper.DbConnectionPlus.Converters;
using RentADeveloper.DbConnectionPlus.Entities;
using RentADeveloper.DbConnectionPlus.Extensions;

namespace RentADeveloper.DbConnectionPlus.Materializers;

/// <summary>
/// A factory that creates functions to materialize instances of <see cref="DbDataReader" /> to instances of entities.
/// </summary>
internal static class EntityMaterializerFactory
{
    /// <summary>
    /// Gets a materializer function that materializes the data in a <see cref="DbDataReader" /> to an instance of the
    /// type <typeparamref name="TEntity" />.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity to materialize.</typeparam>
    /// <param name="dataReader">The <see cref="DbDataReader" /> for which to create the materializer function.</param>
    /// <returns>
    /// A function that materializes the data in a <see cref="DbDataReader" /> to an instance of
    /// <typeparamref name="TEntity" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="dataReader" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="dataReader" /> has no fields.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="dataReader" /> contains a field with no field name.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="dataReader" /> contains a field with an unsupported field type.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <typeparamref name="TEntity" /> does not satisfy the conditions described in the remarks.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <remarks>
    /// <para>The type <typeparamref name="TEntity" /> must either:</para>
    /// <para>
    /// 1. Have a constructor whose parameters match the fields in <paramref name="dataReader" />.
    ///    The names of the parameters must match the names of the fields (case-insensitive).
    ///    The types of the parameters must be compatible with the types of the fields.
    ///    The compatibility is determined using <see cref="ValueConverter.CanConvert" />.
    ///    The parameters can be in any order.
    /// </para>
    /// <para>Or</para>
    /// <para>
    /// 2. Have a parameterless constructor and instance properties (with public setters) that match the fields in
    /// <paramref name="dataReader" />.
    /// </para>
    /// <para>   The names of the properties must match the names of the fields (case-insensitive).</para>
    /// <para>
    ///    The types of the properties must be compatible with the types of the fields.
    ///    The compatibility is determined using <see cref="ValueConverter.CanConvert" />.
    /// </para>
    /// <para>   Fields without a matching property will be ignored.</para>
    /// <para>If neither condition is satisfied, an <see cref="ArgumentException" /> will be thrown.</para>
    /// <para>
    /// If a constructor parameter or a property cannot be set to the value of the corresponding field of
    /// <paramref name="dataReader" /> (for example, due to a type mismatch), the returned function throws an
    /// <see cref="InvalidCastException" />.
    /// </para>
    /// </remarks>
    internal static Func<DbDataReader, TEntity> GetMaterializer<TEntity>(DbDataReader dataReader)
    {
        ArgumentNullException.ThrowIfNull(dataReader);

        var entityType = typeof(TEntity);

        var dataReaderFieldNames = dataReader.GetFieldNames();
        var dataReaderFieldTypes = dataReader.GetFieldTypes();

        ValidateDataReader(entityType, dataReader, dataReaderFieldNames, dataReaderFieldTypes);

        // We can only re-use a cached materializer function if the entity type, the data reader field names and
        // the data reader field types are the same:
        var cacheKey = new MaterializerCacheKey(entityType, dataReaderFieldNames, dataReaderFieldTypes);

        return (Func<DbDataReader, TEntity>)materializerCache.GetOrAdd(
            cacheKey,
            static (cacheKey2, args) =>
                CreateMaterializer(
                    cacheKey2.EntityType,
                    args.dataReader,
                    args.dataReaderFieldNames,
                    args.dataReaderFieldTypes
                ),
            (dataReader, dataReaderFieldNames, dataReaderFieldTypes)
        );
    }

    /// <summary>
    /// Creates a materializer function that materializes the data in a <see cref="DbDataReader" /> to an instance of
    /// the type <paramref name="entityType" />.
    /// </summary>
    /// <param name="entityType">The type of entity to materialize.</param>
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
    /// A function that materializes the data in a <see cref="DbDataReader" /> to an instance of the type
    /// <paramref name="entityType" />.
    /// </returns>
    /// <remarks>
    /// <para>The type <paramref name="entityType" /> must either:</para>
    /// <para>
    /// 1. Have a constructor whose parameters match the fields in <paramref name="dataReader" />.
    ///    The names of the parameters must match the names of the fields (case-insensitive).
    ///    The types of the parameters must be compatible with the types of the fields.
    ///    The compatibility is determined using <see cref="ValueConverter.CanConvert" />.
    ///    The parameters can be in any order.
    /// </para>
    /// <para>Or</para>
    /// <para>
    /// 2. Have a parameterless constructor and instance properties (with public setters) that match the fields in
    /// <paramref name="dataReader" />.
    /// </para>
    /// <para>   The names of the properties must match the names of the fields (case-insensitive).</para>
    /// <para>
    ///    The types of the properties must be compatible with the types of the fields.
    ///    The compatibility is determined using <see cref="ValueConverter.CanConvert" />.
    /// </para>
    /// <para>   Fields without a matching property will be ignored.</para>
    /// <para>
    /// If a constructor parameter or a property cannot be set to the value of the corresponding field of
    /// <paramref name="dataReader" /> (for example, due to a type mismatch), the returned function throws an
    /// <see cref="InvalidCastException" />.
    /// </para>
    /// </remarks>
    private static Delegate CreateMaterializer(
        Type entityType,
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

        var fieldOrdinalToTargetType = new Dictionary<Int32, Type>(dataReader.FieldCount);
        var fieldOrdinalToConstructorParameterIndex = new Dictionary<Int32, Int32>(dataReader.FieldCount);

        var compatibleConstructor = EntityHelper.FindCompatibleConstructor(
            entityType,
            dataReaderFieldNames.Zip(dataReaderFieldTypes, (name, type) => (name, type)).ToArray()
        );

        var entityPropertiesByName = EntityHelper.GetEntityTypeMetadata(entityType)
            .MappedProperties.Where(a => a.CanWrite)
            .ToDictionary(a => a.PropertyName, StringComparer.OrdinalIgnoreCase);

        if (compatibleConstructor is not null)
        {
            var constructorParameters = compatibleConstructor.GetParameters().ToList();

            for (var fieldOrdinal = 0; fieldOrdinal < dataReader.FieldCount; fieldOrdinal++)
            {
                var constructorParameter = constructorParameters.First(p =>
                    !String.IsNullOrWhiteSpace(p.Name) &&
                    p.Name.Equals(dataReaderFieldNames[fieldOrdinal], StringComparison.OrdinalIgnoreCase) &&
                    ValueConverter.CanConvert(dataReaderFieldTypes[fieldOrdinal], p.ParameterType)
                );

                fieldOrdinalToConstructorParameterIndex.Add(
                    fieldOrdinal,
                    constructorParameters.IndexOf(constructorParameter)
                );

                fieldOrdinalToTargetType.Add(fieldOrdinal, constructorParameter.ParameterType);
            }
        }
        else
        {
            for (var fieldOrdinal = 0; fieldOrdinal < dataReader.FieldCount; fieldOrdinal++)
            {
                var dataReaderFieldName = dataReaderFieldNames[fieldOrdinal];

                if (entityPropertiesByName.TryGetValue(dataReaderFieldName, out var entityProperty))
                {
                    fieldOrdinalToTargetType.Add(
                        fieldOrdinal,
                        entityProperty.PropertyType
                    );
                }
                else
                {
                    fieldOrdinalToTargetType.Add(
                        fieldOrdinal,
                        dataReaderFieldTypes[fieldOrdinal]
                    );
                }
            }
        }

        for (var fieldOrdinal = 0; fieldOrdinal < dataReader.FieldCount; fieldOrdinal++)
        {
            var fieldOrdinalExpression = Expression.Constant(fieldOrdinal);

            var dataReaderFieldName = dataReaderFieldNames[fieldOrdinal];

            if (compatibleConstructor is null && !entityPropertiesByName.ContainsKey(dataReaderFieldName))
            {
                // No need to read the field when we are using properties to materialize and there is no matching
                // property for the field.
                continue;
            }

            var dataReaderFieldType = dataReaderFieldTypes[fieldOrdinal];
            var targetType = fieldOrdinalToTargetType[fieldOrdinal];

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
                            $"The column '{dataReaderFieldName}' returned by the SQL statement contains a " +
                            $"NULL value, but the corresponding property of the type {entityType} is " +
                            "non-nullable."
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
                        $"The column '{dataReaderFieldName}' returned by the SQL statement " +
                        $"contains a value that could not be converted to the type {targetType} " +
                        $"of the corresponding property of the type {entityType}. See inner " +
                        "exception for details."
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

        Expression bodyExpression;

        if (compatibleConstructor is not null)
        {
            var constructorArgumentExpressions = new Expression[dataReader.FieldCount];

            for (var fieldOrdinal = 0; fieldOrdinal < dataReader.FieldCount; fieldOrdinal++)
            {
                var constructorArgumentIndex = fieldOrdinalToConstructorParameterIndex[fieldOrdinal];

                constructorArgumentExpressions[constructorArgumentIndex] =
                    dataReaderFieldValueExpressions[fieldOrdinal];
            }

            // Basically:
            // new TEntity(constructorArgumentExpressions...)
            bodyExpression = Expression.New(compatibleConstructor, constructorArgumentExpressions);
        }
        else
        {
            var memberBindings = new List<MemberBinding>();

            for (var fieldOrdinal = 0; fieldOrdinal < dataReader.FieldCount; fieldOrdinal++)
            {
                var dataReaderFieldName = dataReaderFieldNames[fieldOrdinal];

                if (!entityPropertiesByName.TryGetValue(dataReaderFieldName, out var entityProperty))
                {
                    continue;
                }

                memberBindings.Add(
                    Expression.Bind(entityProperty.PropertyInfo, dataReaderFieldValueExpressions[fieldOrdinal])
                );
            }

            // Basically:
            // new TEntity { Property1 = ..., Property2 = ..., ... }
            bodyExpression = Expression.MemberInit(Expression.New(entityType), memberBindings);
        }

        return Expression.Lambda(bodyExpression, dataReaderParameterExpression).Compile();
    }

    /// <summary>
    /// Validates that instances of the type <paramref name="entityType" /> can be materialized from the data in
    /// <paramref name="dataReader" />.
    /// </summary>
    /// <param name="entityType">The type of entity to materialize.</param>
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
    ///                 <paramref name="dataReader" /> contains a field with no field name.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="dataReader" /> contains a field with an unsupported field type.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="entityType" /> does not have a parameterless constructor and no constructor
    ///                 whose parameters match the fields in <paramref name="dataReader" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    private static void ValidateDataReader(
        Type entityType,
        DbDataReader dataReader,
        String[] dataReaderFieldNames,
        Type[] dataReaderFieldTypes
    )
    {
        if (dataReader.FieldCount == 0)
        {
            throw new ArgumentException("The SQL statement did not return any columns.", nameof(dataReader));
        }

        for (var fieldOrdinal = 0; fieldOrdinal < dataReader.FieldCount; fieldOrdinal++)
        {
            var dataReaderFieldName = dataReaderFieldNames[fieldOrdinal];
            var dataReaderFieldType = dataReaderFieldTypes[fieldOrdinal];

            if (String.IsNullOrWhiteSpace(dataReaderFieldName))
            {
                throw new ArgumentException(
                    $"The {(fieldOrdinal + 1).OrdinalizeEnglish()} column returned by the SQL statement does not " +
                    "have a name. Make sure that all columns the statement returns have a name.",
                    nameof(dataReader)
                );
            }

            if (!MaterializerFactoryHelper.IsDbDataReaderTypedGetMethodAvailable(dataReaderFieldType))
            {
                throw new ArgumentException(
                    $"The data type {dataReaderFieldType} of the column '{dataReaderFieldName}' returned by the " +
                    "SQL statement is not supported.",
                    nameof(dataReader)
                );
            }
        }

        var compatibleConstructor = EntityHelper.FindCompatibleConstructor(
            entityType,
            dataReaderFieldNames.Zip(dataReaderFieldTypes, (name, type) => (name, type)).ToArray()
        );

        if (compatibleConstructor is not null)
        {
            // If we found a compatible constructor, we're done.
            return;
        }

        // To materialize entities of the entity type using properties, we need a parameterless constructor:
        var parameterlessConstructor = EntityHelper.FindParameterlessConstructor(entityType);

        if (parameterlessConstructor is null)
        {
            var exampleConstructorSignature =
                "(" +
                String.Join(
                    ", ",
                    dataReaderFieldNames.Zip(dataReaderFieldTypes, (name, type) => $"{type.Name} {name}")
                ) +
                ")";

            throw new ArgumentException(
                $"Could not materialize an instance of the type {entityType}. The type either needs to have a " +
                "parameterless constructor or a constructor whose parameters match the columns returned by the SQL " +
                $"statement, e.g. a constructor that has the following signature:{Environment.NewLine}" +
                $"{exampleConstructorSignature}.",
                nameof(entityType)
            );
        }

        var entityPropertiesByName = EntityHelper.GetEntityTypeMetadata(entityType)
            .MappedProperties.Where(a => a.CanWrite)
            .ToDictionary(a => a.PropertyName, StringComparer.OrdinalIgnoreCase);

        for (var fieldOrdinal = 0; fieldOrdinal < dataReader.FieldCount; fieldOrdinal++)
        {
            var dataReaderFieldName = dataReaderFieldNames[fieldOrdinal];

            if (!entityPropertiesByName.TryGetValue(dataReaderFieldName, out var entityProperty))
            {
                continue;
            }

            var entityPropertyType = entityProperty.PropertyType;
            var dataReaderFieldType = dataReaderFieldTypes[fieldOrdinal];

            if (!ValueConverter.CanConvert(dataReaderFieldType, entityPropertyType))
            {
                throw new ArgumentException(
                    $"The data type {dataReaderFieldType} of the column '{dataReaderFieldName}' returned by the " +
                    $"SQL statement is not compatible with the property type {entityPropertyType} of the " +
                    $"corresponding property of the type {entityType}.",
                    nameof(dataReader)
                );
            }
        }
    }

    private static readonly ConcurrentDictionary<MaterializerCacheKey, Delegate> materializerCache = [];

    /// <summary>
    /// A cache key used to uniquely identify an entity materializer.
    /// </summary>
    /// <param name="entityType">The type of entity the materializer materializes.</param>
    /// <param name="dataReaderFieldNames">
    /// The field names of the <see cref="DbDataReader" /> from which to materialize.
    /// The order of the names must match the order of the fields in the data reader.
    /// </param>
    /// <param name="dataReaderFieldTypes">
    /// The field types of the <see cref="DbDataReader" /> from which to materialize.
    /// The order of the types must match the order of the fields in the data reader.
    /// </param>
    private readonly struct MaterializerCacheKey(
        Type entityType,
        String[] dataReaderFieldNames,
        Type[] dataReaderFieldTypes
    )
        : IEquatable<MaterializerCacheKey>
    {
        /// <summary>
        /// The type of entity the materializer materializes.
        /// </summary>
        public Type EntityType { get; } = entityType;

        /// <inheritdoc />
        public Boolean Equals(MaterializerCacheKey other) =>
            this.EntityType == other.EntityType &&
            this.DataReaderFieldNames.SequenceEqual(other.DataReaderFieldNames) &&
            this.DataReaderFieldTypes.SequenceEqual(other.DataReaderFieldTypes);

        /// <inheritdoc />
        public override Boolean Equals(Object? obj) =>
            obj is MaterializerCacheKey other && this.Equals(other);

        /// <inheritdoc />
        public override Int32 GetHashCode()
        {
            var hashCode = new HashCode();

            hashCode.Add(this.EntityType);

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
    }
}
