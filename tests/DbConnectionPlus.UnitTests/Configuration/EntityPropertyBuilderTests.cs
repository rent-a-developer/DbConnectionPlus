namespace RentADeveloper.DbConnectionPlus.UnitTests.Configuration;

public class EntityPropertyBuilderTests
{
    [Fact]
    public void Freeze_ShouldFreezeBuilder()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");
        ((IFreezable)builder).Freeze();

        Invoking(() => builder.HasColumnName("Identifier"))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("The configuration of DbConnectionPlus is frozen and can no longer be modified.");

        Invoking(() => builder.IsComputed())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("The configuration of DbConnectionPlus is frozen and can no longer be modified.");

        Invoking(() => builder.IsIdentity())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("The configuration of DbConnectionPlus is frozen and can no longer be modified.");

        Invoking(() => builder.IsIgnored())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("The configuration of DbConnectionPlus is frozen and can no longer be modified.");

        Invoking(() => builder.IsKey())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("The configuration of DbConnectionPlus is frozen and can no longer be modified.");
    }

    [Fact]
    public void GetColumnName_Configured_ShouldReturnColumnName()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");
        builder.HasColumnName("Identifier");

        ((IEntityPropertyBuilder)builder).ColumnName
            .Should().Be("Identifier");
    }

    [Fact]
    public void GetColumnName_NotConfigured_ShouldReturnNull()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        ((IEntityPropertyBuilder)builder).ColumnName
            .Should().BeNull();
    }

    [Fact]
    public void GetIsComputed_Configured_ShouldReturnTrue()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        builder.IsComputed();

        ((IEntityPropertyBuilder)builder).IsComputed
            .Should().BeTrue();
    }

    [Fact]
    public void GetIsComputed_NotConfigured_ShouldReturnFalse()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        ((IEntityPropertyBuilder)builder).IsComputed
            .Should().BeFalse();
    }

    [Fact]
    public void GetIsIdentity_Configured_ShouldReturnTrue()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        builder.IsIdentity();

        ((IEntityPropertyBuilder)builder).IsIdentity
            .Should().BeTrue();
    }

    [Fact]
    public void GetIsIdentity_NotConfigured_ShouldReturnFalse()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        ((IEntityPropertyBuilder)builder).IsIdentity
            .Should().BeFalse();
    }

    [Fact]
    public void GetIsIgnored_Configured_ShouldReturnTrue()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        builder.IsIgnored();

        ((IEntityPropertyBuilder)builder).IsIgnored
            .Should().BeTrue();
    }

    [Fact]
    public void GetIsIgnored_NotConfigured_ShouldReturnFalse()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        ((IEntityPropertyBuilder)builder).IsIgnored
            .Should().BeFalse();
    }

    [Fact]
    public void GetIsKey_Configured_ShouldReturnTrue()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        builder.IsKey();

        ((IEntityPropertyBuilder)builder).IsKey
            .Should().BeTrue();
    }

    [Fact]
    public void GetIsKey_NotConfigured_ShouldReturnFalse()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        ((IEntityPropertyBuilder)builder).IsKey
            .Should().BeFalse();
    }

    [Fact]
    public void HasColumnName_ShouldSetColumnName()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");
        builder.HasColumnName("Identifier");

        ((IEntityPropertyBuilder)builder).ColumnName
            .Should().Be("Identifier");
    }

    [Fact]
    public void IsComputed_ShouldMarkPropertyAsComputed()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        builder.IsComputed();

        ((IEntityPropertyBuilder)builder).IsComputed
            .Should().BeTrue();
    }

    [Fact]
    public void IsIdentity_OtherPropertyIsAlreadyMarked_ShouldThrow()
    {
        var entityTypeBuilder = new EntityTypeBuilder<Entity>();
        entityTypeBuilder.Property(a => a.Id).IsIdentity();

        var builder = new EntityPropertyBuilder(entityTypeBuilder, "Property");

        Invoking(() => builder.IsIdentity())
            .Should().Throw<InvalidOperationException>()
            .WithMessage(
                "There is already the property 'Id' marked as an identity property for the entity type " +
                $"{typeof(Entity)}. Only one property can be marked as identity property per entity type."
            );
    }

    [Fact]
    public void IsIdentity_ShouldMarkPropertyAsIdentity()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        builder.IsIdentity();

        ((IEntityPropertyBuilder)builder).IsIdentity
            .Should().BeTrue();
    }

    [Fact]
    public void IsIgnored_ShouldMarkPropertyAsIgnored()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");
        builder.IsIgnored();

        ((IEntityPropertyBuilder)builder).IsIgnored
            .Should().BeTrue();
    }

    [Fact]
    public void IsKey_ShouldMarkPropertyAsKey()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");
        builder.IsKey();

        ((IEntityPropertyBuilder)builder).IsKey
            .Should().BeTrue();
    }
}
