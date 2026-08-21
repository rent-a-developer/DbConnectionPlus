// ReSharper disable RedundantExplicitArrayCreation
// ReSharper disable RedundantCast
// ReSharper disable NotAccessedPositionalProperty.Local
// ReSharper disable UnusedAutoPropertyAccessor.Local

using RentADeveloper.DbConnectionPlus.Extensions;

namespace RentADeveloper.DbConnectionPlus.UnitTests.Extensions;

public class ObjectExtensionsTests : UnitTestsBase
{
    [Fact]
    public void ToDebugString_ShouldUseToStringForUnhandledTypes() => new Item("A").ToDebugString()
            .Should().Be(
                "'Item A' (RentADeveloper.DbConnectionPlus.UnitTests.Extensions.ObjectExtensionsTests+Item)"
            );

    [Fact]
    public void ToDebugString_ShouldRenderSequencesElementByElement()
    {
        new List<String> { "A", "B" }.ToDebugString()
            .Should().Be("'[A,B]' (System.Collections.Generic.List`1[System.String])");

        new Object?[] { 1, null, "A", true }.ToDebugString()
            .Should().Be("'[1,{null},A,True]' (System.Object[])");

        new Int32[][] { [1, 2], [3] }.ToDebugString()
            .Should().Be("'[[1,2],[3]]' (System.Int32[][])");

        Array.Empty<Int32>().ToDebugString()
            .Should().Be("'[]' (System.Int32[])");
    }

    [Fact]
    public void ToDebugString_ShouldTruncateSelfReferencingSequencesInsteadOfRecursingForever()
    {
        var values = new List<Object?> { 1 };

        values.Add(values);

        // The depth bound replaces the cycle handling that the previous JsonSerializer-based implementation got
        // from ReferenceHandler.IgnoreCycles. What matters is that this terminates at all; the exact nesting
        // depth at which it stops is an implementation detail.
        var debugString = values.ToDebugString();

        debugString.Should().StartWith("'[1,[1,[1,");
        debugString.Should().Contain("[...]");
    }

    [Fact]
    public void ToDebugString_ShouldReturnStringRepresentationOfValue()
    {
#pragma warning disable RCS1202
        (null as Object).ToDebugString()
            .Should().Be("{null}");
#pragma warning restore RCS1202

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

#pragma warning disable CA1861 // Avoid constant arrays as arguments
        new Int32[] { 1, 2, 3 }.ToDebugString()
            .Should().Be("'[1,2,3]' (System.Int32[])");
#pragma warning restore CA1861 // Avoid constant arrays as arguments

        new Object().ToDebugString()
            .Should().Be("'System.Object' (System.Object)");

        new EntityWithEnumStoredAsString { Enum = TestEnum.Value3, Id = 1 }.ToDebugString()
            .Should().Be(
                "'EntityWithEnumStoredAsString { Enum = Value3, Id = 1 }' " +
                "(RentADeveloper.DbConnectionPlus.UnitTests.TestData.EntityWithEnumStoredAsString)"
            );
    }

    private sealed class Item(String id)
    {
        /// <inheritdoc />
        public override String ToString() =>
            $"Item {id}";
    }
}
