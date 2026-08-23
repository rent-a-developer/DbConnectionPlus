namespace RentADeveloper.DbConnectionPlus.UnitTests.Mocks;

/// <summary>
/// A simple mock implementation of <see cref="DbParameterCollection" />.
/// </summary>
public class MockDbParameterCollection : DbParameterCollection
{
    private readonly List<DbParameter> parameters = [];

    /// <inheritdoc />
    public override int Count => this.parameters.Count;

    /// <inheritdoc />
    public override object SyncRoot => ((ICollection)this.parameters).SyncRoot;

    /// <inheritdoc />
    public override int Add(object value)
    {
        this.parameters.Add((DbParameter)value);
        return this.Count - 1;
    }

    /// <inheritdoc />
    public override void AddRange(Array values) => this.parameters.AddRange(values.Cast<DbParameter>());

    /// <inheritdoc />
    public override void Clear() => this.parameters.Clear();

    /// <inheritdoc />
    public override bool Contains(object value) => this.parameters.Contains(value);

    /// <inheritdoc />
    public override bool Contains(string value) => this.IndexOf(value) != -1;

    /// <inheritdoc />
    public override void CopyTo(Array array, int index) => this.parameters.CopyTo((DbParameter[])array, index);

    /// <inheritdoc />
    public override IEnumerator GetEnumerator() => this.parameters.GetEnumerator();

    /// <inheritdoc />
    public override int IndexOf(object value) => this.parameters.IndexOf((DbParameter)value);

    /// <inheritdoc />
    public override int IndexOf(string parameterName)
    {
        for (var index = 0; index < this.parameters.Count; ++index)
        {
            if (this.parameters[index].ParameterName == parameterName)
            {
                return index;
            }
        }

        return -1;
    }

    /// <inheritdoc />
    public override void Insert(int index, object value) => this.parameters.Insert(index, (DbParameter)value);

    /// <inheritdoc />
    public override void Remove(object value) => this.parameters.Remove((DbParameter)value);

    /// <inheritdoc />
    public override void RemoveAt(int index) => this.parameters.RemoveAt(index);

    /// <inheritdoc />
    public override void RemoveAt(string parameterName) => this.RemoveAt(this.IndexOfChecked(parameterName));

    /// <inheritdoc />
    protected override DbParameter GetParameter(int index) => this.parameters[index];

    /// <inheritdoc />
    protected override DbParameter GetParameter(string parameterName) =>
        this.GetParameter(this.IndexOfChecked(parameterName));

    /// <inheritdoc />
    protected override void SetParameter(int index, DbParameter value) => this.parameters[index] = value;

    /// <inheritdoc />
    protected override void SetParameter(string parameterName, DbParameter value) =>
        this.SetParameter(this.IndexOfChecked(parameterName), value);

    private int IndexOfChecked(string parameterName)
    {
        var index = this.IndexOf(parameterName);
        return index != -1 ? index : throw new IndexOutOfRangeException();
    }
}
