// ReSharper disable InconsistentNaming

namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public record MappingTestEntityFluentApi
{
    public Int32 Computed_ { get; set; }
    public Byte[]? ConcurrencyToken_ { get; set; }
    public Int32 Identity_ { get; set; }
    public Int64 Key1_ { get; set; }
    public Int64 Key2_ { get; set; }
    public String? NotMapped { get; set; }
    public Byte[]? RowVersion_ { get; set; }
    public Int32 Value_ { get; set; }

    /// <summary>
    /// Configures the mapping for this entity using the Fluent API.
    /// </summary>
    public static void Configure() =>
        DbConnectionExtensions.Configure(config =>
            {
                config.Entity<MappingTestEntityFluentApi>()
                    .ToTable("MappingTestEntity");

                config.Entity<MappingTestEntityFluentApi>()
                    .Property(a => a.Computed_)
                    .HasColumnName("Computed")
                    .IsComputed();

                config.Entity<MappingTestEntityFluentApi>()
                    .Property(a => a.ConcurrencyToken_)
                    .HasColumnName("ConcurrencyToken")
                    .IsConcurrencyToken();

                config.Entity<MappingTestEntityFluentApi>()
                    .Property(a => a.Identity_)
                    .HasColumnName("Identity")
                    .IsIdentity();

                config.Entity<MappingTestEntityFluentApi>()
                    .Property(a => a.Key1_)
                    .HasColumnName("Key1")
                    .IsKey();

                config.Entity<MappingTestEntityFluentApi>()
                    .Property(a => a.Key2_)
                    .HasColumnName("Key2")
                    .IsKey();

                config.Entity<MappingTestEntityFluentApi>()
                    .Property(a => a.Value_)
                    .HasColumnName("Value");

                config.Entity<MappingTestEntityFluentApi>()
                    .Property(a => a.NotMapped)
                    .IsIgnored();

                config.Entity<MappingTestEntityFluentApi>()
                    .Property(a => a.RowVersion_)
                    .HasColumnName("RowVersion")
                    .IsRowVersion();
            }
        );
}
