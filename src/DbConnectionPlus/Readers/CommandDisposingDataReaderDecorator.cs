// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using System.Collections.ObjectModel;
using RentADeveloper.DbConnectionPlus.DbCommands;

namespace RentADeveloper.DbConnectionPlus.Readers;

/// <summary>
/// A decorator for a <see cref="DbDataReader" /> that disposes the associated <see cref="DbCommand" /> when disposed
/// and handles the case when a read operation is cancelled by a <see cref="CancellationToken" />.
/// </summary>
internal sealed class CommandDisposingDataReaderDecorator : DbDataReader
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandDisposingDataReaderDecorator" /> class.
    /// </summary>
    /// <param name="dataReader">The <see cref="DbDataReader" /> to decorate.</param>
    /// <param name="databaseAdapter">
    /// The database adapter for the database for which <see cref="DbDataReader" /> was obtained.
    /// </param>
    /// <param name="commandDisposer">
    /// The <see cref="DbCommandDisposer" /> that is responsible for disposing the associated <see cref="DbCommand" />.
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
    public CommandDisposingDataReaderDecorator(
        DbDataReader dataReader,
        IDatabaseAdapter databaseAdapter,
        DbCommandDisposer commandDisposer,
        CancellationToken commandCancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(dataReader);
        ArgumentNullException.ThrowIfNull(databaseAdapter);
        ArgumentNullException.ThrowIfNull(commandDisposer);

        this.dataReader = dataReader;
        this.databaseAdapter = databaseAdapter;
        this.commandDisposer = commandDisposer;
        this.commandCancellationToken = commandCancellationToken;
    }

    /// <inheritdoc />
    public override int Depth => this.dataReader.Depth;

    /// <inheritdoc />
    public override int FieldCount => this.dataReader.FieldCount;

    /// <inheritdoc />
    public override bool HasRows => this.dataReader.HasRows;

    /// <inheritdoc />
    public override bool IsClosed => this.dataReader.IsClosed;

    /// <inheritdoc />
    public override object this[int ordinal] => this.dataReader[ordinal];

    /// <inheritdoc />
    public override object this[string name] => this.dataReader[name];

    /// <inheritdoc />
    public override int RecordsAffected => this.dataReader.RecordsAffected;

    /// <inheritdoc />
    public override int VisibleFieldCount => this.dataReader.VisibleFieldCount;

    /// <inheritdoc />
    public override void Close() => this.dataReader.Close();

    /// <inheritdoc />
    public override Task CloseAsync() => this.dataReader.CloseAsync();

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
        await this.commandDisposer.DisposeAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override bool GetBoolean(int ordinal) => this.dataReader.GetBoolean(ordinal);

    /// <inheritdoc />
    public override byte GetByte(int ordinal) => this.dataReader.GetByte(ordinal);

    /// <inheritdoc />
    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) =>
        this.dataReader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);

    /// <inheritdoc />
    public override char GetChar(int ordinal) => this.dataReader.GetChar(ordinal);

    /// <inheritdoc />
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) =>
        this.dataReader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);

    /// <inheritdoc />
    public override Task<ReadOnlyCollection<DbColumn>> GetColumnSchemaAsync(
        CancellationToken cancellationToken = default
    ) => this.dataReader.GetColumnSchemaAsync(cancellationToken);

    /// <inheritdoc />
    public override string GetDataTypeName(int ordinal) => this.dataReader.GetDataTypeName(ordinal);

    /// <inheritdoc />
    public override DateTime GetDateTime(int ordinal) => this.dataReader.GetDateTime(ordinal);

    /// <inheritdoc />
    public override decimal GetDecimal(int ordinal) => this.dataReader.GetDecimal(ordinal);

    /// <inheritdoc />
    public override double GetDouble(int ordinal) => this.dataReader.GetDouble(ordinal);

    /// <inheritdoc />
    public override IEnumerator GetEnumerator() => this.dataReader.GetEnumerator();

    /// <inheritdoc />
    [return: DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties
    )]
    public override Type GetFieldType(int ordinal) => this.dataReader.GetFieldType(ordinal);

    /// <inheritdoc />
    public override T GetFieldValue<T>(int ordinal) => this.dataReader.GetFieldValue<T>(ordinal);

    /// <inheritdoc />
    public override Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken) =>
        this.dataReader.GetFieldValueAsync<T>(ordinal, cancellationToken);

    /// <inheritdoc />
    public override float GetFloat(int ordinal) => this.dataReader.GetFloat(ordinal);

    /// <inheritdoc />
    public override Guid GetGuid(int ordinal) => this.dataReader.GetGuid(ordinal);

    /// <inheritdoc />
    public override short GetInt16(int ordinal) => this.dataReader.GetInt16(ordinal);

    /// <inheritdoc />
    public override int GetInt32(int ordinal) => this.dataReader.GetInt32(ordinal);

    /// <inheritdoc />
    public override long GetInt64(int ordinal) => this.dataReader.GetInt64(ordinal);

    /// <inheritdoc />
    public override string GetName(int ordinal) => this.dataReader.GetName(ordinal);

    /// <inheritdoc />
    public override int GetOrdinal(string name) => this.dataReader.GetOrdinal(name);

    /// <inheritdoc />
    [return: DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties
    )]
    public override Type GetProviderSpecificFieldType(int ordinal) =>
        this.dataReader.GetProviderSpecificFieldType(ordinal);

    /// <inheritdoc />
    public override object GetProviderSpecificValue(int ordinal) => this.dataReader.GetProviderSpecificValue(ordinal);

    /// <inheritdoc />
    public override int GetProviderSpecificValues(object[] values) => this.dataReader.GetProviderSpecificValues(values);

    /// <inheritdoc />
    public override DataTable? GetSchemaTable() => this.dataReader.GetSchemaTable();

    /// <inheritdoc />
    public override Task<DataTable?> GetSchemaTableAsync(CancellationToken cancellationToken = default) =>
        this.dataReader.GetSchemaTableAsync(cancellationToken);

    /// <inheritdoc />
    public override Stream GetStream(int ordinal) => this.dataReader.GetStream(ordinal);

    /// <inheritdoc />
    public override string GetString(int ordinal) => this.dataReader.GetString(ordinal);

    /// <inheritdoc />
    public override TextReader GetTextReader(int ordinal) => this.dataReader.GetTextReader(ordinal);

    /// <inheritdoc />
    public override object GetValue(int ordinal) => this.dataReader.GetValue(ordinal);

    /// <inheritdoc />
    public override int GetValues(object[] values) => this.dataReader.GetValues(values);

    /// <inheritdoc />
    public override bool IsDBNull(int ordinal) => this.dataReader.IsDBNull(ordinal);

    /// <inheritdoc />
    public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken) =>
        this.dataReader.IsDBNullAsync(ordinal, cancellationToken);

    /// <inheritdoc />
    public override bool NextResult() => this.dataReader.NextResult();

    /// <inheritdoc />
    public override Task<bool> NextResultAsync(CancellationToken cancellationToken) =>
        this.dataReader.NextResultAsync(cancellationToken);

    /// <inheritdoc />
    /// <exception cref="OperationCanceledException">
    /// The operation was canceled via a <see cref="CancellationToken" />.
    /// </exception>
    public override bool Read()
    {
        try
        {
            return this.dataReader.Read();
        }
        catch (Exception exception)
            when (this.databaseAdapter.WasSqlStatementCancelledByCancellationToken(
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
    public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await this.dataReader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
            when (this.databaseAdapter.WasSqlStatementCancelledByCancellationToken(exception, cancellationToken))
        {
            throw new OperationCanceledException(cancellationToken);
        }
        catch (Exception exception)
            when (this.databaseAdapter.WasSqlStatementCancelledByCancellationToken(
                    exception,
                    this.commandCancellationToken
                )
            )
        {
            throw new OperationCanceledException(this.commandCancellationToken);
        }
    }

    /// <inheritdoc />
    public override string? ToString() => this.dataReader.ToString();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
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
            this.commandDisposer.Dispose();
        }
    }

    private readonly CancellationToken commandCancellationToken;
    private readonly DbCommandDisposer commandDisposer;
    private readonly IDatabaseAdapter databaseAdapter;
    private readonly DbDataReader dataReader;
    private bool isDisposed;
}
