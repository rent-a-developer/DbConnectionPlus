// ReSharper disable ConvertToPrimaryConstructor

// An explicit public constructor is the whole point of this fixture: it is what the constructor-injection
// materializer binds to. A primary constructor would change what is under test.
#pragma warning disable IDE0290

namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public class ItemWithConstructor
{
    public ItemWithConstructor(short a, int b, long c)
    {
        this.A = a;
        this.B = b;
        this.C = c;
    }

    public short A { get; init; }
    public int B { get; init; }
    public long C { get; init; }
}
