#pragma warning disable IDE0290

namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public record EntityWithPublicConstructor
{
    public EntityWithPublicConstructor(
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
        Single singleValue,
        String stringValue,
        TimeOnly timeOnlyValue,
        TimeSpan timeSpanValue
    )
    {
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
        this.SingleValue = singleValue;
        this.StringValue = stringValue;
        this.TimeOnlyValue = timeOnlyValue;
        this.TimeSpanValue = timeSpanValue;
    }

    public Boolean BooleanValue { get; }
    public Byte ByteValue { get; }
    public Char CharValue { get; }
    public DateOnly DateOnlyValue { get; }
    public DateTime DateTimeValue { get; }
    public Decimal DecimalValue { get; }
    public Double DoubleValue { get; }
    public TestEnum EnumValue { get; }
    public Guid GuidValue { get; }

    [Key]
    public Int64 Id { get; }

    public Int16 Int16Value { get; }
    public Int32 Int32Value { get; }
    public Int64 Int64Value { get; }
    public Single SingleValue { get; }
    public String StringValue { get; }
    public TimeOnly TimeOnlyValue { get; }
    public TimeSpan TimeSpanValue { get; }
}
