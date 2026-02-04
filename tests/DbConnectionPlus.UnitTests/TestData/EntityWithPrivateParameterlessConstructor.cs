namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public record EntityWithPrivateParameterlessConstructor
{
    private EntityWithPrivateParameterlessConstructor()
    {
    }

    public Byte[] BytesValue { get; set; } = null!;
    public Boolean BooleanValue { get; set; }
    public Byte ByteValue { get; set; }
    public Char CharValue { get; set; }
    public DateOnly DateOnlyValue { get; set; }
    public DateTime DateTimeValue { get; set; }
    public Decimal DecimalValue { get; set; }
    public Double DoubleValue { get; set; }
    public TestEnum EnumValue { get; set; }
    public Guid GuidValue { get; set; }

    [Key]
    public Int64 Id { get; set; }

    public Int16 Int16Value { get; set; }
    public Int32 Int32Value { get; set; }
    public Int64 Int64Value { get; set; }
    public Single SingleValue { get; set; }
    public String StringValue { get; set; } = null!;
    public TimeOnly TimeOnlyValue { get; set; }
    public TimeSpan TimeSpanValue { get; set; }
}
