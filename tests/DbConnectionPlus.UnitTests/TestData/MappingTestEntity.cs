namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public record MappingTestEntity
{
    public Byte[]? ConcurrencyToken { get; set; }

    [Key]
    public Int64 Key1 { get; set; }

    [Key]
    public Int64 Key2 { get; set; }

    public Int32 Value { get; set; }
}
