// ReSharper disable InconsistentNaming

namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public record MappingTestEntityFluentApi
{
    public Int32 ComputedColumn_ { get; set; }
    public Int32 IdentityColumn_ { get; set; }
    public Int64 KeyColumn1_ { get; set; }
    public Int64 KeyColumn2_ { get; set; }
    public String? NotMappedColumn { get; set; }
    public Int32 ValueColumn_ { get; set; }

    /// <summary>
    /// Configures the mapping for this entity using the Fluent API.
    /// </summary>
    public static void Configure() =>
        DbConnectionExtensions.Configure(config =>
            {
                config.Entity<MappingTestEntityFluentApi>()
                    .ToTable("MappingTestEntity");

                config.Entity<MappingTestEntityFluentApi>()
                    .Property(a => a.KeyColumn1_)
                    .HasColumnName("KeyColumn1")
                    .IsKey();

                config.Entity<MappingTestEntityFluentApi>()
                    .Property(a => a.KeyColumn2_)
                    .HasColumnName("KeyColumn2")
                    .IsKey();

                config.Entity<MappingTestEntityFluentApi>()
                    .Property(a => a.ValueColumn_)
                    .HasColumnName("ValueColumn");

                config.Entity<MappingTestEntityFluentApi>()
                    .Property(a => a.ComputedColumn_)
                    .HasColumnName("ComputedColumn")
                    .IsComputed();

                config.Entity<MappingTestEntityFluentApi>()
                    .Property(a => a.IdentityColumn_)
                    .HasColumnName("IdentityColumn")
                    .IsIdentity();

                config.Entity<MappingTestEntityFluentApi>()
                    .Property(a => a.NotMappedColumn)
                    .IsIgnored();
            }
        );
}
