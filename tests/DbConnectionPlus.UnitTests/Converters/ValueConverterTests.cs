// ReSharper disable SpecifyACultureInStringConversionExplicitly

#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
#pragma warning disable IDE0004

using System.Globalization;
using System.Reflection;
using Bogus;
using RentADeveloper.DbConnectionPlus.Converters;
using RentADeveloper.DbConnectionPlus.Extensions;
using RentADeveloper.DbConnectionPlus.Materializers;

namespace RentADeveloper.DbConnectionPlus.UnitTests.Converters;

public class ValueConverterTests : UnitTestsBase
{
    [Theory]
    [MemberData(nameof(GetConvertTestData))]
    public void CanConvert_NullableSourceType_ShouldDetermineIfConversionIsPossible(
        Type sourceType,
        Type targetType,
        Boolean expectedCanConvert,
        Object? sourceValue,
        Object? expectedTargetValue
    )
    {
        Assert.SkipUnless(sourceType.IsValueType, "");

        sourceType = typeof(Nullable<>).MakeGenericType(sourceType);
        sourceValue = Activator.CreateInstance(sourceType, sourceValue);

        this.CanConvert_ShouldDetermineIfConversionIsPossible(
            sourceType,
            targetType,
            expectedCanConvert,
            sourceValue,
            expectedTargetValue
        );
    }

    [Theory]
    [MemberData(nameof(GetConvertTestData))]
    public void CanConvert_NullableTargetType_ShouldDetermineIfConversionIsPossible(
        Type sourceType,
        Type targetType,
        Boolean expectedCanConvert,
        Object? sourceValue,
        Object? expectedTargetValue
    )
    {
        Assert.SkipUnless(targetType.IsValueType, "");

        targetType = typeof(Nullable<>).MakeGenericType(targetType);
        expectedTargetValue = Activator.CreateInstance(targetType, expectedTargetValue);

        this.CanConvert_ShouldDetermineIfConversionIsPossible(
            sourceType,
            targetType,
            expectedCanConvert,
            sourceValue,
            expectedTargetValue
        );
    }

    [Theory]
    [MemberData(nameof(GetConvertTestData))]
    public void CanConvert_ShouldDetermineIfConversionIsPossible(
        Type sourceType,
        Type targetType,
        Boolean expectedCanConvert,
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
#pragma warning disable RCS1163 // Unused parameter
        Object? sourceValue,
        Object? expectedTargetValue
#pragma warning restore RCS1163 // Unused parameter
#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
    ) =>
        ValueConverter.CanConvert(sourceType, targetType)
            .Should().Be(
                expectedCanConvert,
                $"{sourceType} should {(expectedCanConvert ? "" : "not ")}be convertible to {targetType}"
            );

    [Fact]
    public void ConvertValueToType_CharTargetType_StringWithLengthOneValue_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<Char>();

        ValueConverter.ConvertValueToType(character.ToString(), typeof(Char))
            .Should().Be(character);

