using System.Data.SqlTypes;
using System.Numerics;
using NSubstitute.ExceptionExtensions;
using RentADeveloper.DbConnectionPlus.Materializers;
using RentADeveloper.DbConnectionPlus.Readers;

namespace RentADeveloper.DbConnectionPlus.UnitTests.Materializers;

public class EntityMaterializerFactoryTests : UnitTestsBase
{
    [Fact]
    public void GetMaterializer_DataReaderFieldHasNoName_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetFieldType(0).Returns(typeof(String));
        dataReader.GetName(0).Returns(String.Empty);

        Invoking(() => EntityMaterializerFactory.GetMaterializer<Entity>(dataReader))
            .Should().Throw<ArgumentException>()
            .WithMessage(
                "The 1st column returned by the SQL statement does not have a name. Make sure that all columns the " +
                "statement returns have a name.*"
            );
    }

    [Fact]
    public void GetMaterializer_DataReaderFieldTypeNotCompatibleWithEntityPropertyType_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Char");
        dataReader.GetFieldType(0).Returns(typeof(Guid));

        Invoking(() => EntityMaterializerFactory.GetMaterializer<EntityWithCharProperty>(dataReader))
            .Should().Throw<ArgumentException>()
            .WithMessage(
                $"The data type {typeof(Guid)} of the column 'Char' returned by the SQL statement is not compatible " +
                $"with the property type {typeof(Char)} of the corresponding property of the type " +
                $"{typeof(EntityWithCharProperty)}.*"
            );
    }

    [Fact]
    public void GetMaterializer_DataReaderHasNoFields_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(0);

        Invoking(() => EntityMaterializerFactory.GetMaterializer<Entity>(dataReader))
            .Should().Throw<ArgumentException>()
            .WithMessage("The SQL statement did not return any columns.*");
    }

    [Fact]
    public void GetMaterializer_DataReaderHasUnsupportedFieldType_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Id");
        dataReader.GetFieldType(0).Returns(typeof(BigInteger));

        Invoking(() =>
                EntityMaterializerFactory.GetMaterializer<EntityWithUnsupportedPropertyType>(dataReader)
            )
            .Should().Throw<ArgumentException>()
            .WithMessage(
                $"The data type {typeof(BigInteger)} of the column 'Id' returned by the SQL statement is not " +
                "supported.*"
            );
    }


    [Fact]
    public void Materializer_CompatiblePrivateConstructor_ShouldUsePrivateConstructor()
    {
        var entities = Generate.Multiple<Entity>(1);

        var dataReader = new EnumHandlingObjectReader(typeof(Entity), entities);

        dataReader.Read();

        var materializer = EntityMaterializerFactory.GetMaterializer<EntityWithPrivateConstructor>(dataReader);

        var materializedEntity = materializer(dataReader);

        materializedEntity
            .Should().BeEquivalentTo(entities[0]);
    }

    [Fact]
    public void Materializer_CompatiblePublicConstructor_ShouldUsePublicConstructor()
    {
        var entities = Generate.Multiple<Entity>(1);

        var dataReader = new EnumHandlingObjectReader(typeof(Entity), entities);

        dataReader.Read();

        var materializer = EntityMaterializerFactory.GetMaterializer<EntityWithPublicConstructor>(dataReader);

        var materializedEntity = materializer(dataReader);

        materializedEntity
            .Should().BeEquivalentTo(entities[0]);
    }

    [Fact]
    public void Materializer_DataReaderFieldNameMatchesEntityPropertyCaseInsensitively_ShouldMaterialize()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("id"); // lower-case
        dataReader.GetFieldType(0).Returns(typeof(Int64));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetInt64(0).Returns(789);

        var materializer = EntityMaterializerFactory.GetMaterializer<Entity>(dataReader);

        var entity = materializer(dataReader);

        entity.Id
            .Should().Be(789);
    }

    [Fact]
    public void Materializer_DataReaderHasCompatibleFieldTypes_ShouldConvertValues()
    {
        var dataReader = Substitute.For<DbDataReader>();

        var entityId = Generate.Id();
        var enumValue = Generate.Single<TestEnum>();

        dataReader.FieldCount.Returns(2);

        dataReader.GetName(0).Returns("Id");
        dataReader.GetFieldType(0).Returns(typeof(String)); // EntityWithEnumStoredAsInteger.Id is of type Int64.
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns(entityId.ToString());

        dataReader.GetName(1).Returns("Enum");
        dataReader.GetFieldType(1).Returns(typeof(Decimal)); // EntityWithEnumStoredAsInteger.Enum is of type TestEnum.
        dataReader.IsDBNull(1).Returns(false);
        dataReader.GetDecimal(1).Returns((Decimal)enumValue);

        var materializer = EntityMaterializerFactory.GetMaterializer<EntityWithEnumStoredAsInteger>(dataReader);

        var entity = materializer(dataReader);

        entity.Id
            .Should().Be(entityId);

        entity.Enum
            .Should().Be(enumValue);
    }

    [Fact]
    public void Materializer_EntityHasNoCorrespondingPropertyForDataReaderField_ShouldIgnoreField()
    {
        var dataReader = Substitute.For<DbDataReader>();

        var id = Generate.Id();
        var value = Generate.Id();

        dataReader.FieldCount.Returns(3);

        dataReader.GetFieldType(0).Returns(typeof(Int64));
        dataReader.GetName(0).Returns("Id");
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetInt64(0).Returns(id);

        dataReader.GetFieldType(1).Returns(typeof(Int64));
        dataReader.GetName(1).Returns("Value");
        dataReader.IsDBNull(1).Returns(false);
        dataReader.GetInt64(1).Returns(value);

        dataReader.GetFieldType(2).Returns(typeof(Int32));
        dataReader.GetName(2).Returns("NonExistent");
        dataReader.IsDBNull(2).Returns(false);
        dataReader.GetInt64(2).Returns(Generate.SmallNumber());

        var materializer = Invoking(() =>
                EntityMaterializerFactory.GetMaterializer<EntityWithNonNullableProperty>(dataReader)
            )
            .Should().NotThrow().Subject;

        var entity = materializer(dataReader);

        entity.Id
            .Should().Be(id);

        entity.Value
            .Should().Be(value);
    }

    [Fact]
    public void Materializer_EnumEntityProperty_DataReaderFieldContainsInteger_ShouldConvertToEnumMember()
    {
        var dataReader = Substitute.For<DbDataReader>();

        var enumValue = Generate.Single<TestEnum>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Enum");
        dataReader.GetFieldType(0).Returns(typeof(Int32));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetInt32(0).Returns((Int32)enumValue);

        var materializer = EntityMaterializerFactory.GetMaterializer<EntityWithEnumProperty>(dataReader);

        var entity = materializer(dataReader);

        entity.Enum
            .Should().Be(enumValue);
    }

    [Fact]
    public void
        Materializer_EnumEntityProperty_DataReaderFieldContainsIntegerNotMatchingAnyEnumMemberValue_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Enum");
        dataReader.GetFieldType(0).Returns(typeof(Int32));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetInt32(0).Returns(999);

        var materializer = EntityMaterializerFactory.GetMaterializer<EntityWithEnumProperty>(dataReader);

        Invoking(() => materializer(dataReader))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Enum' returned by the SQL statement contains a value that could not be converted to " +
                $"the type {typeof(TestEnum)} of the corresponding property of the type " +
                $"{typeof(EntityWithEnumProperty)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value '999' ({typeof(Int32)}) to an enum member of the type " +
                $"{typeof(TestEnum)}. That value does not match any of the values of the enum's members.*"
            );
    }

    [Fact]
    public void Materializer_EnumEntityProperty_DataReaderFieldContainsString_ShouldConvertToEnumMember()
    {
        var dataReader = Substitute.For<DbDataReader>();

        var enumValue = Generate.Single<TestEnum>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Enum");
        dataReader.GetFieldType(0).Returns(typeof(String));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns(enumValue.ToString());

        var materializer = EntityMaterializerFactory.GetMaterializer<EntityWithEnumProperty>(dataReader);

        var entity = materializer(dataReader);

        entity.Enum
            .Should().Be(enumValue);
    }

    [Fact]
    public void Materializer_EnumEntityProperty_DataReaderFieldContainsStringNotMatchingAnyEnumMemberName_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Enum");
        dataReader.GetFieldType(0).Returns(typeof(String));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns("NonExistent");

        var materializer = EntityMaterializerFactory.GetMaterializer<EntityWithEnumProperty>(dataReader);

        Invoking(() => materializer(dataReader))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Enum' returned by the SQL statement contains a value that could not be converted to " +
                $"the type {typeof(TestEnum)} of the corresponding property of the type " +
                $"{typeof(EntityWithEnumProperty)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'NonExistent' to an enum member of the type {typeof(TestEnum)}. That " +
                "string does not match any of the names of the enum's members.*"
            );
    }

    [Fact]
    public void Materializer_Mapping_Attributes_ShouldUseAttributesMapping()
    {
        var entity = Generate.Single<MappingTestEntityAttributes>();

        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(6);

        var ordinal = 0;
        dataReader.GetName(ordinal).Returns("KeyColumn1");
        dataReader.GetFieldType(ordinal).Returns(typeof(Int64));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt64(ordinal).Returns(entity.KeyColumn1_);

        ordinal++;
        dataReader.GetName(ordinal).Returns("KeyColumn2");
        dataReader.GetFieldType(ordinal).Returns(typeof(Int64));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt64(ordinal).Returns(entity.KeyColumn2_);

        ordinal++;
        dataReader.GetName(ordinal).Returns("ValueColumn");
        dataReader.GetFieldType(ordinal).Returns(typeof(Int32));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt32(ordinal).Returns(entity.ValueColumn_);

        ordinal++;
        dataReader.GetName(ordinal).Returns("ComputedColumn");
        dataReader.GetFieldType(ordinal).Returns(typeof(Int32));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt32(ordinal).Returns(entity.ComputedColumn_);

        ordinal++;
        dataReader.GetName(ordinal).Returns("IdentityColumn");
        dataReader.GetFieldType(ordinal).Returns(typeof(Int32));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt32(ordinal).Returns(entity.IdentityColumn_);

        ordinal++;
        var notMappedColumnOrdinal = ordinal;
        dataReader.GetName(notMappedColumnOrdinal).Returns("NotMappedColumn");
        dataReader.GetFieldType(notMappedColumnOrdinal).Returns(typeof(String));

        var materializer = EntityMaterializerFactory.GetMaterializer<MappingTestEntityAttributes>(dataReader);

        var materializedEntity = materializer(dataReader);

        _ = dataReader.DidNotReceive().IsDBNull(notMappedColumnOrdinal);
        _ = dataReader.DidNotReceive().GetString(notMappedColumnOrdinal);

        materializedEntity.KeyColumn1_
            .Should().Be(entity.KeyColumn1_);

        materializedEntity.KeyColumn2_
            .Should().Be(entity.KeyColumn2_);

        materializedEntity.ValueColumn_
            .Should().Be(entity.ValueColumn_);

        materializedEntity.ComputedColumn_
            .Should().Be(entity.ComputedColumn_);

        materializedEntity.IdentityColumn_
            .Should().Be(entity.IdentityColumn_);

        materializedEntity.NotMappedColumn
            .Should().BeNull();
    }

    [Fact]
    public void Materializer_Mapping_FluentApi_ShouldUseFluentApiMapping()
    {
        MappingTestEntityFluentApi.Configure();

        var entity = Generate.Single<MappingTestEntityFluentApi>();

        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(6);

        var ordinal = 0;
        dataReader.GetName(ordinal).Returns("KeyColumn1");
        dataReader.GetFieldType(ordinal).Returns(typeof(Int64));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt64(ordinal).Returns(entity.KeyColumn1_);

        ordinal++;
        dataReader.GetName(ordinal).Returns("KeyColumn2");
        dataReader.GetFieldType(ordinal).Returns(typeof(Int64));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt64(ordinal).Returns(entity.KeyColumn2_);

        ordinal++;
        dataReader.GetName(ordinal).Returns("ValueColumn");
        dataReader.GetFieldType(ordinal).Returns(typeof(Int32));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt32(ordinal).Returns(entity.ValueColumn_);

        ordinal++;
        dataReader.GetName(ordinal).Returns("ComputedColumn");
        dataReader.GetFieldType(ordinal).Returns(typeof(Int32));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt32(ordinal).Returns(entity.ComputedColumn_);

        ordinal++;
        dataReader.GetName(ordinal).Returns("IdentityColumn");
        dataReader.GetFieldType(ordinal).Returns(typeof(Int32));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt32(ordinal).Returns(entity.IdentityColumn_);

        ordinal++;
        var notMappedColumnOrdinal = ordinal;
        dataReader.GetName(notMappedColumnOrdinal).Returns("NotMappedColumn");
        dataReader.GetFieldType(notMappedColumnOrdinal).Returns(typeof(String));

        var materializer = EntityMaterializerFactory.GetMaterializer<MappingTestEntityFluentApi>(dataReader);

        var materializedEntity = materializer(dataReader);

        _ = dataReader.DidNotReceive().IsDBNull(notMappedColumnOrdinal);
        _ = dataReader.DidNotReceive().GetString(notMappedColumnOrdinal);

        materializedEntity.KeyColumn1_
            .Should().Be(entity.KeyColumn1_);

        materializedEntity.KeyColumn2_
            .Should().Be(entity.KeyColumn2_);

        materializedEntity.ValueColumn_
            .Should().Be(entity.ValueColumn_);

        materializedEntity.ComputedColumn_
            .Should().Be(entity.ComputedColumn_);

        materializedEntity.IdentityColumn_
            .Should().Be(entity.IdentityColumn_);

        materializedEntity.NotMappedColumn
            .Should().BeNull();
    }

    [Fact]
    public void Materializer_Mapping_NoMapping_ShouldUseEntityTypeNameAndPropertyNames()
    {
        var entity = Generate.Single<MappingTestEntity>();

        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(3);

        var ordinal = 0;
        dataReader.GetName(ordinal).Returns("KeyColumn1");
        dataReader.GetFieldType(ordinal).Returns(typeof(Int64));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt64(ordinal).Returns(entity.KeyColumn1);

        ordinal++;
        dataReader.GetName(ordinal).Returns("KeyColumn2");
        dataReader.GetFieldType(ordinal).Returns(typeof(Int64));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt64(ordinal).Returns(entity.KeyColumn2);

        ordinal++;
        dataReader.GetName(ordinal).Returns("ValueColumn");
        dataReader.GetFieldType(ordinal).Returns(typeof(Int32));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt32(ordinal).Returns(entity.ValueColumn);

        var materializer = EntityMaterializerFactory.GetMaterializer<MappingTestEntity>(dataReader);

        var materializedEntity = materializer(dataReader);

        materializedEntity.KeyColumn1
            .Should().Be(entity.KeyColumn1);

        materializedEntity.KeyColumn2
            .Should().Be(entity.KeyColumn2);

        materializedEntity.ValueColumn
            .Should().Be(entity.ValueColumn);
    }

    [Fact]
    public void Materializer_NoCompatibleConstructor_NoParameterlessConstructor_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("NonExistent");
        dataReader.GetFieldType(0).Returns(typeof(Int64));

        Invoking(() => EntityMaterializerFactory.GetMaterializer<EntityWithPublicConstructor>(dataReader))
            .Should().Throw<ArgumentException>()
            .WithMessage(
                $"Could not materialize an instance of the type {typeof(EntityWithPublicConstructor)}. The type " +
                "either needs to have a parameterless constructor or a constructor whose parameters match the " +
                "columns returned by the SQL statement, e.g. a constructor that has the following " +
                $"signature:{Environment.NewLine}" +
                "(Int64 NonExistent).*"
            );
    }

    [Fact]
    public void
        Materializer_NoCompatibleConstructor_PrivateParameterlessConstructor_ShouldUsePrivateConstructorAndProperties()
    {
        var entities = Generate.Multiple<Entity>(1);

        var dataReader = new EnumHandlingObjectReader(typeof(Entity), entities);

        dataReader.Read();

        var materializer =
            EntityMaterializerFactory.GetMaterializer<EntityWithPrivateParameterlessConstructor>(dataReader);

        var materializedEntity = materializer(dataReader);

        materializedEntity
            .Should().BeEquivalentTo(entities[0]);
    }

    [Fact]
    public void
        Materializer_NoCompatibleConstructor_PublicParameterlessConstructor_ShouldUsePublicConstructorAndProperties()
    {
        var entities = Generate.Multiple<Entity>(1);

        var dataReader = new EnumHandlingObjectReader(typeof(Entity), entities);

        dataReader.Read();

        var materializer = EntityMaterializerFactory.GetMaterializer<Entity>(dataReader);

        var materializedEntity = materializer(dataReader);

        materializedEntity
            .Should().BeEquivalentTo(entities[0]);
    }

    [Fact]
    public void
        Materializer_NonNullableCharEntityProperty_DataReaderFieldContainsStringWithLengthNotOne_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Char");
        dataReader.GetFieldType(0).Returns(typeof(String));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns(String.Empty);

        var materializer = EntityMaterializerFactory.GetMaterializer<EntityWithCharProperty>(dataReader);

        Invoking(() => materializer(dataReader))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Char' returned by the SQL statement contains a value that could not be converted to " +
                $"the type {typeof(Char)} of the corresponding property of the type " +
                $"{typeof(EntityWithCharProperty)}. See inner exception for details.*"
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
                $"the type {typeof(Char)} of the corresponding property of the type " +
                $"{typeof(EntityWithCharProperty)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(Char)}. The string must be exactly " +
                "one character long."
            );
    }

    [Fact]
    public void
        Materializer_NonNullableCharEntityProperty_DataReaderFieldContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        var character = Generate.Single<Char>();

        dataReader.GetName(0).Returns("Char");
        dataReader.GetFieldType(0).Returns(typeof(String));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns(character.ToString());

        var materializer = EntityMaterializerFactory.GetMaterializer<EntityWithCharProperty>(dataReader);

        var entity = materializer(dataReader);

        entity.Char
            .Should().Be(character);
    }

    [Fact]
    public void
        Materializer_NonNullableEntityProperty_DataReaderFieldContainsNull_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Id");
        dataReader.GetFieldType(0).Returns(typeof(Int64));
        dataReader.IsDBNull(0).Returns(true);

        var materializer = EntityMaterializerFactory.GetMaterializer<Entity>(dataReader);

        Invoking(() => materializer(dataReader))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Id' returned by the SQL statement contains a NULL value, but the corresponding " +
                $"property of the type {typeof(Entity)} is non-nullable.*"
            );
    }

    [Fact]
    public void
        Materializer_NullableCharEntityProperty_DataReaderFieldContainsStringWithLengthNotOne_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Char");
        dataReader.GetFieldType(0).Returns(typeof(String));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns(String.Empty);

        var materializer = EntityMaterializerFactory.GetMaterializer<EntityWithNullableCharProperty>(dataReader);

        Invoking(() => materializer(dataReader))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Char' returned by the SQL statement contains a value that could not be converted to " +
                $"the type {typeof(Char?)} of the corresponding property of the type " +
                $"{typeof(EntityWithNullableCharProperty)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string '' to the type {typeof(Char?)}. " +
                "The string must be exactly one character long."
            );

        dataReader.GetString(0).Returns("ab");

        Invoking(() => materializer(dataReader))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Char' returned by the SQL statement contains a value that could not be converted to " +
                $"the type {typeof(Char?)} of the corresponding property of the type " +
                $"{typeof(EntityWithNullableCharProperty)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(Char?)}. " +
                "The string must be exactly one character long."
            );
    }

    [Fact]
    public void
        Materializer_NullableCharEntityProperty_DataReaderFieldContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        var character = Generate.Single<Char>();

        dataReader.GetName(0).Returns("Char");
        dataReader.GetFieldType(0).Returns(typeof(String));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns(character.ToString());

        var materializer = EntityMaterializerFactory
            .GetMaterializer<EntityWithNullableCharProperty>(dataReader);

        var entity = materializer(dataReader);

        entity.Char
            .Should().Be(character);
    }

    [Fact]
    public void Materializer_NullableEntityProperty_DataReaderFieldContainsNull_ShouldMaterializeNull()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Value");
        dataReader.GetFieldType(0).Returns(typeof(Int32));
        dataReader.IsDBNull(0).Returns(true);
        dataReader.GetInt32(0).Throws(new SqlNullValueException());

        var materializer = EntityMaterializerFactory
            .GetMaterializer<EntityWithNullableProperty>(dataReader);

        var entity = Invoking(() => materializer(dataReader))
            .Should().NotThrow().Subject;

        entity.Value
            .Should().BeNull();
    }

    [Fact]
    public void Materializer_PropertiesWithDifferentCasing_ShouldMatchPropertiesCaseInsensitive()
    {
        var entities = Generate.Multiple<Entity>(1);
        var entityWithDifferentCasingProperties = Generate.MapTo<EntityWithDifferentCasingProperties>(entities[0]);

        var dataReader = new EnumHandlingObjectReader(typeof(Entity), entities);

        dataReader.Read();

        var materializer = EntityMaterializerFactory.GetMaterializer<EntityWithDifferentCasingProperties>(dataReader);

        var materializedEntityWithDifferentCasingProperties = materializer(dataReader);

        materializedEntityWithDifferentCasingProperties
            .Should().BeEquivalentTo(entityWithDifferentCasingProperties);
    }

    [Fact]
    public void Materializer_ShouldMaterializeBinaryData()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        var bytes = Generate.Single<Byte[]>();

        dataReader.GetName(0).Returns("BinaryData");
        dataReader.GetFieldType(0).Returns(typeof(Byte[]));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetValue(0).Returns(bytes);

        var materializer = EntityMaterializerFactory.GetMaterializer<EntityWithBinaryProperty>(dataReader);

        var entity = materializer(dataReader);

        entity.BinaryData
            .Should().BeEquivalentTo(bytes);
    }

    [Fact]
    public void Materializer_ShouldMaterializeDateTimeOffsetValue()
    {
        var entity = Generate.Single<EntityWithDateTimeOffset>();

        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(2);

        var ordinal = 0;
        dataReader.GetName(ordinal).Returns("Id");
        dataReader.GetFieldType(ordinal).Returns(typeof(Int64));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt64(ordinal).Returns(entity.Id);

        ordinal++;
        dataReader.GetName(ordinal).Returns("DateTimeOffsetValue");
        dataReader.GetFieldType(ordinal).Returns(typeof(DateTimeOffset));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetValue(ordinal).Returns(entity.DateTimeOffsetValue);

        var materializer = EntityMaterializerFactory.GetMaterializer<EntityWithDateTimeOffset>(dataReader);

        var materializedEntity = materializer(dataReader);

        materializedEntity
            .Should().BeEquivalentTo(entity);
    }

    [Fact]
    public void ShouldGuardAgainstNullArguments() =>
        ArgumentNullGuardVerifier.Verify(() =>
            EntityMaterializerFactory.GetMaterializer<Entity>(Substitute.For<DbDataReader>())
        );
}
