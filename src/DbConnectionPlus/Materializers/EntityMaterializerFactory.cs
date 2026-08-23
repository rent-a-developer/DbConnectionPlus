// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using System.Linq.Expressions;
using System.Reflection;
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
    /// The message reported when the expression-tree entity materializer is reached from code that is compiled
    /// ahead of time.
    /// </summary>
    /// <remarks>
    /// No consumer sees this. It sits on <see cref="CreateExpressionMaterializer{TEntity}" />, which only
    /// <see cref="CreateMaterializer{TEntity}" /> calls and only from inside an
    /// <see cref="RuntimeFeature.IsDynamicCodeSupported" /> branch; the attribute exists so that the analyzer
    /// verifies that guard rather than so that a warning propagates.
    /// </remarks>
    private const string MaterializerRequiresDynamicCodeMessage =
        "Materializing entities compiles an expression tree at run time, which is not supported when the " +
        "application is published with Native AOT. Reach this only from a RuntimeFeature.IsDynamicCodeSupported " +
        "branch.";

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
    // No [RequiresUnreferencedCode] and no [RequiresDynamicCode] here, so nothing propagates to the generic query
    // methods that reach this factory. The expression-tree materializer, which is the part that needs dynamic code,
    // is reached only from inside the RuntimeFeature.IsDynamicCodeSupported branch in CreateMaterializer; the
    // diagnostics the analyzers do report are answered at the sites that cause them. The full argument is in the
    // "No consumer-facing diagnostics" section of DESIGN-DECISIONS.md.
    internal static Func<DbDataReader, TEntity> GetMaterializer<
        [DynamicallyAccessedMembers(EntityHelper.EntityMemberTypes)] TEntity
    >(DbDataReader dataReader)
    {
        ArgumentNullException.ThrowIfNull(dataReader);

        var entityType = typeof(TEntity);

        var dataReaderFieldNames = dataReader.GetFieldNames();
        var dataReaderFieldTypes = dataReader.GetFieldTypes();

        ValidateDataReader(entityType, dataReader, dataReaderFieldNames, dataReaderFieldTypes);

        // We can only re-use a cached materializer function if the entity type, the data reader field names and
        // the data reader field types are the same:
        var cacheKey = new MaterializerCacheKey(entityType, dataReaderFieldNames, dataReaderFieldTypes);

        if (materializerCache.TryGetValue(cacheKey, out var cachedMaterializer))
        {
            return (Func<DbDataReader, TEntity>)cachedMaterializer;
        }

        // The materializer is created here rather than inside a GetOrAdd factory lambda:
        // [DynamicallyAccessedMembers] does not flow into a lambda, so the annotation on TEntity would be lost on
        // the way to CreateMaterializer and the trimmer would drop the entity's constructors and properties.
        var materializer = CreateMaterializer<TEntity>(dataReader, dataReaderFieldNames, dataReaderFieldTypes);

        return (Func<DbDataReader, TEntity>)materializerCache.GetOrAdd(cacheKey, materializer);
    }

    /// <summary>
    /// Creates a materializer function that materializes the data in a <see cref="DbDataReader" /> to an instance of
    /// the type <typeparamref name="TEntity" /> using reflection instead of a compiled expression tree.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity to materialize.</typeparam>
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
    /// <typeparamref name="TEntity" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="dataReader" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="dataReaderFieldNames" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="dataReaderFieldTypes" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <remarks>
    /// <para>
    /// This is the materializer for applications published with Native AOT, where no run-time code generation is
    /// available. It mirrors the two strategies of <see cref="CreateExpressionMaterializer{TEntity}" /> and picks
    /// between them the same way: constructor injection when the type has a constructor that matches the result
    /// set, and a parameterless constructor followed by property setters otherwise.
    /// </para>
    /// <para>
    /// Everything that depends only on the shape of the result set - the field ordinals, the target types, whether a
    /// field value needs to be converted, and the property setters - is resolved once, exactly as the compiled
    /// expression tree bakes it in. Per row the materializer only walks an array, reads the fields and writes them.
    /// The materializer cache is keyed by that shape, so the resolution happens once per shape.
    /// </para>
    /// <para>
    /// The exception types and the exception messages are identical to the ones of the compiled expression tree, so
    /// that the behaviour a consumer observes does not depend on how the application was published.
    /// </para>
    /// </remarks>
    internal static Func<DbDataReader, TEntity> CreateReflectionMaterializer<
        [DynamicallyAccessedMembers(EntityHelper.EntityMemberTypes)] TEntity
    >(
        DbDataReader dataReader,
        string[] dataReaderFieldNames,
        Type[] dataReaderFieldTypes
    )
    {
        ArgumentNullException.ThrowIfNull(dataReader);
        ArgumentNullException.ThrowIfNull(dataReaderFieldNames);
        ArgumentNullException.ThrowIfNull(dataReaderFieldTypes);

        var compatibleConstructor = EntityHelper.FindCompatibleConstructor(
            typeof(TEntity),
            [
                .. dataReaderFieldNames.Zip(dataReaderFieldTypes, (name, type) => (name, type))
            ]
        );

        return compatibleConstructor is null
            ? CreateReflectionPropertyMaterializer<TEntity>(
                dataReader,
                dataReaderFieldNames,
                dataReaderFieldTypes
            )
            : CreateReflectionConstructorMaterializer<TEntity>(
                dataReader,
                dataReaderFieldNames,
                dataReaderFieldTypes,
                compatibleConstructor
            );
    }

    /// <summary>
    /// Creates a reflection-based materializer function that materializes the data in a <see cref="DbDataReader" />
    /// to an instance of the type <typeparamref name="TEntity" /> by passing the fields of the result set to
    /// <paramref name="compatibleConstructor" />.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity to materialize.</typeparam>
    /// <param name="dataReader">The <see cref="DbDataReader" /> for which to create the materializer function.</param>
    /// <param name="dataReaderFieldNames">
    /// The names of the fields in <paramref name="dataReader" />.
    /// The order of the names must match the order of the fields in <paramref name="dataReader" />.
    /// </param>
    /// <param name="dataReaderFieldTypes">
    /// The field types of the fields in <paramref name="dataReader" />.
    /// The order of the types must match the order of the fields in <paramref name="dataReader" />.
    /// </param>
    /// <param name="compatibleConstructor">
    /// The constructor of the type <typeparamref name="TEntity" /> whose parameters match the fields of the result
    /// set, as returned by <see cref="EntityHelper.FindCompatibleConstructor" />.
    /// </param>
    /// <returns>
    /// A function that materializes the data in a <see cref="DbDataReader" /> to an instance of the type
    /// <typeparamref name="TEntity" />.
    /// </returns>
    /// <remarks>
    /// This is the strategy that materializes entities using constructor injection. Each field of the result set is
    /// matched to the constructor parameter of the same name, exactly as the compiled expression tree matches it,
    /// and the resulting bindings are stored in constructor-argument order. Per row the arguments are therefore
    /// read left to right, which is also the order in which the expression tree evaluates them - so when more than
    /// one field is unusable, both paths report the same one.
    /// </remarks>
    private static Func<DbDataReader, TEntity> CreateReflectionConstructorMaterializer<TEntity>(
        DbDataReader dataReader,
        string[] dataReaderFieldNames,
        Type[] dataReaderFieldTypes,
        ConstructorInfo compatibleConstructor
    )
    {
        var entityType = typeof(TEntity);

        var constructorParameters = compatibleConstructor.GetParameters();
        var constructorArgumentBindings = new ReflectionColumnBinding[constructorParameters.Length];

        for (var fieldOrdinal = 0; fieldOrdinal < dataReader.FieldCount; fieldOrdinal++)
        {
            var dataReaderFieldName = dataReaderFieldNames[fieldOrdinal];
            var dataReaderFieldType = dataReaderFieldTypes[fieldOrdinal];

            var constructorParameter = constructorParameters.First(p =>
                !string.IsNullOrWhiteSpace(p.Name) &&
                p.Name.Equals(dataReaderFieldName, StringComparison.OrdinalIgnoreCase) &&
                ValueConverter.CanConvert(dataReaderFieldType, p.ParameterType)
            );

            constructorArgumentBindings[Array.IndexOf(constructorParameters, constructorParameter)] =
                new ReflectionColumnBinding(
                    dataReaderFieldName,
                    fieldOrdinal,
                    MaterializerFactoryHelper.CreateGetDbDataReaderFieldValueFunction(
                        fieldOrdinal,
                        dataReaderFieldName,
                        dataReaderFieldType
                    ),
                    dataReaderFieldType != constructorParameter.ParameterType,
                    constructorParameter.ParameterType
                );
        }

        var entityConstructor = ConstructorInvoker.Create(compatibleConstructor);

        return rowDataReader => MaterializeEntityThroughConstructor<TEntity>(
            rowDataReader,
            entityType,
            entityConstructor,
            constructorArgumentBindings
        );
    }

    /// <summary>
    /// Creates a reflection-based materializer function that materializes the data in a <see cref="DbDataReader" />
    /// to an instance of the type <typeparamref name="TEntity" /> by constructing it with its parameterless
    /// constructor and then writing each field of the result set to the property it maps to.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity to materialize.</typeparam>
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
    /// <typeparamref name="TEntity" />.
    /// </returns>
    /// <remarks>
    /// Callers must have established that the type <typeparamref name="TEntity" /> has a parameterless constructor
    /// and no constructor compatible with the result set - <see cref="ValidateDataReader" /> does both. Fields
    /// without a matching property are never read, which the compiled expression tree does as well.
    /// </remarks>
    private static Func<DbDataReader, TEntity> CreateReflectionPropertyMaterializer<
        [DynamicallyAccessedMembers(EntityHelper.EntityMemberTypes)] TEntity
    >(
        DbDataReader dataReader,
        string[] dataReaderFieldNames,
        Type[] dataReaderFieldTypes
    )
    {
        var entityType = typeof(TEntity);

        var entityPropertiesByColumnName = EntityHelper.GetEntityTypeMetadata(entityType)
            .MappedProperties.Where(a => a.CanWrite)
            .ToDictionary(a => a.ColumnName, StringComparer.OrdinalIgnoreCase);

        var entityConstructor = ConstructorInvoker.Create(EntityHelper.FindParameterlessConstructor(entityType)!);

        var propertyBindings = new List<ReflectionPropertyBinding>(dataReader.FieldCount);

        for (var fieldOrdinal = 0; fieldOrdinal < dataReader.FieldCount; fieldOrdinal++)
        {
            var dataReaderFieldName = dataReaderFieldNames[fieldOrdinal];

            if (!entityPropertiesByColumnName.TryGetValue(dataReaderFieldName, out var entityProperty))
            {
                // No need to read the field when there is no matching property for it. The expression tree skips
                // these fields as well, and tests assert that the field is never touched.
                continue;
            }

            var dataReaderFieldType = dataReaderFieldTypes[fieldOrdinal];
            var targetType = entityProperty.PropertyType;

            propertyBindings.Add(
                new ReflectionPropertyBinding(
                    new ReflectionColumnBinding(
                        dataReaderFieldName,
                        fieldOrdinal,
                        MaterializerFactoryHelper.CreateGetDbDataReaderFieldValueFunction(
                            fieldOrdinal,
                            dataReaderFieldName,
                            dataReaderFieldType
                        ),
                        dataReaderFieldType != targetType,
                        targetType
                    ),
                    // entityPropertiesByColumnName only contains writable properties, so the setter always exists.
                    entityProperty.PropertySetter!
                )
            );
        }

        var resolvedPropertyBindings = propertyBindings.ToArray();

        return rowDataReader => MaterializeEntityThroughProperties<TEntity>(
            rowDataReader,
            entityType,
            entityConstructor,
            resolvedPropertyBindings
        );
    }

    /// <summary>
    /// Throws if none of the fields of the result set can be mapped to a writable property of
    /// <paramref name="entityType" />.
    /// </summary>
    /// <param name="entityType">The entity type the result set is being materialized to.</param>
    /// <param name="dataReaderFieldNames">The field names of the result set.</param>
    /// <param name="entityPropertiesByColumnName">The writable properties of the entity type, by column name.</param>
    /// <exception cref="InvalidOperationException">
    /// The result set has fields, but none of them maps to a writable property of <paramref name="entityType" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Without this check, materialization silently succeeds and returns entities whose properties are all left at
    /// their default values, which is indistinguishable from a query that legitimately returned default data.
    /// </para>
    /// <para>
    /// The check matters most in an application that is trimmed or published with Native AOT. If a
    /// <see cref="DynamicallyAccessedMembersAttribute" /> annotation is missing anywhere on the call path, the
    /// trimmer removes the entity's properties, reflection then reports fewer members than the type really has, and
    /// every column silently fails to bind. Failing loudly here is the backstop for that.
    /// </para>
    /// </remarks>
    private static void GuardAgainstResultSetBindingNoProperties(
        Type entityType,
        string[] dataReaderFieldNames,
        Dictionary<string, EntityPropertyMetadata> entityPropertiesByColumnName
    )
    {
        if (dataReaderFieldNames.Length == 0)
        {
            return;
        }

        if (dataReaderFieldNames.Any(entityPropertiesByColumnName.ContainsKey))
        {
            return;
        }

        throw new InvalidOperationException(
            $"None of the {dataReaderFieldNames.Length} field(s) of the result set " +
            $"({string.Join(", ", dataReaderFieldNames)}) could be mapped to a writable property of the entity " +
            $"type {entityType}. Materializing the result set would return entities whose properties are all left " +
            "at their default values. Check that the field names of the result set match the property names, or " +
            "the mapped column names, of the entity type. If the application is trimmed or published with Native " +
            "AOT, this usually means the properties of the entity type were removed by the trimmer because a " +
            "[DynamicallyAccessedMembers] annotation is missing on the call path."
        );
    }

    /// <summary>
    /// Creates a materializer function that materializes the data in a <see cref="DbDataReader" /> to an instance of
    /// the type <typeparamref name="TEntity" />.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity to materialize.</typeparam>
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
    /// <typeparamref name="TEntity" />.
    /// </returns>
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
    /// <para>
    /// If a constructor parameter or a property cannot be set to the value of the corresponding field of
    /// <paramref name="dataReader" /> (for example, due to a type mismatch), the returned function throws an
    /// <see cref="InvalidCastException" />.
    /// </para>
    /// <para>
    /// Which of the two implementations builds the materializer is decided here, by
    /// <see cref="RuntimeFeature.IsDynamicCodeSupported" />: the compiled expression tree when the runtime can
    /// generate code, and the reflection-based materializer when it cannot. The AOT compiler folds that check to a
    /// constant and removes the branch it does not need, so an application published with Native AOT does not
    /// carry the expression-tree implementation at all.
    /// </para>
    /// </remarks>
