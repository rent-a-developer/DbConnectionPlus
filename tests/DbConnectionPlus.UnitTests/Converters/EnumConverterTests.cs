using RentADeveloper.DbConnectionPlus.Converters;

namespace RentADeveloper.DbConnectionPlus.UnitTests.Converters;

public class EnumConverterTests : UnitTestsBase
{
    [Fact]
    public void ConvertValueToEnumMember_EmptyStringValue_ShouldThrow() =>
        Invoking(() => EnumConverter.ConvertValueToEnumMember<TestEnum>(String.Empty))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert an empty string or a string that consists only of white-space characters to an " +
                $"enum member of the type {typeof(TestEnum)}."
            );

    [Fact]
    public void ConvertValueToEnumMember_NonEnumTargetType_ShouldThrow()
    {
        Invoking(() => EnumConverter.ConvertValueToEnumMember<Int32>("ValueA"))
            .Should().Throw<ArgumentException>()
            .WithMessage(
                $"Could not convert the value 'ValueA' ({typeof(String)}) to an enum member of the type " +
                $"{typeof(Int32)}, because the type {typeof(Int32)} is not an enum type.*"
            );

        Invoking(() => EnumConverter.ConvertValueToEnumMember<Int32?>("ValueA"))
            .Should().Throw<ArgumentException>()
            .WithMessage(
                $"Could not convert the value 'ValueA' ({typeof(String)}) to an enum member of the type " +
                $"{typeof(Int32?)}, because the type {typeof(Int32?)} is not an enum type.*"
            );
    }

    [Fact]
    public void ConvertValueToEnumMember_NonNullableTargetType_NullOrDBNullValue_ShouldThrow()
    {
        Invoking(() => EnumConverter.ConvertValueToEnumMember<TestEnum>(DBNull.Value))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert {{null}} to an enum member of the type {typeof(TestEnum)}."
            );

        Invoking(() => EnumConverter.ConvertValueToEnumMember<TestEnum>(null))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert {{null}} to an enum member of the type {typeof(TestEnum)}."
            );
    }

    [Fact]
    public void ConvertValueToEnumMember_NullableTargetType_NullOrDBNullValue_ShouldReturnNull()
    {
        EnumConverter.ConvertValueToEnumMember<TestEnum?>(DBNull.Value)
            .Should().BeNull();

        EnumConverter.ConvertValueToEnumMember<TestEnum?>(null)
            .Should().BeNull();
    }

    [Fact]
    public void ConvertValueToEnumMember_NumericValueNotMatchingAnyEnumMemberValue_ShouldThrow() =>
        Invoking(() => EnumConverter.ConvertValueToEnumMember<TestEnum>(999))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value '999' ({typeof(Int32)}) to an enum member of the type " +
                $"{typeof(TestEnum)}. That value does not match any of the values of the enum's members."
            );

    [Theory]
    [MemberData(nameof(GetConvertValueToEnumMemberTestData))]
    public void
        ConvertValueToEnumMember_ShouldConvertValueToEnumMember(Object value, TestEnum expectedResult)
    {
        EnumConverter.ConvertValueToEnumMember<TestEnum>(value)
            .Should().Be(expectedResult);

        EnumConverter.ConvertValueToEnumMember<TestEnum?>(value)
            .Should().Be(expectedResult);
    }

    [Fact]
    public void ConvertValueToEnumMember_StringValueNotMatchingAnyEnumMemberName_ShouldThrow() =>
        Invoking(() => EnumConverter.ConvertValueToEnumMember<TestEnum>("NonExistent"))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'NonExistent' to an enum member of the type {typeof(TestEnum)}. " +
                $"That string does not match any of the names of the enum's members."
            );

    [Fact]
    public void ConvertValueToEnumMember_ValueIsNeitherEnumValueNorStringNorNumeric_ShouldThrow() =>
        Invoking(() => EnumConverter.ConvertValueToEnumMember<TestEnum>(Guid.Empty))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value '{Guid.Empty}' ({typeof(Guid)}) to an enum member of the type " +
                $"{typeof(TestEnum)}. The value must either be an enum value of that type or a string or a numeric " +
                $"value."
            );

    [Fact]
    public void ConvertValueToEnumMember_ValueIsOfDifferentEnumType_ShouldThrow() =>
        Invoking(() => EnumConverter.ConvertValueToEnumMember<TestEnum>(ConsoleColor.Red))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value 'Red' ({typeof(ConsoleColor)}) to an enum member of the type " +
                $"{typeof(TestEnum)}. The value must either be an enum value of that type or a string or a numeric " +
                $"value."
            );

    [Fact]
    public void ConvertValueToEnumMember_WhitespaceStringValue_ShouldThrow() =>
        Invoking(() => EnumConverter.ConvertValueToEnumMember<TestEnum>("   "))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert an empty string or a string that consists only of white-space characters to an " +
                $"enum member of the type {typeof(TestEnum)}."
            );

    public static IEnumerable<(Object value, TestEnum expectedResult)> GetConvertValueToEnumMemberTestData() =>
    [
        ((Int16)1, TestEnum.Value1),
        ((Int16)2, TestEnum.Value2),
        ((Int16)3, TestEnum.Value3),
        ((Int16)4, TestEnum.Value4),
        ((Int16)5, TestEnum.Value5),
        (1, TestEnum.Value1),
        (2, TestEnum.Value2),
        (3, TestEnum.Value3),
        (4, TestEnum.Value4),
        (5, TestEnum.Value5),
        (1L, TestEnum.Value1),
        (2L, TestEnum.Value2),
        (3L, TestEnum.Value3),
        (4L, TestEnum.Value4),
        (5L, TestEnum.Value5),
        ((Byte)1, TestEnum.Value1),
        ((Byte)2, TestEnum.Value2),
        ((Byte)3, TestEnum.Value3),
        ((Byte)4, TestEnum.Value4),
        ((Byte)5, TestEnum.Value5),
        ((Single)1.0, TestEnum.Value1),
        ((Single)2.0, TestEnum.Value2),
        ((Single)3.0, TestEnum.Value3),
        ((Single)4.0, TestEnum.Value4),
        ((Single)5.0, TestEnum.Value5),
        (1.0, TestEnum.Value1),
        (2.0, TestEnum.Value2),
        (3.0, TestEnum.Value3),
        (4.0, TestEnum.Value4),
        (5.0, TestEnum.Value5),
        ((Decimal)1.0, TestEnum.Value1),
        ((Decimal)2.0, TestEnum.Value2),
        ((Decimal)3.0, TestEnum.Value3),
        ((Decimal)4.0, TestEnum.Value4),
        ((Decimal)5.0, TestEnum.Value5),
        ("Value1", TestEnum.Value1),
        ("Value2", TestEnum.Value2),
        ("Value3", TestEnum.Value3),
        ("Value4", TestEnum.Value4),
        ("Value5", TestEnum.Value5),
        ("VALUE1", TestEnum.Value1),
        ("VALUE2", TestEnum.Value2),
        ("VALUE3", TestEnum.Value3),
        ("VALUE4", TestEnum.Value4),
        ("VALUE5", TestEnum.Value5),
        ("1", TestEnum.Value1),
        ("2", TestEnum.Value2),
        ("3", TestEnum.Value3),
        ("4", TestEnum.Value4),
        ("5", TestEnum.Value5),
        (TestEnum.Value1, TestEnum.Value1),
        (TestEnum.Value2, TestEnum.Value2),
        (TestEnum.Value3, TestEnum.Value3),
        (TestEnum.Value4, TestEnum.Value4),
        (TestEnum.Value5, TestEnum.Value5)
    ];
}
