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
            typeof(byte[])
        );

        expression.ToString().Should().Match("Convert(*DbDataReader*.GetValue(1), Byte[])");
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

        expression.ToString().Should().Match("Convert(*DbDataReader*.GetValue(1), DateOnly)");
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

        expression.ToString().Should().Match("Convert(*DbDataReader*.GetValue(1), DateTimeOffset)");
    }

    [Theory]
    [InlineData(typeof(bool), "*DbDataReader*.GetBoolean(1)")]
    [InlineData(typeof(byte), "*DbDataReader*.GetByte(1)")]
    [InlineData(typeof(DateTime), "*DbDataReader*.GetDateTime(1)")]
    [InlineData(typeof(decimal), "*DbDataReader*.GetDecimal(1)")]
    [InlineData(typeof(double), "*DbDataReader*.GetDouble(1)")]
    [InlineData(typeof(float), "*DbDataReader*.GetFloat(1)")]
    [InlineData(typeof(Guid), "*DbDataReader*.GetGuid(1)")]
    [InlineData(typeof(short), "*DbDataReader*.GetInt16(1)")]
    [InlineData(typeof(int), "*DbDataReader*.GetInt32(1)")]
    [InlineData(typeof(long), "*DbDataReader*.GetInt64(1)")]
    [InlineData(typeof(string), "*DbDataReader*.GetString(1)")]
    public void CreateGetDbDataReaderFieldValueExpression_ShouldCallTypedGetMethod(
        Type fieldType,
        string expectedExpression
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

        expression.ToString().Should().Match(expectedExpression);
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

        expression.ToString().Should().Match("Convert(*DbDataReader*.GetValue(1), TimeOnly)");
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

        expression.ToString().Should().Match("Convert(*DbDataReader*.GetValue(1), TimeSpan)");
    }

    [Fact]
    public void CreateGetDbDataReaderFieldValueExpression_UnsupportedFieldType_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        Invoking(() =>
                MaterializerFactoryHelper.CreateGetDbDataReaderFieldValueExpression(
                    Expression.Constant(dataReader),
                    Expression.Constant(1),
                    1,
                    "FieldA",
                    typeof(BigInteger)
                )
            )
            .Should()
            .Throw<ArgumentException>()
            .WithMessage(
                $"The data type {typeof(BigInteger)} of the column 'FieldA' returned by the SQL statement is not "
                    + "supported.*"
            );

        Invoking(() =>
                MaterializerFactoryHelper.CreateGetDbDataReaderFieldValueExpression(
                    Expression.Constant(dataReader),
                    Expression.Constant(1),
                    1,
                    "",
                    typeof(BigInteger)
                )
            )
            .Should()
            .Throw<ArgumentException>()
            .WithMessage(
                $"The data type {typeof(BigInteger)} of the 2nd column returned by the SQL statement is not "
                    + "supported.*"
            );
    }

    [Theory]
    [InlineData(typeof(bool), nameof(DbDataReader.GetBoolean))]
    [InlineData(typeof(byte), nameof(DbDataReader.GetByte))]
    [InlineData(typeof(DateTime), nameof(DbDataReader.GetDateTime))]
    [InlineData(typeof(decimal), nameof(DbDataReader.GetDecimal))]
    [InlineData(typeof(double), nameof(DbDataReader.GetDouble))]
    [InlineData(typeof(float), nameof(DbDataReader.GetFloat))]
    [InlineData(typeof(Guid), nameof(DbDataReader.GetGuid))]
    [InlineData(typeof(short), nameof(DbDataReader.GetInt16))]
    [InlineData(typeof(int), nameof(DbDataReader.GetInt32))]
    [InlineData(typeof(long), nameof(DbDataReader.GetInt64))]
    [InlineData(typeof(string), nameof(DbDataReader.GetString))]
    [InlineData(typeof(byte[]), nameof(DbDataReader.GetValue))]
    [InlineData(typeof(DateOnly), nameof(DbDataReader.GetValue))]
    [InlineData(typeof(DateTimeOffset), nameof(DbDataReader.GetValue))]
    [InlineData(typeof(TimeOnly), nameof(DbDataReader.GetValue))]
    [InlineData(typeof(TimeSpan), nameof(DbDataReader.GetValue))]
    public void CreateGetDbDataReaderFieldValueFunction_ShouldCallTheSameMethodAsTheExpression(
        Type fieldType,
        string expectedDbDataReaderMethodName
    )
    {
        var dataReader = Substitute.For<DbDataReader>();

        MaterializerFactoryHelper
            .CreateGetDbDataReaderFieldValueExpression(
                Expression.Constant(dataReader),
                Expression.Constant(1),
                1,
                "FieldA",
                fieldType
            )
            .ToString()
            .Should()
            .Contain($".{expectedDbDataReaderMethodName}(1)");

        _ = MaterializerFactoryHelper.CreateGetDbDataReaderFieldValueFunction(1, "FieldA", fieldType)(dataReader);

        dataReader
            .ReceivedCalls()
            .Select(call => call.GetMethodInfo().Name)
            .Should()
            .Equal(expectedDbDataReaderMethodName);
    }

    [Fact]
    public void CreateGetDbDataReaderFieldValueFunction_UnsupportedFieldType_ShouldThrow()
    {
        Invoking(() =>
                MaterializerFactoryHelper.CreateGetDbDataReaderFieldValueFunction(1, "FieldA", typeof(BigInteger))
            )
            .Should()
            .Throw<ArgumentException>()
            .WithMessage(
                $"The data type {typeof(BigInteger)} of the column 'FieldA' returned by the SQL statement is not "
                    + "supported.*"
            );

        Invoking(() => MaterializerFactoryHelper.CreateGetDbDataReaderFieldValueFunction(1, "", typeof(BigInteger)))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage(
                $"The data type {typeof(BigInteger)} of the 2nd column returned by the SQL statement is not "
                    + "supported.*"
            );
    }

    [Fact]
    public void DbDataReaderGetValueMethod_ShouldReferenceDbDataReaderGetValue()
    {
        var method = MaterializerFactoryHelper.DbDataReaderGetValueMethod;

        method.DeclaringType.Should().Be(typeof(DbDataReader));

        method.Name.Should().Be(nameof(DbDataReader.GetValue));

        method
            .GetParameters()
            .Select(p => (p.Name, p.ParameterType))
            .Should()
            .BeEquivalentTo([("ordinal", typeof(int))]);
    }

    [Fact]
    public void DbDataReaderIsDBNullMethod_ShouldReferenceDbDataReaderIsDBNull()
    {
        var method = MaterializerFactoryHelper.DbDataReaderIsDBNullMethod;

        method.DeclaringType.Should().Be(typeof(DbDataReader));

        method.Name.Should().Be(nameof(DbDataReader.IsDBNull));

        method
            .GetParameters()
            .Select(p => (p.Name, p.ParameterType))
            .Should()
            .BeEquivalentTo([("ordinal", typeof(int))]);
    }

    [Theory]
    [InlineData(typeof(bool), true)]
    [InlineData(typeof(byte), true)]
    [InlineData(typeof(DateOnly), true)]
    [InlineData(typeof(DateTime), true)]
    [InlineData(typeof(decimal), true)]
    [InlineData(typeof(double), true)]
    [InlineData(typeof(float), true)]
    [InlineData(typeof(Guid), true)]
    [InlineData(typeof(short), true)]
    [InlineData(typeof(int), true)]
    [InlineData(typeof(long), true)]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(byte[]), true)]
    [InlineData(typeof(TimeSpan), true)]
    [InlineData(typeof(TimeOnly), true)]
    [InlineData(typeof(DateTimeOffset), true)]
    [InlineData(typeof(char), false)]
    [InlineData(typeof(BigInteger), false)]
    public void IsDbDataReaderTypedGetMethodAvailable_ShouldReturnWhetherTypedGetMethodIsAvailable(
        Type fieldType,
        bool expectedResult
    ) => MaterializerFactoryHelper.IsDbDataReaderTypedGetMethodAvailable(fieldType).Should().Be(expectedResult);

    [Fact]
    public void MakeValueConverterConvertValueToTypeMethod_ShouldReferenceValueConverterConvertValueToType()
    {
        var method = MaterializerFactoryHelper.MakeValueConverterConvertValueToTypeMethod(typeof(int));

        method.DeclaringType.Should().Be(typeof(ValueConverter));

        method.Name.Should().Be(nameof(ValueConverter.ConvertValueToType));

        method.GetGenericArguments().Should().Equal(typeof(int));

        method.GetParameters().Select(p => (p.Name, p.ParameterType)).Should().Equal(("value", typeof(object)));
    }

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
                typeof(int)
            )
        );

        ArgumentNullGuardVerifier.Verify(() =>
            MaterializerFactoryHelper.CreateGetDbDataReaderFieldValueFunction(1, "FieldA", typeof(int))
        );

        ArgumentNullGuardVerifier.Verify(() =>
            MaterializerFactoryHelper.IsDbDataReaderTypedGetMethodAvailable(typeof(int))
        );
    }

    [Fact]
    public void StringCharsProperty_ShouldReferenceStringCharsIndexer()
    {
        var property = MaterializerFactoryHelper.StringCharsProperty;

        property.DeclaringType.Should().Be(typeof(string));

        property.Name.Should().Be("Chars");

        property.PropertyType.Should().Be(typeof(char));
    }

    [Fact]
    public void StringConcatMethod_ShouldReferenceStringConcatWithThreeStringParameters()
    {
        var method = MaterializerFactoryHelper.StringConcatMethod;

        method.DeclaringType.Should().Be(typeof(string));

        method.Name.Should().Be(nameof(String.Concat));

        method
            .GetParameters()
            .Select(p => (p.Name, p.ParameterType))
            .Should()
            .Equal(("str0", typeof(string)), ("str1", typeof(string)), ("str2", typeof(string)));

        method.ReturnType.Should().Be(typeof(string));
    }

    [Fact]
    public void StringLengthProperty_ShouldReferenceStringLengthProperty()
    {
        var property = MaterializerFactoryHelper.StringLengthProperty;

        property.DeclaringType.Should().Be(typeof(string));

        property.Name.Should().Be(nameof(String.Length));

        property.PropertyType.Should().Be(typeof(int));
    }
}
