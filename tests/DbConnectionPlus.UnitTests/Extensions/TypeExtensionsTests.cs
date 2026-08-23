// ReSharper disable InvokeAsExtensionMethod

using RentADeveloper.DbConnectionPlus.Extensions;
using TypeExtensions = RentADeveloper.DbConnectionPlus.Extensions.TypeExtensions;

namespace RentADeveloper.DbConnectionPlus.UnitTests.Extensions;

public class TypeExtensionsTests : UnitTestsBase
{
    [Theory]
    [InlineData(typeof(bool), true)]
    [InlineData(typeof(bool?), true)]
    [InlineData(typeof(byte), true)]
    [InlineData(typeof(byte?), true)]
    [InlineData(typeof(char), true)]
    [InlineData(typeof(char?), true)]
    [InlineData(typeof(DateOnly), true)]
    [InlineData(typeof(DateOnly?), true)]
    [InlineData(typeof(DateTime), true)]
    [InlineData(typeof(DateTime?), true)]
    [InlineData(typeof(DateTimeOffset), true)]
    [InlineData(typeof(DateTimeOffset?), true)]
    [InlineData(typeof(decimal), true)]
    [InlineData(typeof(decimal?), true)]
    [InlineData(typeof(double), true)]
    [InlineData(typeof(double?), true)]
    [InlineData(typeof(Guid), true)]
    [InlineData(typeof(Guid?), true)]
    [InlineData(typeof(short), true)]
    [InlineData(typeof(short?), true)]
    [InlineData(typeof(int), true)]
    [InlineData(typeof(int?), true)]
    [InlineData(typeof(long), true)]
    [InlineData(typeof(long?), true)]
    [InlineData(typeof(IntPtr), true)]
    [InlineData(typeof(IntPtr?), true)]
    [InlineData(typeof(sbyte), true)]
    [InlineData(typeof(sbyte?), true)]
    [InlineData(typeof(float), true)]
    [InlineData(typeof(float?), true)]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(TimeOnly), true)]
    [InlineData(typeof(TimeOnly?), true)]
    [InlineData(typeof(TimeSpan), true)]
    [InlineData(typeof(TimeSpan?), true)]
    [InlineData(typeof(ushort), true)]
    [InlineData(typeof(ushort?), true)]
    [InlineData(typeof(uint), true)]
    [InlineData(typeof(uint?), true)]
    [InlineData(typeof(ulong), true)]
    [InlineData(typeof(ulong?), true)]
    [InlineData(typeof(UIntPtr), true)]
    [InlineData(typeof(UIntPtr?), true)]
    [InlineData(typeof(Entity), false)]
    [InlineData(typeof(TestEnum), false)]
    public void IsBuiltInTypeOrNullableBuiltInType_ShouldDetermineWhetherTypeIsBuiltInTypeOrNullableBuiltInType(
        Type type,
        bool expectedResult
    ) => type.IsBuiltInTypeOrNullableBuiltInType().Should().Be(expectedResult);

    [Theory]
    [InlineData(typeof(char), true)]
    [InlineData(typeof(char?), true)]
    [InlineData(typeof(string), false)]
    [InlineData(typeof(DateTime), false)]
    [InlineData(typeof(Entity), false)]
    public void IsCharOrNullableCharType_ShouldDetermineWhetherTypeIsCharOrNullableCharType(
        Type type,
        bool expectedResult
    ) => type.IsCharOrNullableCharType().Should().Be(expectedResult);

    [Theory]
    [InlineData(typeof(TestEnum), true)]
    [InlineData(typeof(TestEnum?), true)]
    [InlineData(typeof(ConsoleColor), true)]
    [InlineData(typeof(ConsoleColor?), true)]
    [InlineData(typeof(string), false)]
    [InlineData(typeof(DateTime), false)]
    [InlineData(typeof(Entity), false)]
    public void IsEnumOrNullableEnumType_ShouldDetermineWhetherTypeIsEnumTypeOrNullableEnumType(
        Type type,
        bool expectedResult
    ) => type.IsEnumOrNullableEnumType().Should().Be(expectedResult);

    [Theory]
    [InlineData(typeof(int?), true)]
    [InlineData(typeof(DateTime?), true)]
    [InlineData(typeof(object), true)]
    [InlineData(typeof(Entity), true)]
    [InlineData(typeof(int[]), true)]
    [InlineData(typeof(string[]), true)]
    [InlineData(typeof(int), false)]
    [InlineData(typeof(DateTime), false)]
    public void IsReferenceTypeOrNullableType_ShouldDetermineWhetherTypeIsReferenceTypeOrNullableType(
        Type type,
        bool expectedResult
    ) => type.IsReferenceTypeOrNullableType().Should().Be(expectedResult);

    [Theory]
    [InlineData(typeof(ValueTuple<int>), true)]
    [InlineData(typeof(ValueTuple<int, int>), true)]
    [InlineData(typeof(ValueTuple<int, int, int>), true)]
    [InlineData(typeof(ValueTuple<int, int, int, int>), true)]
    [InlineData(typeof(ValueTuple<int, int, int, int, int>), true)]
    [InlineData(typeof(ValueTuple<int, int, int, int, int, int>), true)]
    [InlineData(typeof(ValueTuple<int, int, int, int, int, int, int>), true)]
    [InlineData(typeof(ValueTuple<int, int, int, int, int, int, int, int>), true)]
    [InlineData(typeof(DateTime), false)]
    [InlineData(typeof(Entity), false)]
    [InlineData(typeof(Tuple<int>), false)]
    [InlineData(typeof(Tuple<int, int>), false)]
    public void IsValueTupleType_ShouldDetermineWhetherTypeIsValueTupleType(Type type, bool expectedResult) =>
        type.IsValueTupleType().Should().Be(expectedResult);

    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        ArgumentNullGuardVerifier.Verify(() => TypeExtensions.IsBuiltInTypeOrNullableBuiltInType(typeof(DateTime)));
        ArgumentNullGuardVerifier.Verify(() => TypeExtensions.IsCharOrNullableCharType(typeof(char)));
        ArgumentNullGuardVerifier.Verify(() => TypeExtensions.IsEnumOrNullableEnumType(typeof(TestEnum)));
        ArgumentNullGuardVerifier.Verify(() => TypeExtensions.IsReferenceTypeOrNullableType(typeof(Entity)));
        ArgumentNullGuardVerifier.Verify(() => TypeExtensions.IsValueTupleType(typeof(ValueTuple<int>)));
    }
}
