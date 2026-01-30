namespace RentADeveloper.DbConnectionPlus.UnitTests;

public class DbConnectionExtensions_ConfigurationTests : UnitTestsBase
{
    [Fact]
    public void Configure_ShouldConfigureDbConnectionPlus()
    {
        InterceptDbCommand interceptDbCommand = (_, _) => { };

        Configure(configuration =>
            {
                configuration.EnumSerializationMode = EnumSerializationMode.Integers;
                configuration.InterceptDbCommand = interceptDbCommand;

                configuration.Entity<Entity>()
                    .ToTable("Entities");

                configuration.Entity<Entity>()
                    .Property(a => a.Id)
                    .HasColumnName("Identifier")
                    .IsKey();

                configuration.Entity<EntityWithIdentityAndComputedProperties>()
                    .Property(a => a.ComputedValue)
                    .IsComputed();

                configuration.Entity<EntityWithNotMappedProperty>()
                    .Property(a => a.NotMappedValue)
                    .IsIgnored();
            }
        );

        DbConnectionPlusConfiguration.Instance.EnumSerializationMode
            .Should().Be(EnumSerializationMode.Integers);

        DbConnectionPlusConfiguration.Instance.InterceptDbCommand
            .Should().Be(interceptDbCommand);

        var entityTypeBuilders = DbConnectionPlusConfiguration.Instance.GetEntityTypeBuilders();

        entityTypeBuilders
            .Should().HaveCount(3);

        entityTypeBuilders
            .Should().ContainKeys(
                typeof(Entity),
                typeof(EntityWithIdentityAndComputedProperties),
                typeof(EntityWithNotMappedProperty)
            );

        entityTypeBuilders[typeof(Entity)].TableName
            .Should().Be("Entities");

        entityTypeBuilders[typeof(Entity)].PropertyBuilders["Id"].ColumnName
            .Should().Be("Identifier");

        entityTypeBuilders[typeof(Entity)].PropertyBuilders["Id"].IsKey
            .Should().BeTrue();

        entityTypeBuilders
            .Should().ContainKey(typeof(EntityWithIdentityAndComputedProperties));

        entityTypeBuilders[typeof(EntityWithIdentityAndComputedProperties)].TableName
            .Should().BeNull();

        entityTypeBuilders[typeof(EntityWithIdentityAndComputedProperties)].PropertyBuilders["ComputedValue"].IsComputed
            .Should().BeTrue();

        entityTypeBuilders
            .Should().ContainKey(typeof(EntityWithNotMappedProperty));

        entityTypeBuilders[typeof(EntityWithNotMappedProperty)].TableName
            .Should().BeNull();

        entityTypeBuilders[typeof(EntityWithNotMappedProperty)].PropertyBuilders["NotMappedValue"].IsIgnored
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
