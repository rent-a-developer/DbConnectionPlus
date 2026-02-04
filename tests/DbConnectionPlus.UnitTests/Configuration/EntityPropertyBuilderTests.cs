namespace RentADeveloper.DbConnectionPlus.UnitTests.Configuration;

public class EntityPropertyBuilderTests : UnitTestsBase
{
    [Fact]
    public void ColumnName_Configured_ShouldReturnColumnName()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        builder.HasColumnName("Identifier");

        ((IEntityPropertyBuilder)builder).ColumnName
            .Should().Be("Identifier");
    }

    [Fact]
    public void ColumnName_NotConfigured_ShouldReturnNull()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        ((IEntityPropertyBuilder)builder).ColumnName
            .Should().BeNull();
    }

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

        Invoking(() => builder.IsConcurrencyToken())
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

        Invoking(() => builder.IsRowVersion())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("The configuration of DbConnectionPlus is frozen and can no longer be modified.");
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
    public void IsComputed_Configured_ShouldReturnTrue()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        builder.IsComputed();

        ((IEntityPropertyBuilder)builder).IsComputed
            .Should().BeTrue();
    }

    [Fact]
    public void IsComputed_NotConfigured_ShouldReturnFalse()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        ((IEntityPropertyBuilder)builder).IsComputed
            .Should().BeFalse();
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
    public void IsConcurrencyToken_Configured_ShouldReturnTrue()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        builder.IsConcurrencyToken();

        ((IEntityPropertyBuilder)builder).IsConcurrencyToken
            .Should().BeTrue();
    }

    [Fact]
    public void IsConcurrencyToken_NotConfigured_ShouldReturnFalse()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        ((IEntityPropertyBuilder)builder).IsConcurrencyToken
            .Should().BeFalse();
    }

    [Fact]
    public void IsConcurrencyToken_ShouldMarkPropertyAsConcurrencyToken()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        builder.IsConcurrencyToken();

        ((IEntityPropertyBuilder)builder).IsConcurrencyToken
            .Should().BeTrue();
    }

    [Fact]
    public void IsIdentity_Configured_ShouldReturnTrue()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        builder.IsIdentity();

        ((IEntityPropertyBuilder)builder).IsIdentity
            .Should().BeTrue();
    }

    [Fact]
    public void IsIdentity_NotConfigured_ShouldReturnFalse()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        ((IEntityPropertyBuilder)builder).IsIdentity
            .Should().BeFalse();
    }

    [Fact]
    public void IsIdentity_OtherPropertyIsAlreadyMarked_ShouldThrow()
    {
        var entityTypeBuilder = new EntityTypeBuilder<Entity>();

        entityTypeBuilder.Property(a => a.Id).IsIdentity();

        var propertyBuilder = new EntityPropertyBuilder(entityTypeBuilder, "NotId");

        Invoking(() => propertyBuilder.IsIdentity())
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
    public void IsIgnored_Configured_ShouldReturnTrue()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        builder.IsIgnored();

        ((IEntityPropertyBuilder)builder).IsIgnored
            .Should().BeTrue();
    }

    [Fact]
    public void IsIgnored_NotConfigured_ShouldReturnFalse()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        ((IEntityPropertyBuilder)builder).IsIgnored
            .Should().BeFalse();
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
    public void IsKey_Configured_ShouldReturnTrue()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        builder.IsKey();

        ((IEntityPropertyBuilder)builder).IsKey
            .Should().BeTrue();
    }

    [Fact]
    public void IsKey_NotConfigured_ShouldReturnFalse()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        ((IEntityPropertyBuilder)builder).IsKey
            .Should().BeFalse();
    }

    [Fact]
    public void IsKey_ShouldMarkPropertyAsKey()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        builder.IsKey();

        ((IEntityPropertyBuilder)builder).IsKey
            .Should().BeTrue();
    }

    [Fact]
    public void IsRowVersion_Configured_ShouldReturnTrue()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        builder.IsRowVersion();

        ((IEntityPropertyBuilder)builder).IsRowVersion
            .Should().BeTrue();
    }

    [Fact]
    public void IsRowVersion_NotConfigured_ShouldReturnFalse()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        ((IEntityPropertyBuilder)builder).IsRowVersion
            .Should().BeFalse();
    }

    [Fact]
    public void IsRowVersion_ShouldMarkPropertyAsRowVersion()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        builder.IsRowVersion();

        ((IEntityPropertyBuilder)builder).IsRowVersion
            .Should().BeTrue();
    }

    [Fact]
    public void PropertyName_ShouldReturnPropertyName()
    {
        var builder = new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property");

        ((IEntityPropertyBuilder)builder).PropertyName
            .Should().Be("Property");
    }

    [Fact]
    public void ShouldGuardAgainstNullArguments() =>
        ArgumentNullGuardVerifier.Verify(() =>
            new EntityPropertyBuilder(Substitute.For<IEntityTypeBuilder>(), "Property")
        );
}
