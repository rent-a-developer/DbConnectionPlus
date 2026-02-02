namespace RentADeveloper.DbConnectionPlus.UnitTests;

public class DbConnectionExtensions_ConfigurationTests : UnitTestsBase
{
    [Fact]
    public void Configure_ShouldConfigureDbConnectionPlus()
    {
        InterceptDbCommand interceptDbCommand = (_, _) => { };

        Configure(config =>
            {
                config.EnumSerializationMode = EnumSerializationMode.Integers;
                config.InterceptDbCommand = interceptDbCommand;

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
                    .Property(a => a.Name_)
                    .HasColumnName("Name");

                config.Entity<MappingTestEntityFluentApi>()
                    .Property(a => a.NotMapped)
                    .IsIgnored();

                config.Entity<MappingTestEntityFluentApi>()
                    .Property(a => a.RowVersion_)
                    .HasColumnName("RowVersion")
                    .IsRowVersion();
            }
        );

        DbConnectionPlusConfiguration.Instance.EnumSerializationMode
            .Should().Be(EnumSerializationMode.Integers);

        DbConnectionPlusConfiguration.Instance.InterceptDbCommand
            .Should().Be(interceptDbCommand);

        var entityTypeBuilders = DbConnectionPlusConfiguration.Instance.GetEntityTypeBuilders();

        entityTypeBuilders
            .Should().HaveCount(1);

        entityTypeBuilders
            .Should().ContainKeys(
                typeof(MappingTestEntityFluentApi)
            );

        entityTypeBuilders[typeof(MappingTestEntityFluentApi)].TableName
            .Should().Be("MappingTestEntity");

        entityTypeBuilders[typeof(MappingTestEntityFluentApi)].PropertyBuilders["Computed_"].ColumnName
            .Should().Be("Computed");

        entityTypeBuilders[typeof(MappingTestEntityFluentApi)].PropertyBuilders["Computed_"].IsComputed
            .Should().BeTrue();

        entityTypeBuilders[typeof(MappingTestEntityFluentApi)].PropertyBuilders["ConcurrencyToken_"].ColumnName
            .Should().Be("ConcurrencyToken");

        entityTypeBuilders[typeof(MappingTestEntityFluentApi)].PropertyBuilders["ConcurrencyToken_"].IsConcurrencyToken
            .Should().BeTrue();

        entityTypeBuilders[typeof(MappingTestEntityFluentApi)].PropertyBuilders["Identity_"].ColumnName
            .Should().Be("Identity");

        entityTypeBuilders[typeof(MappingTestEntityFluentApi)].PropertyBuilders["Identity_"].IsIdentity
            .Should().BeTrue();

        entityTypeBuilders[typeof(MappingTestEntityFluentApi)].PropertyBuilders["Key1_"].ColumnName
            .Should().Be("Key1");

        entityTypeBuilders[typeof(MappingTestEntityFluentApi)].PropertyBuilders["Key1_"].IsKey
            .Should().BeTrue();

        entityTypeBuilders[typeof(MappingTestEntityFluentApi)].PropertyBuilders["Key2_"].ColumnName
            .Should().Be("Key2");

        entityTypeBuilders[typeof(MappingTestEntityFluentApi)].PropertyBuilders["Key2_"].IsKey
            .Should().BeTrue();

        entityTypeBuilders[typeof(MappingTestEntityFluentApi)].PropertyBuilders["Name_"].ColumnName
            .Should().Be("Name");

        entityTypeBuilders[typeof(MappingTestEntityFluentApi)].PropertyBuilders["NotMapped"].IsIgnored
            .Should().BeTrue();

        entityTypeBuilders[typeof(MappingTestEntityFluentApi)].PropertyBuilders["RowVersion_"].ColumnName
            .Should().Be("RowVersion");

        entityTypeBuilders[typeof(MappingTestEntityFluentApi)].PropertyBuilders["RowVersion_"].IsRowVersion
            .Should().BeTrue();
    }

    [Fact]
    public void Configure_ShouldFreezeConfiguration()
    {
        Configure(configuration => configuration.EnumSerializationMode = EnumSerializationMode.Integers);

        Invoking(() => Configure(configuration => configuration.EnumSerializationMode = EnumSerializationMode.Strings))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("The configuration of DbConnectionPlus is frozen and can no longer be modified.");
    }
}
