namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public sealed class ItemWithPrivateConstructor
{
    private ItemWithPrivateConstructor(short a, int b, long c)
    {
        this.A = a;
        this.B = b;
        this.C = c;
    }

    public short A { get; init; }
    public int B { get; init; }
    public long C { get; init; }
}
