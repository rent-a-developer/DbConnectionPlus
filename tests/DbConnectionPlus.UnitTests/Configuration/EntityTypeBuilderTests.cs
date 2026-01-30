namespace RentADeveloper.DbConnectionPlus.UnitTests.Configuration;

public class EntityTypeBuilderTests
{
    [Fact]
    public void Freeze_ShouldFreezeBuilderAndAllPropertyBuilders()
    {
        var builder = new EntityTypeBuilder<Entity>();

        builder.ToTable("Entities");
        builder.Property(a => a.Id).IsKey();
        builder.Property(a => a.StringValue).IsIgnored();

        ((IFreezable)builder).Freeze();

        Invoking(() => builder.ToTable("Entities2"))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("This builder is frozen and can no longer be modified.");

        Invoking(() => builder.Property(a => a.Id).HasColumnName("Identifier"))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("This builder is frozen and can no longer be modified.");

        Invoking(() => builder.Property(a => a.StringValue).HasColumnName("String"))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("This builder is frozen and can no longer be modified.");
    }

    [Fact]
    public void Property_InvalidExpression_ShouldThrow()
    {
        var builder = new EntityTypeBuilder<Entity>();

        Invoking(() => builder.Property(a => a.Id.ToString()))
            .Should().Throw<ArgumentException>()
            .WithMessage(
                "The expression 'a => a.Id.ToString()' is not a valid property access expression. The expression " +
                "should represent a simple property access: 'a => a.MyProperty'.*"
            );
    }

    [Fact]
    public void Property_ShouldGetPropertyBuilder()
    {
        var builder = new EntityTypeBuilder<Entity>();

        var propertyBuilder = builder.Property(a => a.Id);

        propertyBuilder
            .Should().NotBeNull();

        builder.Property(a => a.Id)
            .Should().BeSameAs(propertyBuilder);
    }

    [Fact]
    public void PropertyBuilders_ShouldGetBuildersOfConfiguredProperties()
    {
        var builder = new EntityTypeBuilder<Entity>();

        builder.Property(a => a.Id).IsKey();
        builder.Property(a => a.StringValue).IsComputed();
        builder.Property(a => a.Int64Value).IsIgnored();

        var propertyBuilders = ((IEntityTypeBuilder)builder).PropertyBuilders;

        propertyBuilders
            .Should().HaveCount(3);

        propertyBuilders
            .Should().ContainKeys("Id", "StringValue", "Int64Value");

        propertyBuilders["Id"]
            .Should().BeSameAs(builder.Property(a => a.Id));

        propertyBuilders["StringValue"]
            .Should().BeSameAs(builder.Property(a => a.StringValue));

        propertyBuilders["Int64Value"]
            .Should().BeSameAs(builder.Property(a => a.Int64Value));
    }

    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        var builder = new EntityTypeBuilder<Entity>();

        ArgumentNullGuardVerifier.Verify(() =>
            builder.Property(a => a.Id)
        );
    }

    [Fact]
    public void TableName_NotConfigured_ShouldReturnNull()
    {
        var builder = new EntityTypeBuilder<Entity>();

        ((IEntityTypeBuilder)builder).TableName
            .Should().BeNull();
    }

    [Fact]
    public void ToTable_Configured_ShouldGetTableName()
    {
        var builder = new EntityTypeBuilder<Entity>();

        builder.ToTable("Entities");

        ((IEntityTypeBuilder)builder).TableName
            .Should().Be("Entities");
    }

    [Fact]
    public void ToTable_ShouldSetTableName()
    {
        var builder = new EntityTypeBuilder<Entity>();

        builder.ToTable("Entities");

        ((IEntityTypeBuilder)builder).TableName
            .Should().Be("Entities");
    }
}
