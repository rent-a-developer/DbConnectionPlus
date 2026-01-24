namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public record EntityWithDateTimeOffset
{
    public DateTimeOffset DateTimeOffsetValue { get; set; }

    [Key]
    public Int64 Id { get; set; }
}
