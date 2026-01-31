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
                    .Property(a => a.KeyColumn1_)
                    .HasColumnName("KeyColumn1")
                    .IsKey();

                config.Entity<MappingTestEntityFluentApi>()
                    .Property(a => a.KeyColumn2_)
                    .HasColumnName("KeyColumn2")
                    .IsKey();

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

        entityTypeBuilders[typeof(MappingTestEntityFluentApi)].PropertyBuilders["KeyColumn1_"].ColumnName
            .Should().Be("KeyColumn1");

        entityTypeBuilders[typeof(MappingTestEntityFluentApi)].PropertyBuilders["KeyColumn2_"].ColumnName
            .Should().Be("KeyColumn2");

        entityTypeBuilders[typeof(MappingTestEntityFluentApi)].PropertyBuilders["ComputedColumn_"].ColumnName
            .Should().Be("ComputedColumn");

        entityTypeBuilders[typeof(MappingTestEntityFluentApi)].PropertyBuilders["ComputedColumn_"].IsComputed
            .Should().BeTrue();

        entityTypeBuilders[typeof(MappingTestEntityFluentApi)].PropertyBuilders["IdentityColumn_"].IsIdentity
            .Should().BeTrue();

        entityTypeBuilders[typeof(MappingTestEntityFluentApi)].PropertyBuilders["IdentityColumn_"].ColumnName
            .Should().Be("IdentityColumn");

        entityTypeBuilders[typeof(MappingTestEntityFluentApi)].PropertyBuilders["NotMappedColumn"].IsIgnored
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
