namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public sealed class ItemWithPrivateConstructor
{
    private ItemWithPrivateConstructor(Int16 a, Int32 b, Int64 c)
    {
        this.A = a;
        this.B = b;
        this.C = c;
    }

    public Int16 A { get; init; }
    public Int32 B { get; init; }
    public Int64 C { get; init; }
}
