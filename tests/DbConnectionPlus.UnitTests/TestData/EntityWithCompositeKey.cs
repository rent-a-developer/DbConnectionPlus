namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public record EntityWithCompositeKey
{
    [Key]
    public Int64 Key1 { get; set; }

    [Key]
    public Int64 Key2 { get; set; }

    public String StringValue { get; set; } = "";
}
