using RentADeveloper.DbConnectionPlus.Converters;

namespace RentADeveloper.DbConnectionPlus.UnitTests.Converters;

public class EnumConverterTests : UnitTestsBase
{
    [Fact]
    public void ConvertValueToEnumMember_EmptyStringValue_ShouldThrow() =>
        Invoking(() => EnumConverter.ConvertValueToEnumMember(string.Empty, typeof(TestEnum)))
            .Should()
            .Throw<InvalidCastException>()
            .WithMessage(
                "Could not convert an empty string or a string that consists only of white-space characters to an "
                    + $"enum member of the type {typeof(TestEnum)}."
            );

    [Fact]
    public void ConvertValueToEnumMember_NonEnumTargetType_ShouldThrow()
    {
        Invoking(() => EnumConverter.ConvertValueToEnumMember("ValueA", typeof(int)))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage(
                $"Could not convert the value 'ValueA' ({typeof(string)}) to an enum member of the type "
                    + $"{typeof(int)}, because the type {typeof(int)} is not an enum type.*"
            );

        Invoking(() => EnumConverter.ConvertValueToEnumMember("ValueA", typeof(int?)))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage(
                $"Could not convert the value 'ValueA' ({typeof(string)}) to an enum member of the type "
                    + $"{typeof(int?)}, because the type {typeof(int?)} is not an enum type.*"
            );
    }

    [Fact]
    public void ConvertValueToEnumMember_NonNullableTargetType_NullOrDBNullValue_ShouldThrow()
    {
        Invoking(() => EnumConverter.ConvertValueToEnumMember(DBNull.Value, typeof(TestEnum)))
            .Should()
            .Throw<InvalidCastException>()
            .WithMessage($"Could not convert {{null}} to an enum member of the type {typeof(TestEnum)}.");

        Invoking(() => EnumConverter.ConvertValueToEnumMember(null, typeof(TestEnum)))
            .Should()
            .Throw<InvalidCastException>()
            .WithMessage($"Could not convert {{null}} to an enum member of the type {typeof(TestEnum)}.");
    }

    [Fact]
    public void ConvertValueToEnumMember_NullableTargetType_NullOrDBNullValue_ShouldReturnNull()
    {
        EnumConverter.ConvertValueToEnumMember(DBNull.Value, typeof(TestEnum?)).Should().BeNull();

        EnumConverter.ConvertValueToEnumMember(null, typeof(TestEnum?)).Should().BeNull();
    }

