// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using RentADeveloper.DbConnectionPlus.Converters;
using RentADeveloper.DbConnectionPlus.Extensions;

namespace RentADeveloper.DbConnectionPlus.Readers;

/// <summary>
/// A <see cref="DbDataReader" /> that reads from an <see cref="IEnumerable" />.
/// </summary>
/// <remarks>
/// <para>
/// The reader works in two modes. In <em>single-column</em> mode each element of the sequence <em>is</em> the
/// value of the one and only column; this is the mode the scalar temporary-table path uses. In
/// <em>multi-column</em> mode each element is an entity and every mapped, readable property becomes a column,
/// read through <see cref="EntityPropertyMetadata.PropertyGetter" />; this is the mode the complex-object
/// temporary-table path uses. Nothing here emits code: the accessors are plain reflection, so the same reader
/// serves the bulk-copy APIs on the just-in-time compiler and under Native AOT alike.
/// </para>
/// </remarks>
internal sealed class EnumerableReader : DbDataReader
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnumerableReader" /> class that reads a single column, where
    /// each element of <paramref name="values" /> is the value of that column.
    /// </summary>
    /// <param name="values">The sequence of values from which the reader will read values.</param>
    /// <param name="valuesType">The type of values in <paramref name="values" />.</param>
    /// <param name="fieldName">
    /// The field name that the reader will use to represent the values of the sequence <paramref name="values" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="values" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="valuesType" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="fieldName" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="fieldName" /> is empty or consists only of white-space characters.
    /// </exception>
    public EnumerableReader(
        IEnumerable values,
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
        Type valuesType,
        String fieldName)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(valuesType);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        this.valuesType = valuesType;
        this.fieldNames = [fieldName];
        this.properties = [];
        this.options = EnumerableReaderOptions.None;

        // ReSharper disable once GenericEnumeratorNotDisposed
        // The enumerator will be disposed in the Close/Dispose/DisposeAsync method.
        this.enumerator = values.GetEnumerator();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumerableReader" /> class that reads one column per entry of
    /// <paramref name="properties" />, where each element of <paramref name="values" /> is an entity.
    /// </summary>
    /// <param name="values">The sequence of entities from which the reader will read values.</param>
    /// <param name="properties">
    /// The metadata of the properties that become the columns of the reader, in column order. Every entry must be
    /// readable, that is, expose a <see cref="EntityPropertyMetadata.PropertyGetter" />.
    /// </param>
    /// <param name="options">The behaviours the reader applies to the values it reads.</param>
    /// <exception cref="ArgumentNullException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="values" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="properties" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="properties" /> is empty, or one of its entries has no
    /// <see cref="EntityPropertyMetadata.PropertyGetter" />.
    /// </exception>
    public EnumerableReader(
        IEnumerable values,
        IReadOnlyList<EntityPropertyMetadata> properties,
        EnumerableReaderOptions options)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(properties);

        if (properties.Count == 0)
        {
            throw new ArgumentException("The specified property list must not be empty.", nameof(properties));
        }

        foreach (var property in properties)
        {
            if (property.PropertyGetter is null)
            {
                throw new ArgumentException(
                    $"The property '{property.PropertyName}' cannot be read and therefore cannot become a column.",
                    nameof(properties)
                );
            }
        }

        this.properties = [.. properties];
        this.fieldNames = [.. properties.Select(a => a.PropertyName)];
        this.options = options;

        // ReSharper disable once GenericEnumeratorNotDisposed
        // The enumerator will be disposed in the Close/Dispose/DisposeAsync method.
        this.enumerator = values.GetEnumerator();
    }

    /// <inheritdoc />
    public override Int32 Depth => 0;

    /// <inheritdoc />
    public override Int32 FieldCount => this.fieldNames.Length;

    /// <inheritdoc />
    public override Boolean HasRows => true;

    /// <inheritdoc />
    // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
    public override Boolean IsClosed => this.isClosed;

    /// <inheritdoc />
    public override Object this[Int32 ordinal] => this.GetValue(ordinal);

    /// <inheritdoc />
    public override Object this[String name] => this.GetValue(this.GetOrdinalOrThrow(name));

    /// <inheritdoc />
    public override Int32 RecordsAffected => -1;

    /// <inheritdoc />
    public override void Close()
    {
        if (this.isClosed)
        {
            return;
        }

        this.isClosed = true;
        this.DisposeEnumerator();
    }

    /// <inheritdoc />
    public override Boolean GetBoolean(Int32 ordinal) =>
        (Boolean)this.GetValue(ordinal);

    /// <inheritdoc />
    public override Byte GetByte(Int32 ordinal) =>
        (Byte)this.GetValue(ordinal);

    /// <inheritdoc />
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    public override Int64 GetBytes(
        Int32 ordinal,
        Int64 dataOffset,
        Byte[]? buffer,
        Int32 bufferOffset,
        Int32 length
    ) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public override Char GetChar(Int32 ordinal) =>
        (Char)this.GetValue(ordinal);

    /// <inheritdoc />
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    public override Int64 GetChars(
        Int32 ordinal,
        Int64 dataOffset,
        Char[]? buffer,
        Int32 bufferOffset,
        Int32 length
    ) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">
    /// The specified ordinal <paramref name="ordinal" /> is not one of the ordinals the reader supports.
    /// </exception>
    public override String GetDataTypeName(Int32 ordinal) =>
        this.GetFieldType(ordinal).Name;

    /// <inheritdoc />
    public override DateTime GetDateTime(Int32 ordinal) =>
        (DateTime)this.GetValue(ordinal);

    /// <inheritdoc />
    public override Decimal GetDecimal(Int32 ordinal) =>
        (Decimal)this.GetValue(ordinal);

    /// <inheritdoc />
    public override Double GetDouble(Int32 ordinal) =>
        (Double)this.GetValue(ordinal);

    /// <inheritdoc />
    public override IEnumerator GetEnumerator() =>
        this.enumerator;

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">
    /// The specified ordinal <paramref name="ordinal" /> is not one of the ordinals the reader supports.
    /// </exception>
    /// <remarks>
    /// In multi-column mode the reported type is resolved from a fixed table of <c>typeof</c> literals rather than
    /// returned straight from <see cref="Type" /> metadata. <see cref="DbDataReader.GetFieldType" /> annotates its
    /// return value with <see cref="DynamicallyAccessedMemberTypes.PublicFields" /> and
    /// <see cref="DynamicallyAccessedMemberTypes.PublicProperties" />, and no annotation can be carried across a
    /// <c>PropertyInfo.PropertyType</c> read or across a <see cref="Type" /> array element — both produce
    /// <c>IL2073</c>, which is a defect to fix rather than to suppress. A statically known type satisfies the
    /// contract by construction.
    /// </remarks>
    [return: DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
    public override Type GetFieldType(Int32 ordinal)
    {
        this.EnsureValidFieldOrdinal(ordinal);

        return this.valuesType ?? MapReportedFieldType(this.properties[ordinal].PropertyType, this.options);
    }

    /// <inheritdoc />
    public override Single GetFloat(Int32 ordinal) =>
        (Single)this.GetValue(ordinal);

    /// <inheritdoc />
    public override Guid GetGuid(Int32 ordinal) =>
        (Guid)this.GetValue(ordinal);

    /// <inheritdoc />
    public override Int16 GetInt16(Int32 ordinal) =>
        (Int16)this.GetValue(ordinal);

    /// <inheritdoc />
    public override Int32 GetInt32(Int32 ordinal)
    {
        var value = this.GetValue(ordinal);

        if (this.SerializesEnums && this.IsEnumColumn(ordinal) && value is Enum enumValue)
        {
            return (Int32)(Object)enumValue;
        }

        return (Int32)value;
    }

    /// <inheritdoc />
    public override Int64 GetInt64(Int32 ordinal) =>
        (Int64)this.GetValue(ordinal);

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">
    /// The specified ordinal <paramref name="ordinal" /> is not one of the ordinals the reader supports.
    /// </exception>
    public override String GetName(Int32 ordinal)
    {
        this.EnsureValidFieldOrdinal(ordinal);

        return this.fieldNames[ordinal];
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">
    /// The reader reads a single column and the specified field name <paramref name="name" /> is not the field name
    /// that was passed to the constructor of this class.
    /// </exception>
    /// <remarks>
    /// In multi-column mode an unknown name yields <c>-1</c> rather than an exception, which is what the
    /// bulk-copy APIs of the database providers expect - they probe for columns they may not find.
    /// </remarks>
    public override Int32 GetOrdinal(String name) =>
        this.IsSingleColumn
            ? this.GetOrdinalOrThrow(name)
            : Array.IndexOf(this.fieldNames, name);

    /// <inheritdoc />
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    /// <remarks>
    /// This reader deliberately does not describe itself. A schema table has to carry each column's
    /// <c>DataType</c>, which means putting <see cref="Type" /> itself into a <see cref="DataTable" /> - and the
    /// trimmer reports that as <c>IL2111</c> (<c>Type.TypeInitializer</c> reached through reflection), a warning
    /// this project does not suppress. Nothing asks for it either: the bulk-copy APIs of all five providers drive
    /// this reader through <see cref="FieldCount" />, <see cref="GetName" /> and <see cref="GetValue" />.
    /// </remarks>
    public override DataTable GetSchemaTable() =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public override String GetString(Int32 ordinal)
    {
        var value = this.GetValue(ordinal);

        if (this.SerializesEnums && this.IsEnumColumn(ordinal) && value is Enum enumValue)
        {
            return enumValue.ToString();
        }

        if (this.ReadsCharsAsStrings && this.GetColumnType(ordinal).IsCharOrNullableCharType())
        {
            // The data readers of all major database systems return the type String for CHAR columns, which means
            // that GetString is called to retrieve the value. Casting a Char to a String would throw an
            // InvalidCastException, so the conversion happens here.

            return (value as Char?)?.ToString() ?? String.Empty;
        }

        return (String)value;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">
    /// The specified ordinal <paramref name="ordinal" /> is not one of the ordinals the reader supports.
    /// </exception>
    public override Object GetValue(Int32 ordinal)
    {
        this.EnsureValidFieldOrdinal(ordinal);

        if (this.IsSingleColumn)
        {
            return this.current ?? DBNull.Value;
        }

        return this.properties[ordinal].PropertyGetter!(this.current!) ?? DBNull.Value;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">
    /// The reader reads a single column and <paramref name="values" /> does not have a length of at least 1.
    /// </exception>
    /// <remarks>
    /// In multi-column mode a buffer shorter than <see cref="FieldCount" /> is filled as far as it reaches and the
    /// number of values written is returned, as <see cref="DbDataReader.GetValues" /> specifies.
    /// </remarks>
    public override Int32 GetValues(Object[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (this.IsSingleColumn)
        {
            if (values.Length < 1)
            {
                throw new ArgumentException(
                    "The specified array must have a length greater than or equal to 1.",
                    nameof(values)
                );
            }

            values[0] = this.SerializeValue(this.current ?? DBNull.Value);

            return 1;
        }

        var numberOfValues = Math.Min(values.Length, this.FieldCount);

        for (var ordinal = 0; ordinal < numberOfValues; ordinal++)
        {
            values[ordinal] = this.SerializeValue(this.GetValue(ordinal));
        }

        return numberOfValues;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">
    /// The specified ordinal <paramref name="ordinal" /> is not one of the ordinals the reader supports.
    /// </exception>
    public override Boolean IsDBNull(Int32 ordinal) =>
        this.GetValue(ordinal) is DBNull;

    /// <inheritdoc />
    public override Boolean NextResult() => false;

    /// <inheritdoc />
    public override Boolean Read()
    {
        if (this.isClosed)
        {
            throw new InvalidOperationException("Invalid attempt to call Read when reader is closed.");
        }

        if (this.enumerator.MoveNext())
        {
            this.current = this.enumerator.Current;
            return true;
        }

        this.current = null;
        return false;
    }

    /// <inheritdoc />
    protected override void Dispose(Boolean disposing)
    {
        if (this.isDisposed)
        {
            return;
        }

        this.isDisposed = true;

        base.Dispose(disposing);

        if (disposing)
        {
            this.DisposeEnumerator();
        }
    }

    /// <summary>
    /// Gets a value indicating whether the reader reads a single column whose value is the sequence element itself.
    /// </summary>
    private Boolean IsSingleColumn => this.valuesType is not null;

    /// <summary>
    /// Gets a value indicating whether the reader returns <see cref="Char" /> values as <see cref="String" />.
    /// </summary>
    private Boolean ReadsCharsAsStrings =>
        this.options.HasFlag(EnumerableReaderOptions.ReadCharsAsStrings);

    /// <summary>
    /// Gets a value indicating whether the reader serializes <see cref="Enum" /> values while reading them.
    /// </summary>
    private Boolean SerializesEnums =>
        this.options.HasFlag(EnumerableReaderOptions.SerializeEnums);

    /// <summary>
    /// Disposes the enumerator obtained from the enumerable.
    /// </summary>
    private void DisposeEnumerator()
    {
        if (this.isEnumeratorDisposed)
        {
            return;
        }

        this.isEnumeratorDisposed = true;
        (this.enumerator as IDisposable)?.Dispose();
    }

    /// <summary>
    /// Throws if the specified ordinal is not one of the ordinals the reader supports.
    /// </summary>
    /// <param name="ordinal">The ordinal to check.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The specified ordinal <paramref name="ordinal" /> is not one of the ordinals the reader supports.
    /// </exception>
    private void EnsureValidFieldOrdinal(Int32 ordinal)
    {
        if (ordinal >= 0 && ordinal < this.FieldCount)
        {
            return;
        }

        throw new ArgumentOutOfRangeException(
            nameof(ordinal),
            ordinal,
            this.IsSingleColumn
                ? $"The specified ordinal {ordinal} is not supported. The only supported ordinal is zero."
                : $"The specified ordinal {ordinal} is not supported. The supported ordinals are 0 to " +
                  $"{this.FieldCount - 1}."
        );
    }

    /// <summary>
    /// Gets the type of the values the column with the specified ordinal reads.
    /// </summary>
    /// <param name="ordinal">The ordinal of the column to inspect.</param>
    /// <returns>
    /// The type passed to the constructor if the reader reads a single column; otherwise the type of the property
    /// the column is mapped to.
    /// </returns>
    private Type GetColumnType(Int32 ordinal) =>
        this.valuesType ?? this.properties[ordinal].PropertyType;

    /// <summary>
    /// Resolves the ordinal of the specified field name, throwing when the reader does not have such a field.
    /// </summary>
    /// <param name="name">The field name to resolve.</param>
    /// <returns>The ordinal of the field with the specified name.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The reader does not have a field with the specified name <paramref name="name" />.
    /// </exception>
    private Int32 GetOrdinalOrThrow(String name)
    {
        var ordinal = Array.IndexOf(this.fieldNames, name);

        if (ordinal >= 0)
        {
            return ordinal;
        }

        throw new ArgumentOutOfRangeException(
            nameof(name),
            this.IsSingleColumn
                ? $"The specified field name '{name}' is not supported. The only supported field name is " +
                  $"'{this.fieldNames[0]}'."
                : $"The specified field name '{name}' is not supported. The supported field names are " +
                  $"'{String.Join("', '", this.fieldNames)}'."
        );
    }

    /// <summary>
    /// Determines whether the column with the specified ordinal is mapped to an <see cref="Enum" /> property.
    /// </summary>
    /// <param name="ordinal">The ordinal of the column to inspect.</param>
    /// <returns>
    /// <see langword="true" /> if the column is mapped to an <see cref="Enum" /> property; otherwise,
    /// <see langword="false" />.
    /// </returns>
    private Boolean IsEnumColumn(Int32 ordinal) =>
        this.GetColumnType(ordinal).IsEnumOrNullableEnumType();

    /// <summary>
    /// Applies the reader's <see cref="EnumerableReaderOptions" /> to a value that was read from an entity.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The serialized value.</returns>
    private Object SerializeValue(Object value)
    {
        if (this.SerializesEnums && value is Enum enumValue)
        {
            return EnumSerializer.SerializeEnum(
                enumValue,
                DbConnectionPlusConfiguration.Instance.EnumSerializationMode
            );
        }

        if (this.ReadsCharsAsStrings && value is Char charValue)
        {
            // The data readers of all major database systems return the type String for CHAR columns.
            // So we mimic the same behavior for consistency.

            return charValue.ToString();
        }

        return value;
    }

    /// <summary>
    /// Resolves the type a column is reported as from the type of the property it is mapped to.
    /// </summary>
    /// <param name="propertyType">The type of the property the column is mapped to.</param>
    /// <param name="options">The behaviours the reader applies to the values it reads.</param>
    /// <returns>The type the column is reported as.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The configured <see cref="DbConnectionPlusConfiguration.EnumSerializationMode" /> is not a defined value.
    /// </exception>
    /// <remarks>
    /// Every branch returns a <c>typeof</c> literal so that the value satisfies the
    /// <see cref="DynamicallyAccessedMembersAttribute" /> the base class puts on
    /// <see cref="DbDataReader.GetFieldType" />; see the remarks there. A type that is neither a supported built-in
    /// type nor covered by <paramref name="options" /> — an <see cref="Enum" /> outside MySQL, most notably — is
    /// reported as <see cref="Object" />. Returning the runtime property type would violate the inherited trimming
    /// contract because <c>PropertyInfo.PropertyType</c> carries no member annotation. No caller inside this library
    /// reads that fallback: <c>PostgreSqlTemporaryTableBuilder</c>, the one place that would inspect the reader's
    /// field types, derives its <c>NpgsqlDbType</c> values from the entity metadata instead.
    /// </remarks>
    [return: DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
    private static Type MapReportedFieldType(Type propertyType, EnumerableReaderOptions options)
    {
        if (propertyType.IsEnumOrNullableEnumType())
        {
            if (!options.HasFlag(EnumerableReaderOptions.SerializeEnums))
            {
                return typeof(Object);
            }

            var enumSerializationMode = DbConnectionPlusConfiguration.Instance.EnumSerializationMode;

            return enumSerializationMode switch
            {
                EnumSerializationMode.Strings =>
                    typeof(String),

                EnumSerializationMode.Integers =>
                    typeof(Int32),

                _ => ThrowInvalidEnumSerializationModeException(enumSerializationMode)
            };
        }

        if (propertyType.IsCharOrNullableCharType() && options.HasFlag(EnumerableReaderOptions.ReadCharsAsStrings))
        {
            // The data readers of all major database systems return the type String for CHAR columns.
            // So we mimic the same behavior for consistency.

            return typeof(String);
        }

        return MapBuiltInFieldType(Nullable.GetUnderlyingType(propertyType) ?? propertyType);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> indicating that the specified
    /// <see cref="EnumSerializationMode" /> is invalid.
    /// </summary>
    /// <param name="enumSerializationMode">The <see cref="EnumSerializationMode" /> value that is invalid.</param>
    /// <returns>This method never returns.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Always thrown.</exception>
    [DoesNotReturn]
    [return: DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
    private static Type ThrowInvalidEnumSerializationModeException(EnumSerializationMode enumSerializationMode) =>
        throw new ArgumentOutOfRangeException(
            nameof(enumSerializationMode),
            enumSerializationMode,
            $"The {nameof(EnumSerializationMode)} {enumSerializationMode.ToDebugString()} is not supported."
        );

    /// <summary>
    /// Maps a non-nullable property type onto the statically known <see cref="Type" /> the column is reported as.
    /// </summary>
    /// <param name="propertyType">The non-nullable type of the property the column is mapped to.</param>
    /// <returns>
    /// The statically known type the column is reported as, or <see cref="Object" /> for a type this library does
    /// not store in a temporary table.
    /// </returns>
    /// <remarks>
    /// This cannot be a dictionary lookup. The result flows into the annotated return value of
    /// <see cref="DbDataReader.GetFieldType" />, and a <see cref="Type" /> read out of a collection carries no
    /// annotation, so the trimmer reports <c>IL2073</c> for it. Only a <c>typeof</c> literal satisfies the contract.
    /// </remarks>
    [return: DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
    private static Type MapBuiltInFieldType(Type propertyType)
    {
        if (propertyType == typeof(Boolean))
        {
            return typeof(Boolean);
        }

        if (propertyType == typeof(Byte))
        {
            return typeof(Byte);
        }

        if (propertyType == typeof(Byte[]))
        {
            return typeof(Byte[]);
        }

        if (propertyType == typeof(SByte))
        {
            return typeof(SByte);
        }

        if (propertyType == typeof(Char))
        {
            return typeof(Char);
        }

        if (propertyType == typeof(Decimal))
        {
            return typeof(Decimal);
        }

        if (propertyType == typeof(Double))
        {
            return typeof(Double);
        }

        if (propertyType == typeof(Single))
        {
            return typeof(Single);
        }

        if (propertyType == typeof(Int16))
        {
            return typeof(Int16);
        }

        if (propertyType == typeof(UInt16))
        {
            return typeof(UInt16);
        }

        if (propertyType == typeof(Int32))
        {
            return typeof(Int32);
        }

        if (propertyType == typeof(UInt32))
        {
            return typeof(UInt32);
        }

        if (propertyType == typeof(Int64))
        {
            return typeof(Int64);
        }

        if (propertyType == typeof(UInt64))
        {
            return typeof(UInt64);
        }

        if (propertyType == typeof(IntPtr))
        {
            return typeof(IntPtr);
        }

        if (propertyType == typeof(UIntPtr))
        {
            return typeof(UIntPtr);
        }

        if (propertyType == typeof(String))
        {
            return typeof(String);
        }

        if (propertyType == typeof(DateTime))
        {
            return typeof(DateTime);
        }

        if (propertyType == typeof(DateOnly))
        {
            return typeof(DateOnly);
        }

        if (propertyType == typeof(DateTimeOffset))
        {
            return typeof(DateTimeOffset);
        }

        if (propertyType == typeof(TimeSpan))
        {
            return typeof(TimeSpan);
        }

        if (propertyType == typeof(TimeOnly))
        {
            return typeof(TimeOnly);
        }

        if (propertyType == typeof(Guid))
        {
            return typeof(Guid);
        }

        return typeof(Object);
    }

    private readonly IEnumerator enumerator;
    private readonly String[] fieldNames;
    private readonly EnumerableReaderOptions options;
    private readonly EntityPropertyMetadata[] properties;
    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
    private readonly Type? valuesType;
    private Object? current;
    private Boolean isClosed;
    private Boolean isDisposed;
    private Boolean isEnumeratorDisposed;
}