        ValueConverter.ConvertValueToType(character.ToString(), typeof(Char?))
            .Should().Be(character);
    }

    [Fact]
    public void ConvertValueToType_CharTargetType_ValueIsStringWithLengthNotOne_ShouldThrow()
    {
        Invoking(() => ValueConverter.ConvertValueToType(String.Empty, typeof(Char)))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string '' to the type {typeof(Char)}. The string must be exactly one " +
                "character long."
            );

        Invoking(() => ValueConverter.ConvertValueToType(String.Empty, typeof(Char?)))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string '' to the type {typeof(Char?)}. The string must be exactly one " +
                "character long."
            );

        Invoking(() => ValueConverter.ConvertValueToType("ab", typeof(Char)))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(Char)}. The string must be exactly one " +
                "character long."
            );

        Invoking(() => ValueConverter.ConvertValueToType("ab", typeof(Char?)))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(Char?)}. The string must be exactly one " +
                "character long."
            );
    }

    [Fact]
    public void
        ConvertValueToType_EnumTargetType_IntegerValueNotMatchingAnyEnumMemberValue_ShouldThrow()
    {
        Invoking(() => ValueConverter.ConvertValueToType(999, typeof(TestEnum)))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value '999' ({typeof(Int32)}) to an enum member of the type " +
                $"{typeof(TestEnum)}. That value does not match any of the values of the enum's members.*"
            );

        Invoking(() => ValueConverter.ConvertValueToType(999, typeof(TestEnum?)))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value '999' ({typeof(Int32)}) to an enum member of the type " +
                $"{typeof(TestEnum?)}. That value does not match any of the values of the enum's members.*"
            );
    }

    [Fact]
    public void ConvertValueToType_EnumTargetType_ShouldConvertToEnumMember()
    {
        var enumValue = Generate.Single<TestEnum>();

        ValueConverter.ConvertValueToType((Int32)enumValue, typeof(TestEnum))
            .Should().Be(enumValue);

        ValueConverter.ConvertValueToType((Int32)enumValue, typeof(TestEnum?))
            .Should().Be(enumValue);
    }

    [Fact]
    public void
        ConvertValueToType_EnumTargetType_StringValueNotMatchingAnyEnumMemberName_ShouldThrow()
    {
        Invoking(() => ValueConverter.ConvertValueToType("NonExistent", typeof(TestEnum)))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'NonExistent' to an enum member of the type {typeof(TestEnum)}. " +
                "That string does not match any of the names of the enum's members.*"
            );

        Invoking(() => ValueConverter.ConvertValueToType("NonExistent", typeof(TestEnum?)))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'NonExistent' to an enum member of the type {typeof(TestEnum?)}. " +
                "That string does not match any of the names of the enum's members.*"
            );
    }

    [Fact]
    public void ConvertValueToType_NonNullableTargetType_NullOrDBNullValue_ShouldThrow()
    {
        Invoking(() => ValueConverter.ConvertValueToType(DBNull.Value, typeof(DateTime)))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value {{DBNull}} to the type {typeof(DateTime)}, because the " +
                "type is non-nullable.*"
            );

        Invoking(() => ValueConverter.ConvertValueToType(null, typeof(DateTime)))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value {{null}} to the type {typeof(DateTime)}, because the type is " +
                "non-nullable.*"
            );
    }

    [Theory]
    [MemberData(nameof(GetConvertTestData))]
    public void ConvertValueToType_NullableSourceType_ShouldConvertValueToTargetType(
        Type sourceType,
        Type targetType,
        Boolean expectedCanConvert,
        Object? sourceValue,
        Object? expectedTargetValue
    )
    {
        Assert.SkipUnless(sourceType.IsValueType, "");

        sourceType = typeof(Nullable<>).MakeGenericType(sourceType);
        sourceValue = Activator.CreateInstance(sourceType, sourceValue);

        this.ConvertValueToType_ShouldConvertValueToType(
            sourceType,
            targetType,
            expectedCanConvert,
            sourceValue,
            expectedTargetValue
        );
    }

    [Fact]
    public void ConvertValueToType_NullableTargetType_NullOrDBNullValue_ShouldReturnNull()
    {
        ValueConverter.ConvertValueToType(DBNull.Value, typeof(Object))
            .Should().BeNull();

        ValueConverter.ConvertValueToType(DBNull.Value, typeof(Int32?))
            .Should().BeNull();

        ValueConverter.ConvertValueToType(null, typeof(Object))
            .Should().BeNull();

        ValueConverter.ConvertValueToType(null, typeof(Int32?))
            .Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(GetConvertTestData))]
    public void ConvertValueToType_NullableTargetType_ShouldConvertValueToTargetType(
        Type sourceType,
        Type targetType,
        Boolean expectedCanConvert,
        Object? sourceValue,
        Object? expectedTargetValue
    )
    {
        Assert.SkipUnless(targetType.IsValueType, "");

        targetType = typeof(Nullable<>).MakeGenericType(targetType);
        expectedTargetValue = Activator.CreateInstance(targetType, expectedTargetValue);

        this.ConvertValueToType_ShouldConvertValueToType(
            sourceType,
            targetType,
            expectedCanConvert,
            sourceValue,
            expectedTargetValue
        );
    }

    [Theory]
    [MemberData(nameof(GetConvertTestData))]
    public void ConvertValueToType_ShouldConvertValueToType(
        Type _,
        Type targetType,
        Boolean expectedCanConvert,
        Object? sourceValue,
        Object? expectedTargetValue
    )
    {
        if (expectedCanConvert)
        {
            var result = ValueConverter.ConvertValueToType(sourceValue, targetType);

            if (result is Byte[] resultBytes && expectedTargetValue is Byte[] expectedTargetValueBytes)
            {
                resultBytes
                    .Should().BeEquivalentTo(
                        expectedTargetValueBytes,
                        $"{sourceValue.ToDebugString()} converted to {targetType} should be " +
                        $"{expectedTargetValue.ToDebugString()}"
                    );
            }
            else
            {
                result
                    .Should().Be(
                        expectedTargetValue,
                        $"{sourceValue.ToDebugString()} converted to {targetType} should be " +
                        $"{expectedTargetValue.ToDebugString()}"
                    );
            }
        }
        else
        {
            Invoking(() => ValueConverter.ConvertValueToType(sourceValue, targetType))
                .Should().Throw<InvalidCastException>()
                .WithMessage(
                    $"Could not convert the value {sourceValue.ToDebugString()} to the type {targetType}.*"
                );
        }
    }

    [Fact]
    public void ConvertValueToType_ValueCannotBeConvertedToTargetType_ShouldThrow() =>
        Invoking(() => ValueConverter.ConvertValueToType("NotADate", typeof(DateTime)))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value 'NotADate' ({typeof(String)}) to the type {typeof(DateTime)}. See " +
                "inner exception for details.*"
            )
            .WithInnerException<FormatException>()
            .WithMessage("The string 'NotADate' was not recognized as a valid DateTime.*");

    [Fact]
    public void ConvertValueToTypeOfT_CharTargetType_StringWithLengthOneValue_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<Char>();

        ValueConverter.ConvertValueToType<Char>(character.ToString())
            .Should().Be(character);

        ValueConverter.ConvertValueToType<Char?>(character.ToString())
            .Should().Be(character);
    }

    [Fact]
    public void ConvertValueToTypeOfT_CharTargetType_ValueIsStringWithLengthNotOne_ShouldThrow()
    {
        Invoking(() => ValueConverter.ConvertValueToType<Char>(String.Empty))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string '' to the type {typeof(Char)}. The string must be exactly one " +
                "character long."
            );

        Invoking(() => ValueConverter.ConvertValueToType<Char?>(String.Empty))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string '' to the type {typeof(Char?)}. The string must be exactly one " +
                "character long."
            );

        Invoking(() => ValueConverter.ConvertValueToType<Char>("ab"))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(Char)}. The string must be exactly one " +
                "character long."
            );

        Invoking(() => ValueConverter.ConvertValueToType<Char?>("ab"))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(Char?)}. The string must be exactly one " +
                "character long."
            );
    }

    [Fact]
    public void
        ConvertValueToTypeOfT_EnumTargetType_IntegerValueNotMatchingAnyEnumMemberValue_ShouldThrow()
    {
        Invoking(() => ValueConverter.ConvertValueToType<TestEnum>(999))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value '999' ({typeof(Int32)}) to an enum member of the type " +
                $"{typeof(TestEnum)}. That value does not match any of the values of the enum's members.*"
            );

        Invoking(() => ValueConverter.ConvertValueToType<TestEnum?>(999))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value '999' ({typeof(Int32)}) to an enum member of the type " +
                $"{typeof(TestEnum?)}. That value does not match any of the values of the enum's members.*"
            );
    }

    [Fact]
    public void ConvertValueToTypeOfT_EnumTargetType_ShouldConvertToEnumMember()
    {
        var enumValue = Generate.Single<TestEnum>();

        ValueConverter.ConvertValueToType<TestEnum>((Int32)enumValue)
            .Should().Be(enumValue);

        ValueConverter.ConvertValueToType<TestEnum?>((Int32)enumValue)
            .Should().Be(enumValue);
    }

    [Fact]
    public void
        ConvertValueToTypeOfT_EnumTargetType_StringValueNotMatchingAnyEnumMemberName_ShouldThrow()
    {
        Invoking(() => ValueConverter.ConvertValueToType<TestEnum>("NonExistent"))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'NonExistent' to an enum member of the type {typeof(TestEnum)}. " +
                "That string does not match any of the names of the enum's members.*"
            );

        Invoking(() => ValueConverter.ConvertValueToType<TestEnum?>("NonExistent"))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'NonExistent' to an enum member of the type {typeof(TestEnum?)}. " +
                "That string does not match any of the names of the enum's members.*"
            );
    }

    [Fact]
    public void ConvertValueToTypeOfT_NonNullableTargetType_NullOrDBNullValue_ShouldThrow()
    {
        Invoking(() => ValueConverter.ConvertValueToType<DateTime>(DBNull.Value))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value {{DBNull}} to the type {typeof(DateTime)}, because the " +
                "type is non-nullable.*"
            );

        Invoking(() => ValueConverter.ConvertValueToType<DateTime>(null))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value {{null}} to the type {typeof(DateTime)}, because the type is " +
                "non-nullable.*"
            );
    }

    [Theory]
    [MemberData(nameof(GetConvertTestData))]
    public void ConvertValueToTypeOfT_NullableSourceType_ShouldConvertValueToTargetType(
        Type sourceType,
        Type targetType,
        Boolean expectedCanConvert,
        Object? sourceValue,
        Object? expectedTargetValue
    )
    {
        Assert.SkipUnless(sourceType.IsValueType, "");

        sourceType = typeof(Nullable<>).MakeGenericType(sourceType);
        sourceValue = Activator.CreateInstance(sourceType, sourceValue);

        this.ConvertValueToTypeOfT_ShouldConvertValueToType(
            sourceType,
            targetType,
            expectedCanConvert,
            sourceValue,
            expectedTargetValue
        );
    }

    [Fact]
    public void ConvertValueToTypeOfT_NullableTargetType_NullOrDBNullValue_ShouldReturnNull()
    {
        ValueConverter.ConvertValueToType<Object>(DBNull.Value)
            .Should().BeNull();

        ValueConverter.ConvertValueToType<Int32?>(DBNull.Value)
            .Should().BeNull();

        ValueConverter.ConvertValueToType<Object>(null)
            .Should().BeNull();

        ValueConverter.ConvertValueToType<Int32?>(null)
            .Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(GetConvertTestData))]
    public void ConvertValueToTypeOfT_NullableTargetType_ShouldConvertValueToTargetType(
        Type sourceType,
        Type targetType,
        Boolean expectedCanConvert,
        Object? sourceValue,
        Object? expectedTargetValue
    )
    {
        Assert.SkipUnless(targetType.IsValueType, "");

        targetType = typeof(Nullable<>).MakeGenericType(targetType);
        expectedTargetValue = Activator.CreateInstance(targetType, expectedTargetValue);

        this.ConvertValueToTypeOfT_ShouldConvertValueToType(
            sourceType,
            targetType,
            expectedCanConvert,
            sourceValue,
            expectedTargetValue
        );
    }

    [Theory]
    [MemberData(nameof(GetConvertTestData))]
    public void ConvertValueToTypeOfT_ShouldConvertValueToType(
        Type _,
        Type targetType,
        Boolean expectedCanConvert,
        Object? sourceValue,
        Object? expectedTargetValue
    )
    {
        if (expectedCanConvert)
        {
            var result = MaterializerFactoryHelper.ValueConverterConvertValueToTypeMethod.MakeGenericMethod(targetType)
                .Invoke(null, [sourceValue]);

            if (result is Byte[] resultBytes && expectedTargetValue is Byte[] expectedTargetValueBytes)
            {
                resultBytes
                    .Should().BeEquivalentTo(
                        expectedTargetValueBytes,
                        $"{sourceValue.ToDebugString()} converted to {targetType} should be " +
                        $"{expectedTargetValue.ToDebugString()}"
                    );
            }
            else
            {
                result
                    .Should().Be(
                        expectedTargetValue,
                        $"{sourceValue.ToDebugString()} converted to {targetType} should be " +
                        $"{expectedTargetValue.ToDebugString()}"
                    );
            }
        }
        else
        {
            Invoking(() =>
                    MaterializerFactoryHelper.ValueConverterConvertValueToTypeMethod.MakeGenericMethod(targetType)
                        .Invoke(null, [sourceValue])
                )
                .Should().Throw<TargetInvocationException>()
                .WithInnerException<InvalidCastException>()
                .WithMessage(
                    $"Could not convert the value {sourceValue.ToDebugString()} to the type {targetType}.*"
                );
        }
    }

    [Fact]
    public void ConvertValueToTypeOfT_ValueCannotBeConvertedToTargetType_ShouldThrow() =>
        Invoking(() => ValueConverter.ConvertValueToType<DateTime>("NotADate"))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value 'NotADate' ({typeof(String)}) to the type {typeof(DateTime)}. See " +
                "inner exception for details.*"
            )
            .WithInnerException<FormatException>()
            .WithMessage("The string 'NotADate' was not recognized as a valid DateTime.*");

    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        ArgumentNullGuardVerifier.Verify(() => ValueConverter.CanConvert(typeof(Int16), typeof(Int32)));
        ArgumentNullGuardVerifier.Verify(() => ValueConverter.ConvertValueToType(1, typeof(Int32)));
    }

    public static IEnumerable<(
            Type SourceType,
            Type TargetType,
            Boolean ExpectedCanConvert,
            Object SourceValue,
            Object ExpectedTargetValue
            )>
        GetConvertTestData()
    {
        var faker = new Faker();

        // All numeric values are kept within the range 0-127 so they are convertible to the smallest target type
        // (SByte) without overflow.
        var byteValue = faker.Random.Byte(0, 127);
        var charValue = faker.Random.Char('A', 'Z');
        var dateOnlyValue = faker.Date.PastDateOnly();
        var dateTimeValue = faker.Date.Past();
        var dateTimeOffsetValue = faker.Date.PastOffset();
        var decimalValue = faker.Random.Decimal(0, 127);
        var doubleValue = faker.Random.Double(0, 127);
        var guidValue = faker.Random.Guid();
        var int16Value = faker.Random.Short(0, 127);
        var int32Value = faker.Random.Int(0, 127);
        var int64Value = faker.Random.Long(0, 127);
        var intPtrValue = (IntPtr)faker.Random.Int(0, 127);
        var sbyteValue = faker.Random.SByte(0);
        var singleValue = faker.Random.Float(0, 127);
        var stringValue = faker.Lorem.Sentence();
        var uint16Value = faker.Random.UShort(0, 127);
        var uint32Value = faker.Random.UInt(0, 127);
        var uint64Value = faker.Random.ULong(0, 127);
        var timeSpanValue = faker.Date.Timespan(TimeSpan.FromHours(23));
        var timeOnlyValue = faker.Date.RecentTimeOnly();
        var uintPtrValue = (UIntPtr)faker.Random.Int(0, 127);
        var enumValue = faker.Random.Enum<TestEnum>();

        // @formatter:off

        return new List<(
            Type SourceType,
            Type TargetType,
            Boolean ExpectedCanConvert,
            Object? SourceValue,
            Object? ExpectedTargetValue
            )>
        {
            (typeof(Boolean), typeof(Boolean), true, true, true),
            (typeof(Boolean), typeof(Byte), true, true, (Byte)1),
            (typeof(Boolean), typeof(Decimal), true, true, (Decimal)1),
            (typeof(Boolean), typeof(Double), true, true, (Double)1),
            (typeof(Boolean), typeof(Int16), true, true, (Int16)1),
            (typeof(Boolean), typeof(Int32), true, true, 1),
            (typeof(Boolean), typeof(Int64), true, true, (Int64)1),
            (typeof(Boolean), typeof(Object), true, true, true),
            (typeof(Boolean), typeof(SByte), true, true, (SByte)1),
            (typeof(Boolean), typeof(Single), true, true, (Single)1),
            (typeof(Boolean), typeof(String), true, true, "True"),
            (typeof(Boolean), typeof(UInt16), true, true, (UInt16)1),
            (typeof(Boolean), typeof(UInt32), true, true, (UInt32)1),
            (typeof(Boolean), typeof(UInt64), true, true, (UInt64)1),
            (typeof(Byte), typeof(Boolean), true, (Byte)1, true),
            (typeof(Byte), typeof(Byte), true, byteValue, byteValue),
            (typeof(Byte), typeof(Char), true, byteValue, (Char)byteValue),
            (typeof(Byte), typeof(Decimal), true, byteValue, (Decimal)byteValue),
            (typeof(Byte), typeof(Double), true, byteValue, (Double)byteValue),
            (typeof(Byte), typeof(Int16), true, byteValue, (Int16)byteValue),
            (typeof(Byte), typeof(Int32), true, byteValue, (Int32)byteValue),
            (typeof(Byte), typeof(Int64), true, byteValue, (Int64)byteValue),
            (typeof(Byte), typeof(Object), true, byteValue, byteValue),
            (typeof(Byte), typeof(SByte), true, byteValue, (SByte)byteValue),
            (typeof(Byte), typeof(Single), true, byteValue, (Single)byteValue),
            (typeof(Byte), typeof(String), true, byteValue, byteValue.ToString(CultureInfo.InvariantCulture)),
            (typeof(Byte), typeof(TestEnum), true, (Byte)enumValue, enumValue),
            (typeof(Byte), typeof(UInt16), true, byteValue, (UInt16)byteValue),
            (typeof(Byte), typeof(UInt32), true, byteValue, (UInt32)byteValue),
            (typeof(Byte), typeof(UInt64), true, byteValue, (UInt64)byteValue),
            (typeof(Byte[]), typeof(Guid), true, guidValue.ToByteArray(), guidValue),
            (typeof(Char), typeof(Byte), true, charValue, (Byte)charValue),
            (typeof(Char), typeof(Char), true, charValue, charValue),
            (typeof(Char), typeof(Int16), true, charValue, (Int16)charValue),
            (typeof(Char), typeof(Int32), true, charValue, (Int32)charValue),
            (typeof(Char), typeof(Int64), true, charValue, (Int64)charValue),
            (typeof(Char), typeof(Object), true, charValue, charValue),
            (typeof(Char), typeof(SByte), true, charValue, (SByte)charValue),
            (typeof(Char), typeof(String), true, charValue, charValue.ToString(CultureInfo.InvariantCulture)),
            (typeof(Char), typeof(UInt16), true, charValue, (UInt16)charValue),
            (typeof(Char), typeof(UInt32), true, charValue, (UInt32)charValue),
            (typeof(Char), typeof(UInt64), true, charValue, (UInt64)charValue),
            (typeof(DateOnly), typeof(DateOnly), true, dateOnlyValue, dateOnlyValue),
            (typeof(DateOnly), typeof(Object), true, dateOnlyValue, dateOnlyValue),
            (typeof(DateOnly), typeof(String), true, dateOnlyValue, dateOnlyValue.ToString("O", CultureInfo.InvariantCulture)),
            (typeof(DateTime), typeof(DateOnly), true, dateOnlyValue.ToDateTime(TimeOnly.MinValue), dateOnlyValue),
            (typeof(DateTime), typeof(DateTime), true, dateTimeValue, dateTimeValue),
            (typeof(DateTime), typeof(Object), true, dateTimeValue, dateTimeValue),
            (typeof(DateTime), typeof(String), true, dateTimeValue, dateTimeValue.ToString("O", CultureInfo.InvariantCulture)),
            (typeof(DateTimeOffset), typeof(DateTimeOffset), true, dateTimeOffsetValue, dateTimeOffsetValue),
            (typeof(DateTimeOffset), typeof(Object), true, dateTimeOffsetValue, dateTimeOffsetValue),
            (typeof(DateTimeOffset), typeof(String), true, dateTimeOffsetValue, dateTimeOffsetValue.ToString("O", CultureInfo.InvariantCulture)),
            (typeof(Decimal), typeof(Boolean), true, 1M, true),
            (typeof(Decimal), typeof(Byte), true, decimalValue, Convert.ChangeType(decimalValue, typeof(Byte), CultureInfo.InvariantCulture)),
            (typeof(Decimal), typeof(Decimal), true, decimalValue, decimalValue),
            (typeof(Decimal), typeof(Double), true, decimalValue, Convert.ChangeType(decimalValue, typeof(Double), CultureInfo.InvariantCulture)),
            (typeof(Decimal), typeof(Int16), true, decimalValue, Convert.ChangeType(decimalValue, typeof(Int16), CultureInfo.InvariantCulture)),
            (typeof(Decimal), typeof(Int32), true, decimalValue, Convert.ChangeType(decimalValue, typeof(Int32), CultureInfo.InvariantCulture)),
            (typeof(Decimal), typeof(Int64), true, decimalValue, Convert.ChangeType(decimalValue, typeof(Int64), CultureInfo.InvariantCulture)),
            (typeof(Decimal), typeof(Object), true, decimalValue, decimalValue),
            (typeof(Decimal), typeof(SByte), true, decimalValue, Convert.ChangeType(decimalValue, typeof(SByte), CultureInfo.InvariantCulture)),
            (typeof(Decimal), typeof(Single), true, decimalValue, Convert.ChangeType(decimalValue, typeof(Single), CultureInfo.InvariantCulture)),
            (typeof(Decimal), typeof(String), true, decimalValue, decimalValue.ToString(CultureInfo.InvariantCulture)),
            (typeof(Decimal), typeof(TestEnum), true, (Decimal)enumValue, enumValue),
            (typeof(Decimal), typeof(UInt16), true, decimalValue, Convert.ChangeType(decimalValue, typeof(UInt16), CultureInfo.InvariantCulture)),
            (typeof(Decimal), typeof(UInt32), true, decimalValue, Convert.ChangeType(decimalValue, typeof(UInt32), CultureInfo.InvariantCulture)),
            (typeof(Decimal), typeof(UInt64), true, decimalValue, Convert.ChangeType(decimalValue, typeof(UInt64), CultureInfo.InvariantCulture)),
            (typeof(Double), typeof(Boolean), true, 1.0, true),
            (typeof(Double), typeof(Byte), true, doubleValue, Convert.ChangeType(doubleValue, typeof(Byte), CultureInfo.InvariantCulture)),
            (typeof(Double), typeof(Decimal), true, doubleValue, Convert.ChangeType(doubleValue, typeof(Decimal), CultureInfo.InvariantCulture)),
            (typeof(Double), typeof(Double), true, doubleValue, doubleValue),
            (typeof(Double), typeof(Int16), true, doubleValue, Convert.ChangeType(doubleValue, typeof(Int16), CultureInfo.InvariantCulture)),
            (typeof(Double), typeof(Int32), true, doubleValue, Convert.ChangeType(doubleValue, typeof(Int32), CultureInfo.InvariantCulture)),
            (typeof(Double), typeof(Int64), true, doubleValue, Convert.ChangeType(doubleValue, typeof(Int64), CultureInfo.InvariantCulture)),
            (typeof(Double), typeof(Object), true, doubleValue, doubleValue),
            (typeof(Double), typeof(SByte), true, doubleValue, Convert.ChangeType(doubleValue, typeof(SByte), CultureInfo.InvariantCulture)),
            (typeof(Double), typeof(Single), true, doubleValue, Convert.ChangeType(doubleValue, typeof(Single), CultureInfo.InvariantCulture)),
            (typeof(Double), typeof(String), true, doubleValue, doubleValue.ToString(CultureInfo.InvariantCulture)),
            (typeof(Double), typeof(TestEnum), true, (Double)enumValue, enumValue),
            (typeof(Double), typeof(UInt16), true, doubleValue, Convert.ChangeType(doubleValue, typeof(UInt16), CultureInfo.InvariantCulture)),
            (typeof(Double), typeof(UInt32), true, doubleValue, Convert.ChangeType(doubleValue, typeof(UInt32), CultureInfo.InvariantCulture)),
            (typeof(Double), typeof(UInt64), true, doubleValue, Convert.ChangeType(doubleValue, typeof(UInt64), CultureInfo.InvariantCulture)),
            (typeof(Guid), typeof(Byte[]), true, guidValue, guidValue.ToByteArray()),
            (typeof(Guid), typeof(Guid), true, guidValue, guidValue),
            (typeof(Guid), typeof(Object), true, guidValue, guidValue),
            (typeof(Guid), typeof(String), true, guidValue, guidValue.ToString("D")),
            (typeof(Int16), typeof(Boolean), true, (Int16)1, true),
            (typeof(Int16), typeof(Byte), true, int16Value, (Byte) int16Value),
            (typeof(Int16), typeof(Char), true, int16Value, (Char) int16Value),
            (typeof(Int16), typeof(Decimal), true, int16Value, (Decimal) int16Value),
            (typeof(Int16), typeof(Double), true, int16Value, (Double)int16Value),
            (typeof(Int16), typeof(Int16), true, int16Value, int16Value),
            (typeof(Int16), typeof(Int32), true, int16Value, (Int32)int16Value),
            (typeof(Int16), typeof(Int64), true, int16Value, (Int64)int16Value),
            (typeof(Int16), typeof(Object), true, int16Value, int16Value),
            (typeof(Int16), typeof(SByte), true, int16Value, (SByte)int16Value),
            (typeof(Int16), typeof(Single), true, int16Value, (Single)int16Value),
            (typeof(Int16), typeof(String), true, int16Value, int16Value.ToString(CultureInfo.InvariantCulture)),
            (typeof(Int16), typeof(TestEnum), true, (Int16)enumValue, enumValue),
            (typeof(Int16), typeof(UInt16), true, int16Value, (UInt16)int16Value),
            (typeof(Int16), typeof(UInt32), true, int16Value, (UInt32)int16Value),
            (typeof(Int16), typeof(UInt64), true, int16Value, (UInt64)int16Value),
            (typeof(Int32), typeof(Boolean), true, 1, true),
            (typeof(Int32), typeof(Byte), true, int32Value, (Byte)int32Value),
            (typeof(Int32), typeof(Char), true, int32Value, (Char) int32Value),
            (typeof(Int32), typeof(Decimal), true, int32Value, (Decimal)int32Value),
            (typeof(Int32), typeof(Double), true, int32Value, (Double)int32Value),
            (typeof(Int32), typeof(Int16), true, int32Value, (Int16)int32Value),
            (typeof(Int32), typeof(Int32), true, int32Value, int32Value),
            (typeof(Int32), typeof(Int64), true, int32Value, (Int64)int32Value),
            (typeof(Int32), typeof(Object), true, int32Value, int32Value),
            (typeof(Int32), typeof(SByte), true, int32Value, (SByte)int32Value),
            (typeof(Int32), typeof(Single), true, int32Value, (Single)int32Value),
            (typeof(Int32), typeof(String), true, int32Value, int32Value.ToString(CultureInfo.InvariantCulture)),
            (typeof(Int32), typeof(TestEnum), true, (Int32)enumValue, enumValue),
            (typeof(Int32), typeof(UInt16), true, int32Value, (UInt16)int32Value),
            (typeof(Int32), typeof(UInt32), true, int32Value, (UInt32)int32Value),
            (typeof(Int32), typeof(UInt64), true, int32Value, (UInt64)int32Value),
            (typeof(Int64), typeof(Boolean), true, (Int64)1, true),
            (typeof(Int64), typeof(Byte), true, int64Value, (Byte) int64Value),
            (typeof(Int64), typeof(Char), true, int64Value, (Char) int64Value),
            (typeof(Int64), typeof(Decimal), true, int64Value, (Decimal) int64Value),
            (typeof(Int64), typeof(Double), true, int64Value, (Double)int64Value),
            (typeof(Int64), typeof(Int16), true, int64Value, (Int16)int64Value),
            (typeof(Int64), typeof(Int32), true, int64Value, (Int32)int64Value),
            (typeof(Int64), typeof(Int64), true, int64Value, int64Value),
            (typeof(Int64), typeof(Object), true, int64Value, int64Value),
            (typeof(Int64), typeof(SByte), true, int64Value, (SByte)int64Value),
            (typeof(Int64), typeof(Single), true, int64Value, (Single)int64Value),
            (typeof(Int64), typeof(String), true, int64Value, int64Value.ToString(CultureInfo.InvariantCulture)),
            (typeof(Int64), typeof(TestEnum), true, (Int64)enumValue, enumValue),
            (typeof(Int64), typeof(UInt16), true, int64Value, (UInt16)int64Value),
            (typeof(Int64), typeof(UInt32), true, int64Value, (UInt32)int64Value),
            (typeof(Int64), typeof(UInt64), true, int64Value, (UInt64)int64Value),
            (typeof(IntPtr), typeof(IntPtr), true, intPtrValue, intPtrValue),
            (typeof(IntPtr), typeof(Object), true, intPtrValue, intPtrValue),
            (typeof(SByte), typeof(Boolean), true, (SByte)1, true),
            (typeof(SByte), typeof(Byte), true, sbyteValue, (Byte)sbyteValue),
            (typeof(SByte), typeof(Char), true, sbyteValue, (Char) sbyteValue),
            (typeof(SByte), typeof(Decimal), true, sbyteValue, (Decimal)sbyteValue),
            (typeof(SByte), typeof(Double), true, sbyteValue, (Double)sbyteValue),
            (typeof(SByte), typeof(Int16), true, sbyteValue, (Int16)sbyteValue),
            (typeof(SByte), typeof(Int32), true, sbyteValue, (Int32)sbyteValue),
            (typeof(SByte), typeof(Int64), true, sbyteValue, (Int64)sbyteValue),
            (typeof(SByte), typeof(Object), true, sbyteValue, sbyteValue),
            (typeof(SByte), typeof(SByte), true, sbyteValue, sbyteValue),
            (typeof(SByte), typeof(Single), true, sbyteValue, (Single)sbyteValue),
            (typeof(SByte), typeof(String), true, sbyteValue, sbyteValue.ToString(CultureInfo.InvariantCulture)),
            (typeof(SByte), typeof(TestEnum), true, (SByte)enumValue, enumValue),
            (typeof(SByte), typeof(UInt16), true, sbyteValue, (UInt16)sbyteValue),
            (typeof(SByte), typeof(UInt32), true, sbyteValue, (UInt32)sbyteValue),
            (typeof(SByte), typeof(UInt64), true, sbyteValue, (UInt64)sbyteValue),
            (typeof(Single), typeof(Boolean), true, (Single)1, true),
            (typeof(Single), typeof(Byte), true, singleValue, Convert.ChangeType(singleValue, typeof(Byte), CultureInfo.InvariantCulture)),
            (typeof(Single), typeof(Decimal), true, singleValue, Convert.ChangeType(singleValue, typeof(Decimal), CultureInfo.InvariantCulture)),
            (typeof(Single), typeof(Double), true, singleValue, Convert.ChangeType(singleValue, typeof(Double), CultureInfo.InvariantCulture)),
            (typeof(Single), typeof(Int16), true, singleValue, Convert.ChangeType(singleValue, typeof(Int16), CultureInfo.InvariantCulture)),
            (typeof(Single), typeof(Int32), true, singleValue, Convert.ChangeType(singleValue, typeof(Int32), CultureInfo.InvariantCulture)),
            (typeof(Single), typeof(Int64), true, singleValue, Convert.ChangeType(singleValue, typeof(Int64), CultureInfo.InvariantCulture)),
            (typeof(Single), typeof(Object), true, singleValue, singleValue),
            (typeof(Single), typeof(SByte), true, singleValue, Convert.ChangeType(singleValue, typeof(SByte), CultureInfo.InvariantCulture)),
            (typeof(Single), typeof(Single), true, singleValue, Convert.ChangeType(singleValue, typeof(Single), CultureInfo.InvariantCulture)),
            (typeof(Single), typeof(String), true, singleValue, singleValue.ToString(CultureInfo.InvariantCulture)),
            (typeof(Single), typeof(TestEnum), true, (Single)enumValue, enumValue),
            (typeof(Single), typeof(UInt16), true, singleValue, Convert.ChangeType(singleValue, typeof(UInt16), CultureInfo.InvariantCulture)),
            (typeof(Single), typeof(UInt32), true, singleValue, Convert.ChangeType(singleValue, typeof(UInt32), CultureInfo.InvariantCulture)),
            (typeof(Single), typeof(UInt64), true, singleValue, Convert.ChangeType(singleValue, typeof(UInt64), CultureInfo.InvariantCulture)),
            (typeof(String), typeof(Boolean), true, "True", true),
            (typeof(String), typeof(Byte), true, byteValue.ToString(CultureInfo.InvariantCulture), byteValue),
            (typeof(String), typeof(Char), true, charValue.ToString(CultureInfo.InvariantCulture), charValue),
            (typeof(String), typeof(DateOnly), true, dateOnlyValue.ToString("O", CultureInfo.InvariantCulture), dateOnlyValue),
            (typeof(String), typeof(DateTime), true, dateTimeValue.ToString("O", CultureInfo.InvariantCulture), dateTimeValue),
            (typeof(String), typeof(DateTimeOffset), true, dateTimeOffsetValue.ToString("O", CultureInfo.InvariantCulture), dateTimeOffsetValue),
            (typeof(String), typeof(Decimal), true, decimalValue.ToString(CultureInfo.InvariantCulture), decimalValue),
            (typeof(String), typeof(Double), true, doubleValue.ToString(CultureInfo.InvariantCulture), doubleValue),
            (typeof(String), typeof(Guid), true, guidValue.ToString("D"), guidValue),
            (typeof(String), typeof(Int16), true, int16Value.ToString(CultureInfo.InvariantCulture), int16Value),
            (typeof(String), typeof(Int32), true, int32Value.ToString(CultureInfo.InvariantCulture), int32Value),
            (typeof(String), typeof(Int64), true, int64Value.ToString(CultureInfo.InvariantCulture), int64Value),
            (typeof(String), typeof(Object), true, stringValue, stringValue),
            (typeof(String), typeof(SByte), true, sbyteValue.ToString(CultureInfo.InvariantCulture), sbyteValue),
            (typeof(String), typeof(Single), true, singleValue.ToString(CultureInfo.InvariantCulture), singleValue),
            (typeof(String), typeof(String), true, stringValue, stringValue),
            (typeof(String), typeof(TestEnum), true, enumValue.ToString(), enumValue),
            (typeof(String), typeof(TimeSpan), true, timeSpanValue.ToString("g", CultureInfo.InvariantCulture), timeSpanValue),
            (typeof(String), typeof(UInt16), true, uint16Value.ToString(CultureInfo.InvariantCulture), uint16Value),
            (typeof(String), typeof(UInt32), true, uint32Value.ToString(CultureInfo.InvariantCulture), uint32Value),
            (typeof(String), typeof(UInt64), true, uint64Value.ToString(CultureInfo.InvariantCulture), uint64Value),
            (typeof(TestEnum), typeof(Byte), true, enumValue, (Byte)enumValue),
            (typeof(TestEnum), typeof(Decimal), true, enumValue, (Decimal)enumValue),
            (typeof(TestEnum), typeof(Double), true, enumValue, (Double)enumValue),
            (typeof(TestEnum), typeof(Int16), true, enumValue, (Int16)enumValue),
            (typeof(TestEnum), typeof(Int32), true, enumValue, (Int32)enumValue),
            (typeof(TestEnum), typeof(Int64), true, enumValue, (Int64)enumValue),
            (typeof(TestEnum), typeof(Object), true, enumValue, enumValue),
            (typeof(TestEnum), typeof(SByte), true, enumValue, (SByte)enumValue),
            (typeof(TestEnum), typeof(Single), true, enumValue, (Single)enumValue),
            (typeof(TestEnum), typeof(String), true, enumValue, enumValue.ToString()),
            (typeof(TestEnum), typeof(TestEnum), true, enumValue, enumValue),
            (typeof(TestEnum), typeof(UInt16), true, enumValue, (UInt16)enumValue),
            (typeof(TestEnum), typeof(UInt32), true, enumValue, (UInt32)enumValue),
            (typeof(TestEnum), typeof(UInt64), true, enumValue, (UInt64)enumValue),
            (typeof(TimeOnly), typeof(Object), true, timeOnlyValue, timeOnlyValue),
            (typeof(TimeOnly), typeof(String), true, timeOnlyValue, timeOnlyValue.ToString("O", CultureInfo.InvariantCulture)),
            (typeof(TimeOnly), typeof(TimeOnly), true, timeOnlyValue, timeOnlyValue),
            (typeof(TimeSpan), typeof(Object), true, timeSpanValue, timeSpanValue),
            (typeof(TimeSpan), typeof(String), true, timeSpanValue, timeSpanValue.ToString("g", CultureInfo.InvariantCulture)),
            (typeof(TimeSpan), typeof(TimeOnly), true, timeSpanValue, TimeOnly.FromTimeSpan(timeSpanValue)),
            (typeof(TimeSpan), typeof(TimeSpan), true, timeSpanValue, timeSpanValue),
            (typeof(UInt16), typeof(Boolean), true, (UInt16)1, true),
            (typeof(UInt16), typeof(Byte), true, uint16Value, (Byte) uint16Value),
            (typeof(UInt16), typeof(Char), true, uint16Value, (Char) uint16Value),
            (typeof(UInt16), typeof(Decimal), true, uint16Value, (Decimal) uint16Value),
            (typeof(UInt16), typeof(Double), true, uint16Value, (Double)uint16Value),
            (typeof(UInt16), typeof(Int16), true, uint16Value, (Int16)uint16Value),
            (typeof(UInt16), typeof(Int32), true, uint16Value, (Int32)uint16Value),
            (typeof(UInt16), typeof(Int64), true, uint16Value, (Int64)uint16Value),
            (typeof(UInt16), typeof(Object), true, uint16Value, uint16Value),
            (typeof(UInt16), typeof(SByte), true, uint16Value, (SByte)uint16Value),
            (typeof(UInt16), typeof(Single), true, uint16Value, (Single)uint16Value),
            (typeof(UInt16), typeof(String), true, uint16Value, uint16Value.ToString(CultureInfo.InvariantCulture)),
            (typeof(UInt16), typeof(TestEnum), true, (UInt16)enumValue, enumValue),
            (typeof(UInt16), typeof(UInt16), true, uint16Value, uint16Value),
            (typeof(UInt16), typeof(UInt32), true, uint16Value, (UInt32)uint16Value),
            (typeof(UInt16), typeof(UInt64), true, uint16Value, (UInt64)uint16Value),
            (typeof(UInt32), typeof(Boolean), true, (UInt32)1, true),
            (typeof(UInt32), typeof(Byte), true, uint32Value, (Byte) uint32Value),
            (typeof(UInt32), typeof(Char), true, uint32Value, (Char) uint32Value),
            (typeof(UInt32), typeof(Decimal), true, uint32Value, (Decimal) uint32Value),
            (typeof(UInt32), typeof(Double), true, uint32Value, (Double)uint32Value),
            (typeof(UInt32), typeof(Int32), true, uint32Value, (Int32)uint32Value),
            (typeof(UInt32), typeof(Int32), true, uint32Value, (Int32)uint32Value),
            (typeof(UInt32), typeof(Int64), true, uint32Value, (Int64)uint32Value),
            (typeof(UInt32), typeof(Object), true, uint32Value, uint32Value),
            (typeof(UInt32), typeof(SByte), true, uint32Value, (SByte)uint32Value),
            (typeof(UInt32), typeof(Single), true, uint32Value, (Single)uint32Value),
            (typeof(UInt32), typeof(String), true, uint32Value, uint32Value.ToString(CultureInfo.InvariantCulture)),
            (typeof(UInt32), typeof(TestEnum), true, (UInt32)enumValue, enumValue),
            (typeof(UInt32), typeof(UInt16), true, uint32Value, (UInt16)uint32Value),
            (typeof(UInt32), typeof(UInt32), true, uint32Value, uint32Value),
            (typeof(UInt32), typeof(UInt64), true, uint32Value, (UInt64)uint32Value),
            (typeof(UInt64), typeof(Boolean), true, (UInt64)1, true),
            (typeof(UInt64), typeof(Byte), true, uint64Value, (Byte) uint64Value),
            (typeof(UInt64), typeof(Char), true, uint64Value, (Char) uint64Value),
            (typeof(UInt64), typeof(Decimal), true, uint64Value, (Decimal) uint64Value),
            (typeof(UInt64), typeof(Double), true, uint64Value, (Double)uint64Value),
            (typeof(UInt64), typeof(Int16), true, uint64Value, (Int16)uint64Value),
            (typeof(UInt64), typeof(Int32), true, uint64Value, (Int32)uint64Value),
            (typeof(UInt64), typeof(Int64), true, uint64Value, (Int64)uint64Value),
            (typeof(UInt64), typeof(Object), true, uint64Value, uint64Value),
            (typeof(UInt64), typeof(SByte), true, uint64Value, (SByte)uint64Value),
            (typeof(UInt64), typeof(Single), true, uint64Value, (Single)uint64Value),
            (typeof(UInt64), typeof(String), true, uint64Value, uint64Value.ToString(CultureInfo.InvariantCulture)),
            (typeof(UInt64), typeof(TestEnum), true, (UInt64)enumValue, enumValue),
            (typeof(UInt64), typeof(UInt16), true, uint64Value, (UInt16)uint64Value),
            (typeof(UInt64), typeof(UInt32), true, uint64Value, (UInt32)uint64Value),
            (typeof(UInt64), typeof(UInt64), true, uint64Value, uint64Value),
            (typeof(UIntPtr), typeof(Object), true, uintPtrValue, uintPtrValue),
            (typeof(UIntPtr), typeof(UIntPtr), true, uintPtrValue, uintPtrValue),
            (typeof(Char), typeof(Guid), false, charValue, null),
            (typeof(Int32), typeof(Guid), false, int32Value, null),
            (typeof(DateTime), typeof(Guid), false, dateTimeValue, null),
            (typeof(Guid), typeof(DateTime), false, guidValue, null),
            (typeof(DateOnly), typeof(DateTime), false, dateOnlyValue, null),
            (typeof(TimeOnly), typeof(TimeSpan), false, timeOnlyValue, null),
            (typeof(DateOnly), typeof(Guid), false, dateOnlyValue, null),
            (typeof(TimeOnly), typeof(Guid), false, timeOnlyValue, null)
        };

        // @formatter:on
    }
}
