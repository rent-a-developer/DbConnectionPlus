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
    /// The message reported when the expression-tree value tuple materializer is reached from code that is compiled
    /// ahead of time.
    /// </summary>
    /// <remarks>
    /// No consumer sees this. It sits on <see cref="CreateExpressionMaterializer{TValueTuple}" />, which only
    /// <see cref="CreateMaterializer{TValueTuple}" /> calls and only from inside an
    /// <see cref="RuntimeFeature.IsDynamicCodeSupported" /> branch; the attribute exists so that the analyzer
    /// verifies that guard rather than so that a warning propagates.
    /// </remarks>
    internal const string MaterializerRequiresDynamicCodeMessage =
        "Materializing value tuples compiles an expression tree at run time, which is not supported when the "
        + "application is published with Native AOT. Reach this only from a RuntimeFeature.IsDynamicCodeSupported "
        + "branch.";

    /// <summary>
    /// The members of a value tuple type that this library reflects over, and which therefore must survive trimming.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Public constructors cover the <see cref="Type.GetConstructor(BindingFlags, Type[])" /> lookups used to build
    /// the (possibly nested) tuple. The types the tuple is built from are read with
    /// <see cref="Type.GetGenericArguments" />, which needs no annotation at all - generic arguments are type
    /// metadata rather than members, so the trimmer cannot remove them.
    /// </para>
    /// <para>
    /// Public fields are not reflected over by this file, and are kept in the annotation deliberately.
    /// Widening what survives trimming is the safe direction, and narrowing it would change the annotation on the
    /// public query methods - a public API change - to buy nothing observable.
    /// </para>
    /// <para>
    /// Neither flag reaches the <em>nested</em> value tuple type of a tuple with more than seven fields:
    /// annotations are not recursive, and the nested type is only reached at run time. That is what
    /// <c>ILLink.Descriptors.xml</c> is for; without it, the nested type's constructor is trimmed away and
    /// materialization fails in an application published with Native AOT.
    /// </para>
    /// </remarks>
    internal const DynamicallyAccessedMemberTypes ValueTupleMemberTypes =
        DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicConstructors;

    /// <summary>
    /// The number of fields a value tuple holds before the runtime represents the remaining ones as a nested value
    /// tuple in its <c>Rest</c> field.
    /// </summary>
    private const int ValueTupleFieldCountBeforeNesting = 7;

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
    /// <para>
    /// The order of the fields in the value tuple must match the order of the fields in <paramref name="dataReader" />.
    /// </para>
    /// <para>
    /// The field types of the fields in <paramref name="dataReader" /> must be compatible with the field types of the
    /// fields in <paramref name="dataReader" />.
    /// The compatibility is determined using <see cref="ValueConverter.CanConvert(Type, Type)" />.
    /// </para>
    /// </remarks>
    // No [RequiresUnreferencedCode] and no [RequiresDynamicCode] here, so nothing propagates to the generic query
    // methods that reach this factory. Discovering the fields and the constructors of a value tuple with more than
    // seven fields walks Type.GetGenericArguments() - type metadata that cannot be trimmed away - and the members of
    // System.ValueTuple`1-`8 are kept by the ILLink.Descriptors.xml embedded in this assembly. The full argument is
    // in the "No consumer-facing diagnostics" section of DESIGN-DECISIONS.md.
    internal static Func<DbDataReader, TValueTuple> GetMaterializer<
        [DynamicallyAccessedMembers(ValueTupleMemberTypes)] TValueTuple
    >(DbDataReader dataReader)
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

        if (materializerCache.TryGetValue(cacheKey, out var cachedMaterializer))
        {
            return (Func<DbDataReader, TValueTuple>)cachedMaterializer;
        }

        // The materializer is created here rather than inside a GetOrAdd factory lambda:
        // [DynamicallyAccessedMembers] does not flow into a lambda, so the annotation on TValueTuple would be lost
        // on the way to CreateMaterializer and the trimmer would drop the value tuple's fields and constructors.
        var materializer = CreateMaterializer<TValueTuple>(
            valueTupleFieldTypes,
            dataReader,
            dataReaderFieldNames,
            dataReaderFieldTypes
        );

        return (Func<DbDataReader, TValueTuple>)materializerCache.GetOrAdd(cacheKey, materializer);
    }

    /// <summary>
    /// Creates a materializer function that materializes the data in a <see cref="DbDataReader" /> to an instance of
    /// the value tuple type <typeparamref name="TValueTuple" /> using reflection instead of a compiled expression
    /// tree.
    /// </summary>
    /// <typeparam name="TValueTuple">The type of value tuple to materialize.</typeparam>
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
    /// <typeparamref name="TValueTuple" />.
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
    /// available. Value tuples are materialized ordinal-positionally, exactly as
    /// <see cref="CreateExpressionMaterializer{TValueTuple}" /> materializes them, including value tuples with more
    /// than seven fields, which the runtime represents as nested value tuples.
    /// </para>
    /// <para>
    /// Everything that depends only on the shape of the result set - the field ordinals, the target types, whether a
    /// field value needs to be converted, and the constructors of the value tuple types - is resolved once, exactly
    /// as the compiled expression tree bakes it in. Per row the materializer only walks an array, reads the fields
    /// and passes them to the constructors. The materializer cache is keyed by that shape, so the resolution happens
    /// once per shape.
    /// </para>
    /// <para>
    /// The exception types and the exception messages are identical to the ones of the compiled expression tree, so
    /// that the behaviour a consumer observes does not depend on how the application was published.
    /// </para>
    /// </remarks>
    internal static Func<DbDataReader, TValueTuple> CreateReflectionMaterializer<
        [DynamicallyAccessedMembers(ValueTupleMemberTypes)] TValueTuple
    >(DbDataReader dataReader, string[] dataReaderFieldNames, Type[] dataReaderFieldTypes)
    {
        ArgumentNullException.ThrowIfNull(dataReader);
        ArgumentNullException.ThrowIfNull(dataReaderFieldNames);
        ArgumentNullException.ThrowIfNull(dataReaderFieldTypes);

        var valueTupleType = typeof(TValueTuple);

        // Resolved here rather than taken from the caller, so that this method can be reached directly from a test:
        // on the JIT the dispatch in CreateMaterializer always picks the expression tree.
        var valueTupleFieldTypes = GetValueTupleFieldTypes(valueTupleType);

        var columnBindings = new ReflectionColumnBinding[dataReader.FieldCount];

        for (var fieldOrdinal = 0; fieldOrdinal < dataReader.FieldCount; fieldOrdinal++)
        {
            var dataReaderFieldName = dataReaderFieldNames[fieldOrdinal];
            var dataReaderFieldType = dataReaderFieldTypes[fieldOrdinal];
            var targetType = valueTupleFieldTypes[fieldOrdinal];

            columnBindings[fieldOrdinal] = new ReflectionColumnBinding(
                GetColumnNameOrPosition(fieldOrdinal, dataReaderFieldName),
                fieldOrdinal,
                MaterializerFactoryHelper.CreateGetDbDataReaderFieldValueFunction(
                    fieldOrdinal,
                    dataReaderFieldName,
                    dataReaderFieldType
                ),
                dataReaderFieldType != targetType,
                targetType
            );
        }

        var valueTupleConstructors = GetValueTupleConstructors(valueTupleType)
            .Select(ConstructorInvoker.Create)
            .ToArray();

        return rowDataReader =>
            MaterializeValueTuple<TValueTuple>(rowDataReader, valueTupleType, valueTupleConstructors, columnBindings);
    }

    /// <summary>
    /// Creates a materializer function that materializes the data in a <see cref="DbDataReader" /> to an instance of
    /// the value tuple type <typeparamref name="TValueTuple" />.
    /// </summary>
    /// <typeparam name="TValueTuple">The type of value tuple to materialize.</typeparam>
    /// <param name="valueTupleFieldTypes">
    /// The field types of the value tuple type <typeparamref name="TValueTuple" />.
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
    /// <typeparamref name="TValueTuple" />.
    /// </returns>
    /// <remarks>
    /// Which of the two implementations builds the materializer is decided here, by
    /// <see cref="RuntimeFeature.IsDynamicCodeSupported" />: the compiled expression tree when the runtime can
    /// generate code, and the reflection-based materializer when it cannot. The AOT compiler folds that check to a
    /// constant and removes the branch it does not need, so an application published with Native AOT does not carry
    /// the expression-tree implementation at all.
    /// </remarks>
