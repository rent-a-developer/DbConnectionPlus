namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public record Entity
{
    public bool BooleanValue { get; set; }
    public byte[] BytesValue { get; set; } = null!;
    public byte ByteValue { get; set; }
    public char CharValue { get; set; }
    public DateOnly DateOnlyValue { get; set; }
    public DateTime DateTimeValue { get; set; }
    public decimal DecimalValue { get; set; }
    public double DoubleValue { get; set; }
    public TestEnum EnumValue { get; set; }
    public Guid GuidValue { get; set; }

    [Key]
    public long Id { get; set; }

    public short Int16Value { get; set; }
    public int Int32Value { get; set; }
    public long Int64Value { get; set; }

    public bool? NullableBooleanValue { get; set; }
    public float SingleValue { get; set; }
    public string StringValue { get; set; } = null!;
    public TimeOnly TimeOnlyValue { get; set; }
    public TimeSpan TimeSpanValue { get; set; }
}
