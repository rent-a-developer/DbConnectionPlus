using RentADeveloper.DbConnectionPlus.Converters;

namespace RentADeveloper.DbConnectionPlus.UnitTests.Converters;

public class EnumSerializerTests : UnitTestsBase
{
    [Fact]
    public void SerializeEnum_InvalidEnumSerializationMode_ShouldThrow() =>
        Invoking(() => EnumSerializer.SerializeEnum(TestEnum.Value3, (EnumSerializationMode)999))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage(
                $"The {nameof(EnumSerializationMode)} '999' ({typeof(EnumSerializationMode)}) is not supported.*"
            );

    [Theory]
    [InlineData(TestEnum.Value1, EnumSerializationMode.Strings, "Value1")]
    [InlineData(TestEnum.Value2, EnumSerializationMode.Strings, "Value2")]
    [InlineData(TestEnum.Value3, EnumSerializationMode.Strings, "Value3")]
    [InlineData(TestEnum.Value4, EnumSerializationMode.Strings, "Value4")]
    [InlineData(TestEnum.Value5, EnumSerializationMode.Strings, "Value5")]
    [InlineData(TestEnum.Value1, EnumSerializationMode.Integers, 1)]
    [InlineData(TestEnum.Value2, EnumSerializationMode.Integers, 2)]
    [InlineData(TestEnum.Value3, EnumSerializationMode.Integers, 3)]
    [InlineData(TestEnum.Value4, EnumSerializationMode.Integers, 4)]
    [InlineData(TestEnum.Value5, EnumSerializationMode.Integers, 5)]
    public void SerializeEnum_ShouldSerializeEnumValueAccordingToSerializationMode(
        TestEnum enumValue,
        EnumSerializationMode enumSerializationMode,
        Object expectedResult
    ) =>
        EnumSerializer.SerializeEnum(enumValue, enumSerializationMode)
            .Should().Be(expectedResult);

    [Fact]
    public void ShouldGuardAgainstNullArguments() =>
        ArgumentNullGuardVerifier.Verify(() =>
            EnumSerializer.SerializeEnum(TestEnum.Value3, EnumSerializationMode.Strings)
        );
}
