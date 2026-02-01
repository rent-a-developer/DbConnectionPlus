using RentADeveloper.DbConnectionPlus.Converters;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.Assertions;

/// <summary>
/// Provides assertion functions related to entities.
/// </summary>
public static class EntityAssertions
{
    /// <summary>
    /// Asserts that the given dynamic object matches the given entity.
    /// </summary>
    /// <param name="dynamicObject">The dynamic object to assert.</param>
    /// <param name="entity">The entity to assert against.</param>
    public static void AssertDynamicObjectMatchesEntity(
        dynamic dynamicObject,
        Entity entity
    )
    {
        // We need to use the ValueConverter here because each database provider handles the types a bit
        // differently.

        ValueConverter.ConvertValueToType<Boolean>((Object)dynamicObject.BooleanValue)
            .Should().Be(entity.BooleanValue);

        ValueConverter.ConvertValueToType<Byte>((Object)dynamicObject.ByteValue)
            .Should().Be(entity.ByteValue);

        ValueConverter.ConvertValueToType<Char>((Object)dynamicObject.CharValue)
            .Should().Be(entity.CharValue);

        ValueConverter.ConvertValueToType<DateOnly>((Object)dynamicObject.DateOnlyValue)
            .Should().Be(entity.DateOnlyValue);

        ValueConverter.ConvertValueToType<DateTime>((Object)dynamicObject.DateTimeValue)
            .Should().Be(entity.DateTimeValue);

        ValueConverter.ConvertValueToType<Decimal>((Object)dynamicObject.DecimalValue)
            .Should().Be(entity.DecimalValue);

        ValueConverter.ConvertValueToType<Double>((Object)dynamicObject.DoubleValue)
            .Should().Be(entity.DoubleValue);

        ValueConverter.ConvertValueToType<TestEnum>((Object)dynamicObject.EnumValue)
            .Should().Be(entity.EnumValue);

        ValueConverter.ConvertValueToType<Guid>((Object)dynamicObject.GuidValue)
            .Should().Be(entity.GuidValue);

        ValueConverter.ConvertValueToType<Int64>((Object)dynamicObject.Id)
            .Should().Be(entity.Id);

        ValueConverter.ConvertValueToType<Int16>((Object)dynamicObject.Int16Value)
            .Should().Be(entity.Int16Value);

        ValueConverter.ConvertValueToType<Int32>((Object)dynamicObject.Int32Value)
            .Should().Be(entity.Int32Value);

        ValueConverter.ConvertValueToType<Int64>((Object)dynamicObject.Int64Value)
            .Should().Be(entity.Int64Value);

        ValueConverter.ConvertValueToType<Single>((Object)dynamicObject.SingleValue)
            .Should().Be(entity.SingleValue);

        ValueConverter.ConvertValueToType<String>((Object)dynamicObject.StringValue)
            .Should().Be(entity.StringValue);

        ValueConverter.ConvertValueToType<TimeOnly>((Object)dynamicObject.TimeOnlyValue)
            .Should().Be(entity.TimeOnlyValue);

        ValueConverter.ConvertValueToType<TimeSpan>((Object)dynamicObject.TimeSpanValue)
            .Should().Be(entity.TimeSpanValue);
    }

    /// <summary>
    /// Asserts that the given list of dynamic objects matches the given list of entities.
    /// </summary>
    /// <param name="dynamicObjects">The list of dynamic objects to assert.</param>
    /// <param name="entities">The list of entities to assert against.</param>
    public static void AssertDynamicObjectsMatchEntities(
        List<dynamic> dynamicObjects,
        List<Entity> entities
    )
    {
        for (var i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            var dynamicObject = dynamicObjects[i];

            AssertDynamicObjectMatchesEntity(dynamicObject, entity);
        }
    }
}
