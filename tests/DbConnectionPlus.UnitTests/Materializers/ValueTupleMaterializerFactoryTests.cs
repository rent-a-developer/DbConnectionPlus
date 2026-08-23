using System.Data.SqlTypes;
using System.Numerics;
using System.Runtime.CompilerServices;
using NSubstitute.ExceptionExtensions;
using RentADeveloper.DbConnectionPlus.Extensions;
using RentADeveloper.DbConnectionPlus.Materializers;

namespace RentADeveloper.DbConnectionPlus.UnitTests.Materializers;

public class ValueTupleMaterializerFactoryTests : UnitTestsBase
{
    [Fact]
    public void GetMaterializer_DataReaderFieldCountDoesNotMatchValueTupleFieldCount_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(2);

        Invoking(() => ValueTupleMaterializerFactory.GetMaterializer<(int, int, int)>(dataReader))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage(
                $"The SQL statement returned 2 columns, but the value tuple type {typeof((int, int, int))} has "
                    + "3 fields. Make sure that the SQL statement returns the same number of columns as the number of "
                    + "fields in the value tuple type.*"
            );
    }

    [Fact]
    public void GetMaterializer_DataReaderFieldTypeNotCompatibleWithValueTupleFieldType_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("DateTime");
        dataReader.GetFieldType(0).Returns(typeof(Guid));

        Invoking(() => ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<DateTime>>(dataReader))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage(
                $"The data type {typeof(Guid)} of the column 'DateTime' returned by the SQL statement is not "
                    + $"compatible with the field type {typeof(DateTime)} of the corresponding field of the value tuple "
                    + $"type {typeof(ValueTuple<DateTime>)}.*"
            );

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("");
        dataReader.GetFieldType(0).Returns(typeof(Guid));

        Invoking(() => ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<DateTime>>(dataReader))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage(
                $"The data type {typeof(Guid)} of the 1st column returned by the SQL statement is not "
                    + $"compatible with the field type {typeof(DateTime)} of the corresponding field of the value tuple "
                    + $"type {typeof(ValueTuple<DateTime>)}.*"
            );
    }

    [Fact]
    public void GetMaterializer_DataReaderHasNoFields_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(0);

        Invoking(() => ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<int>>(dataReader))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("The SQL statement did not return any columns.*");
    }

    [Fact]
    public void GetMaterializer_DataReaderHasUnsupportedFieldType_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Value");
        dataReader.GetFieldType(0).Returns(typeof(BigInteger));

        Invoking(() => ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<BigInteger>>(dataReader))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage(
                $"The data type {typeof(BigInteger)} of the column 'Value' returned by the SQL statement is not "
                    + "supported.*"
            );

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("");
        dataReader.GetFieldType(0).Returns(typeof(BigInteger));

        Invoking(() => ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<BigInteger>>(dataReader))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage(
                $"The data type {typeof(BigInteger)} of the 1st column returned by the SQL statement is not supported.*"
            );
    }

    [Fact]
    public void GetMaterializer_TypeIsNotAValueTupleType_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        Invoking(() => ValueTupleMaterializerFactory.GetMaterializer<NotAValueTuple>(dataReader))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage($"The specified type {typeof(NotAValueTuple)} is not a {typeof(ValueTuple)} type.*");
    }

    [Fact]
    public void Materializer_DataReaderHasCompatibleFieldTypes_ShouldConvertValues()
    {
        var dataReader = Substitute.For<DbDataReader>();

        var entityId = Generate.Id();
        var enumValue = Generate.Single<TestEnum>();

        dataReader.FieldCount.Returns(2);

        dataReader.GetName(0).Returns("Id");
        dataReader.GetFieldType(0).Returns(typeof(string));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns(entityId.ToString());

        dataReader.GetName(1).Returns("Enum");
        dataReader.GetFieldType(1).Returns(typeof(decimal));
        dataReader.IsDBNull(1).Returns(false);
        dataReader.GetDecimal(1).Returns((decimal)enumValue);

        var materializer = ValueTupleMaterializerFactory.GetMaterializer<(long Id, TestEnum Enum)>(dataReader);

        var entity = materializer(dataReader);

        entity.Id.Should().Be(entityId);

        entity.Enum.Should().Be(enumValue);
    }

    [Fact]
    public void Materializer_EnumValueTupleField_DataReaderContainsInteger_ShouldConvertToEnumMember()
    {
        var dataReader = Substitute.For<DbDataReader>();

        var enumValue = Generate.Single<TestEnum>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Enum");
        dataReader.GetFieldType(0).Returns(typeof(int));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetInt32(0).Returns((int)enumValue);

        var materializer = ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<TestEnum>>(dataReader);

        var valueTuple = materializer(dataReader);

        valueTuple.Item1.Should().Be(enumValue);
    }

    [Fact]
    public void Materializer_EnumValueTupleField_DataReaderContainsIntegerNotMatchingAnyEnumMemberValue_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Enum");
        dataReader.GetFieldType(0).Returns(typeof(int));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetInt32(0).Returns(999);

        var materializer = ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<TestEnum>>(dataReader);

        Invoking(() => materializer(dataReader))
            .Should()
            .Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Enum' returned by the SQL statement contains a value that could not be converted to "
                    + $"the type {typeof(TestEnum)} of the corresponding field of the value tuple type "
                    + $"{typeof(ValueTuple<TestEnum>)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value '999' ({typeof(int)}) to an enum member of the type "
                    + $"{typeof(TestEnum)}. That value does not match any of the values of the enum's members.*"
            );
    }

    [Fact]
    public void Materializer_EnumValueTupleField_DataReaderContainsString_ShouldConvertToEnumMember()
    {
        var dataReader = Substitute.For<DbDataReader>();

        var enumValue = Generate.Single<TestEnum>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Enum");
        dataReader.GetFieldType(0).Returns(typeof(string));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns(enumValue.ToString());

        var materializer = ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<TestEnum>>(dataReader);

        var valueTuple = materializer(dataReader);

        valueTuple.Item1.Should().Be(enumValue);
    }

    [Fact]
    public void Materializer_EnumValueTupleField_DataReaderContainsStringNotMatchingAnyEnumMemberName_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Enum");
        dataReader.GetFieldType(0).Returns(typeof(string));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns("NonExistent");

        var materializer = ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<TestEnum>>(dataReader);

        Invoking(() => materializer(dataReader))
            .Should()
            .Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Enum' returned by the SQL statement contains a value that could not be converted to "
                    + $"the type {typeof(TestEnum)} of the corresponding field of the value tuple type "
                    + $"{typeof(ValueTuple<TestEnum>)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'NonExistent' to an enum member of the type {typeof(TestEnum)}. That "
                    + "string does not match any of the names of the enum's members.*"
            );
    }

    [Fact]
    public void Materializer_MoreThan7FieldsValueTupleType_ShouldMaterializeNestedValueTuples()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(15);

        for (var i = 0; i < 15; i++)
        {
            dataReader.GetName(i).Returns($"Value{i + 1}");
            dataReader.GetFieldType(i).Returns(typeof(int));
            dataReader.IsDBNull(i).Returns(false);
            dataReader.GetInt32(i).Returns(i + 1);
        }

        var materializer = ValueTupleMaterializerFactory.GetMaterializer<(
            int,
            int,
            int,
            int,
            int,
            int,
            int,
            int,
            int,
            int,
            int,
            int,
            int,
            int,
            int
        )>(dataReader);

        var valueTuple = materializer(dataReader);

        valueTuple.Item1.Should().Be(1);

        valueTuple.Item2.Should().Be(2);

        valueTuple.Item3.Should().Be(3);

        valueTuple.Item4.Should().Be(4);

        valueTuple.Item5.Should().Be(5);

        valueTuple.Item6.Should().Be(6);

        valueTuple.Item7.Should().Be(7);

        valueTuple.Rest.Item1.Should().Be(8);

        valueTuple.Rest.Item2.Should().Be(9);

        valueTuple.Rest.Item3.Should().Be(10);

        valueTuple.Rest.Item4.Should().Be(11);

        valueTuple.Rest.Item5.Should().Be(12);

        valueTuple.Rest.Item6.Should().Be(13);

        valueTuple.Rest.Item7.Should().Be(14);

        valueTuple.Rest.Rest.Item1.Should().Be(15);
    }

    [Fact]
    public void Materializer_NonNullableCharValueTupleField_DataReaderFieldContainsStringWithLengthNotOne_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Char");
        dataReader.GetFieldType(0).Returns(typeof(string));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns(string.Empty);

        var materializer = ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<char>>(dataReader);

        Invoking(() => materializer(dataReader))
            .Should()
            .Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Char' returned by the SQL statement contains a value that could not be converted to "
                    + $"the type {typeof(char)} of the corresponding field of the value tuple type "
                    + $"{typeof(ValueTuple<char>)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string '' to the type {typeof(char)}. The string must be exactly one "
                    + "character long."
            );

        dataReader.GetString(0).Returns("ab");

        Invoking(() => materializer(dataReader))
            .Should()
            .Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Char' returned by the SQL statement contains a value that could not be converted to "
                    + $"the type {typeof(char)} of the corresponding field of the value tuple type "
                    + $"{typeof(ValueTuple<char>)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(char)}. The string must be exactly one "
                    + "character long."
            );
    }

    [Fact]
    public void Materializer_NonNullableCharValueTupleField_DataReaderFieldContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        var character = Generate.Single<char>();

        dataReader.GetName(0).Returns("Char");
        dataReader.GetFieldType(0).Returns(typeof(string));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns(character.ToString());

        var materializer = ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<char>>(dataReader);

        var valueTuple = materializer(dataReader);

        valueTuple.Item1.Should().Be(character);
    }

    [Fact]
    public void Materializer_NonNullableValueTupleField_DataReaderFieldContainsNull_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Id");
        dataReader.GetFieldType(0).Returns(typeof(long));
        dataReader.IsDBNull(0).Returns(true);

        var materializer = ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<long>>(dataReader);

        Invoking(() => materializer(dataReader))
            .Should()
            .Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Id' returned by the SQL statement contains a NULL value, but the corresponding field "
                    + $"of the value tuple type {typeof(ValueTuple<long>)} is non-nullable.*"
            );
    }

    [Fact]
    public void Materializer_NullableCharValueTupleField_DataReaderFieldContainsStringWithLengthNotOne_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Char");
        dataReader.GetFieldType(0).Returns(typeof(string));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns(string.Empty);

        var materializer = ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<char?>>(dataReader);

        Invoking(() => materializer(dataReader))
            .Should()
            .Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Char' returned by the SQL statement contains a value that could not be converted to "
                    + $"the type {typeof(char?)} of the corresponding field of the value tuple type "
                    + $"{typeof(ValueTuple<char?>)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string '' to the type {typeof(char?)}. The string must be exactly one "
                    + "character long."
            );

        dataReader.GetString(0).Returns("ab");

        Invoking(() => materializer(dataReader))
            .Should()
            .Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Char' returned by the SQL statement contains a value that could not be converted to "
                    + $"the type {typeof(char?)} of the corresponding field of the value tuple type "
                    + $"{typeof(ValueTuple<char?>)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(char?)}. The string must be exactly "
                    + "one character long."
            );
    }

    [Fact]
    public void Materializer_NullableCharValueTupleField_DataReaderFieldContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        var character = Generate.Single<char>();

        dataReader.GetName(0).Returns("Char");
        dataReader.GetFieldType(0).Returns(typeof(string));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns(character.ToString());

        var materializer = ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<char?>>(dataReader);

        var valueTuple = materializer(dataReader);

        valueTuple.Item1.Should().Be(character);
    }

    [Fact]
    public void Materializer_NullableValueTupleField_DataReaderFieldContainsNull_ShouldMaterializeNull()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetFieldType(0).Returns(typeof(long));
        dataReader.GetName(0).Returns("Id");
        dataReader.IsDBNull(0).Returns(true);
        dataReader.GetInt64(0).Throws(new SqlNullValueException());

        var materializer = ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<long?>>(dataReader);

        var valueTuple = Invoking(() => materializer(dataReader)).Should().NotThrow().Subject;

        valueTuple.Item1.Should().BeNull();
    }

    [Fact]
    public void Materializer_ShouldMaterialize()
    {
        var entity = Generate.Single<Entity>();

        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(7);

        var ordinal = 0;

        dataReader.GetName(ordinal).Returns("Boolean");
        dataReader.GetFieldType(ordinal).Returns(typeof(bool));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetBoolean(ordinal).Returns(entity.BooleanValue);

        ordinal++;
        dataReader.GetName(ordinal).Returns("Char");
        dataReader.GetFieldType(ordinal).Returns(typeof(string));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetString(ordinal).Returns(entity.CharValue.ToString());

        ordinal++;
        dataReader.GetName(ordinal).Returns("DateTime");
        dataReader.GetFieldType(ordinal).Returns(typeof(DateTime));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetDateTime(ordinal).Returns(entity.DateTimeValue);

        ordinal++;
        dataReader.GetName(ordinal).Returns("Nullable");
        dataReader.GetFieldType(ordinal).Returns(typeof(decimal));
        dataReader.IsDBNull(ordinal).Returns(true);

        ordinal++;
        dataReader.GetName(ordinal).Returns("Enum");
        dataReader.GetFieldType(ordinal).Returns(typeof(string));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetString(ordinal).Returns(entity.EnumValue.ToString());

        ordinal++;
        dataReader.GetName(ordinal).Returns("Guid");
        dataReader.GetFieldType(ordinal).Returns(typeof(Guid));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetGuid(ordinal).Returns(entity.GuidValue);

        ordinal++;
        dataReader.GetName(ordinal).Returns("Int32");
        dataReader.GetFieldType(ordinal).Returns(typeof(int));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt32(ordinal).Returns(entity.Int32Value);

        var materializer = ValueTupleMaterializerFactory.GetMaterializer<(
            bool,
            char,
            DateTime,
            decimal?,
            TestEnum,
            Guid,
            int
        )>(dataReader);

        var valueTuple = materializer(dataReader);

        valueTuple.Item1.Should().Be(entity.BooleanValue);

        valueTuple.Item2.Should().Be(entity.CharValue);

        valueTuple.Item3.Should().Be(entity.DateTimeValue);

        valueTuple.Item4.Should().BeNull();

        valueTuple.Item5.Should().Be(entity.EnumValue);

        valueTuple.Item6.Should().Be(entity.GuidValue);

        valueTuple.Item7.Should().Be(entity.Int32Value);
    }

    [Fact]
    public void Materializer_ShouldMaterializeBinaryData()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        var bytes = Generate.Single<byte[]>();

        dataReader.GetName(0).Returns("Data");
        dataReader.GetFieldType(0).Returns(typeof(byte[]));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetValue(0).Returns(bytes);

        var materializer = ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<byte[]>>(dataReader);

        var valueTuple = materializer(dataReader);

        valueTuple.Item1.Should().BeEquivalentTo(bytes);
    }

    [Fact]
    public void Materializer_ShouldSupportSingleFieldValueTupleType()
    {
        var entity = Generate.Single<Entity>();

        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Boolean");
        dataReader.GetFieldType(0).Returns(typeof(bool));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetBoolean(0).Returns(entity.BooleanValue);

        var materializer = ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<bool>>(dataReader);

        var valueTuple = materializer(dataReader);

        valueTuple.Item1.Should().Be(entity.BooleanValue);
    }

    [Fact]
    public void ReflectionMaterializer_ShouldMaterializeTheSameValueTupleAsTheExpressionMaterializer()
    {
        var entity = Generate.Single<Entity>();

        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(7);

        var ordinal = 0;

        dataReader.GetName(ordinal).Returns("Boolean");
        dataReader.GetFieldType(ordinal).Returns(typeof(bool));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetBoolean(ordinal).Returns(entity.BooleanValue);

        ordinal++;
        dataReader.GetName(ordinal).Returns("Char");
        dataReader.GetFieldType(ordinal).Returns(typeof(string));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetString(ordinal).Returns(entity.CharValue.ToString());

        ordinal++;
        dataReader.GetName(ordinal).Returns("DateTime");
        dataReader.GetFieldType(ordinal).Returns(typeof(DateTime));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetDateTime(ordinal).Returns(entity.DateTimeValue);

        ordinal++;
        dataReader.GetName(ordinal).Returns("Nullable");
        dataReader.GetFieldType(ordinal).Returns(typeof(decimal));
        dataReader.IsDBNull(ordinal).Returns(true);

        ordinal++;
        dataReader.GetName(ordinal).Returns("Enum");
        dataReader.GetFieldType(ordinal).Returns(typeof(string));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetString(ordinal).Returns(entity.EnumValue.ToString());

        ordinal++;
        dataReader.GetName(ordinal).Returns("Guid");
        dataReader.GetFieldType(ordinal).Returns(typeof(Guid));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetGuid(ordinal).Returns(entity.GuidValue);

        ordinal++;
        dataReader.GetName(ordinal).Returns("Int32");
        dataReader.GetFieldType(ordinal).Returns(typeof(int));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt32(ordinal).Returns(entity.Int32Value);

        var expressionMaterializer = ValueTupleMaterializerFactory.GetMaterializer<(
            bool,
            char,
            DateTime,
            decimal?,
            TestEnum,
            Guid,
            int
        )>(dataReader);

        var reflectionMaterializer = GetReflectionMaterializer<(bool, char, DateTime, decimal?, TestEnum, Guid, int)>(
            dataReader
        );

        var valueTuple = reflectionMaterializer(dataReader);

        valueTuple
            .Should()
            .Be(
                (
                    entity.BooleanValue,
                    entity.CharValue,
                    entity.DateTimeValue,
                    (decimal?)null,
                    entity.EnumValue,
                    entity.GuidValue,
                    entity.Int32Value
                )
            );

        valueTuple.Should().Be(expressionMaterializer(dataReader));
    }

    [Fact]
    public void ReflectionMaterializer_MoreThan7FieldsValueTupleType_ShouldMaterializeNestedValueTuples()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(15);

        for (var i = 0; i < 15; i++)
        {
            dataReader.GetName(i).Returns($"Value{i + 1}");
            dataReader.GetFieldType(i).Returns(typeof(int));
            dataReader.IsDBNull(i).Returns(false);
            dataReader.GetInt32(i).Returns(i + 1);
        }

        var expressionMaterializer = ValueTupleMaterializerFactory.GetMaterializer<(
            int,
            int,
            int,
            int,
            int,
            int,
            int,
            int,
            int,
            int,
            int,
            int,
            int,
            int,
            int
        )>(dataReader);

        var reflectionMaterializer = GetReflectionMaterializer<(
            int,
            int,
            int,
            int,
            int,
            int,
            int,
            int,
            int,
            int,
            int,
            int,
            int,
            int,
            int
        )>(dataReader);

        var valueTuple = reflectionMaterializer(dataReader);

        valueTuple.Should().Be((1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15));

        valueTuple.Should().Be(expressionMaterializer(dataReader));

        // The innermost value tuple is the one that only carries the 15th field.
        valueTuple.Rest.Rest.Item1.Should().Be(15);
    }

    [Fact]
    public void ReflectionMaterializer_EightFieldsValueTupleType_ShouldMaterializeNestedValueTuple()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(8);

        for (var i = 0; i < 8; i++)
        {
            dataReader.GetName(i).Returns($"Value{i + 1}");
            dataReader.GetFieldType(i).Returns(typeof(int));
            dataReader.IsDBNull(i).Returns(false);
            dataReader.GetInt32(i).Returns(i + 1);
        }

        var materializer = GetReflectionMaterializer<(int, int, int, int, int, int, int, int)>(dataReader);

        materializer(dataReader).Should().Be((1, 2, 3, 4, 5, 6, 7, 8));
    }

    [Fact]
    public void ReflectionMaterializer_DataReaderHasCompatibleFieldTypes_ShouldConvertValues()
    {
        var dataReader = Substitute.For<DbDataReader>();

        var entityId = Generate.Id();
        var enumValue = Generate.Single<TestEnum>();

        dataReader.FieldCount.Returns(2);

        dataReader.GetName(0).Returns("Id");
        dataReader.GetFieldType(0).Returns(typeof(string));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns(entityId.ToString());

        dataReader.GetName(1).Returns("Enum");
        dataReader.GetFieldType(1).Returns(typeof(decimal));
        dataReader.IsDBNull(1).Returns(false);
        dataReader.GetDecimal(1).Returns((decimal)enumValue);

        var materializer = GetReflectionMaterializer<(long Id, TestEnum Enum)>(dataReader);

        var valueTuple = materializer(dataReader);

        valueTuple.Id.Should().Be(entityId);

        valueTuple.Enum.Should().Be(enumValue);
    }

    [Fact]
    public void ReflectionMaterializer_ShouldMaterializeBinaryData()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        var bytes = Generate.Single<byte[]>();

        dataReader.GetName(0).Returns("Data");
        dataReader.GetFieldType(0).Returns(typeof(byte[]));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetValue(0).Returns(bytes);

        var materializer = GetReflectionMaterializer<ValueTuple<byte[]>>(dataReader);

        materializer(dataReader).Item1.Should().BeEquivalentTo(bytes);
    }

    [Fact]
    public void ReflectionMaterializer_NonNullableValueTupleField_DataReaderFieldContainsNull_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Id");
        dataReader.GetFieldType(0).Returns(typeof(long));
        dataReader.IsDBNull(0).Returns(true);

        var expressionMaterializer = ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<long>>(dataReader);
        var reflectionMaterializer = GetReflectionMaterializer<ValueTuple<long>>(dataReader);

        var expectedMessage = Invoking(() => expressionMaterializer(dataReader))
            .Should()
            .Throw<InvalidCastException>()
            .Which.Message;

        Invoking(() => reflectionMaterializer(dataReader))
            .Should()
            .Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Id' returned by the SQL statement contains a NULL value, but the corresponding field "
                    + $"of the value tuple type {typeof(ValueTuple<long>)} is non-nullable."
            )
            .And.Message.Should()
            .Be(expectedMessage);
    }

    [Fact]
    public void ReflectionMaterializer_NullableValueTupleField_DataReaderFieldContainsNull_ShouldMaterializeNull()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetFieldType(0).Returns(typeof(long));
        dataReader.GetName(0).Returns("Id");
        dataReader.IsDBNull(0).Returns(true);
        dataReader.GetInt64(0).Throws(new SqlNullValueException());

        var materializer = GetReflectionMaterializer<ValueTuple<long?>>(dataReader);

        Invoking(() => materializer(dataReader)).Should().NotThrow().Subject.Item1.Should().BeNull();
    }

    [Fact]
    public void ReflectionMaterializer_DataReaderFieldValueCannotBeConverted_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Enum");
        dataReader.GetFieldType(0).Returns(typeof(int));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetInt32(0).Returns(999);

        var expressionMaterializer = ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<TestEnum>>(dataReader);
        var reflectionMaterializer = GetReflectionMaterializer<ValueTuple<TestEnum>>(dataReader);

        var expectedMessage = Invoking(() => expressionMaterializer(dataReader))
            .Should()
            .Throw<InvalidCastException>()
            .Which.Message;

        Invoking(() => reflectionMaterializer(dataReader))
            .Should()
            .Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Enum' returned by the SQL statement contains a value that could not be converted to "
                    + $"the type {typeof(TestEnum)} of the corresponding field of the value tuple type "
                    + $"{typeof(ValueTuple<TestEnum>)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value '999' ({typeof(int)}) to an enum member of the type "
                    + $"{typeof(TestEnum)}. That value does not match any of the values of the enum's members.*"
            );

        Invoking(() => reflectionMaterializer(dataReader))
            .Should()
            .Throw<InvalidCastException>()
            .Which.Message.Should()
            .Be(expectedMessage);
    }

    [Fact]
    public void ReflectionMaterializer_DataReaderFieldHasNoName_ShouldReportThePositionOfTheField()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(2);

        dataReader.GetName(0).Returns("");
        dataReader.GetFieldType(0).Returns(typeof(long));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetInt64(0).Returns(Generate.Id());

        dataReader.GetName(1).Returns("");
        dataReader.GetFieldType(1).Returns(typeof(long));
        dataReader.IsDBNull(1).Returns(true);

        var expressionMaterializer = ValueTupleMaterializerFactory.GetMaterializer<(long, long)>(dataReader);
        var reflectionMaterializer = GetReflectionMaterializer<(long, long)>(dataReader);

        var expectedMessage = Invoking(() => expressionMaterializer(dataReader))
            .Should()
            .Throw<InvalidCastException>()
            .Which.Message;

        Invoking(() => reflectionMaterializer(dataReader))
            .Should()
            .Throw<InvalidCastException>()
            .WithMessage(
                "The 2nd column returned by the SQL statement contains a NULL value, but the corresponding field "
                    + $"of the value tuple type {typeof((long, long))} is non-nullable."
            )
            .And.Message.Should()
            .Be(expectedMessage);
    }

    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        ArgumentNullGuardVerifier.Verify(() =>
            ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<int>>(Substitute.For<DbDataReader>())
        );

        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);
        dataReader.GetName(0).Returns("Value");
        dataReader.GetFieldType(0).Returns(typeof(int));

        ArgumentNullGuardVerifier.Verify(() =>
            ValueTupleMaterializerFactory.CreateReflectionMaterializer<ValueTuple<int>>(
                dataReader,
                dataReader.GetFieldNames(),
                dataReader.GetFieldTypes()
            )
        );
    }

    /// <summary>
    /// Creates the reflection materializer - the one that serves applications published with Native AOT - for the
    /// shape of <paramref name="dataReader" />.
    /// </summary>
    /// <typeparam name="TValueTuple">The type of value tuple to materialize.</typeparam>
    /// <param name="dataReader">The data reader to create the materializer for.</param>
    /// <returns>The reflection materializer.</returns>
    /// <remarks>
    /// The unit tests run on the JIT, where <see cref="RuntimeFeature.IsDynamicCodeSupported" /> is
    /// <see langword="true" /> and <c>GetMaterializer</c> therefore always picks the expression-compiled path. The
    /// reflection path is reached directly instead.
    /// </remarks>
    private static Func<DbDataReader, TValueTuple> GetReflectionMaterializer<TValueTuple>(DbDataReader dataReader) =>
        ValueTupleMaterializerFactory.CreateReflectionMaterializer<TValueTuple>(
            dataReader,
            dataReader.GetFieldNames(),
            dataReader.GetFieldTypes()
        );
}
