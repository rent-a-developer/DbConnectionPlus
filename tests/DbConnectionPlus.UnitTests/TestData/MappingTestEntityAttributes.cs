// ReSharper disable InconsistentNaming

namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

[Table("MappingTestEntity")]
public record MappingTestEntityAttributes
{
    [Column("Computed")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public Int32 Computed_ { get; set; }

    [Column("ConcurrencyToken")]
    [ConcurrencyCheck]
    public Byte[]? ConcurrencyToken_ { get; set; }

    [Column("Identity")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Int32 Identity_ { get; set; }

    [Key]
    [Column("Key1")]
    public Int64 Key1_ { get; set; }

    [Key]
    [Column("Key2")]
    public Int64 Key2_ { get; set; }

    [NotMapped]
    public String? NotMapped { get; set; }

    [Column("RowVersion")]
    [Timestamp]
    public Byte[]? RowVersion_ { get; set; }

    [Column("Value")]
    public Int32 Value_ { get; set; }
}
