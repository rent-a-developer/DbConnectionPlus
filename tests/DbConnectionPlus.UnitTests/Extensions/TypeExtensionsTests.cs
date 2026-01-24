using RentADeveloper.DbConnectionPlus.Extensions;
using TypeExtensions = RentADeveloper.DbConnectionPlus.Extensions.TypeExtensions;

// ReSharper disable InvokeAsExtensionMethod

namespace RentADeveloper.DbConnectionPlus.UnitTests.Extensions;

public class TypeExtensionsTests : UnitTestsBase
{
    [Theory]
    [InlineData(typeof(Boolean), true)]
    [InlineData(typeof(Boolean?), true)]
    [InlineData(typeof(Byte), true)]
    [InlineData(typeof(Byte?), true)]
    [InlineData(typeof(Char), true)]
    [InlineData(typeof(Char?), true)]
    [InlineData(typeof(DateOnly), true)]
    [InlineData(typeof(DateOnly?), true)]
    [InlineData(typeof(DateTime), true)]
    [InlineData(typeof(DateTime?), true)]
    [InlineData(typeof(DateTimeOffset), true)]
    [InlineData(typeof(DateTimeOffset?), true)]
    [InlineData(typeof(Decimal), true)]
    [InlineData(typeof(Decimal?), true)]
    [InlineData(typeof(Double), true)]
    [InlineData(typeof(Double?), true)]
    [InlineData(typeof(Guid), true)]
    [InlineData(typeof(Guid?), true)]
    [InlineData(typeof(Int16), true)]
    [InlineData(typeof(Int16?), true)]
    [InlineData(typeof(Int32), true)]
    [InlineData(typeof(Int32?), true)]
    [InlineData(typeof(Int64), true)]
    [InlineData(typeof(Int64?), true)]
    [InlineData(typeof(IntPtr), true)]
    [InlineData(typeof(IntPtr?), true)]
    [InlineData(typeof(SByte), true)]
    [InlineData(typeof(SByte?), true)]
    [InlineData(typeof(Single), true)]
    [InlineData(typeof(Single?), true)]
    [InlineData(typeof(String), true)]
    [InlineData(typeof(TimeOnly), true)]
    [InlineData(typeof(TimeOnly?), true)]
    [InlineData(typeof(TimeSpan), true)]
    [InlineData(typeof(TimeSpan?), true)]
    [InlineData(typeof(UInt16), true)]
    [InlineData(typeof(UInt16?), true)]
    [InlineData(typeof(UInt32), true)]
    [InlineData(typeof(UInt32?), true)]
    [InlineData(typeof(UInt64), true)]
    [InlineData(typeof(UInt64?), true)]
    [InlineData(typeof(UIntPtr), true)]
    [InlineData(typeof(UIntPtr?), true)]
    [InlineData(typeof(Entity), false)]
    [InlineData(typeof(TestEnum), false)]
    public void IsBuiltInTypeOrNullableBuiltInType_ShouldDetermineWhetherTypeIsBuiltInTypeOrNullableBuiltInType(
        Type type,
        Boolean expectedResult
    ) =>
        type.IsBuiltInTypeOrNullableBuiltInType()
            .Should().Be(expectedResult);

    [Theory]
    [InlineData(typeof(Char), true)]
    [InlineData(typeof(Char?), true)]
    [InlineData(typeof(String), false)]
    [InlineData(typeof(DateTime), false)]
    [InlineData(typeof(Entity), false)]
    public void IsCharOrNullableCharType_ShouldDetermineWhetherTypeIsCharOrNullableCharType(
        Type type,
        Boolean expectedResult
    ) =>
        type.IsCharOrNullableCharType()
            .Should().Be(expectedResult);

    [Theory]
    [InlineData(typeof(TestEnum), true)]
    [InlineData(typeof(TestEnum?), true)]
    [InlineData(typeof(ConsoleColor), true)]
    [InlineData(typeof(ConsoleColor?), true)]
    [InlineData(typeof(String), false)]
    [InlineData(typeof(DateTime), false)]
    [InlineData(typeof(Entity), false)]
    public void IsEnumOrNullableEnumType_ShouldDetermineWhetherTypeIsEnumTypeOrNullableEnumType(
        Type type,
        Boolean expectedResult
    ) =>
        type.IsEnumOrNullableEnumType()
            .Should().Be(expectedResult);

    [Theory]
    [InlineData(typeof(Int32?), true)]
    [InlineData(typeof(DateTime?), true)]
    [InlineData(typeof(Object), true)]
    [InlineData(typeof(Entity), true)]
    [InlineData(typeof(Int32[]), true)]
    [InlineData(typeof(String[]), true)]
    [InlineData(typeof(Int32), false)]
    [InlineData(typeof(DateTime), false)]
    public void IsReferenceTypeOrNullableType_ShouldDetermineWhetherTypeIsReferenceTypeOrNullableType(
        Type type,
        Boolean expectedResult
    ) =>
        type.IsReferenceTypeOrNullableType()
            .Should().Be(expectedResult);

    [Theory]
    [InlineData(typeof(ValueTuple<Int32>), true)]
    [InlineData(typeof(ValueTuple<Int32, Int32>), true)]
    [InlineData(typeof(ValueTuple<Int32, Int32, Int32>), true)]
    [InlineData(typeof(ValueTuple<Int32, Int32, Int32, Int32>), true)]
    [InlineData(typeof(ValueTuple<Int32, Int32, Int32, Int32, Int32>), true)]
    [InlineData(typeof(ValueTuple<Int32, Int32, Int32, Int32, Int32, Int32>), true)]
    [InlineData(typeof(ValueTuple<Int32, Int32, Int32, Int32, Int32, Int32, Int32>), true)]
    [InlineData(typeof(ValueTuple<Int32, Int32, Int32, Int32, Int32, Int32, Int32, Int32>), true)]
    [InlineData(typeof(DateTime), false)]
    [InlineData(typeof(Entity), false)]
    [InlineData(typeof(Tuple<Int32>), false)]
    [InlineData(typeof(Tuple<Int32, Int32>), false)]
    public void IsValueTupleType_ShouldDetermineWhetherTypeIsValueTupleType(
        Type type,
        Boolean expectedResult
    ) =>
        type.IsValueTupleType()
            .Should().Be(expectedResult);

    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        ArgumentNullGuardVerifier.Verify(() => TypeExtensions.IsBuiltInTypeOrNullableBuiltInType(typeof(DateTime)));
        ArgumentNullGuardVerifier.Verify(() => TypeExtensions.IsCharOrNullableCharType(typeof(Char)));
        ArgumentNullGuardVerifier.Verify(() => TypeExtensions.IsEnumOrNullableEnumType(typeof(TestEnum)));
        ArgumentNullGuardVerifier.Verify(() => TypeExtensions.IsReferenceTypeOrNullableType(typeof(Entity)));
        ArgumentNullGuardVerifier.Verify(() => TypeExtensions.IsValueTupleType(typeof(ValueTuple<Int32>)));
    }
}