#if !NET9_0_OR_GREATER
    // The dispatch below is what keeps the generic query methods free of [RequiresDynamicCode], and therefore what
    // keeps a consumer's Native AOT publish free of IL3050. .NET 9 annotated
    // RuntimeFeature.IsDynamicCodeSupported as a [FeatureGuard], so from net9.0 onwards the analyzer recognizes the
    // branch as unreachable under Native AOT and reports nothing here. net8.0's reference assembly does not carry
    // that annotation, so the same, correct code warns there and only there.
    //
    // This suppression is therefore not an assertion - it is a transcription. The net10.0 inner build compiles this
    // file WITHOUT it, which is what proves the reasoning; net8.0 borrows the result. Both target frameworks stay in
    // scripts/verify-package-aot.ps1 so that remains true rather than becoming folklore.
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050:Requires dynamic code",
        Justification =
            "The call is inside an if (RuntimeFeature.IsDynamicCodeSupported) branch, which the AOT compiler folds " +
            "to false and removes together with the expression-tree implementation. The net9.0+ analyzer " +
            "recognizes that guard and reports nothing here; net8.0 lacks the [FeatureGuard] annotation on " +
            "IsDynamicCodeSupported that lets it do so."
    )]
#endif
    private static Delegate CreateMaterializer<
        [DynamicallyAccessedMembers(EntityHelper.EntityMemberTypes)] TEntity
    >(
        DbDataReader dataReader,
        string[] dataReaderFieldNames,
        Type[] dataReaderFieldTypes
    )
    {
        var entityType = typeof(TEntity);

        var compatibleConstructor = EntityHelper.FindCompatibleConstructor(
            entityType,
            [.. dataReaderFieldNames.Zip(dataReaderFieldTypes, (name, type) => (name, type))]
        );

        if (compatibleConstructor is null)
        {
            // Constructor injection already fails loudly when nothing matches, so the guard only has to cover the
            // property-setter strategy. It runs before the dispatch below and is therefore shared by both
            // implementations.
            GuardAgainstResultSetBindingNoProperties(
                entityType,
                dataReaderFieldNames,
                EntityHelper.GetEntityTypeMetadata(entityType)
                    .MappedProperties.Where(a => a.CanWrite)
                    .ToDictionary(a => a.ColumnName, StringComparer.OrdinalIgnoreCase)
            );
        }

        if (RuntimeFeature.IsDynamicCodeSupported)
        {
            return CreateExpressionMaterializer<TEntity>(dataReader, dataReaderFieldNames, dataReaderFieldTypes);
        }

        return CreateReflectionMaterializer<TEntity>(dataReader, dataReaderFieldNames, dataReaderFieldTypes);
    }

    /// <summary>
    /// Creates a materializer function that materializes the data in a <see cref="DbDataReader" /> to an instance of
    /// the type <typeparamref name="TEntity" /> by compiling an expression tree.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity to materialize.</typeparam>
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
    /// <typeparamref name="TEntity" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <remarks>
    /// This is the fast path for runtimes that can generate code, and the only implementation the library had
    /// before it grew a Native AOT counterpart. It is reached exclusively through the
    /// <see cref="RuntimeFeature.IsDynamicCodeSupported" /> check in <see cref="CreateMaterializer{TEntity}" />;
    /// <see cref="CreateReflectionMaterializer{TEntity}" /> is the counterpart that produces the same results
    /// without generating code.
    /// </remarks>
    [RequiresDynamicCode(MaterializerRequiresDynamicCodeMessage)]
    private static Delegate CreateExpressionMaterializer<
        [DynamicallyAccessedMembers(EntityHelper.EntityMemberTypes)] TEntity
    >(
        DbDataReader dataReader,
        string[] dataReaderFieldNames,
        Type[] dataReaderFieldTypes
    )
    {
        var entityType = typeof(TEntity);

        /*
         * This method creates an expression tree to generate a materializer function instead of using reflection for
         * the materialization, because using reflection would be significantly slower.
         * Using expression trees also allows us to use the typed GetXXX methods of DbDataReader, which avoids boxing
         * in many cases.
         */

        var dataReaderParameterExpression = Expression.Parameter(typeof(DbDataReader), "dataReader");
        var dataReaderFieldValueExpressions = new Expression[dataReader.FieldCount];

        var fieldOrdinalToTargetType = new Dictionary<int, Type>(dataReader.FieldCount);
        var fieldOrdinalToConstructorParameterIndex = new Dictionary<int, int>(dataReader.FieldCount);

        var compatibleConstructor = EntityHelper.FindCompatibleConstructor(
            entityType,
            [.. dataReaderFieldNames.Zip(dataReaderFieldTypes, (name, type) => (name, type))]
        );

        var entityPropertiesByColumnName = EntityHelper.GetEntityTypeMetadata(entityType)
            .MappedProperties.Where(a => a.CanWrite)
            .ToDictionary(a => a.ColumnName, StringComparer.OrdinalIgnoreCase);

        if (compatibleConstructor is not null)
        {
            var constructorParameters = compatibleConstructor.GetParameters().ToList();

            for (var fieldOrdinal = 0; fieldOrdinal < dataReader.FieldCount; fieldOrdinal++)
            {
                var constructorParameter = constructorParameters.First(p =>
                    !string.IsNullOrWhiteSpace(p.Name) &&
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

                if (entityPropertiesByColumnName.TryGetValue(dataReaderFieldName, out var entityProperty))
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

            if (compatibleConstructor is null && !entityPropertiesByColumnName.ContainsKey(dataReaderFieldName))
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
                        typeof(InvalidCastException).GetConstructor([typeof(string)])!,
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
                        [typeof(string), typeof(Exception)]
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
                        MaterializerFactoryHelper.MakeValueConverterConvertValueToTypeMethod(targetType),
                        Expression.Convert(getFieldValueCallExpression, typeof(object))
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

                if (!entityPropertiesByColumnName.TryGetValue(dataReaderFieldName, out var entityProperty))
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
    /// Materializes the current row of <paramref name="dataReader" /> to an instance of the type
    /// <typeparamref name="TEntity" /> by passing the fields of the result set to its constructor.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity to materialize.</typeparam>
    /// <param name="dataReader">The <see cref="DbDataReader" /> to materialize the current row of.</param>
    /// <param name="entityType">The type of entity to materialize. Used in the exception messages.</param>
    /// <param name="entityConstructor">
    /// The constructor of the type <paramref name="entityType" /> whose parameters match the fields of the result
    /// set.
    /// </param>
    /// <param name="constructorArgumentBindings">
    /// The fields of the result set that feed the constructor arguments, in constructor-argument order.
    /// </param>
    /// <returns>The materialized instance of the type <typeparamref name="TEntity" />.</returns>
    /// <exception cref="InvalidCastException">
    /// A field of the result set could not be passed to the corresponding constructor parameter.
    /// </exception>
    private static TEntity MaterializeEntityThroughConstructor<TEntity>(
        DbDataReader dataReader,
        Type entityType,
        ConstructorInvoker entityConstructor,
        ReflectionColumnBinding[] constructorArgumentBindings
    )
    {
        var constructorArguments = new object?[constructorArgumentBindings.Length];

        for (var argumentIndex = 0; argumentIndex < constructorArgumentBindings.Length; argumentIndex++)
        {
            constructorArguments[argumentIndex] =
                ReadFieldValue(dataReader, entityType, constructorArgumentBindings[argumentIndex]);
        }

        return (TEntity)entityConstructor.Invoke(constructorArguments.AsSpan());
    }

    /// <summary>
    /// Materializes the current row of <paramref name="dataReader" /> to an instance of the type
    /// <typeparamref name="TEntity" /> by constructing it and then writing each field to the property it maps to.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity to materialize.</typeparam>
    /// <param name="dataReader">The <see cref="DbDataReader" /> to materialize the current row of.</param>
    /// <param name="entityType">The type of entity to materialize. Used in the exception messages.</param>
    /// <param name="entityConstructor">The parameterless constructor of the type <paramref name="entityType" />.</param>
    /// <param name="propertyBindings">The fields of the result set that are written to a property of the entity.</param>
    /// <returns>The materialized instance of the type <typeparamref name="TEntity" />.</returns>
    /// <exception cref="InvalidCastException">
    /// A field of the result set could not be written to the corresponding property of the entity.
    /// </exception>
    private static TEntity MaterializeEntityThroughProperties<TEntity>(
        DbDataReader dataReader,
        Type entityType,
        ConstructorInvoker entityConstructor,
        ReflectionPropertyBinding[] propertyBindings
    )
    {
        var entity = entityConstructor.Invoke();

        foreach (var propertyBinding in propertyBindings)
        {
            propertyBinding.PropertySetter(entity, ReadFieldValue(dataReader, entityType, propertyBinding.Column));
        }

        return (TEntity)entity;
    }

    /// <summary>
    /// Reads the value of the field described by <paramref name="columnBinding" /> from the current row of
    /// <paramref name="dataReader" />, converting it to the type of the property it is written to.
    /// </summary>
    /// <param name="dataReader">The <see cref="DbDataReader" /> to read the field value from.</param>
    /// <param name="entityType">The type of entity being materialized. Used in the exception messages.</param>
    /// <param name="columnBinding">The column of the result set to read.</param>
    /// <returns>The field value, ready to be written to the property.</returns>
    /// <exception cref="InvalidCastException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 The field value is <see langword="null" /> and the property is non-nullable.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 The field value could not be converted to the type of the property.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    private static object? ReadFieldValue(
        DbDataReader dataReader,
        Type entityType,
        ReflectionColumnBinding columnBinding
    )
    {
        if (dataReader.IsDBNull(columnBinding.FieldOrdinal))
        {
            if (columnBinding.TargetType.IsReferenceTypeOrNullableType())
            {
                return null;
            }

            throw new InvalidCastException(
                $"The column '{columnBinding.FieldName}' returned by the SQL statement contains a NULL value, but " +
                $"the corresponding property of the type {entityType} is non-nullable."
            );
        }

        var dataReaderFieldValue = columnBinding.GetFieldValue(dataReader);

        if (!columnBinding.NeedsConversion)
        {
            return dataReaderFieldValue;
        }

        try
        {
            return ValueConverter.ConvertValueToType(dataReaderFieldValue, columnBinding.TargetType);
        }
        catch (Exception exception)
        {
            throw new InvalidCastException(
                $"The column '{columnBinding.FieldName}' returned by the SQL statement contains a value that could " +
                $"not be converted to the type {columnBinding.TargetType} of the corresponding property of the " +
                $"type {entityType}. See inner exception for details.",
                exception
            );
        }
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
        [DynamicallyAccessedMembers(EntityHelper.EntityMemberTypes)] Type entityType,
        DbDataReader dataReader,
        string[] dataReaderFieldNames,
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

            if (string.IsNullOrWhiteSpace(dataReaderFieldName))
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
            [.. dataReaderFieldNames.Zip(dataReaderFieldTypes, (name, type) => (name, type))]
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
                string.Join(
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

        var entityPropertiesByColumnName = EntityHelper.GetEntityTypeMetadata(entityType)
            .MappedProperties.Where(a => a.CanWrite)
            .ToDictionary(a => a.ColumnName, StringComparer.OrdinalIgnoreCase);

        for (var fieldOrdinal = 0; fieldOrdinal < dataReader.FieldCount; fieldOrdinal++)
        {
            var dataReaderFieldName = dataReaderFieldNames[fieldOrdinal];

            if (!entityPropertiesByColumnName.TryGetValue(dataReaderFieldName, out var entityProperty))
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
        string[] dataReaderFieldNames,
        Type[] dataReaderFieldTypes
    )
        : IEquatable<MaterializerCacheKey>
    {
        /// <summary>
        /// The type of entity the materializer materializes.
        /// </summary>
        public Type EntityType { get; } = entityType;

        /// <inheritdoc />
        public bool Equals(MaterializerCacheKey other) =>
            this.EntityType == other.EntityType &&
            this.DataReaderFieldNames.SequenceEqual(other.DataReaderFieldNames) &&
            this.DataReaderFieldTypes.SequenceEqual(other.DataReaderFieldTypes);

        /// <inheritdoc />
        public override bool Equals(object? obj) =>
            obj is MaterializerCacheKey other && this.Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
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

        private string[] DataReaderFieldNames { get; } = dataReaderFieldNames;
        private Type[] DataReaderFieldTypes { get; } = dataReaderFieldTypes;
    }

    /// <summary>
    /// Everything the reflection materializer needs to read one field of a result set and turn it into a value the
    /// entity accepts, resolved once per result-set shape.
    /// </summary>
    /// <param name="FieldName">The name of the field of the result set.</param>
    /// <param name="FieldOrdinal">The ordinal of the field in the result set.</param>
    /// <param name="GetFieldValue">
    /// Gets the value of the field from a <see cref="DbDataReader" />, using the same
    /// <see cref="DbDataReader" />.GetXXX method the compiled expression tree would call.
    /// </param>
    /// <param name="NeedsConversion">
    /// Determines whether the field value has to be converted to <paramref name="TargetType" /> before the entity
    /// accepts it. This is the case when the field type differs from the target type.
    /// </param>
    /// <param name="TargetType">
    /// The type the field value is converted to - the type of the property it is written to, or the type of the
    /// constructor parameter it is passed to.
    /// </param>
    private readonly record struct ReflectionColumnBinding(
        string FieldName,
        int FieldOrdinal,
        Func<DbDataReader, object?> GetFieldValue,
        bool NeedsConversion,
        Type TargetType
    );

    /// <summary>
    /// A field of the result set together with the property setter it is written to, resolved once per result-set
    /// shape. Used by the property-setter strategy of the reflection materializer.
    /// </summary>
    /// <param name="Column">The field of the result set to read.</param>
    /// <param name="PropertySetter">
    /// The setter function of the property, taking the entity and the value to assign to the property.
    /// </param>
    private readonly record struct ReflectionPropertyBinding(
        ReflectionColumnBinding Column,
        Action<object, object?> PropertySetter
    );
}
