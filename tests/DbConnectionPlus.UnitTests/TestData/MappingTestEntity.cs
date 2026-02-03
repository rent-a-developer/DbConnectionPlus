namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public record MappingTestEntity
{
    [Key]
    public Int64 Key1 { get; set; }

    [Key]
    public Int64 Key2 { get; set; }

    public Int32 Value { get; set; }

    public Byte[]? ConcurrencyToken { get; set; }
}
