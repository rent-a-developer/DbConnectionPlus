namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public record MappingTestEntityFluentApi
{
    public Int64 KeyColumn1_ { get; set; }
    public Int64 KeyColumn2_ { get; set; }
    public Int32 ValueColumn_ { get; set; }
    public Int32 ComputedColumn_ { get; set; }
    public Int32 IdentityColumn_ { get; set; }
    public String? NotMappedColumn { get; set; }
}
