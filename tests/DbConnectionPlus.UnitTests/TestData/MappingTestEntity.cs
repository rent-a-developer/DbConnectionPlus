namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public record MappingTestEntity
{
    public byte[]? ConcurrencyToken { get; set; }

    [Key]
    public long Key1 { get; set; }

    [Key]
    public long Key2 { get; set; }

    public int Value { get; set; }
}
