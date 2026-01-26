using System.Linq.Expressions;
using System.Numerics;
using RentADeveloper.DbConnectionPlus.Converters;
using RentADeveloper.DbConnectionPlus.Materializers;

namespace RentADeveloper.DbConnectionPlus.UnitTests.Materializers;

public class MaterializerFactoryHelperTests : UnitTestsBase
{
    [Fact]
    public void CreateGetDbDataReaderFieldValueExpression_BytesFieldType_ShouldCallGetValueAndConvert()
    {
        var dataReader = Substitute.For<DbDataReader>();

        var expression = MaterializerFactoryHelper.CreateGetDbDataReaderFieldValueExpression(
            Expression.Constant(dataReader),
            Expression.Constant(1),
            1,
            "FieldA",
            typeof(Byte[])
        );

        expression.ToString()
            .Should().Match("Convert(*DbDataReader*.GetValue(1), Byte[])");
    }

    [Fact]
    public void CreateGetDbDataReaderFieldValueExpression_DateOnlyFieldType_ShouldCallGetValueAndConvert()
    {
        var dataReader = Substitute.For<DbDataReader>();

        var expression = MaterializerFactoryHelper.CreateGetDbDataReaderFieldValueExpression(
            Expression.Constant(dataReader),
            Expression.Constant(1),
            1,
            "FieldA",
            typeof(DateOnly)
        );

        expression.ToString()
            .Should().Match("Convert(*DbDataReader*.GetValue(1), DateOnly)");
    }

    [Fact]
    public void CreateGetDbDataReaderFieldValueExpression_DateTimeOffsetFieldType_ShouldCallGetValueAndConvert()
    {
        var dataReader = Substitute.For<DbDataReader>();

        var expression = MaterializerFactoryHelper.CreateGetDbDataReaderFieldValueExpression(
            Expression.Constant(dataReader),
            Expression.Constant(1),
            1,
            "FieldA",
            typeof(DateTimeOffset)
        );

        expression.ToString()
            .Should().Match("Convert(*DbDataReader*.GetValue(1), DateTimeOffset)");
    }

    [Theory]
    [InlineData(typeof(Boolean), "*DbDataReader*.GetBoolean(1)")]
    [InlineData(typeof(Byte), "*DbDataReader*.GetByte(1)")]
    [InlineData(typeof(DateTime), "*DbDataReader*.GetDateTime(1)")]
    [InlineData(typeof(Decimal), "*DbDataReader*.GetDecimal(1)")]
    [InlineData(typeof(Double), "*DbDataReader*.GetDouble(1)")]
    [InlineData(typeof(Single), "*DbDataReader*.GetFloat(1)")]
    [InlineData(typeof(Guid), "*DbDataReader*.GetGuid(1)")]
    [InlineData(typeof(Int16), "*DbDataReader*.GetInt16(1)")]
    [InlineData(typeof(Int32), "*DbDataReader*.GetInt32(1)")]
    [InlineData(typeof(Int64), "*DbDataReader*.GetInt64(1)")]
    [InlineData(typeof(String), "*DbDataReader*.GetString(1)")]
    public void CreateGetDbDataReaderFieldValueExpression_ShouldCallTypedGetMethod(
        Type fieldType,
        String expectedExpression
    )
    {
        var dataReader = Substitute.For<DbDataReader>();

        var expression = MaterializerFactoryHelper.CreateGetDbDataReaderFieldValueExpression(
            Expression.Constant(dataReader),
            Expression.Constant(1),
            1,
            "FieldA",
            fieldType
        );

        expression.ToString()
            .Should().Match(expectedExpression);
    }

    [Fact]
    public void CreateGetDbDataReaderFieldValueExpression_TimeOnlyFieldType_ShouldCallGetValueAndConvert()
    {
        var dataReader = Substitute.For<DbDataReader>();

        var expression = MaterializerFactoryHelper.CreateGetDbDataReaderFieldValueExpression(
            Expression.Constant(dataReader),
            Expression.Constant(1),
            1,
            "FieldA",
            typeof(TimeOnly)
        );

        expression.ToString()
            .Should().Match("Convert(*DbDataReader*.GetValue(1), TimeOnly)");
    }

    [Fact]
    public void CreateGetDbDataReaderFieldValueExpression_TimeSpanFieldType_ShouldCallGetValueAndConvert()
    {
        var dataReader = Substitute.For<DbDataReader>();

        var expression = MaterializerFactoryHelper.CreateGetDbDataReaderFieldValueExpression(
            Expression.Constant(dataReader),
            Expression.Constant(1),
            1,
            "FieldA",
            typeof(TimeSpan)
        );

        expression.ToString()
            .Should().Match("Convert(*DbDataReader*.GetValue(1), TimeSpan)");
    }

    [Fact]
    public void CreateGetDbDataReaderFieldValueExpression_UnsupportedFieldType_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        Invoking(() => MaterializerFactoryHelper.CreateGetDbDataReaderFieldValueExpression(
                    Expression.Constant(dataReader),
                    Expression.Constant(1),
                    1,
                    "FieldA",
                    typeof(BigInteger)
                )
            )
            .Should().Throw<ArgumentException>()
            .WithMessage(
                $"The data type {typeof(BigInteger)} of the column 'FieldA' returned by the SQL statement is not " +
                "supported.*"
            );

