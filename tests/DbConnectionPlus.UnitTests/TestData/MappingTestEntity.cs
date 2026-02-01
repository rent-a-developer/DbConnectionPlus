namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public record MappingTestEntity
{
    [Key]
    public Int64 KeyColumn1 { get; set; }

    [Key]
    public Int64 KeyColumn2 { get; set; }

    public Int32 ValueColumn { get; set; }
}
