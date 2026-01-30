// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using System.Collections.ObjectModel;

namespace RentADeveloper.DbConnectionPlus.Readers;

/// <summary>
/// A decorator for a <see cref="DbDataReader" /> that signals when it is being disposed and handles the case when a
/// read operation is cancelled by a <see cref="CancellationToken" />.
/// </summary>
internal sealed class DisposeSignalingDataReaderDecorator : DbDataReader
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DisposeSignalingDataReaderDecorator" /> class.
    /// </summary>
    /// <param name="dataReader">The <see cref="DbDataReader" /> to decorate.</param>
    /// <param name="databaseAdapter">
    /// The database adapter for the database for which <see cref="DbDataReader" /> was obtained.
    /// </param>
    /// <param name="commandCancellationToken">
    /// The <see cref="CancellationToken" /> that is associated with the <see cref="DbCommand" /> from which the
    /// <see cref="DbDataReader" /> to decorate was obtained.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="dataReader" /> is <see langword="null" />
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="databaseAdapter" /> is <see langword="null" />
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    public DisposeSignalingDataReaderDecorator(
        DbDataReader dataReader,
        IDatabaseAdapter databaseAdapter,
        CancellationToken commandCancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(dataReader);
        ArgumentNullException.ThrowIfNull(databaseAdapter);

        this.dataReader = dataReader;
        this.databaseAdapter = databaseAdapter;
        this.commandCancellationToken = commandCancellationToken;
    }

    /// <inheritdoc />
    public override Int32 Depth => this.dataReader.Depth;

    /// <inheritdoc />
    public override Int32 FieldCount => this.dataReader.FieldCount;

    /// <inheritdoc />
    public override Boolean HasRows => this.dataReader.HasRows;

    /// <inheritdoc />
    public override Boolean IsClosed => this.dataReader.IsClosed;

    /// <inheritdoc />
    public override Object this[Int32 ordinal] => this.dataReader[ordinal];

    /// <inheritdoc />
    public override Object this[String name] => this.dataReader[name];

    /// <inheritdoc />
    public override Int32 RecordsAffected => this.dataReader.RecordsAffected;

    /// <inheritdoc />
    public override Int32 VisibleFieldCount => this.dataReader.VisibleFieldCount;

    /// <inheritdoc />
    public override void Close() =>
        this.dataReader.Close();

    /// <inheritdoc />
    public override Task CloseAsync() =>
        this.dataReader.CloseAsync();

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        if (this.isDisposed)
        {
            return;
        }

        this.isDisposed = true;

        await base.DisposeAsync().ConfigureAwait(false);
        await this.dataReader.DisposeAsync().ConfigureAwait(false);

        if (this.OnDisposingAsync is not null)
        {
            await this.OnDisposingAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public override Boolean GetBoolean(Int32 ordinal) =>
        this.dataReader.GetBoolean(ordinal);

    /// <inheritdoc />
    public override Byte GetByte(Int32 ordinal) =>
        this.dataReader.GetByte(ordinal);

    /// <inheritdoc />
    public override Int64 GetBytes(
        Int32 ordinal,
        Int64 dataOffset,
        Byte[]? buffer,
        Int32 bufferOffset,
        Int32 length
    ) =>
        this.dataReader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);

    /// <inheritdoc />
    public override Char GetChar(Int32 ordinal) =>
        this.dataReader.GetChar(ordinal);

    /// <inheritdoc />
    public override Int64 GetChars(
        Int32 ordinal,
        Int64 dataOffset,
        Char[]? buffer,
        Int32 bufferOffset,
        Int32 length
    ) =>
        this.dataReader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);

    /// <inheritdoc />
    public override Task<ReadOnlyCollection<DbColumn>> GetColumnSchemaAsync(
        CancellationToken cancellationToken = default
    ) =>
        this.dataReader.GetColumnSchemaAsync(cancellationToken);

    /// <inheritdoc />
    public override String GetDataTypeName(Int32 ordinal) =>
        this.dataReader.GetDataTypeName(ordinal);

    /// <inheritdoc />
    public override DateTime GetDateTime(Int32 ordinal) =>
        this.dataReader.GetDateTime(ordinal);

    /// <inheritdoc />
    public override Decimal GetDecimal(Int32 ordinal) =>
        this.dataReader.GetDecimal(ordinal);

    /// <inheritdoc />
    public override Double GetDouble(Int32 ordinal) =>
        this.dataReader.GetDouble(ordinal);

    /// <inheritdoc />
    public override IEnumerator GetEnumerator() =>
        this.dataReader.GetEnumerator();

    /// <inheritdoc />
    public override Type GetFieldType(Int32 ordinal) =>
        this.dataReader.GetFieldType(ordinal);

    /// <inheritdoc />
    public override T GetFieldValue<T>(Int32 ordinal) =>
        this.dataReader.GetFieldValue<T>(ordinal);

    /// <inheritdoc />
    public override Task<T> GetFieldValueAsync<T>(Int32 ordinal, CancellationToken cancellationToken) =>
        this.dataReader.GetFieldValueAsync<T>(ordinal, cancellationToken);

    /// <inheritdoc />
    public override Single GetFloat(Int32 ordinal) =>
        this.dataReader.GetFloat(ordinal);

    /// <inheritdoc />
    public override Guid GetGuid(Int32 ordinal) =>
        this.dataReader.GetGuid(ordinal);

    /// <inheritdoc />
    public override Int16 GetInt16(Int32 ordinal) =>
        this.dataReader.GetInt16(ordinal);

    /// <inheritdoc />
    public override Int32 GetInt32(Int32 ordinal) =>
        this.dataReader.GetInt32(ordinal);

    /// <inheritdoc />
    public override Int64 GetInt64(Int32 ordinal) =>
        this.dataReader.GetInt64(ordinal);

    /// <inheritdoc />
    public override String GetName(Int32 ordinal) =>
        this.dataReader.GetName(ordinal);

    /// <inheritdoc />
    public override Int32 GetOrdinal(String name) =>
        this.dataReader.GetOrdinal(name);

    /// <inheritdoc />
    public override Type GetProviderSpecificFieldType(Int32 ordinal) =>
        this.dataReader.GetProviderSpecificFieldType(ordinal);

    /// <inheritdoc />
    public override Object GetProviderSpecificValue(Int32 ordinal) =>
        this.dataReader.GetProviderSpecificValue(ordinal);

    /// <inheritdoc />
    public override Int32 GetProviderSpecificValues(Object[] values) =>
        this.dataReader.GetProviderSpecificValues(values);

    /// <inheritdoc />
    public override DataTable? GetSchemaTable() =>
        this.dataReader.GetSchemaTable();

    /// <inheritdoc />
    public override Task<DataTable?> GetSchemaTableAsync(CancellationToken cancellationToken = default) =>
        this.dataReader.GetSchemaTableAsync(cancellationToken);

    /// <inheritdoc />
    public override Stream GetStream(Int32 ordinal) =>
        this.dataReader.GetStream(ordinal);

    /// <inheritdoc />
    public override String GetString(Int32 ordinal) =>
        this.dataReader.GetString(ordinal);

    /// <inheritdoc />
    public override TextReader GetTextReader(Int32 ordinal) =>
        this.dataReader.GetTextReader(ordinal);

    /// <inheritdoc />
    public override Object GetValue(Int32 ordinal) =>
        this.dataReader.GetValue(ordinal);

    /// <inheritdoc />
    public override Int32 GetValues(Object[] values) =>
        this.dataReader.GetValues(values);

    /// <inheritdoc />
    public override Boolean IsDBNull(Int32 ordinal) =>
        this.dataReader.IsDBNull(ordinal);

    /// <inheritdoc />
    public override Task<Boolean> IsDBNullAsync(Int32 ordinal, CancellationToken cancellationToken) =>
        this.dataReader.IsDBNullAsync(ordinal, cancellationToken);

    /// <inheritdoc />
    public override Boolean NextResult() =>
        this.dataReader.NextResult();

    /// <inheritdoc />
    public override Task<Boolean> NextResultAsync(CancellationToken cancellationToken) =>
        this.dataReader.NextResultAsync(cancellationToken);

    /// <inheritdoc />
    /// <exception cref="OperationCanceledException">
    /// The operation was canceled via a <see cref="CancellationToken" />.
    /// </exception>
    public override Boolean Read()
    {
        try
        {
            return this.dataReader.Read();
        }
        catch (Exception exception)
            when (
                this.databaseAdapter.WasSqlStatementCancelledByCancellationToken(
                    exception,
                    this.commandCancellationToken
                )
            )
        {
            throw new OperationCanceledException(this.commandCancellationToken);
        }
    }

    /// <inheritdoc />
    /// <exception cref="OperationCanceledException">
    /// The operation was canceled via a <see cref="CancellationToken" />.
    /// </exception>
    public override async Task<Boolean> ReadAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await this.dataReader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
            when (
                this.databaseAdapter
                    .WasSqlStatementCancelledByCancellationToken(
                        exception,
                        cancellationToken
                    )
            )
        {
            throw new OperationCanceledException(cancellationToken);
        }
        catch (Exception exception)
            when (
                this.databaseAdapter.WasSqlStatementCancelledByCancellationToken(
                    exception,
                    this.commandCancellationToken
                )
            )
        {
            throw new OperationCanceledException(this.commandCancellationToken);
        }
    }

    /// <inheritdoc />
    public override String? ToString() =>
        this.dataReader.ToString();

    /// <summary>
    /// A function that is invoked when this instance is being disposed synchronously.
    /// </summary>
    internal Action? OnDisposing { get; set; }

    /// <summary>
    /// A function that is invoked when this instance is being disposed asynchronously.
    /// </summary>
    internal Func<ValueTask>? OnDisposingAsync { get; set; }

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
            this.dataReader.Dispose();

            this.OnDisposing?.Invoke();
        }
    }

    private readonly CancellationToken commandCancellationToken;
    private readonly IDatabaseAdapter databaseAdapter;
    private readonly DbDataReader dataReader;
    private Boolean isDisposed;
}
