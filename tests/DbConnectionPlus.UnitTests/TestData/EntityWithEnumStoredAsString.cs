namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public record EntityWithEnumStoredAsString
{
    public TestEnum Enum { get; set; }

    [Key]
    public long Id { get; set; }
}
