// ReSharper disable InconsistentNaming

namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public record MappingTestEntityFluentApi
{
    public int Computed_ { get; set; }
    public byte[]? ConcurrencyToken_ { get; set; }
    public int Identity_ { get; set; }
    public long Key1_ { get; set; }
    public long Key2_ { get; set; }
    public string? NotMapped { get; set; }
    public byte[]? RowVersion_ { get; set; }
    public int Value_ { get; set; }

    /// <summary>
    /// Configures the mapping for this entity using the Fluent API.
    /// </summary>
    public static void Configure() =>
        DbConnectionExtensions.Configure(config =>
        {
            config.Entity<MappingTestEntityFluentApi>().ToTable("MappingTestEntity");

            config
                .Entity<MappingTestEntityFluentApi>()
                .Property(a => a.Computed_)
                .HasColumnName("Computed")
                .IsComputed();

            config
                .Entity<MappingTestEntityFluentApi>()
                .Property(a => a.ConcurrencyToken_)
                .HasColumnName("ConcurrencyToken")
                .IsConcurrencyToken();

            config
                .Entity<MappingTestEntityFluentApi>()
                .Property(a => a.Identity_)
                .HasColumnName("Identity")
                .IsIdentity();

            config.Entity<MappingTestEntityFluentApi>().Property(a => a.Key1_).HasColumnName("Key1").IsKey();

            config.Entity<MappingTestEntityFluentApi>().Property(a => a.Key2_).HasColumnName("Key2").IsKey();

            config.Entity<MappingTestEntityFluentApi>().Property(a => a.Value_).HasColumnName("Value");

            config.Entity<MappingTestEntityFluentApi>().Property(a => a.NotMapped).IsIgnored();

            config
                .Entity<MappingTestEntityFluentApi>()
                .Property(a => a.RowVersion_)
                .HasColumnName("RowVersion")
                .IsRowVersion();
        });
}
