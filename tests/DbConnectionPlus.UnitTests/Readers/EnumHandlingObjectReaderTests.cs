using RentADeveloper.DbConnectionPlus.Readers;

namespace RentADeveloper.DbConnectionPlus.UnitTests.Readers;

public class EnumHandlingObjectReaderTests
{
    [Fact]
    public void GetFieldType_CharProperty_ShouldReturnString()
    {
        EntityWithCharProperty[] entities = [new()];

        var reader = new EnumHandlingObjectReader(typeof(EntityWithCharProperty), entities);

        reader.GetFieldType(0)
            .Should().Be(typeof(String));
    }

    [Fact]
    public void GetFieldType_EnumValues_EnumSerializationModeIsIntegers_ShouldReturnInt32()
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Integers;

        var entities = Generate.Multiple<EntityWithEnumStoredAsInteger>(1);

        var reader = new EnumHandlingObjectReader(typeof(EntityWithEnumStoredAsInteger), entities);

        reader.GetFieldType(0)
            .Should().Be(typeof(Int32));
    }

    [Fact]
    public void GetFieldType_EnumValues_EnumSerializationModeIsStrings_ShouldReturnString()
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        var entities = Generate.Multiple<EntityWithEnumStoredAsString>(1);

        var reader = new EnumHandlingObjectReader(typeof(EntityWithEnumStoredAsString), entities);

        reader.GetFieldType(0)
            .Should().Be(typeof(String));
    }

    [Fact]
    public void GetInt32_EnumValues_ShouldReturnEnumAsInt32()
    {
        var entities = Generate.Multiple<EntityWithEnumStoredAsInteger>();

        var reader = new EnumHandlingObjectReader(typeof(EntityWithEnumStoredAsInteger), entities);

        foreach (var entity in entities)
        {
            reader.Read()
                .Should().BeTrue();

            reader.GetInt32(0)
                .Should().Be((Int32)entity.Enum);
        }
    }

    [Fact]
    public void GetString_CharProperty_ShouldConvertToString()
    {
        EntityWithCharProperty[] entities = [new() { Char = Generate.Single<Char>() }];

        var reader = new EnumHandlingObjectReader(typeof(EntityWithCharProperty), entities);

        reader.Read();

        reader.GetString(0)
            .Should().Be(entities[0].Char.ToString());
    }

    [Fact]
    public void GetString_EnumValues_ShouldReturnEnumAsString()
    {
        var entities = Generate.Multiple<EntityWithEnumStoredAsString>();

        var reader = new EnumHandlingObjectReader(typeof(EntityWithEnumStoredAsString), entities);

        foreach (var entity in entities)
        {
            reader.Read()
                .Should().BeTrue();

            reader.GetString(0)
                .Should().Be(entity.Enum.ToString());
        }
    }

    [Fact]
    public void GetValues_CharProperty_ShouldConvertToString()
    {
        EntityWithCharProperty[] entities = [new() { Char = Generate.Single<Char>() }];

        var reader = new EnumHandlingObjectReader(typeof(EntityWithCharProperty), entities);

        reader.Read();

        var values = new Object[1];

        reader.GetValues(values);

        values[0]
            .Should().Be(entities[0].Char.ToString());
    }

    [Fact]
    public void GetValues_EnumValues_EnumSerializationModeIsIntegers_ShouldSerializeEnumsAsIntegers()
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Integers;

        var entities = Generate.Multiple<EntityWithEnumStoredAsInteger>();

        var reader = new EnumHandlingObjectReader(typeof(EntityWithEnumStoredAsInteger), entities);

        foreach (var entity in entities)
        {
            reader.Read()
                .Should().BeTrue();

            var values = new Object[2];

            reader.GetValues(values)
                .Should().Be(2);

            values[0]
                .Should().Be((Int32)entity.Enum);
        }
    }

    [Fact]
    public void GetValues_EnumValues_EnumSerializationModeIsStrings_ShouldSerializeEnumsAsStrings()
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        var entities = Generate.Multiple<EntityWithEnumStoredAsString>();

        var reader = new EnumHandlingObjectReader(typeof(EntityWithEnumStoredAsString), entities);

        foreach (var entity in entities)
        {
            reader.Read()
                .Should().BeTrue();

            var values = new Object[2];

            reader.GetValues(values)
                .Should().Be(2);

            values[0]
                .Should().Be(entity.Enum.ToString());
        }
    }
}
