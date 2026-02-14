namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public record EntityWithPrivateConstructor : Entity
{
    private EntityWithPrivateConstructor(
        Byte[] bytesValue,
        Boolean booleanValue,
        Byte byteValue,
        Char charValue,
        DateOnly dateOnlyValue,
        DateTime dateTimeValue,
        Decimal decimalValue,
        Double doubleValue,
        TestEnum enumValue,
        Guid guidValue,
        Int64 id,
        Int16 int16Value,
        Int32 int32Value,
        Int64 int64Value,
        Boolean? nullableBooleanValue,
        Single singleValue,
        String stringValue,
        TimeOnly timeOnlyValue,
        TimeSpan timeSpanValue
    )
    {
        this.BytesValue = bytesValue;
        this.BooleanValue = booleanValue;
        this.ByteValue = byteValue;
        this.CharValue = charValue;
        this.DateOnlyValue = dateOnlyValue;
        this.DateTimeValue = dateTimeValue;
        this.DecimalValue = decimalValue;
        this.DoubleValue = doubleValue;
        this.EnumValue = enumValue;
        this.GuidValue = guidValue;
        this.Id = id;
        this.Int16Value = int16Value;
        this.Int32Value = int32Value;
        this.Int64Value = int64Value;
        this.NullableBooleanValue = nullableBooleanValue;
        this.SingleValue = singleValue;
        this.StringValue = stringValue;
        this.TimeOnlyValue = timeOnlyValue;
        this.TimeSpanValue = timeSpanValue;
    }
}
