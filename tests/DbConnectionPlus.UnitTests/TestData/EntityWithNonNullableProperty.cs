namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public record EntityWithNonNullableProperty
{
    [Key]
    public Int64 Id { get; set; }

    public Int64 Value { get; set; }
}
