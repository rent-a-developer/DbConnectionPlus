using System.Data.SqlTypes;
using System.Numerics;
using System.Runtime.CompilerServices;
using NSubstitute.ExceptionExtensions;
using RentADeveloper.DbConnectionPlus.Entities;
using RentADeveloper.DbConnectionPlus.Extensions;
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

        dataReader.GetFieldType(0).Returns(typeof(string));
        dataReader.GetName(0).Returns(string.Empty);

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

        dataReader.GetName(0).Returns("CharValue");
        dataReader.GetFieldType(0).Returns(typeof(Guid));

        Invoking(() => EntityMaterializerFactory.GetMaterializer<Entity>(dataReader))
            .Should().Throw<ArgumentException>()
            .WithMessage(
                $"The data type {typeof(Guid)} of the column 'CharValue' returned by the SQL statement is not " +
                $"compatible with the property type {typeof(char)} of the corresponding property of the type " +
                $"{typeof(Entity)}.*"
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
    public void GetMaterializer_NoFieldMatchesAWritableProperty_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(2);

        dataReader.GetName(0).Returns("NotAPropertyOfEntity");
        dataReader.GetFieldType(0).Returns(typeof(string));

        dataReader.GetName(1).Returns("AlsoNotAPropertyOfEntity");
        dataReader.GetFieldType(1).Returns(typeof(int));

        Invoking(() => EntityMaterializerFactory.GetMaterializer<Entity>(dataReader))
            .Should().Throw<InvalidOperationException>()
            .WithMessage(
                "None of the 2 field(s) of the result set (NotAPropertyOfEntity, AlsoNotAPropertyOfEntity) could " +
                $"be mapped to a writable property of the entity type {typeof(Entity)}.*"
            );
    }

    [Fact]
    public void GetMaterializer_SomeFieldsMatchAWritableProperty_ShouldNotThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(2);

        dataReader.GetName(0).Returns("CharValue");
        dataReader.GetFieldType(0).Returns(typeof(string));

        dataReader.GetName(1).Returns("NotAPropertyOfEntity");
        dataReader.GetFieldType(1).Returns(typeof(string));

        Invoking(() => EntityMaterializerFactory.GetMaterializer<Entity>(dataReader))
            .Should().NotThrow();
    }

    [Fact]
    public void GetMaterializer_DataReaderHasUnsupportedFieldType_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Value");
        dataReader.GetFieldType(0).Returns(typeof(BigInteger));

        Invoking(() =>
                EntityMaterializerFactory.GetMaterializer<EntityWithObjectProperty>(dataReader)
            )
            .Should().Throw<ArgumentException>()
            .WithMessage(
                $"The data type {typeof(BigInteger)} of the column 'Value' returned by the SQL statement is not " +
                "supported.*"
            );
    }

    [Fact]
    public void
        Materializer_CharEntityProperty_DataReaderFieldContainsStringWithLengthNotOne_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("CharValue");
        dataReader.GetFieldType(0).Returns(typeof(string));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns(string.Empty);

        var materializer = EntityMaterializerFactory.GetMaterializer<Entity>(dataReader);

        Invoking(() => materializer(dataReader))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The column 'CharValue' returned by the SQL statement contains a value that could not be converted " +
                $"to the type {typeof(char)} of the corresponding property of the type " +
                $"{typeof(Entity)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string '' to the type {typeof(char)}. The string must be exactly one " +
                "character long."
            );

        dataReader.GetString(0).Returns("ab");

        Invoking(() => materializer(dataReader))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The column 'CharValue' returned by the SQL statement contains a value that could not be converted " +
                $"to the type {typeof(char)} of the corresponding property of the type " +
                $"{typeof(Entity)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(char)}. The string must be exactly " +
                "one character long."
            );
    }

    [Fact]
    public void
        Materializer_CharEntityProperty_DataReaderFieldContainsStringWithLengthOne_ShouldGetFirstCharacter()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        var character = Generate.Single<char>();

        dataReader.GetName(0).Returns("CharValue");
        dataReader.GetFieldType(0).Returns(typeof(string));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns(character.ToString());

        var materializer = EntityMaterializerFactory.GetMaterializer<Entity>(dataReader);

        var entity = materializer(dataReader);

        entity.CharValue
            .Should().Be(character);
    }

    [Fact]
    public void Materializer_CompatiblePrivateConstructor_ShouldUsePrivateConstructor()
    {
        var entities = Generate.Multiple<Entity>(1);

        var dataReader = CreateEntityDataReader(entities);

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

        var dataReader = CreateEntityDataReader(entities);

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
        dataReader.GetFieldType(0).Returns(typeof(long));
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
        dataReader.GetFieldType(0).Returns(typeof(string)); // EntityWithEnumStoredAsInteger.Id is of type Int64.
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns(entityId.ToString());

        dataReader.GetName(1).Returns("Enum");
        dataReader.GetFieldType(1).Returns(typeof(decimal)); // EntityWithEnumStoredAsInteger.Enum is of type TestEnum.
        dataReader.IsDBNull(1).Returns(false);
        dataReader.GetDecimal(1).Returns((decimal)enumValue);

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
        var value = Generate.Single<int>();

        dataReader.FieldCount.Returns(3);

        dataReader.GetFieldType(0).Returns(typeof(long));
        dataReader.GetName(0).Returns("Id");
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetInt64(0).Returns(id);

        dataReader.GetFieldType(1).Returns(typeof(int));
        dataReader.GetName(1).Returns("Int32Value");
        dataReader.IsDBNull(1).Returns(false);
        dataReader.GetInt32(1).Returns(value);

        dataReader.GetFieldType(2).Returns(typeof(int));
        dataReader.GetName(2).Returns("NonExistent");
        dataReader.IsDBNull(2).Returns(false);
        dataReader.GetInt64(2).Returns(Generate.SmallNumber());

        var materializer = Invoking(() =>
                EntityMaterializerFactory.GetMaterializer<Entity>(dataReader)
            )
            .Should().NotThrow().Subject;

        var entity = materializer(dataReader);

        entity.Id
            .Should().Be(id);

        entity.Int32Value
            .Should().Be(value);
    }

    [Fact]
    public void Materializer_EnumEntityProperty_DataReaderFieldContainsInteger_ShouldConvertToEnumMember()
    {
        var dataReader = Substitute.For<DbDataReader>();

        var enumValue = Generate.Single<TestEnum>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Enum");
        dataReader.GetFieldType(0).Returns(typeof(int));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetInt32(0).Returns((int)enumValue);

        var materializer = EntityMaterializerFactory.GetMaterializer<EntityWithEnumStoredAsInteger>(dataReader);

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
        dataReader.GetFieldType(0).Returns(typeof(int));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetInt32(0).Returns(999);

        var materializer = EntityMaterializerFactory.GetMaterializer<EntityWithEnumStoredAsInteger>(dataReader);

        Invoking(() => materializer(dataReader))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Enum' returned by the SQL statement contains a value that could not be converted to " +
                $"the type {typeof(TestEnum)} of the corresponding property of the type " +
                $"{typeof(EntityWithEnumStoredAsInteger)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value '999' ({typeof(int)}) to an enum member of the type " +
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
        dataReader.GetFieldType(0).Returns(typeof(string));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns(enumValue.ToString());

        var materializer = EntityMaterializerFactory.GetMaterializer<EntityWithEnumStoredAsString>(dataReader);

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
        dataReader.GetFieldType(0).Returns(typeof(string));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns("NonExistent");

        var materializer = EntityMaterializerFactory.GetMaterializer<EntityWithEnumStoredAsString>(dataReader);

        Invoking(() => materializer(dataReader))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Enum' returned by the SQL statement contains a value that could not be converted to " +
                $"the type {typeof(TestEnum)} of the corresponding property of the type " +
                $"{typeof(EntityWithEnumStoredAsString)}. See inner exception for details.*"
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

        dataReader.FieldCount.Returns(8);

        var ordinal = 0;
        dataReader.GetName(ordinal).Returns("Computed");
        dataReader.GetFieldType(ordinal).Returns(typeof(int));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt32(ordinal).Returns(entity.Computed_);

        ordinal++;
        dataReader.GetName(ordinal).Returns("ConcurrencyToken");
        dataReader.GetFieldType(ordinal).Returns(typeof(byte[]));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetValue(ordinal).Returns(entity.ConcurrencyToken_);

        ordinal++;
        dataReader.GetName(ordinal).Returns("Identity");
        dataReader.GetFieldType(ordinal).Returns(typeof(int));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt32(ordinal).Returns(entity.Identity_);

        ordinal++;
        dataReader.GetName(ordinal).Returns("Key1");
        dataReader.GetFieldType(ordinal).Returns(typeof(long));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt64(ordinal).Returns(entity.Key1_);

        ordinal++;
        dataReader.GetName(ordinal).Returns("Key2");
        dataReader.GetFieldType(ordinal).Returns(typeof(long));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt64(ordinal).Returns(entity.Key2_);

        ordinal++;
        dataReader.GetName(ordinal).Returns("Value");
        dataReader.GetFieldType(ordinal).Returns(typeof(int));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt32(ordinal).Returns(entity.Value_);

        ordinal++;
        var notMappedColumnOrdinal = ordinal;
        dataReader.GetName(notMappedColumnOrdinal).Returns("NotMapped");
        dataReader.GetFieldType(notMappedColumnOrdinal).Returns(typeof(string));

        ordinal++;
        dataReader.GetName(ordinal).Returns("RowVersion");
        dataReader.GetFieldType(ordinal).Returns(typeof(byte[]));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetValue(ordinal).Returns(entity.RowVersion_);

        var materializer = EntityMaterializerFactory.GetMaterializer<MappingTestEntityAttributes>(dataReader);

        var materializedEntity = materializer(dataReader);

        _ = dataReader.DidNotReceive().IsDBNull(notMappedColumnOrdinal);
        _ = dataReader.DidNotReceive().GetString(notMappedColumnOrdinal);

        materializedEntity.Computed_
            .Should().Be(entity.Computed_);

        materializedEntity.ConcurrencyToken_
            .Should().BeEquivalentTo(entity.ConcurrencyToken_);

        materializedEntity.Identity_
            .Should().Be(entity.Identity_);

        materializedEntity.Key1_
            .Should().Be(entity.Key1_);

        materializedEntity.Key2_
            .Should().Be(entity.Key2_);

        materializedEntity.Value_
            .Should().Be(entity.Value_);

        materializedEntity.NotMapped
            .Should().BeNull();

        materializedEntity.RowVersion_
            .Should().BeEquivalentTo(entity.RowVersion_);
    }

    [Fact]
    public void Materializer_Mapping_FluentApi_ShouldUseFluentApiMapping()
    {
        MappingTestEntityFluentApi.Configure();

        var entity = Generate.Single<MappingTestEntityFluentApi>();

        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(8);

        var ordinal = 0;
        dataReader.GetName(ordinal).Returns("Computed");
        dataReader.GetFieldType(ordinal).Returns(typeof(int));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt32(ordinal).Returns(entity.Computed_);

        ordinal++;
        dataReader.GetName(ordinal).Returns("ConcurrencyToken");
        dataReader.GetFieldType(ordinal).Returns(typeof(byte[]));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetValue(ordinal).Returns(entity.ConcurrencyToken_);

        ordinal++;
        dataReader.GetName(ordinal).Returns("Identity");
        dataReader.GetFieldType(ordinal).Returns(typeof(int));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt32(ordinal).Returns(entity.Identity_);

        ordinal++;
        dataReader.GetName(ordinal).Returns("Key1");
        dataReader.GetFieldType(ordinal).Returns(typeof(long));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt64(ordinal).Returns(entity.Key1_);

        ordinal++;
        dataReader.GetName(ordinal).Returns("Key2");
        dataReader.GetFieldType(ordinal).Returns(typeof(long));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt64(ordinal).Returns(entity.Key2_);

        ordinal++;
        dataReader.GetName(ordinal).Returns("Value");
        dataReader.GetFieldType(ordinal).Returns(typeof(int));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt32(ordinal).Returns(entity.Value_);

        ordinal++;
        var notMappedColumnOrdinal = ordinal;
        dataReader.GetName(notMappedColumnOrdinal).Returns("NotMapped");
        dataReader.GetFieldType(notMappedColumnOrdinal).Returns(typeof(string));

        ordinal++;
        dataReader.GetName(ordinal).Returns("RowVersion");
        dataReader.GetFieldType(ordinal).Returns(typeof(byte[]));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetValue(ordinal).Returns(entity.RowVersion_);

        var materializer = EntityMaterializerFactory.GetMaterializer<MappingTestEntityFluentApi>(dataReader);

        var materializedEntity = materializer(dataReader);

        _ = dataReader.DidNotReceive().IsDBNull(notMappedColumnOrdinal);
        _ = dataReader.DidNotReceive().GetString(notMappedColumnOrdinal);

        materializedEntity.Computed_
            .Should().Be(entity.Computed_);

        materializedEntity.ConcurrencyToken_
            .Should().BeEquivalentTo(entity.ConcurrencyToken_);

        materializedEntity.Identity_
            .Should().Be(entity.Identity_);

        materializedEntity.Key1_
            .Should().Be(entity.Key1_);

        materializedEntity.Key2_
            .Should().Be(entity.Key2_);

        materializedEntity.Value_
            .Should().Be(entity.Value_);

        materializedEntity.NotMapped
            .Should().BeNull();

        materializedEntity.RowVersion_
            .Should().BeEquivalentTo(entity.RowVersion_);
    }

    [Fact]
    public void Materializer_Mapping_NoMapping_ShouldUseEntityTypeNameAndPropertyNames()
    {
        var entity = Generate.Single<MappingTestEntity>();

        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(3);

        var ordinal = 0;
        dataReader.GetName(ordinal).Returns("Key1");
        dataReader.GetFieldType(ordinal).Returns(typeof(long));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt64(ordinal).Returns(entity.Key1);

        ordinal++;
        dataReader.GetName(ordinal).Returns("Key2");
        dataReader.GetFieldType(ordinal).Returns(typeof(long));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt64(ordinal).Returns(entity.Key2);

        ordinal++;
        dataReader.GetName(ordinal).Returns("Value");
        dataReader.GetFieldType(ordinal).Returns(typeof(int));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt32(ordinal).Returns(entity.Value);

        var materializer = EntityMaterializerFactory.GetMaterializer<MappingTestEntity>(dataReader);

        var materializedEntity = materializer(dataReader);

        materializedEntity.Key1
            .Should().Be(entity.Key1);

        materializedEntity.Key2
            .Should().Be(entity.Key2);

        materializedEntity.Value
            .Should().Be(entity.Value);
    }

    [Fact]
    public void Materializer_NoCompatibleConstructor_NoParameterlessConstructor_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("NonExistent");
        dataReader.GetFieldType(0).Returns(typeof(long));

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

        var dataReader = CreateEntityDataReader(entities);

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

        var dataReader = CreateEntityDataReader(entities);

        dataReader.Read();

        var materializer = EntityMaterializerFactory.GetMaterializer<Entity>(dataReader);

        var materializedEntity = materializer(dataReader);

        materializedEntity
            .Should().BeEquivalentTo(entities[0]);
    }

    [Fact]
    public void
        Materializer_NonNullableEntityProperty_DataReaderFieldContainsNull_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Id");
        dataReader.GetFieldType(0).Returns(typeof(long));
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
    public void Materializer_NullableEntityProperty_DataReaderFieldContainsNull_ShouldMaterializeNull()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("NullableBooleanValue");
        dataReader.GetFieldType(0).Returns(typeof(bool));
        dataReader.IsDBNull(0).Returns(true);
        dataReader.GetBoolean(0).Throws(new SqlNullValueException());

        var materializer = EntityMaterializerFactory
            .GetMaterializer<Entity>(dataReader);

        var entity = Invoking(() => materializer(dataReader))
            .Should().NotThrow().Subject;

        entity.NullableBooleanValue
            .Should().BeNull();
    }

    [Fact]
    public void Materializer_PropertiesWithDifferentCasing_ShouldMatchPropertiesCaseInsensitive()
    {
        var entities = Generate.Multiple<Entity>(1);
        var entityWithDifferentCasingProperties = Generate.MapTo<EntityWithDifferentCasingProperties>(entities[0]);

        var dataReader = CreateEntityDataReader(entities);

        dataReader.Read();

        var materializer = EntityMaterializerFactory.GetMaterializer<EntityWithDifferentCasingProperties>(dataReader);

        var materializedEntityWithDifferentCasingProperties = materializer(dataReader);

        materializedEntityWithDifferentCasingProperties
            .Should().BeEquivalentTo(entityWithDifferentCasingProperties);
    }

    [Fact]
    public void Materializer_ShouldMaterializeDateTimeOffsetValue()
    {
        var entity = Generate.Single<EntityWithDateTimeOffset>();

        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(2);

        var ordinal = 0;
        dataReader.GetName(ordinal).Returns("Id");
        dataReader.GetFieldType(ordinal).Returns(typeof(long));
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
    public void ReflectionMaterializer_ShouldMaterializeTheSameEntityAsTheExpressionMaterializer()
    {
        var entities = Generate.Multiple<Entity>(1);

        var dataReader = CreateEntityDataReader(entities);

        dataReader.Read();

        var expressionMaterializer = EntityMaterializerFactory.GetMaterializer<Entity>(dataReader);
        var reflectionMaterializer = GetReflectionMaterializer<Entity>(dataReader);

        var materializedEntity = reflectionMaterializer(dataReader);

        materializedEntity
            .Should().BeEquivalentTo(entities[0]);

        materializedEntity
            .Should().BeEquivalentTo(expressionMaterializer(dataReader));
    }

    [Fact]
    public void ReflectionMaterializer_Mapping_Attributes_ShouldUseAttributesMapping()
    {
        var entity = Generate.Single<MappingTestEntityAttributes>();

        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(3);

        var ordinal = 0;
        dataReader.GetName(ordinal).Returns("Key1");
        dataReader.GetFieldType(ordinal).Returns(typeof(long));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt64(ordinal).Returns(entity.Key1_);

        ordinal++;
        dataReader.GetName(ordinal).Returns("Value");
        dataReader.GetFieldType(ordinal).Returns(typeof(int));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt32(ordinal).Returns(entity.Value_);

        ordinal++;
        var notMappedColumnOrdinal = ordinal;
        dataReader.GetName(notMappedColumnOrdinal).Returns("NotMapped");
        dataReader.GetFieldType(notMappedColumnOrdinal).Returns(typeof(string));

        var materializer = GetReflectionMaterializer<MappingTestEntityAttributes>(dataReader);

        var materializedEntity = materializer(dataReader);

        _ = dataReader.DidNotReceive().IsDBNull(notMappedColumnOrdinal);
        _ = dataReader.DidNotReceive().GetString(notMappedColumnOrdinal);

        materializedEntity.Key1_
            .Should().Be(entity.Key1_);

        materializedEntity.Value_
            .Should().Be(entity.Value_);

        materializedEntity.NotMapped
            .Should().BeNull();
    }

    [Fact]
    public void ReflectionMaterializer_DataReaderFieldNameMatchesEntityPropertyCaseInsensitively_ShouldMaterialize()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("id"); // lower-case
        dataReader.GetFieldType(0).Returns(typeof(long));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetInt64(0).Returns(789);

        var materializer = GetReflectionMaterializer<Entity>(dataReader);

        materializer(dataReader).Id
            .Should().Be(789);
    }

    [Fact]
    public void ReflectionMaterializer_DataReaderHasCompatibleFieldTypes_ShouldConvertValues()
    {
        var dataReader = Substitute.For<DbDataReader>();

        var entityId = Generate.Id();
        var enumValue = Generate.Single<TestEnum>();

        dataReader.FieldCount.Returns(2);

        dataReader.GetName(0).Returns("Id");
        dataReader.GetFieldType(0).Returns(typeof(string)); // EntityWithEnumStoredAsInteger.Id is of type Int64.
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns(entityId.ToString());

        dataReader.GetName(1).Returns("Enum");
        dataReader.GetFieldType(1).Returns(typeof(decimal)); // EntityWithEnumStoredAsInteger.Enum is of type TestEnum.
        dataReader.IsDBNull(1).Returns(false);
        dataReader.GetDecimal(1).Returns((decimal)enumValue);

        var materializer = GetReflectionMaterializer<EntityWithEnumStoredAsInteger>(dataReader);

        var entity = materializer(dataReader);

        entity.Id
            .Should().Be(entityId);

        entity.Enum
            .Should().Be(enumValue);
    }

    [Fact]
    public void ReflectionMaterializer_DataReaderFieldValueCannotBeConverted_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("CharValue");
        dataReader.GetFieldType(0).Returns(typeof(string));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns("ab");

        var materializer = GetReflectionMaterializer<Entity>(dataReader);

        Invoking(() => materializer(dataReader))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The column 'CharValue' returned by the SQL statement contains a value that could not be converted " +
                $"to the type {typeof(char)} of the corresponding property of the type " +
                $"{typeof(Entity)}. See inner exception for details.*"
            )
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the string 'ab' to the type {typeof(char)}. The string must be exactly " +
                "one character long."
            );
    }

    [Fact]
    public void ReflectionMaterializer_NonNullableEntityProperty_DataReaderFieldContainsNull_ShouldThrow()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("Id");
        dataReader.GetFieldType(0).Returns(typeof(long));
        dataReader.IsDBNull(0).Returns(true);

        var materializer = GetReflectionMaterializer<Entity>(dataReader);

        Invoking(() => materializer(dataReader))
            .Should().Throw<InvalidCastException>()
            .WithMessage(
                "The column 'Id' returned by the SQL statement contains a NULL value, but the corresponding " +
                $"property of the type {typeof(Entity)} is non-nullable.*"
            );
    }

    [Fact]
    public void ReflectionMaterializer_NullableEntityProperty_DataReaderFieldContainsNull_ShouldMaterializeNull()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(1);

        dataReader.GetName(0).Returns("NullableBooleanValue");
        dataReader.GetFieldType(0).Returns(typeof(bool));
        dataReader.IsDBNull(0).Returns(true);
        dataReader.GetBoolean(0).Throws(new SqlNullValueException());

        var materializer = GetReflectionMaterializer<Entity>(dataReader);

        var entity = Invoking(() => materializer(dataReader))
            .Should().NotThrow().Subject;

        entity.NullableBooleanValue
            .Should().BeNull();
    }

    [Fact]
    public void ReflectionMaterializer_ShouldMaterializeDateTimeOffsetValue()
    {
        var entity = Generate.Single<EntityWithDateTimeOffset>();

        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(2);

        var ordinal = 0;
        dataReader.GetName(ordinal).Returns("Id");
        dataReader.GetFieldType(ordinal).Returns(typeof(long));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetInt64(ordinal).Returns(entity.Id);

        ordinal++;
        dataReader.GetName(ordinal).Returns("DateTimeOffsetValue");
        dataReader.GetFieldType(ordinal).Returns(typeof(DateTimeOffset));
        dataReader.IsDBNull(ordinal).Returns(false);
        dataReader.GetValue(ordinal).Returns(entity.DateTimeOffsetValue);

        var materializer = GetReflectionMaterializer<EntityWithDateTimeOffset>(dataReader);

        materializer(dataReader)
            .Should().BeEquivalentTo(entity);
    }

    [Fact]
    public void ReflectionMaterializer_CompatiblePublicConstructor_ShouldUsePublicConstructor()
    {
        var entities = Generate.Multiple<Entity>(1);

        var dataReader = CreateEntityDataReader(entities);

        dataReader.Read();

        var expressionMaterializer = EntityMaterializerFactory.GetMaterializer<EntityWithPublicConstructor>(dataReader);
        var reflectionMaterializer = GetReflectionMaterializer<EntityWithPublicConstructor>(dataReader);

        var materializedEntity = reflectionMaterializer(dataReader);

        materializedEntity
            .Should().BeEquivalentTo(entities[0]);

        materializedEntity
            .Should().BeEquivalentTo(expressionMaterializer(dataReader));
    }

    [Fact]
    public void ReflectionMaterializer_CompatiblePrivateConstructor_ShouldUsePrivateConstructor()
    {
        var entities = Generate.Multiple<Entity>(1);

        var dataReader = CreateEntityDataReader(entities);

        dataReader.Read();

        var materializer = GetReflectionMaterializer<EntityWithPrivateConstructor>(dataReader);

        materializer(dataReader)
            .Should().BeEquivalentTo(entities[0]);
    }

    [Fact]
    public void ReflectionMaterializer_ConstructorParametersInADifferentOrderThanTheFields_ShouldMaterialize()
    {
        var enumValue = Generate.Single<TestEnum>();
        var name = Generate.Single<string>();
        var id = Generate.Id();

        // Item's constructor is (Id, Name, Enum); the result set deliberately returns the columns in another order.
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(3);

        dataReader.GetName(0).Returns("Name");
        dataReader.GetFieldType(0).Returns(typeof(string));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetString(0).Returns(name);

        dataReader.GetName(1).Returns("Enum");
        dataReader.GetFieldType(1).Returns(typeof(int)); // Item.Enum is of type TestEnum.
        dataReader.IsDBNull(1).Returns(false);
        dataReader.GetInt32(1).Returns((int)enumValue);

        dataReader.GetName(2).Returns("Id");
        dataReader.GetFieldType(2).Returns(typeof(long));
        dataReader.IsDBNull(2).Returns(false);
        dataReader.GetInt64(2).Returns(id);

        var materializer = GetReflectionMaterializer<Item>(dataReader);

        materializer(dataReader)
            .Should().Be(new Item(id, name, enumValue));
    }

    [Fact]
    public void ReflectionMaterializer_NonNullableConstructorParameter_DataReaderFieldContainsNull_ShouldThrow()
    {
        var dataReader = CreateItemDataReader();

        dataReader.IsDBNull(0).Returns(true);

        var expectedMessage =
            "The column 'Id' returned by the SQL statement contains a NULL value, but the corresponding property " +
            $"of the type {typeof(Item)} is non-nullable.*";

        var reflectionMaterializer = GetReflectionMaterializer<Item>(dataReader);
        var expressionMaterializer = EntityMaterializerFactory.GetMaterializer<Item>(dataReader);

        Invoking(() => reflectionMaterializer(dataReader))
            .Should().Throw<InvalidCastException>()
            .WithMessage(expectedMessage);

        Invoking(() => expressionMaterializer(dataReader))
            .Should().Throw<InvalidCastException>()
            .WithMessage(expectedMessage);
    }

    [Fact]
    public void ReflectionMaterializer_NullableConstructorParameter_DataReaderFieldContainsNull_ShouldPassNull()
    {
        var dataReader = CreateItemDataReader();

        dataReader.IsDBNull(1).Returns(true);

        var materializer = GetReflectionMaterializer<Item>(dataReader);

        materializer(dataReader).Name
            .Should().BeNull();
    }

    [Fact]
    public void ReflectionMaterializer_ConstructorParameterValueCannotBeConverted_ShouldThrow()
    {
        var dataReader = CreateItemDataReader();

        dataReader.GetString(2).Returns("NonExistent");

        var expectedMessage =
            "The column 'Enum' returned by the SQL statement contains a value that could not be converted to the " +
            $"type {typeof(TestEnum)} of the corresponding property of the type {typeof(Item)}. See inner " +
            "exception for details.*";

        var expectedInnerMessage =
            $"Could not convert the string 'NonExistent' to an enum member of the type {typeof(TestEnum)}. That " +
            "string does not match any of the names of the enum's members.*";

        var reflectionMaterializer = GetReflectionMaterializer<Item>(dataReader);
        var expressionMaterializer = EntityMaterializerFactory.GetMaterializer<Item>(dataReader);

        Invoking(() => reflectionMaterializer(dataReader))
            .Should().Throw<InvalidCastException>()
            .WithMessage(expectedMessage)
            .WithInnerException<InvalidCastException>()
            .WithMessage(expectedInnerMessage);

        Invoking(() => expressionMaterializer(dataReader))
            .Should().Throw<InvalidCastException>()
            .WithMessage(expectedMessage)
            .WithInnerException<InvalidCastException>()
            .WithMessage(expectedInnerMessage);
    }

    [Fact]
    public void ReflectionMaterializer_PrivateParameterlessConstructor_ShouldUsePrivateConstructor()
    {
        var entities = Generate.Multiple<Entity>(1);

        var dataReader = CreateEntityDataReader(entities);

        dataReader.Read();

        var materializer = GetReflectionMaterializer<EntityWithPrivateParameterlessConstructor>(dataReader);

        materializer(dataReader)
            .Should().BeEquivalentTo(entities[0]);
    }

    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        ArgumentNullGuardVerifier.Verify(() =>
            EntityMaterializerFactory.GetMaterializer<Entity>(Substitute.For<DbDataReader>())
        );

        var dataReader = CreateEntityDataReader(Generate.Multiple<Entity>(1));

        ArgumentNullGuardVerifier.Verify(() =>
            EntityMaterializerFactory.CreateReflectionMaterializer<Entity>(
                dataReader,
                dataReader.GetFieldNames(),
                dataReader.GetFieldTypes()
            )
        );
    }

    /// <summary>
    /// Creates a data reader whose three columns match the constructor of <see cref="Item" />, so that both
    /// materializers take the constructor-injection strategy.
    /// </summary>
    /// <returns>The data reader, positioned on a row of valid values.</returns>
    private static DbDataReader CreateItemDataReader()
    {
        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(3);

        dataReader.GetName(0).Returns("Id");
        dataReader.GetFieldType(0).Returns(typeof(long));
        dataReader.IsDBNull(0).Returns(false);
        dataReader.GetInt64(0).Returns(Generate.Id());

        dataReader.GetName(1).Returns("Name");
        dataReader.GetFieldType(1).Returns(typeof(string));
        dataReader.IsDBNull(1).Returns(false);
        dataReader.GetString(1).Returns(Generate.Single<string>());

        dataReader.GetName(2).Returns("Enum");
        dataReader.GetFieldType(2).Returns(typeof(string)); // Item.Enum is of type TestEnum.
        dataReader.IsDBNull(2).Returns(false);
        dataReader.GetString(2).Returns(Generate.Single<TestEnum>().ToString());

        return dataReader;
    }

    /// <summary>
    /// Creates a multi-column reader over the mapped, readable properties of <see cref="Entity" />.
    /// </summary>
    /// <param name="entities">The entities the reader reads.</param>
    /// <returns>The created reader.</returns>
    private static EnumerableReader CreateEntityDataReader(IEnumerable entities) =>
        new(
            entities,
            [.. EntityHelper.GetEntityTypeMetadata(typeof(Entity)).MappedProperties.Where(a => a.CanRead)],
            EnumerableReaderOptions.SerializeEnums | EnumerableReaderOptions.ReadCharsAsStrings
        );

    /// <summary>
    /// Creates the reflection materializer - the one that serves applications published with Native AOT - for the
    /// shape of <paramref name="dataReader" />.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity to materialize.</typeparam>
    /// <param name="dataReader">The data reader to create the materializer for.</param>
    /// <returns>The reflection materializer.</returns>
    /// <remarks>
    /// The unit tests run on the JIT, where <see cref="RuntimeFeature.IsDynamicCodeSupported" /> is
    /// <see langword="true" /> and <c>GetMaterializer</c> therefore always picks the expression-compiled path. The
    /// reflection path is reached directly instead.
    /// </remarks>
    private static Func<DbDataReader, TEntity> GetReflectionMaterializer<TEntity>(DbDataReader dataReader) =>
        EntityMaterializerFactory.CreateReflectionMaterializer<TEntity>(
            dataReader,
            dataReader.GetFieldNames(),
            dataReader.GetFieldTypes()
        );
}
