using RentADeveloper.DbConnectionPlus.Converters;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.Assertions;

/// <summary>
/// Provides assertion functions related to entities.
/// </summary>
public static class EntityAssertions
{
    /// <summary>
    /// Asserts that the given data row matches the given entity.
    /// </summary>
    /// <param name="dataRow">The data row to assert.</param>
    /// <param name="entity">The entity to assert against.</param>
    public static void AssertDataRowMatchesEntity(
        DataRow dataRow,
        Entity entity
    )
    {
        // We need to use the ValueConverter here because each database provider handles the types a bit
        // differently.

        ValueConverter.ConvertValueToType<bool>(dataRow["BooleanValue"])
            .Should().Be(entity.BooleanValue);

        ValueConverter.ConvertValueToType<byte>(dataRow["ByteValue"])
            .Should().Be(entity.ByteValue);

        ValueConverter.ConvertValueToType<char>(dataRow["CharValue"])
            .Should().Be(entity.CharValue);

        ValueConverter.ConvertValueToType<DateOnly>(dataRow["DateOnlyValue"])
            .Should().Be(entity.DateOnlyValue);

        ValueConverter.ConvertValueToType<DateTime>(dataRow["DateTimeValue"])
            .Should().Be(entity.DateTimeValue);

        ValueConverter.ConvertValueToType<decimal>(dataRow["DecimalValue"])
            .Should().Be(entity.DecimalValue);

        ValueConverter.ConvertValueToType<double>(dataRow["DoubleValue"])
            .Should().Be(entity.DoubleValue);

        ValueConverter.ConvertValueToType<TestEnum>(dataRow["EnumValue"])
            .Should().Be(entity.EnumValue);

        ValueConverter.ConvertValueToType<Guid>(dataRow["GuidValue"])
            .Should().Be(entity.GuidValue);

        ValueConverter.ConvertValueToType<long>(dataRow["Id"])
            .Should().Be(entity.Id);

        ValueConverter.ConvertValueToType<short>(dataRow["Int16Value"])
            .Should().Be(entity.Int16Value);

        ValueConverter.ConvertValueToType<int>(dataRow["Int32Value"])
            .Should().Be(entity.Int32Value);

        ValueConverter.ConvertValueToType<long>(dataRow["Int64Value"])
            .Should().Be(entity.Int64Value);

        ValueConverter.ConvertValueToType<float>(dataRow["SingleValue"])
            .Should().Be(entity.SingleValue);

        ValueConverter.ConvertValueToType<string>(dataRow["StringValue"])
            .Should().Be(entity.StringValue);

        ValueConverter.ConvertValueToType<TimeOnly>(dataRow["TimeOnlyValue"])
            .Should().Be(entity.TimeOnlyValue);

        ValueConverter.ConvertValueToType<TimeSpan>(dataRow["TimeSpanValue"])
            .Should().Be(entity.TimeSpanValue);
    }

    /// <summary>
    /// Asserts that the given list of data rows matches the given list of entities.
    /// </summary>
    /// <param name="dataRows">The list of data rows to assert.</param>
    /// <param name="entities">The list of entities to assert against.</param>
    public static void AssertDataRowsMatchEntities(
        List<DataRow> dataRows,
        List<Entity> entities
    )
    {
        for (var i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            var dataRow = dataRows[i];

            AssertDataRowMatchesEntity(dataRow, entity);
        }
    }
}
