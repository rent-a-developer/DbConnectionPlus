namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public class EntityWithMultipleIdentityProperties
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Int64 Identity1 { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Int64 Identity2 { get; set; }
}
