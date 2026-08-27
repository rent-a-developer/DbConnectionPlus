// ReSharper disable ConvertToPrimaryConstructor

// An explicit public constructor is the whole point of this fixture: it is what the constructor-injection
// materializer binds to. A primary constructor would change what is under test.
#pragma warning disable IDE0290

namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public record EntityWithPublicConstructor : Entity
{
    public EntityWithPublicConstructor(
        byte[] bytesValue,
        bool booleanValue,
        byte byteValue,
        char charValue,
        DateOnly dateOnlyValue,
        DateTime dateTimeValue,
        decimal decimalValue,
        double doubleValue,
        TestEnum enumValue,
        Guid guidValue,
        long id,
        short int16Value,
        int int32Value,
        long int64Value,
        bool? nullableBooleanValue,
        float singleValue,
        string stringValue,
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
