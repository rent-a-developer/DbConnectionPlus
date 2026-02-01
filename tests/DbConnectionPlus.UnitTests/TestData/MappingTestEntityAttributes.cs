// ReSharper disable InconsistentNaming

namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

[Table("MappingTestEntity")]
public record MappingTestEntityAttributes
{
    [Column("ComputedColumn")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public Int32 ComputedColumn_ { get; set; }

    [Column("IdentityColumn")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Int32 IdentityColumn_ { get; set; }

    [Key]
    [Column("KeyColumn1")]
    public Int64 KeyColumn1_ { get; set; }

    [Key]
    [Column("KeyColumn2")]
    public Int64 KeyColumn2_ { get; set; }

    [NotMapped]
    public String? NotMappedColumn { get; set; }

    [Column("ValueColumn")]
    public Int32 ValueColumn_ { get; set; }
}
