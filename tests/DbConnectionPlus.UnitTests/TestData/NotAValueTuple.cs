namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

/// <summary>
/// A type that implements the same interfaces as <see cref="ValueTuple" /> but is not a <see cref="ValueTuple" />.
/// </summary>
public struct NotAValueTuple : IStructuralEquatable, IStructuralComparable, IComparable
{
    /// <inheritdoc />
    public int CompareTo(object? other, IComparer comparer) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public int CompareTo(object? obj) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public bool Equals(object? other, IEqualityComparer comparer) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public int GetHashCode(IEqualityComparer comparer) =>
        throw new NotImplementedException();
}
