using System.Data.SqlTypes;
using System.Numerics;
using NSubstitute.ExceptionExtensions;
using RentADeveloper.DbConnectionPlus.Materializers;

namespace RentADeveloper.DbConnectionPlus.UnitTests.Materializers;

public class ValueTupleMaterializerFactoryTests : UnitTestsBase
{
    [Fact]
    public void GetMaterializer_DataReaderFieldCountDoesNotMatchValueTupleFieldCount_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(2);

        Invoking(() => ValueTupleMaterializerFactory.GetMaterializer<(Int32, Int32, Int32)>(dataReader))
            .Should().Throw<ArgumentException>()
            .WithMessage(
                $"The SQL statement returned 2 columns, but the value tuple type {typeof((Int32, Int32, Int32))} has " +
                "3 fields. Make sure that the SQL statement returns the same number of columns as the number of " +
                "fields in the value tuple type.*"
            );
    }

    [Fact]
    public void
        GetMaterializer_DataReaderFieldTypeNotCompatibleWithValueTupleFieldType_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("DateTime");
        dataReader.GetFieldType(0).Returns(typeof(Guid));

        Invoking(() => ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<DateTime>>(dataReader))
            .Should().Throw<ArgumentException>()
            .WithMessage(
                $"The data type {typeof(Guid)} of the column 'DateTime' returned by the SQL statement is not " +
                $"compatible with the field type {typeof(DateTime)} of the corresponding field of the value tuple " +
                $"type {typeof(ValueTuple<DateTime>)}.*"
            );

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("");
        dataReader.GetFieldType(0).Returns(typeof(Guid));

        Invoking(() => ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<DateTime>>(dataReader))
            .Should().Throw<ArgumentException>()
            .WithMessage(
                $"The data type {typeof(Guid)} of the 1st column returned by the SQL statement is not " +
                $"compatible with the field type {typeof(DateTime)} of the corresponding field of the value tuple " +
                $"type {typeof(ValueTuple<DateTime>)}.*"
            );
    }

    [Fact]
    public void GetMaterializer_DataReaderHasNoFields_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(0);

        Invoking(() => ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<Int32>>(dataReader))
            .Should().Throw<ArgumentException>()
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
            .Should().Throw<ArgumentException>()
            .WithMessage(
                $"The data type {typeof(BigInteger)} of the column 'Value' returned by the SQL statement is not " +
                "supported.*"
            );

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("");
        dataReader.GetFieldType(0).Returns(typeof(BigInteger));

        Invoking(() => ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<BigInteger>>(dataReader))
            .Should().Throw<ArgumentException>()
            .WithMessage(
                $"The data type {typeof(BigInteger)} of the 1st column returned by the SQL statement is not supported.*"
            );
    }

    [Fact]
    public void GetMaterializer_TypeIsNotAValueTupleType_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        Invoking(() => ValueTupleMaterializerFactory.GetMaterializer<NotAValueTuple>(dataReader))
            .Should().Throw<ArgumentException>()
            .WithMessage(
                $"The specified type {typeof(NotAValueTuple)} is not a {typeof(ValueTuple)} type.*"
            );
    }

    [Fact]
    public void Materializer_DataReaderHasCompatibleFieldTypes_ShouldConvertValues()
    {
        var dataReader = Substitute.For<DbDataReader>();

        var entityId = Generate.Id();
        var enumValue = Generate.Single<TestEnum>();

        dataReader.FieldCount.Returns(2);

        dataReader.GetName(0).Returns("Id");
        dataReader.GetFieldType(0).Returns(typeof(String));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns(entityId.ToString());

        dataReader.GetName(1).Returns("Enum");
        dataReader.GetFieldType(1).Returns(typeof(Decimal));
        dataReader.IsDBNull(1).Returns(false);
        dataReader.GetDecimal(1).Returns((Decimal)enumValue);

        var materializer = ValueTupleMaterializerFactory.GetMaterializer<(Int64 Id, TestEnum Enum)>(dataReader);

        var entity = materializer(dataReader);

        entity.Id
            .Should().Be(entityId);

        entity.Enum
            .Should().Be(enumValue);
    }

    [Fact]
    public void Materializer_EnumValueTupleField_DataReaderContainsInteger_ShouldConvertToEnumMember()
    {
        var dataReader = Substitute.For<DbDataReader>();

        var enumValue = Generate.Single<TestEnum>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Enum");
        dataReader.GetFieldType(0).Returns(typeof(Int32));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetInt32(0).Returns((Int32)enumValue);

        var materializer = ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<TestEnum>>(dataReader);

        var valueTuple = materializer(dataReader);

        valueTuple.Item1
            .Should().Be(enumValue);
    }

    [Fact]
    public void Materializer_EnumValueTupleField_DataReaderContainsIntegerNotMatchingAnyEnumMemberValue_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Enum");
        dataReader.GetFieldType(0).Returns(typeof(Int32));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetInt32(0).Returns(999);

        var materializer = ValueTupleMaterializerFactory
            .GetMaterializer<ValueTuple<TestEnum>>(dataReader);

        Invoking(() => materializer(dataReader))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Enum' returned by the SQL statement contains a value that could not be converted to " +
                $"the type {typeof(TestEnum)} of the corresponding field of the value tuple type " +
                $"{typeof(ValueTuple<TestEnum>)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value '999' ({typeof(Int32)}) to an enum member of the type " +
                $"{typeof(TestEnum)}. That value does not match any of the values of the enum's members.*"
            );
    }

    [Fact]
    public void Materializer_EnumValueTupleField_DataReaderContainsString_ShouldConvertToEnumMember()
    {
        var dataReader = Substitute.For<DbDataReader>();

        var enumValue = Generate.Single<TestEnum>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Enum");
        dataReader.GetFieldType(0).Returns(typeof(String));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns(enumValue.ToString());

        var materializer = ValueTupleMaterializerFactory
            .GetMaterializer<ValueTuple<TestEnum>>(dataReader);

        var valueTuple = materializer(dataReader);

        valueTuple.Item1
            .Should().Be(enumValue);
    }

    [Fact]
    public void Materializer_EnumValueTupleField_DataReaderContainsStringNotMatchingAnyEnumMemberName_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Enum");
        dataReader.GetFieldType(0).Returns(typeof(String));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns("NonExistent");

        var materializer = ValueTupleMaterializerFactory
            .GetMaterializer<ValueTuple<TestEnum>>(dataReader);

        Invoking(() => materializer(dataReader))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Enum' returned by the SQL statement contains a value that could not be converted to " +
                $"the type {typeof(TestEnum)} of the corresponding field of the value tuple type " +
                $"{typeof(ValueTuple<TestEnum>)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'NonExistent' to an enum member of the type {typeof(TestEnum)}. That " +
                "string does not match any of the names of the enum's members.*"
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
            dataReader.GetFieldType(i).Returns(typeof(Int32));
            dataReader.IsDBNull(i).Returns(false);
            dataReader.GetInt32(i).Returns(i + 1);
        }

        var materializer = ValueTupleMaterializerFactory
            .GetMaterializer<(
                Int32, Int32, Int32, Int32, Int32, Int32, Int32,
                Int32, Int32, Int32, Int32, Int32, Int32, Int32,
                Int32
                )>(dataReader);

        var valueTuple = materializer(dataReader);

        valueTuple.Item1
            .Should().Be(1);

        valueTuple.Item2
            .Should().Be(2);

        valueTuple.Item3
            .Should().Be(3);

        valueTuple.Item4
            .Should().Be(4);

        valueTuple.Item5
            .Should().Be(5);

        valueTuple.Item6
            .Should().Be(6);

        valueTuple.Item7
            .Should().Be(7);

        valueTuple.Rest.Item1
            .Should().Be(8);

        valueTuple.Rest.Item2
            .Should().Be(9);

        valueTuple.Rest.Item3
            .Should().Be(10);

        valueTuple.Rest.Item4
            .Should().Be(11);

        valueTuple.Rest.Item5
            .Should().Be(12);

        valueTuple.Rest.Item6
            .Should().Be(13);

        valueTuple.Rest.Item7
            .Should().Be(14);

        valueTuple.Rest.Rest.Item1
            .Should().Be(15);
    }

    [Fact]
    public void
        Materializer_NonNullableCharValueTupleField_DataReaderFieldContainsStringWithLengthNotOne_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Char");
        dataReader.GetFieldType(0).Returns(typeof(String));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns(String.Empty);

        var materializer = ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<Char>>(dataReader);

        Invoking(() => materializer(dataReader))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Char' returned by the SQL statement contains a value that could not be converted to " +
                $"the type {typeof(Char)} of the corresponding field of the value tuple type " +
                $"{typeof(ValueTuple<Char>)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string '' to the type {typeof(Char)}. The string must be exactly one " +
                "character long."
            );

        dataReader.GetString(0).Returns("ab");

        Invoking(() => materializer(dataReader))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Char' returned by the SQL statement contains a value that could not be converted to " +
                $"the type {typeof(Char)} of the corresponding field of the value tuple type " +
                $"{typeof(ValueTuple<Char>)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(Char)}. The string must be exactly one " +
                "character long."
            );
    }

    [Fact]
    public void
        Materializer_NonNullableCharValueTupleField_DataReaderFieldContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        var character = Generate.Single<Char>();

        dataReader.GetName(0).Returns("Char");
        dataReader.GetFieldType(0).Returns(typeof(String));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns(character.ToString());

        var materializer = ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<Char>>(dataReader);

        var valueTuple = materializer(dataReader);

        valueTuple.Item1
            .Should().Be(character);
    }

    [Fact]
    public void Materializer_NonNullableValueTupleField_DataReaderFieldContainsNull_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Id");
        dataReader.GetFieldType(0).Returns(typeof(Int64));
        dataReader.IsDBNull(0).Returns(true);

        var materializer = ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<Int64>>(dataReader);

        Invoking(() => materializer(dataReader))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Id' returned by the SQL statement contains a NULL value, but the corresponding field " +
                $"of the value tuple type {typeof(ValueTuple<Int64>)} is non-nullable.*"
            );
    }

    [Fact]
    public void
        Materializer_NullableCharValueTupleField_DataReaderFieldContainsStringWithLengthNotOne_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Char");
        dataReader.GetFieldType(0).Returns(typeof(String));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns(String.Empty);

        var materializer = ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<Char?>>(dataReader);

        Invoking(() => materializer(dataReader))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Char' returned by the SQL statement contains a value that could not be converted to " +
                $"the type {typeof(Char?)} of the corresponding field of the value tuple type " +
                $"{typeof(ValueTuple<Char?>)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string '' to the type {typeof(Char?)}. The string must be exactly one " +
                "character long."
            );

        dataReader.GetString(0).Returns("ab");

        Invoking(() => materializer(dataReader))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Char' returned by the SQL statement contains a value that could not be converted to " +
                $"the type {typeof(Char?)} of the corresponding field of the value tuple type " +
                $"{typeof(ValueTuple<Char?>)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(Char?)}. The string must be exactly " +
                "one character long."
            );
    }

    [Fact]
    public void
        Materializer_NullableCharValueTupleField_DataReaderFieldContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        var character = Generate.Single<Char>();

        dataReader.GetName(0).Returns("Char");
        dataReader.GetFieldType(0).Returns(typeof(String));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns(character.ToString());

        var materializer = ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<Char?>>(dataReader);

        var valueTuple = materializer(dataReader);

        valueTuple.Item1
            .Should().Be(character);
    }

    [Fact]
    public void Materializer_NullableValueTupleField_DataReaderFieldContainsNull_ShouldMaterializeNull()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetFieldType(0).Returns(typeof(Int64));
        dataReader.GetName(0).Returns("Id");
        dataReader.IsDBNull(0).Returns(true);
        dataReader.GetInt64(0).Throws(new SqlNullValueException());

        var materializer = ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<Int64?>>(dataReader);

        var valueTuple = Invoking(() => materializer(dataReader))
            .Should().NotThrow().Subject;

        valueTuple.Item1
            .Should().BeNull();
    }

    [Fact]
    public void Materializer_ShouldMaterialize()
    {
        var entity = Generate.Single<Entity>();

        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(7);

        var ordinal = 0;

        dataReader.GetName(ordinal).Returns("Boolean");
        dataReader.GetFieldType(ordinal).Returns(typeof(Boolean));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetBoolean(ordinal).Returns(entity.BooleanValue);

        ordinal++;
        dataReader.GetName(ordinal).Returns("Char");
        dataReader.GetFieldType(ordinal).Returns(typeof(String));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetString(ordinal).Returns(entity.CharValue.ToString());

        ordinal++;
        dataReader.GetName(ordinal).Returns("DateTime");
        dataReader.GetFieldType(ordinal).Returns(typeof(DateTime));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetDateTime(ordinal).Returns(entity.DateTimeValue);

        ordinal++;
        dataReader.GetName(ordinal).Returns("Nullable");
        dataReader.GetFieldType(ordinal).Returns(typeof(Decimal));
        dataReader.IsDBNull(ordinal).Returns(true);

        ordinal++;
        dataReader.GetName(ordinal).Returns("Enum");
        dataReader.GetFieldType(ordinal).Returns(typeof(String));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetString(ordinal).Returns(entity.EnumValue.ToString());

        ordinal++;
        dataReader.GetName(ordinal).Returns("Guid");
        dataReader.GetFieldType(ordinal).Returns(typeof(Guid));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetGuid(ordinal).Returns(entity.GuidValue);

        ordinal++;
        dataReader.GetName(ordinal).Returns("Int32");
        dataReader.GetFieldType(ordinal).Returns(typeof(Int32));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt32(ordinal).Returns(entity.Int32Value);

        var materializer = ValueTupleMaterializerFactory
            .GetMaterializer<(Boolean, Char, DateTime, Decimal?, TestEnum, Guid, Int32)>(dataReader);

        var valueTuple = materializer(dataReader);

        valueTuple.Item1
            .Should().Be(entity.BooleanValue);

        valueTuple.Item2
            .Should().Be(entity.CharValue);

        valueTuple.Item3
            .Should().Be(entity.DateTimeValue);

        valueTuple.Item4
            .Should().BeNull();

        valueTuple.Item5
            .Should().Be(entity.EnumValue);

        valueTuple.Item6
            .Should().Be(entity.GuidValue);

        valueTuple.Item7
            .Should().Be(entity.Int32Value);
    }

    [Fact]
    public void Materializer_ShouldMaterializeBinaryData()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        var bytes = Generate.Single<Byte[]>();

        dataReader.GetName(0).Returns("Data");
        dataReader.GetFieldType(0).Returns(typeof(Byte[]));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetValue(0).Returns(bytes);

        var materializer = ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<Byte[]>>(dataReader);

        var valueTuple = materializer(dataReader);

        valueTuple.Item1
            .Should().BeEquivalentTo(bytes);
    }

    [Fact]
    public void Materializer_ShouldSupportSingleFieldValueTupleType()
    {
        var entity = Generate.Single<Entity>();

        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Boolean");
        dataReader.GetFieldType(0).Returns(typeof(Boolean));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetBoolean(0).Returns(entity.BooleanValue);

        var materializer = ValueTupleMaterializerFactory
            .GetMaterializer<ValueTuple<Boolean>>(dataReader);

        var valueTuple = materializer(dataReader);

        valueTuple.Item1
            .Should().Be(entity.BooleanValue);
    }

    [Fact]
    public void ShouldGuardAgainstNullArguments() =>
        ArgumentNullGuardVerifier.Verify(() =>
            ValueTupleMaterializerFactory.GetMaterializer<ValueTuple<Int32>>(Substitute.For<DbDataReader>())
        );
}
