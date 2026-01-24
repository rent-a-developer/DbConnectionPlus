namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public record EntityWithNullableProperty
{
    [Key]
    public Int64 Id { get; set; }

    public Int32? Value { get; set; }
}