#if !NET9_0_OR_GREATER
    // See the identical suppression in EntityMaterializerFactory.CreateMaterializer for why this is here, why it is
    // a transcription of a result the net10.0 build verifies rather than an assertion, and why both target
    // frameworks have to stay in the AOT warning gate.
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050:Requires dynamic code",
        Justification = "The call is inside an if (RuntimeFeature.IsDynamicCodeSupported) branch, which the AOT compiler folds "
            + "to false and removes together with the expression-tree implementation. The net9.0+ analyzer "
            + "recognizes that guard and reports nothing here; net8.0 lacks the [FeatureGuard] annotation on "
            + "IsDynamicCodeSupported that lets it do so."
    )]
#endif
    private static Delegate CreateMaterializer<[DynamicallyAccessedMembers(ValueTupleMemberTypes)] TValueTuple>(
        Type[] valueTupleFieldTypes,
        DbDataReader dataReader,
        string[] dataReaderFieldNames,
        Type[] dataReaderFieldTypes
    )
    {
        if (RuntimeFeature.IsDynamicCodeSupported)
        {
            return CreateExpressionMaterializer<TValueTuple>(
                valueTupleFieldTypes,
                dataReader,
                dataReaderFieldNames,
                dataReaderFieldTypes
            );
        }

        return CreateReflectionMaterializer<TValueTuple>(dataReader, dataReaderFieldNames, dataReaderFieldTypes);
    }

    /// <summary>
    /// Creates a materializer function that materializes the data in a <see cref="DbDataReader" /> to an instance of
    /// the value tuple type <typeparamref name="TValueTuple" /> by compiling an expression tree.
    /// </summary>
    /// <typeparam name="TValueTuple">The type of value tuple to materialize.</typeparam>
    /// <param name="valueTupleFieldTypes">
    /// The field types of the value tuple type <typeparamref name="TValueTuple" />.
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
    /// <typeparamref name="TValueTuple" />.
    /// </returns>
    /// <remarks>
    /// This is the fast path for runtimes that can generate code, and the only implementation the library had before
    /// it grew a Native AOT counterpart. It is reached exclusively through the
    /// <see cref="RuntimeFeature.IsDynamicCodeSupported" /> check in
    /// <see cref="CreateMaterializer{TValueTuple}" />; <see cref="CreateReflectionMaterializer{TValueTuple}" /> is
    /// the counterpart that produces the same results without generating code.
    /// </remarks>
    [RequiresDynamicCode(MaterializerRequiresDynamicCodeMessage)]
    private static Delegate CreateExpressionMaterializer<
        [DynamicallyAccessedMembers(ValueTupleMemberTypes)] TValueTuple
    >(Type[] valueTupleFieldTypes, DbDataReader dataReader, string[] dataReaderFieldNames, Type[] dataReaderFieldTypes)
    {
        var valueTupleType = typeof(TValueTuple);

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
            var columnNameOrPosition = GetColumnNameOrPosition(fieldOrdinal, dataReaderFieldName);

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
                        typeof(InvalidCastException).GetConstructor([typeof(string)])!,
                        Expression.Constant(
                            $"The {columnNameOrPosition} returned by the SQL statement contains a NULL "
                                + $"value, but the corresponding field of the value tuple type {valueTupleType} "
                                + "is non-nullable."
                        )
                    ),
                    targetType
                );

            var throwInvalidCastExceptionExpression = Expression.Throw(
                Expression.New(
                    typeof(InvalidCastException).GetConstructor([typeof(string), typeof(Exception)])!,
                    Expression.Constant(
                        $"The {columnNameOrPosition} returned by the SQL statement contains a "
                            + $"value that could not be converted to the type {targetType} "
                            + $"of the corresponding field of the value tuple type {valueTupleType}. "
                            + "See inner exception for details."
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
                Expression.Catch(exceptionParameterExpression, throwInvalidCastExceptionExpression)
            );

            var isNotDbNullBranchExpression =
                dataReaderFieldType != targetType ? convertFieldValueExpression : getFieldValueCallExpression;

            dataReaderFieldValueExpressions[fieldOrdinal] = Expression.Condition(
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
        var fieldValueExpressionChunks = new Stack<Expression[]>(
            dataReaderFieldValueExpressions.Chunk(ValueTupleFieldCountBeforeNesting)
        );

        // Then we get the constructors for the value tuple types, which GetValueTupleConstructors returns from the
        // outermost to the innermost. Pushing them onto a stack in that order reverses it, so we start with the
        // constructor for the most inner value tuple. Both materializer paths share that one traversal, so they
        // cannot drift apart on the nesting they build.
        var valueTupleConstructors = new Stack<ConstructorInfo>(GetValueTupleConstructors(valueTupleType));

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
            var arguments = newExpression is not null ? [.. chunk, newExpression] : chunk;

            newExpression = Expression.New(constructor, arguments);
        }

        return Expression.Lambda(newExpression!, dataReaderParameterExpression).Compile();
    }

    /// <summary>
    /// Gets the description of the field with the ordinal <paramref name="fieldOrdinal" /> that the exception
    /// messages refer the consumer to.
    /// </summary>
    /// <param name="fieldOrdinal">The ordinal of the field in the result set.</param>
    /// <param name="dataReaderFieldName">The name of the field in the result set, if it has one.</param>
    /// <returns>
    /// The name of the field, or - for a result set whose columns have no name, which a value tuple query is allowed
    /// to have because its fields are matched by position - the position of the field.
    /// </returns>
    private static string GetColumnNameOrPosition(int fieldOrdinal, string? dataReaderFieldName) =>
        !string.IsNullOrWhiteSpace(dataReaderFieldName)
            ? $"column '{dataReaderFieldName}'"
            : $"{(fieldOrdinal + 1).OrdinalizeEnglish()} column";

    /// <summary>
    /// Gets the constructors that build the value tuple type <paramref name="valueTupleType" />, from the outermost
    /// value tuple type to the innermost one.
    /// </summary>
    /// <param name="valueTupleType">The value tuple type to get the constructors of.</param>
    /// <returns>
    /// The constructors, ordered from the one of <paramref name="valueTupleType" /> itself to the one of the
    /// innermost nested value tuple type. A value tuple type with at most seven fields is not nested, so the result
    /// contains exactly one constructor.
    /// </returns>
    /// <remarks>
    /// This is the counterpart of the traversal in <see cref="CreateExpressionMaterializer{TValueTuple}" /> for the
    /// materializer path that cannot compile an expression tree. Both walk the nested value tuple types through
    /// their <c>Rest</c> field and pick the constructor whose parameters are the fields of that type, so that the
    /// two paths build the same nesting from the same arguments.
    /// </remarks>
    // This is the only place in the library where trimming safety rests on something the analyzer cannot check, so
    // the reasoning is written out in full.
    //
    // On the second and later iterations, currentValueTupleType came out of genericArguments[^1] - a runtime Type
    // that the analyzer has lost track of - so it cannot know that GetConstructor's [DynamicallyAccessedMembers]
    // requirement is met. No annotation can tell it: [DynamicallyAccessedMembers] is not recursive and cannot reach a
    // type used as another type's generic argument. That is IL2065, and answering it here is what keeps
    // [RequiresUnreferencedCode] - and therefore an IL2026 in every consumer's build - off the generic query methods.
    //
    // What makes suppressing it correct here, rather than convenient, is that the members ARE preserved, by a shipped
    // mechanism rather than by hope:
    //
    //   1. ILLink.Descriptors.xml, embedded in this assembly, preserves System.ValueTuple`1 through `8. It travels
    //      into a consumer's trimmed or Native AOT publish. See that file for why a descriptor is the sanctioned
    //      instrument for this and not a suppression in disguise.
    //   2. The loop provably reaches nothing else. GetMaterializer rejected any type that is not a value tuple
    //      before this runs, and the runtime represents a value tuple's Rest field as one of those same eight
    //      arities - so arbitrary nesting depth stays inside the descriptor's list.
    //   3. Trimming/ILLinkDescriptorsTests asserts the descriptor is still embedded in the assembly and still lists
    //      all eight arities, so step 1 cannot silently rot.
    //   4. tests/package-consumption/AotConsumer publishes natively on both target frameworks and materializes nested
    //      value tuples for real, including ones whose nested field is an enum. That is what would notice if any of
    //      the above stopped holding.
    //
    // If you are here because you want to remove the descriptor, or to reach a type this loop does not validate:
    // this suppression stops being true at that moment. Restore [RequiresUnreferencedCode] on the query methods
    // rather than leaving it in place.
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2065:Value passed to implicit 'this' parameter cannot be statically determined",
        Justification = "The nested value tuple types this walks are System.ValueTuple`1-`8, whose constructors the embedded "
            + "ILLink.Descriptors.xml preserves in a consumer's trimmed or Native AOT publish. The caller has already "
            + "rejected any type that is not a value tuple, and a unit test guards the descriptor's completeness."
    )]
    private static ConstructorInfo[] GetValueTupleConstructors(
        [DynamicallyAccessedMembers(ValueTupleMemberTypes)] Type valueTupleType
    )
    {
        var valueTupleConstructors = new List<ConstructorInfo>();

        var currentValueTupleType = valueTupleType;

        while (true)
        {
            // The generic arguments of a value tuple type are exactly the parameters of its constructor, in order.
            var genericArguments = currentValueTupleType.GetGenericArguments();

            valueTupleConstructors.Add(
                currentValueTupleType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, genericArguments)!
            );

            // Fewer than eight arguments means there is no "Rest" field, so this is the innermost value tuple type.
            if (genericArguments.Length <= ValueTupleFieldCountBeforeNesting)
            {
                break;
            }

            // Continue with the next inner value tuple type, which is the last generic argument.
            currentValueTupleType = genericArguments[^1];
        }

        return [.. valueTupleConstructors];
    }

    /// <summary>
    /// Materializes the current row of <paramref name="dataReader" /> to an instance of the value tuple type
    /// <typeparamref name="TValueTuple" />.
    /// </summary>
    /// <typeparam name="TValueTuple">The type of value tuple to materialize.</typeparam>
    /// <param name="dataReader">The <see cref="DbDataReader" /> to materialize the current row of.</param>
    /// <param name="valueTupleType">
    /// The type of value tuple to materialize. Used in the exception messages.
    /// </param>
    /// <param name="valueTupleConstructors">
    /// The constructors of the value tuple types, from the outermost to the innermost, as returned by
    /// <see cref="GetValueTupleConstructors" />.
    /// </param>
    /// <param name="columnBindings">The fields of the result set, in the order of the fields of the value tuple.</param>
    /// <returns>The materialized instance of the value tuple type <typeparamref name="TValueTuple" />.</returns>
    /// <exception cref="InvalidCastException">
    /// A field of the result set could not be assigned to the corresponding field of the value tuple.
    /// </exception>
    /// <remarks>
    /// All fields are read first, in the order of the result set, and only then are the value tuples constructed
    /// from the innermost one outwards. The compiled expression tree evaluates its field values in the same order -
    /// a nested value tuple is the last argument of its enclosing constructor - so when more than one field is
    /// unusable, both paths report the same one.
    /// </remarks>
    private static TValueTuple MaterializeValueTuple<TValueTuple>(
        DbDataReader dataReader,
        Type valueTupleType,
        ConstructorInvoker[] valueTupleConstructors,
        ReflectionColumnBinding[] columnBindings
    )
    {
        var fieldValues = ReadFieldValues(dataReader, valueTupleType, columnBindings);

        return (TValueTuple)ConstructValueTuple(valueTupleConstructors, fieldValues);
    }

    /// <summary>
    /// Reads all fields of the current row of <paramref name="dataReader" />, in the order of the result set.
    /// </summary>
    /// <param name="dataReader">The <see cref="DbDataReader" /> to read the current row of.</param>
    /// <param name="valueTupleType">
    /// The type of value tuple being materialized. Used in the exception messages.
    /// </param>
    /// <param name="columnBindings">The fields of the result set, in the order of the fields of the value tuple.</param>
    /// <returns>
    /// The field values, in the order of the result set and therefore in the order of the fields of the value tuple,
    /// each one ready to be passed to the constructor of the value tuple that holds it.
    /// </returns>
    /// <exception cref="InvalidCastException">
    /// A field of the result set could not be assigned to the corresponding field of the value tuple.
    /// </exception>
    private static object?[] ReadFieldValues(
        DbDataReader dataReader,
        Type valueTupleType,
        ReflectionColumnBinding[] columnBindings
    )
    {
        var fieldValues = new object?[columnBindings.Length];

        for (var fieldOrdinal = 0; fieldOrdinal < columnBindings.Length; fieldOrdinal++)
        {
            fieldValues[fieldOrdinal] = ReadFieldValue(dataReader, valueTupleType, columnBindings[fieldOrdinal]);
        }

        return fieldValues;
    }

    /// <summary>
    /// Constructs the value tuple that holds <paramref name="fieldValues" />.
    /// </summary>
    /// <param name="valueTupleConstructors">
    /// The constructors of the value tuple types, from the outermost to the innermost, as returned by
    /// <see cref="GetValueTupleConstructors" />.
    /// </param>
    /// <param name="fieldValues">
    /// The value of every field of the value tuple, including the fields of all nested value tuples, in the order in
    /// which the fields are declared.
    /// </param>
    /// <returns>The constructed value tuple, boxed.</returns>
    /// <remarks>
    /// This does the same as the tail of <see cref="CreateExpressionMaterializer{TValueTuple}" />, which builds the
    /// same nesting out of <see cref="Expression.New(ConstructorInfo, Expression[])" /> instead of constructing it.
    /// </remarks>
    private static object ConstructValueTuple(ConstructorInvoker[] valueTupleConstructors, object?[] fieldValues)
    {
        // In C# value tuples with more than 7 fields are represented as nested value tuples.
        // E.g. a ValueTuple with 15 fields is represented as:
        // ValueTuple<T1, ..., T7, ValueTuple<T8, ..., T14, ValueTuple<T15>>>
        // In this case we need to create the nested value tuples from the inside out.

        // So we chunk the field values into groups of 7, which gives us the field values of one of those value
        // tuples per chunk. The chunks are in the same order as valueTupleConstructors: the first chunk and the
        // first constructor belong to the outermost value tuple, the last ones to the innermost value tuple.
        var fieldValueChunks = fieldValues.Chunk(ValueTupleFieldCountBeforeNesting).ToArray();

        object? valueTuple = null;

        // Now we create the nested value tuples from the inside out, by walking both arrays from their last entry
        // to their first one. When we are done valueTuple contains the outermost value tuple.
        for (var chunkIndex = fieldValueChunks.Length - 1; chunkIndex >= 0; chunkIndex--)
        {
            // If valueTuple is null, it means we are at the innermost value tuple, so we only need to use the
            // current chunk of field values as arguments.
            //
            // Otherwise, if valueTuple is not null, it means we are not at the innermost value tuple, and we need to
            // add valueTuple (which contains the last created inner value tuple) as the argument for the "Rest"
            // parameter.
            var constructorArguments = valueTuple is not null
                ? [.. fieldValueChunks[chunkIndex], valueTuple]
                : fieldValueChunks[chunkIndex];

            valueTuple = valueTupleConstructors[chunkIndex].Invoke(constructorArguments.AsSpan());
        }

        return valueTuple!;
    }

    /// <summary>
    /// Reads the value of the field described by <paramref name="columnBinding" /> from the current row of
    /// <paramref name="dataReader" />, converting it to the type of the value tuple field it is assigned to.
    /// </summary>
    /// <param name="dataReader">The <see cref="DbDataReader" /> to read the field value from.</param>
    /// <param name="valueTupleType">
    /// The type of value tuple being materialized. Used in the exception messages.
    /// </param>
    /// <param name="columnBinding">The field of the result set to read.</param>
    /// <returns>The field value, ready to be passed to the constructor of the value tuple.</returns>
    /// <exception cref="InvalidCastException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 The field value is <see langword="null" /> and the value tuple field is non-nullable.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 The field value could not be converted to the type of the value tuple field.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    private static object? ReadFieldValue(
        DbDataReader dataReader,
        Type valueTupleType,
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
                $"The {columnBinding.ColumnNameOrPosition} returned by the SQL statement contains a NULL "
                    + $"value, but the corresponding field of the value tuple type {valueTupleType} "
                    + "is non-nullable."
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
                $"The {columnBinding.ColumnNameOrPosition} returned by the SQL statement contains a "
                    + $"value that could not be converted to the type {columnBinding.TargetType} "
                    + $"of the corresponding field of the value tuple type {valueTupleType}. "
                    + "See inner exception for details.",
                exception
            );
        }
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
    private static Type[] GetValueTupleFieldTypes(
        [DynamicallyAccessedMembers(ValueTupleMemberTypes)] Type valueTupleType
    )
    {
        var fieldTypes = new List<Type>();
        var currentValueTupleType = valueTupleType;

        while (true)
        {
            // The generic arguments of a value tuple type ARE its field types, in field order.
            var genericArguments = currentValueTupleType.GetGenericArguments();

            var hasNestedValueTuple = genericArguments.Length > ValueTupleFieldCountBeforeNesting;

            if (!hasNestedValueTuple)
            {
                fieldTypes.AddRange(genericArguments);
                break;
            }

            fieldTypes.AddRange(genericArguments.Take(ValueTupleFieldCountBeforeNesting));

            currentValueTupleType = genericArguments[^1];
        }

        return [.. fieldTypes];
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
        [DynamicallyAccessedMembers(ValueTupleMemberTypes)] Type valueTupleType,
        Type[] valueTupleFieldTypes,
        DbDataReader dataReader,
        string[] dataReaderFieldNames,
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
                $"The SQL statement returned {"column".ToQuantity(dataReader.FieldCount)}, but the value tuple type "
                    + $"{valueTupleType} has {"field".ToQuantity(valueTupleFieldTypes.Length)}. Make sure that the SQL "
                    + "statement returns the same number of columns as the number of fields in the value tuple type.",
                nameof(dataReader)
            );
        }

        for (var fieldOrdinal = 0; fieldOrdinal < dataReader.FieldCount; fieldOrdinal++)
        {
            var dataReaderFieldName = dataReaderFieldNames[fieldOrdinal];

            var columnNameOrPosition = GetColumnNameOrPosition(fieldOrdinal, dataReaderFieldName);

            var valueTupleFieldType = valueTupleFieldTypes[fieldOrdinal];
            var dataReaderFieldType = dataReaderFieldTypes[fieldOrdinal];

            if (!ValueConverter.CanConvert(dataReaderFieldType, valueTupleFieldType))
            {
                throw new ArgumentException(
                    $"The data type {dataReaderFieldType} of the {columnNameOrPosition} returned by the SQL "
                        + $"statement is not compatible with the field type {valueTupleFieldType} of the corresponding "
                        + $"field of the value tuple type {valueTupleType}.",
                    nameof(dataReader)
                );
            }

            if (!MaterializerFactoryHelper.IsDbDataReaderTypedGetMethodAvailable(dataReaderFieldType))
            {
                throw new ArgumentException(
                    $"The data type {dataReaderFieldType} of the {columnNameOrPosition} returned by the SQL "
                        + "statement is not supported.",
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
        string[] dataReaderFieldNames,
        Type[] dataReaderFieldTypes
    ) : IEquatable<MaterializerCacheKey>
    {
        /// <inheritdoc />
        public bool Equals(MaterializerCacheKey other) =>
            this.ValueTupleFieldTypes.SequenceEqual(other.ValueTupleFieldTypes)
            && this.DataReaderFieldNames.SequenceEqual(other.DataReaderFieldNames)
            && this.DataReaderFieldTypes.SequenceEqual(other.DataReaderFieldTypes);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is MaterializerCacheKey other && this.Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
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

        private string[] DataReaderFieldNames { get; } = dataReaderFieldNames;
        private Type[] DataReaderFieldTypes { get; } = dataReaderFieldTypes;
        private Type[] ValueTupleFieldTypes { get; } = valueTupleFieldTypes;
    }

    /// <summary>
    /// Everything the reflection materializer needs to read one field of a result set and turn it into a value the
    /// value tuple accepts, resolved once per result-set shape.
    /// </summary>
    /// <param name="ColumnNameOrPosition">
    /// The description of the field the exception messages refer the consumer to - its name, or its position when
    /// the field has no name.
    /// </param>
    /// <param name="FieldOrdinal">The ordinal of the field in the result set.</param>
    /// <param name="GetFieldValue">
    /// Gets the value of the field from a <see cref="DbDataReader" />, using the same
    /// <see cref="DbDataReader" />.GetXXX method the compiled expression tree would call.
    /// </param>
    /// <param name="NeedsConversion">
    /// Determines whether the field value has to be converted to <paramref name="TargetType" /> before the value
    /// tuple accepts it. This is the case when the field type differs from the target type.
    /// </param>
    /// <param name="TargetType">
    /// The type the field value is converted to - the type of the value tuple field it is assigned to.
    /// </param>
    private readonly record struct ReflectionColumnBinding(
        string ColumnNameOrPosition,
        int FieldOrdinal,
        Func<DbDataReader, object?> GetFieldValue,
        bool NeedsConversion,
        Type TargetType
    );
}