    [Fact]
    public void ConvertValueToEnumMember_NumericValueNotMatchingAnyEnumMemberValue_ShouldThrow() =>
        Invoking(() => EnumConverter.ConvertValueToEnumMember(999, typeof(TestEnum)))
            .Should()
            .Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value '999' ({typeof(int)}) to an enum member of the type "
                    + $"{typeof(TestEnum)}. That value does not match any of the values of the enum's members."
            );

    [Theory]
    [MemberData(nameof(GetConvertValueToEnumMemberTestData))]
    public void ConvertValueToEnumMember_ShouldConvertValueToEnumMember(object value, TestEnum expectedResult)
    {
        EnumConverter.ConvertValueToEnumMember(value, typeof(TestEnum)).Should().Be(expectedResult);

        EnumConverter.ConvertValueToEnumMember(value, typeof(TestEnum?)).Should().Be(expectedResult);
    }

    [Fact]
    public void ConvertValueToEnumMember_StringValueNotMatchingAnyEnumMemberName_ShouldThrow() =>
        Invoking(() => EnumConverter.ConvertValueToEnumMember("NonExistent", typeof(TestEnum)))
            .Should()
            .Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'NonExistent' to an enum member of the type {typeof(TestEnum)}. "
                    + "That string does not match any of the names of the enum's members."
            );

    [Fact]
    public void ConvertValueToEnumMember_ValueIsNeitherEnumValueNorStringNorNumeric_ShouldThrow() =>
        Invoking(() => EnumConverter.ConvertValueToEnumMember(Guid.Empty, typeof(TestEnum)))
            .Should()
            .Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value '{Guid.Empty}' ({typeof(Guid)}) to an enum member of the type "
                    + $"{typeof(TestEnum)}. The value must either be an enum value of that type or a string or a numeric "
                    + "value."
            );

    [Fact]
    public void ConvertValueToEnumMember_ValueIsOfDifferentEnumType_ShouldThrow() =>
        Invoking(() => EnumConverter.ConvertValueToEnumMember(ConsoleColor.Red, typeof(TestEnum)))
            .Should()
            .Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value 'Red' ({typeof(ConsoleColor)}) to an enum member of the type "
                    + $"{typeof(TestEnum)}. The value must either be an enum value of that type or a string or a numeric "
                    + "value."
            );

    [Fact]
    public void ConvertValueToEnumMember_WhitespaceStringValue_ShouldThrow() =>
        Invoking(() => EnumConverter.ConvertValueToEnumMember("   ", typeof(TestEnum)))
            .Should()
            .Throw<InvalidCastException>()
            .WithMessage(
                "Could not convert an empty string or a string that consists only of white-space characters to an "
                    + $"enum member of the type {typeof(TestEnum)}."
            );

    [Fact]
    public void ConvertValueToEnumMemberOfT_EmptyStringValue_ShouldThrow() =>
        Invoking(() => EnumConverter.ConvertValueToEnumMember<TestEnum>(string.Empty))
            .Should()
            .Throw<InvalidCastException>()
            .WithMessage(
                "Could not convert an empty string or a string that consists only of white-space characters to an "
                    + $"enum member of the type {typeof(TestEnum)}."
            );

    [Fact]
    public void ConvertValueToEnumMemberOfT_NonEnumTargetType_ShouldThrow()
    {
        Invoking(() => EnumConverter.ConvertValueToEnumMember<int>("ValueA"))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage(
                $"Could not convert the value 'ValueA' ({typeof(string)}) to an enum member of the type "
                    + $"{typeof(int)}, because the type {typeof(int)} is not an enum type.*"
            );

        Invoking(() => EnumConverter.ConvertValueToEnumMember<int?>("ValueA"))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage(
                $"Could not convert the value 'ValueA' ({typeof(string)}) to an enum member of the type "
                    + $"{typeof(int?)}, because the type {typeof(int?)} is not an enum type.*"
            );
    }

    [Fact]
    public void ConvertValueToEnumMemberOfT_NonNullableTargetType_NullOrDBNullValue_ShouldThrow()
    {
        Invoking(() => EnumConverter.ConvertValueToEnumMember<TestEnum>(DBNull.Value))
            .Should()
            .Throw<InvalidCastException>()
            .WithMessage($"Could not convert {{null}} to an enum member of the type {typeof(TestEnum)}.");

        Invoking(() => EnumConverter.ConvertValueToEnumMember<TestEnum>(null))
            .Should()
            .Throw<InvalidCastException>()
            .WithMessage($"Could not convert {{null}} to an enum member of the type {typeof(TestEnum)}.");
    }

    [Fact]
    public void ConvertValueToEnumMemberOfT_NullableTargetType_NullOrDBNullValue_ShouldReturnNull()
    {
        EnumConverter.ConvertValueToEnumMember<TestEnum?>(DBNull.Value).Should().BeNull();

        EnumConverter.ConvertValueToEnumMember<TestEnum?>(null).Should().BeNull();
    }

    [Fact]
    public void ConvertValueToEnumMemberOfT_NumericValueNotMatchingAnyEnumMemberValue_ShouldThrow() =>
        Invoking(() => EnumConverter.ConvertValueToEnumMember<TestEnum>(999))
            .Should()
            .Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value '999' ({typeof(int)}) to an enum member of the type "
                    + $"{typeof(TestEnum)}. That value does not match any of the values of the enum's members."
            );

    [Theory]
    [MemberData(nameof(GetConvertValueToEnumMemberTestData))]
    public void ConvertValueToEnumMemberOfT_ShouldConvertValueToEnumMember(object value, TestEnum expectedResult)
    {
        EnumConverter.ConvertValueToEnumMember<TestEnum>(value).Should().Be(expectedResult);

        EnumConverter.ConvertValueToEnumMember<TestEnum?>(value).Should().Be(expectedResult);
    }

    [Fact]
    public void ConvertValueToEnumMemberOfT_StringValueNotMatchingAnyEnumMemberName_ShouldThrow() =>
        Invoking(() => EnumConverter.ConvertValueToEnumMember<TestEnum>("NonExistent"))
            .Should()
            .Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'NonExistent' to an enum member of the type {typeof(TestEnum)}. "
                    + "That string does not match any of the names of the enum's members."
            );

    [Fact]
    public void ConvertValueToEnumMemberOfT_ValueIsNeitherEnumValueNorStringNorNumeric_ShouldThrow() =>
        Invoking(() => EnumConverter.ConvertValueToEnumMember<TestEnum>(Guid.Empty))
            .Should()
            .Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value '{Guid.Empty}' ({typeof(Guid)}) to an enum member of the type "
                    + $"{typeof(TestEnum)}. The value must either be an enum value of that type or a string or a numeric "
                    + "value."
            );

    [Fact]
    public void ConvertValueToEnumMemberOfT_ValueIsOfDifferentEnumType_ShouldThrow() =>
        Invoking(() => EnumConverter.ConvertValueToEnumMember<TestEnum>(ConsoleColor.Red))
            .Should()
            .Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value 'Red' ({typeof(ConsoleColor)}) to an enum member of the type "
                    + $"{typeof(TestEnum)}. The value must either be an enum value of that type or a string or a numeric "
                    + "value."
            );

    [Fact]
    public void ConvertValueToEnumMemberOfT_WhitespaceStringValue_ShouldThrow() =>
        Invoking(() => EnumConverter.ConvertValueToEnumMember<TestEnum>("   "))
            .Should()
            .Throw<InvalidCastException>()
            .WithMessage(
                "Could not convert an empty string or a string that consists only of white-space characters to an "
                    + $"enum member of the type {typeof(TestEnum)}."
            );

    public static IEnumerable<(object value, TestEnum expectedResult)> GetConvertValueToEnumMemberTestData() =>
        [
            ((short)1, TestEnum.Value1),
            ((short)2, TestEnum.Value2),
            ((short)3, TestEnum.Value3),
            ((short)4, TestEnum.Value4),
            ((short)5, TestEnum.Value5),
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
            ((byte)1, TestEnum.Value1),
            ((byte)2, TestEnum.Value2),
            ((byte)3, TestEnum.Value3),
            ((byte)4, TestEnum.Value4),
            ((byte)5, TestEnum.Value5),
            ((float)1.0, TestEnum.Value1),
            ((float)2.0, TestEnum.Value2),
            ((float)3.0, TestEnum.Value3),
            ((float)4.0, TestEnum.Value4),
            ((float)5.0, TestEnum.Value5),
            (1.0, TestEnum.Value1),
            (2.0, TestEnum.Value2),
            (3.0, TestEnum.Value3),
            (4.0, TestEnum.Value4),
            (5.0, TestEnum.Value5),
            ((decimal)1.0, TestEnum.Value1),
            ((decimal)2.0, TestEnum.Value2),
            ((decimal)3.0, TestEnum.Value3),
            ((decimal)4.0, TestEnum.Value4),
            ((decimal)5.0, TestEnum.Value5),
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
            (TestEnum.Value5, TestEnum.Value5),
        ];
}
