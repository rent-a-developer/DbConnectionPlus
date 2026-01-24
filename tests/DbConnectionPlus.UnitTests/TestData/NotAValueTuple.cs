namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

/// <summary>
/// A type that implements the same interfaces as <see cref="ValueTuple" /> but is not a <see cref="ValueTuple" />.
/// </summary>
public struct NotAValueTuple : IStructuralEquatable, IStructuralComparable, IComparable
{
    /// <inheritdoc />
    public Int32 CompareTo(Object? other, IComparer comparer) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public Int32 CompareTo(Object? obj) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public Boolean Equals(Object? other, IEqualityComparer comparer) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public Int32 GetHashCode(IEqualityComparer comparer) =>
        throw new NotImplementedException();
}
