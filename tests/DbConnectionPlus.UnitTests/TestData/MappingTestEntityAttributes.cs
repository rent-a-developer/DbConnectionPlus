// ReSharper disable InconsistentNaming

namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

[Table("MappingTestEntity")]
public record MappingTestEntityAttributes
{
    [Column("Computed")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public int Computed_ { get; set; }

    [Column("ConcurrencyToken")]
    [ConcurrencyCheck]
    public byte[]? ConcurrencyToken_ { get; set; }

    [Column("Identity")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Identity_ { get; set; }

    [Key]
    [Column("Key1")]
    public long Key1_ { get; set; }

    [Key]
    [Column("Key2")]
    public long Key2_ { get; set; }

    [NotMapped]
    public string? NotMapped { get; set; }

    [Column("RowVersion")]
    [Timestamp]
    public byte[]? RowVersion_ { get; set; }

    [Column("Value")]
    public int Value_ { get; set; }
}
