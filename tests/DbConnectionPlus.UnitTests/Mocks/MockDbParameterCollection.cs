namespace RentADeveloper.DbConnectionPlus.UnitTests.Mocks;

/// <summary>
/// A simple mock implementation of <see cref="DbParameterCollection" />.
/// </summary>
public class MockDbParameterCollection : DbParameterCollection
{
    /// <inheritdoc />
    public override Int32 Count => this.parameters.Count;

    /// <inheritdoc />
    public override Object SyncRoot => ((ICollection)this.parameters).SyncRoot;

    /// <inheritdoc />
    public override Int32 Add(Object value)
    {
        this.parameters.Add((DbParameter)value);
        return this.Count - 1;
    }

    /// <inheritdoc />
    public override void AddRange(Array values) => this.parameters.AddRange(values.Cast<DbParameter>());

    /// <inheritdoc />
    public override void Clear() => this.parameters.Clear();

    /// <inheritdoc />
    public override bool Contains(Object value) => this.parameters.Contains(value);

    /// <inheritdoc />
    public override bool Contains(String value) => this.IndexOf(value) != -1;

    /// <inheritdoc />
    public override void CopyTo(Array array, Int32 index) =>
        this.parameters.CopyTo((DbParameter[])array, index);

    /// <inheritdoc />
    public override IEnumerator GetEnumerator() => this.parameters.GetEnumerator();

    /// <inheritdoc />
    public override Int32 IndexOf(Object value) => this.parameters.IndexOf((DbParameter)value);

    /// <inheritdoc />
    public override Int32 IndexOf(String parameterName)
    {
        for (Int32 index = 0; index < this.parameters.Count; ++index)
        {
            if (this.parameters[index].ParameterName == parameterName)
                return index;
        }

        return -1;
    }

    /// <inheritdoc />
    public override void Insert(Int32 index, Object value) =>
        this.parameters.Insert(index, (DbParameter)value);

    /// <inheritdoc />
    public override void Remove(Object value) => this.parameters.Remove((DbParameter)value);

    /// <inheritdoc />
    public override void RemoveAt(Int32 index) => this.parameters.RemoveAt(index);

    /// <inheritdoc />
    public override void RemoveAt(String parameterName) =>
        this.RemoveAt(this.IndexOfChecked(parameterName));

    /// <inheritdoc />
    protected override DbParameter GetParameter(Int32 index) => this.parameters[index];

    /// <inheritdoc />
    protected override DbParameter GetParameter(String parameterName) =>
        this.GetParameter(this.IndexOfChecked(parameterName));

    /// <inheritdoc />
    protected override void SetParameter(Int32 index, DbParameter value) =>
        this.parameters[index] = value;

    /// <inheritdoc />
    protected override void SetParameter(String parameterName, DbParameter value) =>
        this.SetParameter(this.IndexOfChecked(parameterName), value);

    private Int32 IndexOfChecked(String parameterName)
    {
        Int32 num = this.IndexOf(parameterName);
        return num != -1 ? num : throw new IndexOutOfRangeException();
    }

    private readonly List<DbParameter> parameters = [];
}
