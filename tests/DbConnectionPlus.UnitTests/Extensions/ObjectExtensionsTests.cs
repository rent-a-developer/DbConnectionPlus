using RentADeveloper.DbConnectionPlus.Extensions;

// ReSharper disable RedundantExplicitArrayCreation
// ReSharper disable RedundantCast
// ReSharper disable NotAccessedPositionalProperty.Local
// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace RentADeveloper.DbConnectionPlus.UnitTests.Extensions;

public class ObjectExtensionsTests : UnitTestsBase
{
    [Fact]
    public void ToDebugString_ShouldHandleObjectsWithCyclicReferences()
    {
        var itemA = new Item("A");
        var itemB = new Item("B");

        itemA.Reference = itemB;
        itemB.Reference = itemA;

        itemA.ToDebugString()
            .Should().Be(
                """'{"Id":"A","Reference":{"Id":"B","Reference":null}}' (RentADeveloper.DbConnectionPlus.UnitTests.Extensions.ObjectExtensionsTests+Item)"""
            );
    }

    [Fact]
    public void ToDebugString_ShouldReturnStringRepresentationOfValue()
    {
        (null as Object).ToDebugString()
            .Should().Be("{null}");

        DBNull.Value.ToDebugString()
            .Should().Be("{DBNull}");

        true.ToDebugString()
            .Should().Be("'True' (System.Boolean)");

        ((Byte)123).ToDebugString()
            .Should().Be("'123' (System.Byte)");

        new Byte[] { 1, 2, 3 }.ToDebugString()
            .Should().Be("'AQID' (System.Byte[])");

        'X'.ToDebugString()
            .Should().Be("'X' (System.Char)");

        new DateTime(2025, 12, 31, 23, 59, 59, 999).ToDebugString()
            .Should().Be("'2025-12-31T23:59:59.9990000' (System.DateTime)");

        new DateTimeOffset(2025, 12, 31, 23, 59, 59, TimeSpan.FromHours(1)).ToDebugString()
            .Should().Be("'2025-12-31T23:59:59.0000000+01:00' (System.DateTimeOffset)");

        123.45M.ToDebugString()
            .Should().Be("'123.45' (System.Decimal)");

        123.45.ToDebugString()
            .Should().Be("'123.45' (System.Double)");

        TestEnum.Value3.ToDebugString()
            .Should().Be("'Value3' (RentADeveloper.DbConnectionPlus.UnitTests.TestData.TestEnum)");

        new Guid("889a8be0-f0ff-4555-86d8-8490434b7def").ToDebugString()
            .Should().Be("'889a8be0-f0ff-4555-86d8-8490434b7def' (System.Guid)");

        ((Int16)123).ToDebugString()
            .Should().Be("'123' (System.Int16)");

        123.ToDebugString()
            .Should().Be("'123' (System.Int32)");

        ((Int64)123).ToDebugString()
            .Should().Be("'123' (System.Int64)");

        ((IntPtr)123).ToDebugString()
            .Should().Be("'123' (System.IntPtr)");

        ((SByte)123).ToDebugString()
            .Should().Be("'123' (System.SByte)");

        ((Single)123.45).ToDebugString()
            .Should().Be("'123.449997' (System.Single)");

        "A String".ToDebugString()
            .Should().Be("'A String' (System.String)");

        new TimeSpan(1, 2, 3, 4).ToDebugString()
            .Should().Be("'1.02:03:04' (System.TimeSpan)");

        ((UInt16)123).ToDebugString()
            .Should().Be("'123' (System.UInt16)");

        ((UInt32)123).ToDebugString()
            .Should().Be("'123' (System.UInt32)");

        ((UInt64)123).ToDebugString()
            .Should().Be("'123' (System.UInt64)");

        ((UIntPtr)123).ToDebugString()
            .Should().Be("'123' (System.UIntPtr)");

        new Int32[] { 1, 2, 3 }.ToDebugString()
            .Should().Be("'[1,2,3]' (System.Int32[])");

        new Object().ToDebugString()
            .Should().Be("'{}' (System.Object)");

        new EntityWithStringProperty { String = "A String" }.ToDebugString()
            .Should().Be(
                """'{"String":"A String"}' (RentADeveloper.DbConnectionPlus.UnitTests.TestData.EntityWithStringProperty)"""
            );
    }

    private record Item(String Id)
    {
        public Item? Reference { get; set; }
    }
}
