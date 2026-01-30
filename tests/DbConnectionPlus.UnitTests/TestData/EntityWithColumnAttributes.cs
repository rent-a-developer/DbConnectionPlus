namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

[Table("Entity")]
public record EntityWithColumnAttributes
{
    [Column("BooleanValue")]
    public Boolean ValueBoolean { get; set; }

    [Column("ByteValue")]
    public Byte ValueByte { get; set; }

    [Column("CharValue")]
    public Char ValueChar { get; set; }

    [Column("DateOnlyValue")]
    public DateOnly ValueDateOnly { get; set; }

    [Column("DateTimeValue")]
    public DateTime ValueDateTime { get; set; }

    [Column("DecimalValue")]
    public Decimal ValueDecimal { get; set; }

    [Column("DoubleValue")]
    public Double ValueDouble { get; set; }

    [Column("EnumValue")]
    public TestEnum ValueEnum { get; set; }

    [Column("GuidValue")]
    public Guid ValueGuid { get; set; }

    [Key]
    [Column("Id")]
    public Int64 ValueId { get; set; }

    [Column("Int16Value")]
    public Int16 ValueInt16 { get; set; }

    [Column("Int32Value")]
    public Int32 ValueInt32 { get; set; }

    [Column("Int64Value")]
    public Int64 ValueInt64 { get; set; }

    [Column("SingleValue")]
    public Single ValueSingle { get; set; }

    [Column("StringValue")]
    public String ValueString { get; set; } = null!;

    [Column("TimeOnlyValue")]
    public TimeOnly ValueTimeOnly { get; set; }

    [Column("TimeSpanValue")]
    public TimeSpan ValueTimeSpan { get; set; }
}
