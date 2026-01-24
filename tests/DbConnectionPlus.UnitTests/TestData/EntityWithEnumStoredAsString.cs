namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public record EntityWithEnumStoredAsString
{
    public TestEnum Enum { get; set; }

    [Key]
    public Int64 Id { get; set; }
}
