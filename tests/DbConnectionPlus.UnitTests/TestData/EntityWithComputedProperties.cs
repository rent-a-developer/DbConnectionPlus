namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public record EntityWithComputedProperties
{
    public Int64 BaseValue { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public Int64 ComputedValue { get; set; }

    [Key]
    public Int64 Id { get; set; }
}
