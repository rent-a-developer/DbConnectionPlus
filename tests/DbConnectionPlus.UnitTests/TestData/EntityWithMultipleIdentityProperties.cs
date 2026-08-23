namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public class EntityWithMultipleIdentityProperties
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Identity1 { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Identity2 { get; set; }
}
