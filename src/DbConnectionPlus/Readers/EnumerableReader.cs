// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus.Readers;

/// <summary>
/// A <see cref="DbDataReader" /> that reads from an <see cref="IEnumerable" />.
/// </summary>
internal sealed class EnumerableReader : DbDataReader
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnumerableReader" /> class.
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
    public EnumerableReader(IEnumerable values, Type valuesType, String fieldName)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(valuesType);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        this.valuesType = valuesType;
        this.fieldName = fieldName;

        // ReSharper disable once GenericEnumeratorNotDisposed
        // The enumerator will be disposed in the Close/Dispose/DisposeAsync method.
        this.enumerator = values.GetEnumerator();
    }

    /// <inheritdoc />
    public override Int32 Depth => 0;

    /// <inheritdoc />
    public override Int32 FieldCount => 1;

    /// <inheritdoc />
    public override Boolean HasRows => true;

    /// <inheritdoc />
    // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
    public override Boolean IsClosed => this.isClosed;

    /// <inheritdoc />
    public override Object this[Int32 ordinal] => this.GetValue(ordinal);

    /// <inheritdoc />
    public override Object this[String name] => this.GetValue(this.GetOrdinal(name));

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
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    public override Boolean GetBoolean(Int32 ordinal) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    public override Byte GetByte(Int32 ordinal) =>
        throw new NotImplementedException();

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
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    public override Char GetChar(Int32 ordinal) =>
        throw new NotImplementedException();

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
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    public override String GetDataTypeName(Int32 ordinal) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    public override DateTime GetDateTime(Int32 ordinal) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    public override Decimal GetDecimal(Int32 ordinal) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    public override Double GetDouble(Int32 ordinal) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public override IEnumerator GetEnumerator() =>
        this.enumerator;

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">
    /// The specified ordinal <paramref name="ordinal" /> is not supported. The only supported ordinal is zero.
    /// </exception>
    public override Type GetFieldType(Int32 ordinal)
    {
        EnsureValidFieldOrdinal(ordinal);

        return this.valuesType;
    }

    /// <inheritdoc />
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    public override Single GetFloat(Int32 ordinal) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    public override Guid GetGuid(Int32 ordinal) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    public override Int16 GetInt16(Int32 ordinal) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    public override Int32 GetInt32(Int32 ordinal) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    public override Int64 GetInt64(Int32 ordinal) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">
    /// The specified ordinal <paramref name="ordinal" /> is not supported. The only supported ordinal is zero.
    /// </exception>
    public override String GetName(Int32 ordinal)
    {
        EnsureValidFieldOrdinal(ordinal);

        return this.fieldName;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">
    /// The specified field name <paramref name="name" /> is not supported.
    /// The only supported field name is the field name that was passed to the constructor of this class.
    /// </exception>
    public override Int32 GetOrdinal(String name)
    {
        this.EnsureValidFieldName(name);

        return 0;
    }

    /// <inheritdoc />
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    public override DataTable GetSchemaTable() =>
        throw new NotImplementedException();

    /// <inheritdoc />
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    public override String GetString(Int32 ordinal) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">
    /// The specified ordinal <paramref name="ordinal" /> is not supported. The only supported ordinal is zero.
    /// </exception>
    public override Object GetValue(Int32 ordinal)
    {
        EnsureValidFieldOrdinal(ordinal);

        return this.current ?? DBNull.Value;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">
    /// <paramref name="buffer" /> does not have a length of at least 1.
    /// </exception>
    public override Int32 GetValues(Object[] buffer)
    {
        if (buffer.Length < 1)
        {
            throw new ArgumentException(
                "The specified array must have a length greater than or equal to 1.",
                nameof(buffer)
            );
        }

        buffer[0] = this.current ?? DBNull.Value;

        return 1;
    }

    /// <inheritdoc />
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    public override Boolean IsDBNull(Int32 ordinal)
    {
        EnsureValidFieldOrdinal(ordinal);

        return this.current is null or DBNull;
    }

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

    private void EnsureValidFieldName(String name)
    {
        if (!String.Equals(name, this.fieldName, StringComparison.Ordinal))
        {
            throw new ArgumentOutOfRangeException(
                nameof(name),
                $"The specified field name '{name}' is not supported. The only supported field name is " +
                $"'{this.fieldName}'."
            );
        }
    }

    private static void EnsureValidFieldOrdinal(Int32 ordinal)
    {
        if (ordinal != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ordinal),
                ordinal,
                $"The specified ordinal {ordinal} is not supported. The only supported ordinal is zero."
            );
        }
    }

    private readonly IEnumerator enumerator;
    private readonly String fieldName;
    private readonly Type valuesType;
    private Object? current;
    private Boolean isClosed;
    private Boolean isDisposed;
    private Boolean isEnumeratorDisposed;
}
