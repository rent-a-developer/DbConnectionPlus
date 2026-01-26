// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using System.Dynamic;

#pragma warning disable CA1710

namespace RentADeveloper.DbConnectionPlus.Dynamic;

/// <summary>
/// The data of a single row returned by an SQL query.
/// </summary>
public class DataRow : DynamicObject, IDictionary<String, Object?>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataRow" /> class.
    /// </summary>
    /// <param name="columns">
    /// The columns of the data row.
    /// The keys are expected to be the column names, and the values are expected to be the corresponding column values.
    /// </param>
    public DataRow(IDictionary<String, Object?> columns) =>
        this.columns = columns;

    /// <inheritdoc />
    public Int32 Count => this.columns.Count;

    /// <inheritdoc />
    public Boolean IsReadOnly => this.columns.IsReadOnly;

    /// <inheritdoc />
    public Object? this[String key]
    {
        get => this.columns[key];
        set => this.columns[key] = value;
    }

    /// <inheritdoc />
    public ICollection<String> Keys => this.columns.Keys;

    /// <inheritdoc />
    public ICollection<Object?> Values => this.columns.Values;

    /// <inheritdoc />
    public void Add(KeyValuePair<String, Object?> item) =>
        this.columns.Add(item);

    /// <inheritdoc />
    public void Add(String key, Object? value) =>
        this.columns.Add(key, value);

    /// <inheritdoc />
    public void Clear() =>
        this.columns.Clear();

    /// <inheritdoc />
    public Boolean Contains(KeyValuePair<String, Object?> item) =>
        this.columns.Contains(item);

    /// <inheritdoc />
    public Boolean ContainsKey(String key) =>
        this.columns.ContainsKey(key);

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<String, Object?>[] array, Int32 arrayIndex) =>
        this.columns.CopyTo(array, arrayIndex);

    /// <inheritdoc />
    public override IEnumerable<String> GetDynamicMemberNames() =>
        this.columns.Keys;

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<String, Object?>> GetEnumerator() =>
        this.columns.GetEnumerator();

    /// <inheritdoc />
    public Boolean Remove(KeyValuePair<String, Object?> item) =>
        this.columns.Remove(item);

    /// <inheritdoc />
    public Boolean Remove(String key) =>
        this.columns.Remove(key);

    /// <inheritdoc />
    public override Boolean TryGetMember(GetMemberBinder binder, out Object? result)
    {
        ArgumentNullException.ThrowIfNull(binder);

        return this.columns.TryGetValue(binder.Name, out result);
    }

    /// <inheritdoc />
    public Boolean TryGetValue(String key, out Object? value) =>
        this.columns.TryGetValue(key, out value);

    /// <inheritdoc />
    public override Boolean TrySetMember(SetMemberBinder binder, Object? value)
    {
        ArgumentNullException.ThrowIfNull(binder);

        this.columns[binder.Name] = value;
        return true;
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        this.GetEnumerator();

    private readonly IDictionary<String, Object?> columns;
}