        Invoking(() => MaterializerFactoryHelper.CreateGetDbDataReaderFieldValueExpression(
                    Expression.Constant(dataReader),
                    Expression.Constant(1),
                    1,
                    "",
                    typeof(BigInteger)
                )
            )
            .Should().Throw<ArgumentException>()
            .WithMessage(
                $"The data type {typeof(BigInteger)} of the 2nd column returned by the SQL statement is not " +
                "supported.*"
            );
    }

    [Fact]
    public void DbDataReaderGetValueMethod_ShouldReferenceDbDataReaderGetValue()
    {
        var method = MaterializerFactoryHelper.DbDataReaderGetValueMethod;

        method.DeclaringType
            .Should().Be(typeof(DbDataReader));

        method.Name
            .Should().Be(nameof(DbDataReader.GetValue));

        method.GetParameters().Select(p => (p.Name, p.ParameterType))
            .Should().BeEquivalentTo([("ordinal", typeof(Int32))]);
    }

    [Fact]
    public void DbDataReaderIsDBNullMethod_ShouldReferenceDbDataReaderIsDBNull()
    {
        var method = MaterializerFactoryHelper.DbDataReaderIsDBNullMethod;

        method.DeclaringType
            .Should().Be(typeof(DbDataReader));

        method.Name
            .Should().Be(nameof(DbDataReader.IsDBNull));

        method.GetParameters().Select(p => (p.Name, p.ParameterType))
            .Should().BeEquivalentTo([("ordinal", typeof(Int32))]);
    }

    [Fact]
    public void EnumConverterConvertValueToEnumMemberMethod_ShouldReferenceEnumConverterConvertValueToEnum()
    {
        var method = MaterializerFactoryHelper.EnumConverterConvertValueToEnumMemberMethod;

        method.DeclaringType
            .Should().Be(typeof(EnumConverter));

        method.Name
            .Should().Be(nameof(EnumConverter.ConvertValueToEnumMember));

        method.GetParameters().Select(p => (p.Name, p.ParameterType))
            .Should().BeEquivalentTo([("value", typeof(Object))]);
    }

    [Theory]
    [InlineData(typeof(Boolean), true)]
    [InlineData(typeof(Byte), true)]
    [InlineData(typeof(DateOnly), true)]
    [InlineData(typeof(DateTime), true)]
    [InlineData(typeof(Decimal), true)]
    [InlineData(typeof(Double), true)]
    [InlineData(typeof(Single), true)]
    [InlineData(typeof(Guid), true)]
    [InlineData(typeof(Int16), true)]
    [InlineData(typeof(Int32), true)]
    [InlineData(typeof(Int64), true)]
    [InlineData(typeof(String), true)]
    [InlineData(typeof(Byte[]), true)]
    [InlineData(typeof(TimeSpan), true)]
    [InlineData(typeof(TimeOnly), true)]
    [InlineData(typeof(DateTimeOffset), true)]
    [InlineData(typeof(Char), false)]
    [InlineData(typeof(BigInteger), false)]
    public void IsDbDataReaderTypedGetMethodAvailable_ShouldReturnWhetherTypedGetMethodIsAvailable(
        Type fieldType,
        Boolean expectedResult
    ) =>
        MaterializerFactoryHelper.IsDbDataReaderTypedGetMethodAvailable(fieldType)
            .Should().Be(expectedResult);

    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        var dataReader = Substitute.For<DbDataReader>();

        ArgumentNullGuardVerifier.Verify(() =>
            MaterializerFactoryHelper.CreateGetDbDataReaderFieldValueExpression(
                Expression.Constant(dataReader),
                Expression.Constant(1),
                1,
                "FieldA",
                typeof(Int32)
            )
        );

        ArgumentNullGuardVerifier.Verify(() =>
            MaterializerFactoryHelper.IsDbDataReaderTypedGetMethodAvailable(
                typeof(Int32)
            )
        );
    }

    [Fact]
    public void StringCharsProperty_ShouldReferenceStringCharsIndexer()
    {
        var property = MaterializerFactoryHelper.StringCharsProperty;

        property.DeclaringType
            .Should().Be(typeof(String));

        property.Name
            .Should().Be("Chars");

        property.PropertyType
            .Should().Be(typeof(Char));
    }

    [Fact]
    public void StringConcatMethod_ShouldReferenceStringConcatWithThreeStringParameters()
    {
        var method = MaterializerFactoryHelper.StringConcatMethod;

        method.DeclaringType
            .Should().Be(typeof(String));

        method.Name
            .Should().Be(nameof(String.Concat));

        method.GetParameters().Select(p => (p.Name, p.ParameterType))
            .Should().Equal(("str0", typeof(String)), ("str1", typeof(String)), ("str2", typeof(String)));

        method.ReturnType
            .Should().Be(typeof(String));
    }

    [Fact]
    public void StringLengthProperty_ShouldReferenceStringLengthProperty()
    {
        var property = MaterializerFactoryHelper.StringLengthProperty;

        property.DeclaringType
            .Should().Be(typeof(String));

        property.Name
            .Should().Be(nameof(String.Length));

        property.PropertyType
            .Should().Be(typeof(Int32));
    }

    [Fact]
    public void ValueConverterConvertValueToTypeMethod_ShouldReferenceValueConverterConvertValueToType()
    {
        var method = MaterializerFactoryHelper.ValueConverterConvertValueToTypeMethod;

        method.DeclaringType
            .Should().Be(typeof(ValueConverter));

        method.Name
            .Should().Be(nameof(ValueConverter.ConvertValueToType));

        method.GetParameters().Select(p => (p.Name, p.ParameterType))
            .Should().Equal(("value", typeof(Object)));
    }
}
