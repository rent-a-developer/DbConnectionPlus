// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using RentADeveloper.DbConnectionPlus.Entities;
using RentADeveloper.DbConnectionPlus.Readers;

namespace RentADeveloper.DbConnectionPlus.UnitTests.Readers;

public class EnumerableReaderOptionsTests : UnitTestsBase
{
    [Fact]
    public void GetFieldType_CharPropertyReadAsString_ShouldReturnString()
    {
        Entity[] entities = [new()];

        using var reader = CreateReader(
            typeof(Entity),
            entities,
            EnumerableReaderOptions.ReadCharsAsStrings
        );

        reader.GetFieldType(reader.GetOrdinal("CharValue"))
            .Should().Be(typeof(String));
    }

    [Fact]
    public void GetFieldType_EnumValuesSerializedAsIntegers_ShouldReturnInt32()
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Integers;

        var entities = Generate.Multiple<EntityWithEnumStoredAsInteger>(1);

        using var reader = CreateReader(
            typeof(EntityWithEnumStoredAsInteger),
            entities,
            EnumerableReaderOptions.SerializeEnums
        );

        reader.GetFieldType(0)
            .Should().Be(typeof(Int32));
    }

    [Fact]
    public void GetFieldType_EnumValuesSerializedAsStrings_ShouldReturnString()
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        var entities = Generate.Multiple<EntityWithEnumStoredAsString>(1);

        using var reader = CreateReader(
            typeof(EntityWithEnumStoredAsString),
            entities,
            EnumerableReaderOptions.SerializeEnums
        );

        reader.GetFieldType(0)
            .Should().Be(typeof(String));
    }

    [Fact]
    public void GetInt32_EnumValuesSerialized_ShouldReturnEnumAsInt32()
    {
        var entities = Generate.Multiple<EntityWithEnumStoredAsInteger>();

        using var reader = CreateReader(
            typeof(EntityWithEnumStoredAsInteger),
            entities,
            EnumerableReaderOptions.SerializeEnums
        );

        foreach (var entity in entities)
        {
            reader.Read()
                .Should().BeTrue();

            reader.GetInt32(0)
                .Should().Be((Int32)entity.Enum);
        }
    }

    [Fact]
    public void GetString_CharPropertyReadAsString_ShouldConvertToString()
    {
        Entity[] entities = [new() { CharValue = Generate.Single<Char>() }];

        using var reader = CreateReader(
            typeof(Entity),
            entities,
            EnumerableReaderOptions.ReadCharsAsStrings
        );

        reader.Read();

        reader.GetString(reader.GetOrdinal("CharValue"))
            .Should().Be(entities[0].CharValue.ToString());
    }

    [Fact]
    public void GetString_EnumValuesSerialized_ShouldReturnEnumAsString()
    {
        var entities = Generate.Multiple<EntityWithEnumStoredAsString>();

        using var reader = CreateReader(
            typeof(EntityWithEnumStoredAsString),
            entities,
            EnumerableReaderOptions.SerializeEnums
        );

        foreach (var entity in entities)
        {
            reader.Read()
                .Should().BeTrue();

            reader.GetString(0)
                .Should().Be(entity.Enum.ToString());
        }
    }

    [Fact]
    public void GetValues_CharPropertyReadAsString_ShouldConvertToString()
    {
        Entity[] entities = [new() { CharValue = Generate.Single<Char>() }];

        using var reader = CreateReader(
            typeof(Entity),
            entities,
            EnumerableReaderOptions.ReadCharsAsStrings
        );

        reader.Read();

        var values = new Object[reader.FieldCount];

        reader.GetValues(values);

        values[reader.GetOrdinal("CharValue")]
            .Should().Be(entities[0].CharValue.ToString());
    }

    [Fact]
    public void GetValues_EnumValuesSerializedAsIntegers_ShouldSerializeEnumsAsIntegers()
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Integers;

        var entities = Generate.Multiple<EntityWithEnumStoredAsInteger>();

        using var reader = CreateReader(
            typeof(EntityWithEnumStoredAsInteger),
            entities,
            EnumerableReaderOptions.SerializeEnums
        );

        foreach (var entity in entities)
        {
            reader.Read()
                .Should().BeTrue();

            var values = new Object[reader.FieldCount];

            reader.GetValues(values)
                .Should().Be(reader.FieldCount);

            values[0]
                .Should().Be((Int32)entity.Enum);
        }
    }

    [Fact]
    public void GetValues_EnumValuesSerializedAsStrings_ShouldSerializeEnumsAsStrings()
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        var entities = Generate.Multiple<EntityWithEnumStoredAsString>();

        using var reader = CreateReader(
            typeof(EntityWithEnumStoredAsString),
            entities,
            EnumerableReaderOptions.SerializeEnums
        );

        foreach (var entity in entities)
        {
            reader.Read()
                .Should().BeTrue();

            var values = new Object[reader.FieldCount];

            reader.GetValues(values)
                .Should().Be(reader.FieldCount);

            values[0]
                .Should().Be(entity.Enum.ToString());
        }
    }

    [Fact]
    public void GetValues_NoOptions_ShouldReturnRawEnumAndCharValues()
    {
        var entity = new Entity
        {
            EnumValue = Generate.Single<TestEnum>(),
            CharValue = Generate.Single<Char>()
        };

        using var reader = CreateReader(typeof(Entity), new[] { entity }, EnumerableReaderOptions.None);

        reader.Read();

        var values = new Object[reader.FieldCount];
        reader.GetValues(values);

        values[reader.GetOrdinal("EnumValue")]
            .Should().Be(entity.EnumValue);

        values[reader.GetOrdinal("CharValue")]
            .Should().Be(entity.CharValue);
    }

    [Fact]
    public void GetValue_NullProperty_ShouldReturnDbNull()
    {
        Entity[] entities = [new() { StringValue = null! }];

        using var reader = CreateReader(typeof(Entity), entities, EnumerableReaderOptions.None);

        reader.Read();

        var ordinal = reader.GetOrdinal("StringValue");

        reader.GetValue(ordinal)
            .Should().Be(DBNull.Value);

        reader.IsDBNull(ordinal)
            .Should().BeTrue();
    }

    /// <summary>
    /// Creates a multi-column reader over the mapped, readable properties of the specified entity type.
    /// </summary>
    /// <param name="entityType">The entity type whose properties become reader columns.</param>
    /// <param name="entities">The entities the reader reads.</param>
    /// <param name="options">The behaviours the reader applies.</param>
    /// <returns>The created reader.</returns>
    private static EnumerableReader CreateReader(Type entityType, IEnumerable entities, EnumerableReaderOptions options) =>
        new(
            entities,
            [.. EntityHelper.GetEntityTypeMetadata(entityType).MappedProperties.Where(a => a.CanRead)],
            options
        );
}
