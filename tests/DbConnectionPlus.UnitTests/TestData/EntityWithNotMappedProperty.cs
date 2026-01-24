namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public record EntityWithNotMappedProperty
{
    [Key]
    public Int64 Id { get; set; }

    public String MappedValue { get; set; } = "";

    [NotMapped]
    public String? NotMappedValue { get; set; }
}
