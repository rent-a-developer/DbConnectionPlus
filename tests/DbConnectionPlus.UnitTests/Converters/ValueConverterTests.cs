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
        bool expectedCanConvert,
        object? sourceValue,
        object? expectedTargetValue
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
        bool expectedCanConvert,
        object? sourceValue,
        object? expectedTargetValue
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
        bool expectedCanConvert,
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
#pragma warning disable RCS1163 // Unused parameter
        object? sourceValue,
        object? expectedTargetValue
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
        var character = Generate.Single<char>();

        ValueConverter.ConvertValueToType(character.ToString(), typeof(char))
            .Should().Be(character);

        ValueConverter.ConvertValueToType(character.ToString(), typeof(char?))
            .Should().Be(character);
    }

    [Fact]
    public void ConvertValueToType_CharTargetType_ValueIsStringWithLengthNotOne_ShouldThrow()
    {
        Invoking(() => ValueConverter.ConvertValueToType(string.Empty, typeof(char)))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string '' to the type {typeof(char)}. The string must be exactly one " +
                "character long."
            );

        Invoking(() => ValueConverter.ConvertValueToType(string.Empty, typeof(char?)))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string '' to the type {typeof(char?)}. The string must be exactly one " +
                "character long."
            );

        Invoking(() => ValueConverter.ConvertValueToType("ab", typeof(char)))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(char)}. The string must be exactly one " +
                "character long."
            );

        Invoking(() => ValueConverter.ConvertValueToType("ab", typeof(char?)))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(char?)}. The string must be exactly one " +
                "character long."
            );
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("fr-FR")]
    [InlineData("en-US")]
    public void ConvertValueToType_DateAndTimeStringValue_AmbiguousDate_ShouldNotDependOnTheCurrentCulture(
        string cultureName
    )
    {
        // "03/04/2026" is the 4th of March under en-US and the 3rd of April under de-DE and fr-FR. Read with
        // the invariant culture it is the 4th of March everywhere, so one database value can no longer decode
        // into two different dates depending on the locale of the machine that runs the code.
        var expectedDate = new DateOnly(2026, 3, 4);

        RunUnderCulture(cultureName, () =>
        {
            ValueConverter.ConvertValueToType<DateOnly>("03/04/2026")
                .Should().Be(expectedDate);

            ValueConverter.ConvertValueToType("03/04/2026", typeof(DateOnly))
                .Should().Be(expectedDate);
        });
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("fr-FR")]
    [InlineData("en-US")]
    public void ConvertValueToType_DateAndTimeStringValue_ShouldRoundTripUnderAnyCulture(string cultureName)
    {
        // The converter writes these four types with the invariant culture, so it has to read them back the
        // same way. It did not: under a culture whose decimal separator is a comma, a TimeSpan this library
        // itself had written as "1:2:03:04.567" did not parse back at all, and the conversion threw.
        var timeSpan = new TimeSpan(1, 2, 3, 4, 567);
        var dateTimeOffset = new DateTimeOffset(2026, 3, 4, 14, 30, 0, TimeSpan.FromHours(2));
        var dateOnly = new DateOnly(2026, 3, 4);
        var timeOnly = new TimeOnly(14, 30, 0);

        RunUnderCulture(cultureName, () =>
        {
            AssertRoundTrips(timeSpan);
            AssertRoundTrips(dateTimeOffset);
            AssertRoundTrips(dateOnly);
            AssertRoundTrips(timeOnly);
        });

        // Converts the value to its String representation and back, both through the converter itself, so the
        // assertion is that the writing half and the reading half agree - not that either matches a literal.
        static void AssertRoundTrips<TValue>(TValue value)
        {
            var text = ValueConverter.ConvertValueToType<string>(value);

            ValueConverter.ConvertValueToType<TValue>(text)
                .Should().Be(value, $"{typeof(TValue)} written as '{text}' should read back unchanged");

            ValueConverter.ConvertValueToType(text, typeof(TValue))
                .Should().Be(value, $"{typeof(TValue)} written as '{text}' should read back unchanged");
        }
    }

    [Fact]
    public void
        ConvertValueToType_EnumTargetType_IntegerValueNotMatchingAnyEnumMemberValue_ShouldThrow()
    {
        Invoking(() => ValueConverter.ConvertValueToType(999, typeof(TestEnum)))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value '999' ({typeof(int)}) to an enum member of the type " +
                $"{typeof(TestEnum)}. That value does not match any of the values of the enum's members.*"
            );

        Invoking(() => ValueConverter.ConvertValueToType(999, typeof(TestEnum?)))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value '999' ({typeof(int)}) to an enum member of the type " +
                $"{typeof(TestEnum?)}. That value does not match any of the values of the enum's members.*"
            );
    }

    [Fact]
    public void ConvertValueToType_EnumTargetType_ShouldConvertToEnumMember()
    {
        var enumValue = Generate.Single<TestEnum>();

        ValueConverter.ConvertValueToType((int)enumValue, typeof(TestEnum))
            .Should().Be(enumValue);

        ValueConverter.ConvertValueToType((int)enumValue, typeof(TestEnum?))
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
        bool expectedCanConvert,
        object? sourceValue,
        object? expectedTargetValue
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
        ValueConverter.ConvertValueToType(DBNull.Value, typeof(object))
            .Should().BeNull();

        ValueConverter.ConvertValueToType(DBNull.Value, typeof(int?))
            .Should().BeNull();

        ValueConverter.ConvertValueToType(null, typeof(object))
            .Should().BeNull();

        ValueConverter.ConvertValueToType(null, typeof(int?))
            .Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(GetConvertTestData))]
    public void ConvertValueToType_NullableTargetType_ShouldConvertValueToTargetType(
        Type sourceType,
        Type targetType,
        bool expectedCanConvert,
        object? sourceValue,
        object? expectedTargetValue
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
        bool expectedCanConvert,
        object? sourceValue,
        object? expectedTargetValue
    )
    {
        if (expectedCanConvert)
        {
            var result = ValueConverter.ConvertValueToType(sourceValue, targetType);

            if (result is byte[] resultBytes && expectedTargetValue is byte[] expectedTargetValueBytes)
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
                $"Could not convert the value 'NotADate' ({typeof(string)}) to the type {typeof(DateTime)}. See " +
                "inner exception for details.*"
            )
            .WithInnerException<FormatException>()
            .WithMessage("The string 'NotADate' was not recognized as a valid DateTime.*");

    [Fact]
    public void ConvertValueToTypeOfT_CharTargetType_StringWithLengthOneValue_ShouldGetFirstCharacter()
    {
        var character = Generate.Single<char>();

        ValueConverter.ConvertValueToType<char>(character.ToString())
            .Should().Be(character);

        ValueConverter.ConvertValueToType<char?>(character.ToString())
            .Should().Be(character);
    }

    [Fact]
    public void ConvertValueToTypeOfT_CharTargetType_ValueIsStringWithLengthNotOne_ShouldThrow()
    {
        Invoking(() => ValueConverter.ConvertValueToType<char>(string.Empty))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string '' to the type {typeof(char)}. The string must be exactly one " +
                "character long."
            );

        Invoking(() => ValueConverter.ConvertValueToType<char?>(string.Empty))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string '' to the type {typeof(char?)}. The string must be exactly one " +
                "character long."
            );

        Invoking(() => ValueConverter.ConvertValueToType<char>("ab"))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(char)}. The string must be exactly one " +
                "character long."
            );

        Invoking(() => ValueConverter.ConvertValueToType<char?>("ab"))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(char?)}. The string must be exactly one " +
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
                $"Could not convert the value '999' ({typeof(int)}) to an enum member of the type " +
                $"{typeof(TestEnum)}. That value does not match any of the values of the enum's members.*"
            );

        Invoking(() => ValueConverter.ConvertValueToType<TestEnum?>(999))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value '999' ({typeof(int)}) to an enum member of the type " +
                $"{typeof(TestEnum?)}. That value does not match any of the values of the enum's members.*"
            );
    }

    [Fact]
    public void ConvertValueToTypeOfT_EnumTargetType_ShouldConvertToEnumMember()
    {
        var enumValue = Generate.Single<TestEnum>();

        ValueConverter.ConvertValueToType<TestEnum>((int)enumValue)
            .Should().Be(enumValue);

        ValueConverter.ConvertValueToType<TestEnum?>((int)enumValue)
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
        bool expectedCanConvert,
        object? sourceValue,
        object? expectedTargetValue
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
        ValueConverter.ConvertValueToType<object>(DBNull.Value)
            .Should().BeNull();

        ValueConverter.ConvertValueToType<int?>(DBNull.Value)
            .Should().BeNull();

        ValueConverter.ConvertValueToType<object>(null)
            .Should().BeNull();

        ValueConverter.ConvertValueToType<int?>(null)
            .Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(GetConvertTestData))]
    public void ConvertValueToTypeOfT_NullableTargetType_ShouldConvertValueToTargetType(
        Type sourceType,
        Type targetType,
        bool expectedCanConvert,
        object? sourceValue,
        object? expectedTargetValue
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
        bool expectedCanConvert,
        object? sourceValue,
        object? expectedTargetValue
    )
    {
        if (expectedCanConvert)
        {
            var result = MaterializerFactoryHelper.MakeValueConverterConvertValueToTypeMethod(targetType)
                .Invoke(null, [sourceValue]);

            if (result is byte[] resultBytes && expectedTargetValue is byte[] expectedTargetValueBytes)
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
                    MaterializerFactoryHelper.MakeValueConverterConvertValueToTypeMethod(targetType)
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
                $"Could not convert the value 'NotADate' ({typeof(string)}) to the type {typeof(DateTime)}. See " +
                "inner exception for details.*"
            )
            .WithInnerException<FormatException>()
            .WithMessage("The string 'NotADate' was not recognized as a valid DateTime.*");

    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        ArgumentNullGuardVerifier.Verify(() => ValueConverter.CanConvert(typeof(short), typeof(int)));
        ArgumentNullGuardVerifier.Verify(() => ValueConverter.ConvertValueToType(1, typeof(int)));
    }

    /// <summary>
    /// Runs <paramref name="assertions" /> with the current culture set to <paramref name="cultureName" />,
    /// and restores the previous culture afterwards.
    /// </summary>
    /// <remarks>
    /// <see cref="UnitTestsBase" /> pins every test to en-US, and en-US is exactly the culture under which
    /// culture-dependent date and time parsing still looks correct - which is why the whole suite passed
    /// while the converter was reading with the current culture. A test for that has to leave the pin.
    /// The assembly runs with <c>ParallelMode.None</c>, so changing the culture cannot affect another test.
    /// </remarks>
    /// <param name="cultureName">The name of the culture to run the assertions under.</param>
    /// <param name="assertions">The assertions to run.</param>
    private static void RunUnderCulture(string cultureName, Action assertions)
    {
        var culture = new CultureInfo(cultureName);

        // Without ICU, every culture collapses into the invariant one and the test would pass while proving
        // nothing. de-DE and fr-FR both separate decimals with a comma; the invariant culture uses a dot.
        Assert.SkipWhen(
            cultureName != "en-US" &&
            culture.NumberFormat.NumberDecimalSeparator ==
            CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator,
            $"Globalization is in invariant mode, so '{cultureName}' is not a real culture here."
        );

        var previousCulture = CultureInfo.CurrentCulture;

        CultureInfo.CurrentCulture = Thread.CurrentThread.CurrentCulture = culture;

        try
        {
            assertions();
        }
        finally
        {
            CultureInfo.CurrentCulture = Thread.CurrentThread.CurrentCulture = previousCulture;
        }
    }

    public static IEnumerable<(
            Type SourceType,
            Type TargetType,
            bool ExpectedCanConvert,
            object SourceValue,
            object ExpectedTargetValue
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

        return
        [
            (typeof(bool), typeof(bool), true, true, true),
            (typeof(bool), typeof(byte), true, true, (byte)1),
            (typeof(bool), typeof(decimal), true, true, (decimal)1),
            (typeof(bool), typeof(double), true, true, (double)1),
            (typeof(bool), typeof(short), true, true, (short)1),
            (typeof(bool), typeof(int), true, true, 1),
            (typeof(bool), typeof(long), true, true, (long)1),
            (typeof(bool), typeof(object), true, true, true),
            (typeof(bool), typeof(sbyte), true, true, (sbyte)1),
            (typeof(bool), typeof(float), true, true, (float)1),
            (typeof(bool), typeof(string), true, true, "True"),
            (typeof(bool), typeof(ushort), true, true, (ushort)1),
            (typeof(bool), typeof(uint), true, true, (uint)1),
            (typeof(bool), typeof(ulong), true, true, (ulong)1),
            (typeof(byte), typeof(bool), true, (byte)1, true),
            (typeof(byte), typeof(byte), true, byteValue, byteValue),
            (typeof(byte), typeof(char), true, byteValue, (char)byteValue),
            (typeof(byte), typeof(decimal), true, byteValue, (decimal)byteValue),
            (typeof(byte), typeof(double), true, byteValue, (double)byteValue),
            (typeof(byte), typeof(short), true, byteValue, (short)byteValue),
            (typeof(byte), typeof(int), true, byteValue, (int)byteValue),
            (typeof(byte), typeof(long), true, byteValue, (long)byteValue),
            (typeof(byte), typeof(object), true, byteValue, byteValue),
            (typeof(byte), typeof(sbyte), true, byteValue, (sbyte)byteValue),
            (typeof(byte), typeof(float), true, byteValue, (float)byteValue),
            (typeof(byte), typeof(string), true, byteValue, byteValue.ToString(CultureInfo.InvariantCulture)),
            (typeof(byte), typeof(TestEnum), true, (byte)enumValue, enumValue),
            (typeof(byte), typeof(ushort), true, byteValue, (ushort)byteValue),
            (typeof(byte), typeof(uint), true, byteValue, (uint)byteValue),
            (typeof(byte), typeof(ulong), true, byteValue, (ulong)byteValue),
            (typeof(byte[]), typeof(Guid), true, guidValue.ToByteArray(), guidValue),
            (typeof(char), typeof(byte), true, charValue, (byte)charValue),
            (typeof(char), typeof(char), true, charValue, charValue),
            (typeof(char), typeof(short), true, charValue, (short)charValue),
            (typeof(char), typeof(int), true, charValue, (int)charValue),
            (typeof(char), typeof(long), true, charValue, (long)charValue),
            (typeof(char), typeof(object), true, charValue, charValue),
            (typeof(char), typeof(sbyte), true, charValue, (sbyte)charValue),
            (typeof(char), typeof(string), true, charValue, charValue.ToString(CultureInfo.InvariantCulture)),
            (typeof(char), typeof(ushort), true, charValue, (ushort)charValue),
            (typeof(char), typeof(uint), true, charValue, (uint)charValue),
            (typeof(char), typeof(ulong), true, charValue, (ulong)charValue),
            (typeof(DateOnly), typeof(DateOnly), true, dateOnlyValue, dateOnlyValue),
            (typeof(DateOnly), typeof(object), true, dateOnlyValue, dateOnlyValue),
            (typeof(DateOnly), typeof(string), true, dateOnlyValue, dateOnlyValue.ToString("O", CultureInfo.InvariantCulture)),
            (typeof(DateTime), typeof(DateOnly), true, dateOnlyValue.ToDateTime(TimeOnly.MinValue), dateOnlyValue),
            (typeof(DateTime), typeof(DateTime), true, dateTimeValue, dateTimeValue),
            (typeof(DateTime), typeof(object), true, dateTimeValue, dateTimeValue),
            (typeof(DateTime), typeof(string), true, dateTimeValue, dateTimeValue.ToString("O", CultureInfo.InvariantCulture)),
            (typeof(DateTimeOffset), typeof(DateTimeOffset), true, dateTimeOffsetValue, dateTimeOffsetValue),
            (typeof(DateTimeOffset), typeof(object), true, dateTimeOffsetValue, dateTimeOffsetValue),
            (typeof(DateTimeOffset), typeof(string), true, dateTimeOffsetValue, dateTimeOffsetValue.ToString("O", CultureInfo.InvariantCulture)),
            (typeof(decimal), typeof(bool), true, 1M, true),
            (typeof(decimal), typeof(byte), true, decimalValue, Convert.ChangeType(decimalValue, typeof(byte), CultureInfo.InvariantCulture)),
            (typeof(decimal), typeof(decimal), true, decimalValue, decimalValue),
            (typeof(decimal), typeof(double), true, decimalValue, Convert.ChangeType(decimalValue, typeof(double), CultureInfo.InvariantCulture)),
            (typeof(decimal), typeof(short), true, decimalValue, Convert.ChangeType(decimalValue, typeof(short), CultureInfo.InvariantCulture)),
            (typeof(decimal), typeof(int), true, decimalValue, Convert.ChangeType(decimalValue, typeof(int), CultureInfo.InvariantCulture)),
            (typeof(decimal), typeof(long), true, decimalValue, Convert.ChangeType(decimalValue, typeof(long), CultureInfo.InvariantCulture)),
            (typeof(decimal), typeof(object), true, decimalValue, decimalValue),
            (typeof(decimal), typeof(sbyte), true, decimalValue, Convert.ChangeType(decimalValue, typeof(sbyte), CultureInfo.InvariantCulture)),
            (typeof(decimal), typeof(float), true, decimalValue, Convert.ChangeType(decimalValue, typeof(float), CultureInfo.InvariantCulture)),
            (typeof(decimal), typeof(string), true, decimalValue, decimalValue.ToString(CultureInfo.InvariantCulture)),
            (typeof(decimal), typeof(TestEnum), true, (decimal)enumValue, enumValue),
            (typeof(decimal), typeof(ushort), true, decimalValue, Convert.ChangeType(decimalValue, typeof(ushort), CultureInfo.InvariantCulture)),
            (typeof(decimal), typeof(uint), true, decimalValue, Convert.ChangeType(decimalValue, typeof(uint), CultureInfo.InvariantCulture)),
            (typeof(decimal), typeof(ulong), true, decimalValue, Convert.ChangeType(decimalValue, typeof(ulong), CultureInfo.InvariantCulture)),
            (typeof(double), typeof(bool), true, 1.0, true),
            (typeof(double), typeof(byte), true, doubleValue, Convert.ChangeType(doubleValue, typeof(byte), CultureInfo.InvariantCulture)),
            (typeof(double), typeof(decimal), true, doubleValue, Convert.ChangeType(doubleValue, typeof(decimal), CultureInfo.InvariantCulture)),
            (typeof(double), typeof(double), true, doubleValue, doubleValue),
            (typeof(double), typeof(short), true, doubleValue, Convert.ChangeType(doubleValue, typeof(short), CultureInfo.InvariantCulture)),
            (typeof(double), typeof(int), true, doubleValue, Convert.ChangeType(doubleValue, typeof(int), CultureInfo.InvariantCulture)),
            (typeof(double), typeof(long), true, doubleValue, Convert.ChangeType(doubleValue, typeof(long), CultureInfo.InvariantCulture)),
            (typeof(double), typeof(object), true, doubleValue, doubleValue),
            (typeof(double), typeof(sbyte), true, doubleValue, Convert.ChangeType(doubleValue, typeof(sbyte), CultureInfo.InvariantCulture)),
            (typeof(double), typeof(float), true, doubleValue, Convert.ChangeType(doubleValue, typeof(float), CultureInfo.InvariantCulture)),
            (typeof(double), typeof(string), true, doubleValue, doubleValue.ToString(CultureInfo.InvariantCulture)),
            (typeof(double), typeof(TestEnum), true, (double)enumValue, enumValue),
            (typeof(double), typeof(ushort), true, doubleValue, Convert.ChangeType(doubleValue, typeof(ushort), CultureInfo.InvariantCulture)),
            (typeof(double), typeof(uint), true, doubleValue, Convert.ChangeType(doubleValue, typeof(uint), CultureInfo.InvariantCulture)),
            (typeof(double), typeof(ulong), true, doubleValue, Convert.ChangeType(doubleValue, typeof(ulong), CultureInfo.InvariantCulture)),
            (typeof(Guid), typeof(byte[]), true, guidValue, guidValue.ToByteArray()),
            (typeof(Guid), typeof(Guid), true, guidValue, guidValue),
            (typeof(Guid), typeof(object), true, guidValue, guidValue),
            (typeof(Guid), typeof(string), true, guidValue, guidValue.ToString("D")),
            (typeof(short), typeof(bool), true, (short)1, true),
            (typeof(short), typeof(byte), true, int16Value, (byte) int16Value),
            (typeof(short), typeof(char), true, int16Value, (char) int16Value),
            (typeof(short), typeof(decimal), true, int16Value, (decimal) int16Value),
            (typeof(short), typeof(double), true, int16Value, (double)int16Value),
            (typeof(short), typeof(short), true, int16Value, int16Value),
            (typeof(short), typeof(int), true, int16Value, (int)int16Value),
            (typeof(short), typeof(long), true, int16Value, (long)int16Value),
            (typeof(short), typeof(object), true, int16Value, int16Value),
            (typeof(short), typeof(sbyte), true, int16Value, (sbyte)int16Value),
            (typeof(short), typeof(float), true, int16Value, (float)int16Value),
            (typeof(short), typeof(string), true, int16Value, int16Value.ToString(CultureInfo.InvariantCulture)),
            (typeof(short), typeof(TestEnum), true, (short)enumValue, enumValue),
            (typeof(short), typeof(ushort), true, int16Value, (ushort)int16Value),
            (typeof(short), typeof(uint), true, int16Value, (uint)int16Value),
            (typeof(short), typeof(ulong), true, int16Value, (ulong)int16Value),
            (typeof(int), typeof(bool), true, 1, true),
            (typeof(int), typeof(byte), true, int32Value, (byte)int32Value),
            (typeof(int), typeof(char), true, int32Value, (char) int32Value),
            (typeof(int), typeof(decimal), true, int32Value, (decimal)int32Value),
            (typeof(int), typeof(double), true, int32Value, (double)int32Value),
            (typeof(int), typeof(short), true, int32Value, (short)int32Value),
            (typeof(int), typeof(int), true, int32Value, int32Value),
            (typeof(int), typeof(long), true, int32Value, (long)int32Value),
            (typeof(int), typeof(object), true, int32Value, int32Value),
            (typeof(int), typeof(sbyte), true, int32Value, (sbyte)int32Value),
            (typeof(int), typeof(float), true, int32Value, (float)int32Value),
            (typeof(int), typeof(string), true, int32Value, int32Value.ToString(CultureInfo.InvariantCulture)),
            (typeof(int), typeof(TestEnum), true, (int)enumValue, enumValue),
            (typeof(int), typeof(ushort), true, int32Value, (ushort)int32Value),
            (typeof(int), typeof(uint), true, int32Value, (uint)int32Value),
            (typeof(int), typeof(ulong), true, int32Value, (ulong)int32Value),
            (typeof(long), typeof(bool), true, (long)1, true),
            (typeof(long), typeof(byte), true, int64Value, (byte) int64Value),
            (typeof(long), typeof(char), true, int64Value, (char) int64Value),
            (typeof(long), typeof(decimal), true, int64Value, (decimal) int64Value),
            (typeof(long), typeof(double), true, int64Value, (double)int64Value),
            (typeof(long), typeof(short), true, int64Value, (short)int64Value),
            (typeof(long), typeof(int), true, int64Value, (int)int64Value),
            (typeof(long), typeof(long), true, int64Value, int64Value),
            (typeof(long), typeof(object), true, int64Value, int64Value),
            (typeof(long), typeof(sbyte), true, int64Value, (sbyte)int64Value),
            (typeof(long), typeof(float), true, int64Value, (float)int64Value),
            (typeof(long), typeof(string), true, int64Value, int64Value.ToString(CultureInfo.InvariantCulture)),
            (typeof(long), typeof(TestEnum), true, (long)enumValue, enumValue),
            (typeof(long), typeof(ushort), true, int64Value, (ushort)int64Value),
            (typeof(long), typeof(uint), true, int64Value, (uint)int64Value),
            (typeof(long), typeof(ulong), true, int64Value, (ulong)int64Value),
            (typeof(IntPtr), typeof(IntPtr), true, intPtrValue, intPtrValue),
            (typeof(IntPtr), typeof(object), true, intPtrValue, intPtrValue),
            (typeof(sbyte), typeof(bool), true, (sbyte)1, true),
            (typeof(sbyte), typeof(byte), true, sbyteValue, (byte)sbyteValue),
            (typeof(sbyte), typeof(char), true, sbyteValue, (char) sbyteValue),
            (typeof(sbyte), typeof(decimal), true, sbyteValue, (decimal)sbyteValue),
            (typeof(sbyte), typeof(double), true, sbyteValue, (double)sbyteValue),
            (typeof(sbyte), typeof(short), true, sbyteValue, (short)sbyteValue),
            (typeof(sbyte), typeof(int), true, sbyteValue, (int)sbyteValue),
            (typeof(sbyte), typeof(long), true, sbyteValue, (long)sbyteValue),
            (typeof(sbyte), typeof(object), true, sbyteValue, sbyteValue),
            (typeof(sbyte), typeof(sbyte), true, sbyteValue, sbyteValue),
            (typeof(sbyte), typeof(float), true, sbyteValue, (float)sbyteValue),
            (typeof(sbyte), typeof(string), true, sbyteValue, sbyteValue.ToString(CultureInfo.InvariantCulture)),
            (typeof(sbyte), typeof(TestEnum), true, (sbyte)enumValue, enumValue),
            (typeof(sbyte), typeof(ushort), true, sbyteValue, (ushort)sbyteValue),
            (typeof(sbyte), typeof(uint), true, sbyteValue, (uint)sbyteValue),
            (typeof(sbyte), typeof(ulong), true, sbyteValue, (ulong)sbyteValue),
            (typeof(float), typeof(bool), true, (float)1, true),
            (typeof(float), typeof(byte), true, singleValue, Convert.ChangeType(singleValue, typeof(byte), CultureInfo.InvariantCulture)),
            (typeof(float), typeof(decimal), true, singleValue, Convert.ChangeType(singleValue, typeof(decimal), CultureInfo.InvariantCulture)),
            (typeof(float), typeof(double), true, singleValue, Convert.ChangeType(singleValue, typeof(double), CultureInfo.InvariantCulture)),
            (typeof(float), typeof(short), true, singleValue, Convert.ChangeType(singleValue, typeof(short), CultureInfo.InvariantCulture)),
            (typeof(float), typeof(int), true, singleValue, Convert.ChangeType(singleValue, typeof(int), CultureInfo.InvariantCulture)),
            (typeof(float), typeof(long), true, singleValue, Convert.ChangeType(singleValue, typeof(long), CultureInfo.InvariantCulture)),
            (typeof(float), typeof(object), true, singleValue, singleValue),
            (typeof(float), typeof(sbyte), true, singleValue, Convert.ChangeType(singleValue, typeof(sbyte), CultureInfo.InvariantCulture)),
            (typeof(float), typeof(float), true, singleValue, Convert.ChangeType(singleValue, typeof(float), CultureInfo.InvariantCulture)),
            (typeof(float), typeof(string), true, singleValue, singleValue.ToString(CultureInfo.InvariantCulture)),
            (typeof(float), typeof(TestEnum), true, (float)enumValue, enumValue),
            (typeof(float), typeof(ushort), true, singleValue, Convert.ChangeType(singleValue, typeof(ushort), CultureInfo.InvariantCulture)),
            (typeof(float), typeof(uint), true, singleValue, Convert.ChangeType(singleValue, typeof(uint), CultureInfo.InvariantCulture)),
            (typeof(float), typeof(ulong), true, singleValue, Convert.ChangeType(singleValue, typeof(ulong), CultureInfo.InvariantCulture)),
            (typeof(string), typeof(bool), true, "True", true),
            (typeof(string), typeof(byte), true, byteValue.ToString(CultureInfo.InvariantCulture), byteValue),
            (typeof(string), typeof(char), true, charValue.ToString(CultureInfo.InvariantCulture), charValue),
            (typeof(string), typeof(DateOnly), true, dateOnlyValue.ToString("O", CultureInfo.InvariantCulture), dateOnlyValue),
            (typeof(string), typeof(DateTime), true, dateTimeValue.ToString("O", CultureInfo.InvariantCulture), dateTimeValue),
            (typeof(string), typeof(DateTimeOffset), true, dateTimeOffsetValue.ToString("O", CultureInfo.InvariantCulture), dateTimeOffsetValue),
            (typeof(string), typeof(decimal), true, decimalValue.ToString(CultureInfo.InvariantCulture), decimalValue),
            (typeof(string), typeof(double), true, doubleValue.ToString(CultureInfo.InvariantCulture), doubleValue),
            (typeof(string), typeof(Guid), true, guidValue.ToString("D"), guidValue),
            (typeof(string), typeof(short), true, int16Value.ToString(CultureInfo.InvariantCulture), int16Value),
            (typeof(string), typeof(int), true, int32Value.ToString(CultureInfo.InvariantCulture), int32Value),
            (typeof(string), typeof(long), true, int64Value.ToString(CultureInfo.InvariantCulture), int64Value),
            (typeof(string), typeof(object), true, stringValue, stringValue),
            (typeof(string), typeof(sbyte), true, sbyteValue.ToString(CultureInfo.InvariantCulture), sbyteValue),
            (typeof(string), typeof(float), true, singleValue.ToString(CultureInfo.InvariantCulture), singleValue),
            (typeof(string), typeof(string), true, stringValue, stringValue),
            (typeof(string), typeof(TestEnum), true, enumValue.ToString(), enumValue),
            (typeof(string), typeof(TimeSpan), true, timeSpanValue.ToString("g", CultureInfo.InvariantCulture), timeSpanValue),
            (typeof(string), typeof(ushort), true, uint16Value.ToString(CultureInfo.InvariantCulture), uint16Value),
            (typeof(string), typeof(uint), true, uint32Value.ToString(CultureInfo.InvariantCulture), uint32Value),
            (typeof(string), typeof(ulong), true, uint64Value.ToString(CultureInfo.InvariantCulture), uint64Value),
            (typeof(TestEnum), typeof(byte), true, enumValue, (byte)enumValue),
            (typeof(TestEnum), typeof(decimal), true, enumValue, (decimal)enumValue),
            (typeof(TestEnum), typeof(double), true, enumValue, (double)enumValue),
            (typeof(TestEnum), typeof(short), true, enumValue, (short)enumValue),
            (typeof(TestEnum), typeof(int), true, enumValue, (int)enumValue),
            (typeof(TestEnum), typeof(long), true, enumValue, (long)enumValue),
            (typeof(TestEnum), typeof(object), true, enumValue, enumValue),
            (typeof(TestEnum), typeof(sbyte), true, enumValue, (sbyte)enumValue),
            (typeof(TestEnum), typeof(float), true, enumValue, (float)enumValue),
            (typeof(TestEnum), typeof(string), true, enumValue, enumValue.ToString()),
            (typeof(TestEnum), typeof(TestEnum), true, enumValue, enumValue),
            (typeof(TestEnum), typeof(ushort), true, enumValue, (ushort)enumValue),
            (typeof(TestEnum), typeof(uint), true, enumValue, (uint)enumValue),
            (typeof(TestEnum), typeof(ulong), true, enumValue, (ulong)enumValue),
            (typeof(TimeOnly), typeof(object), true, timeOnlyValue, timeOnlyValue),
            (typeof(TimeOnly), typeof(string), true, timeOnlyValue, timeOnlyValue.ToString("O", CultureInfo.InvariantCulture)),
            (typeof(TimeOnly), typeof(TimeOnly), true, timeOnlyValue, timeOnlyValue),
            (typeof(TimeSpan), typeof(object), true, timeSpanValue, timeSpanValue),
            (typeof(TimeSpan), typeof(string), true, timeSpanValue, timeSpanValue.ToString("g", CultureInfo.InvariantCulture)),
            (typeof(TimeSpan), typeof(TimeOnly), true, timeSpanValue, TimeOnly.FromTimeSpan(timeSpanValue)),
            (typeof(TimeSpan), typeof(TimeSpan), true, timeSpanValue, timeSpanValue),
            (typeof(ushort), typeof(bool), true, (ushort)1, true),
            (typeof(ushort), typeof(byte), true, uint16Value, (byte) uint16Value),
            (typeof(ushort), typeof(char), true, uint16Value, (char) uint16Value),
            (typeof(ushort), typeof(decimal), true, uint16Value, (decimal) uint16Value),
            (typeof(ushort), typeof(double), true, uint16Value, (double)uint16Value),
            (typeof(ushort), typeof(short), true, uint16Value, (short)uint16Value),
            (typeof(ushort), typeof(int), true, uint16Value, (int)uint16Value),
            (typeof(ushort), typeof(long), true, uint16Value, (long)uint16Value),
            (typeof(ushort), typeof(object), true, uint16Value, uint16Value),
            (typeof(ushort), typeof(sbyte), true, uint16Value, (sbyte)uint16Value),
            (typeof(ushort), typeof(float), true, uint16Value, (float)uint16Value),
            (typeof(ushort), typeof(string), true, uint16Value, uint16Value.ToString(CultureInfo.InvariantCulture)),
            (typeof(ushort), typeof(TestEnum), true, (ushort)enumValue, enumValue),
            (typeof(ushort), typeof(ushort), true, uint16Value, uint16Value),
            (typeof(ushort), typeof(uint), true, uint16Value, (uint)uint16Value),
            (typeof(ushort), typeof(ulong), true, uint16Value, (ulong)uint16Value),
            (typeof(uint), typeof(bool), true, (uint)1, true),
            (typeof(uint), typeof(byte), true, uint32Value, (byte) uint32Value),
            (typeof(uint), typeof(char), true, uint32Value, (char) uint32Value),
            (typeof(uint), typeof(decimal), true, uint32Value, (decimal) uint32Value),
            (typeof(uint), typeof(double), true, uint32Value, (double)uint32Value),
            (typeof(uint), typeof(int), true, uint32Value, (int)uint32Value),
            (typeof(uint), typeof(int), true, uint32Value, (int)uint32Value),
            (typeof(uint), typeof(long), true, uint32Value, (long)uint32Value),
            (typeof(uint), typeof(object), true, uint32Value, uint32Value),
            (typeof(uint), typeof(sbyte), true, uint32Value, (sbyte)uint32Value),
            (typeof(uint), typeof(float), true, uint32Value, (float)uint32Value),
            (typeof(uint), typeof(string), true, uint32Value, uint32Value.ToString(CultureInfo.InvariantCulture)),
            (typeof(uint), typeof(TestEnum), true, (uint)enumValue, enumValue),
            (typeof(uint), typeof(ushort), true, uint32Value, (ushort)uint32Value),
            (typeof(uint), typeof(uint), true, uint32Value, uint32Value),
            (typeof(uint), typeof(ulong), true, uint32Value, (ulong)uint32Value),
            (typeof(ulong), typeof(bool), true, (ulong)1, true),
            (typeof(ulong), typeof(byte), true, uint64Value, (byte) uint64Value),
            (typeof(ulong), typeof(char), true, uint64Value, (char) uint64Value),
            (typeof(ulong), typeof(decimal), true, uint64Value, (decimal) uint64Value),
            (typeof(ulong), typeof(double), true, uint64Value, (double)uint64Value),
            (typeof(ulong), typeof(short), true, uint64Value, (short)uint64Value),
            (typeof(ulong), typeof(int), true, uint64Value, (int)uint64Value),
            (typeof(ulong), typeof(long), true, uint64Value, (long)uint64Value),
            (typeof(ulong), typeof(object), true, uint64Value, uint64Value),
            (typeof(ulong), typeof(sbyte), true, uint64Value, (sbyte)uint64Value),
            (typeof(ulong), typeof(float), true, uint64Value, (float)uint64Value),
            (typeof(ulong), typeof(string), true, uint64Value, uint64Value.ToString(CultureInfo.InvariantCulture)),
            (typeof(ulong), typeof(TestEnum), true, (ulong)enumValue, enumValue),
            (typeof(ulong), typeof(ushort), true, uint64Value, (ushort)uint64Value),
            (typeof(ulong), typeof(uint), true, uint64Value, (uint)uint64Value),
            (typeof(ulong), typeof(ulong), true, uint64Value, uint64Value),
            (typeof(UIntPtr), typeof(object), true, uintPtrValue, uintPtrValue),
            (typeof(UIntPtr), typeof(UIntPtr), true, uintPtrValue, uintPtrValue),
            (typeof(char), typeof(Guid), false, charValue, null),
            (typeof(int), typeof(Guid), false, int32Value, null),
            (typeof(DateTime), typeof(Guid), false, dateTimeValue, null),
            (typeof(Guid), typeof(DateTime), false, guidValue, null),
            (typeof(DateOnly), typeof(DateTime), false, dateOnlyValue, null),
            (typeof(TimeOnly), typeof(TimeSpan), false, timeOnlyValue, null),
            (typeof(DateOnly), typeof(Guid), false, dateOnlyValue, null),
            (typeof(TimeOnly), typeof(Guid), false, timeOnlyValue, null),
        ];

        // @formatter:on
    }
}
