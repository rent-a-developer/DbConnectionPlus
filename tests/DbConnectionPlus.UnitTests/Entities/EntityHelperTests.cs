using System.Reflection;
using AutoFixture;
using AutoFixture.Kernel;
using Bogus;
using RentADeveloper.DbConnectionPlus.Entities;

#pragma warning disable IDE0200

namespace RentADeveloper.DbConnectionPlus.UnitTests.Entities;

public class EntityHelperTests : UnitTestsBase
{
    [Fact]
    public void FindCompatibleConstructor_MatchingPrivateConstructor_ShouldReturnPrivateConstructor()
    {
        var constructor = EntityHelper.FindCompatibleConstructor(
            typeof(ItemWithPrivateConstructor),
            [("c", typeof(Int64)), ("b", typeof(Int32)), ("a", typeof(Int16))]
        );

        constructor
            .Should().NotBeNull();

        constructor
            .GetParameters()
            .Select(a => (a.Name, a.ParameterType))
            .Should().BeEquivalentTo([("a", typeof(Int16)), ("b", typeof(Int32)), ("c", typeof(Int64))]);
    }

    [Fact]
    public void FindCompatibleConstructor_NamesAndTypesMatchWithDifferentOrder_ShouldReturnConstructor()
    {
        var constructor = EntityHelper.FindCompatibleConstructor(
            typeof(ItemWithConstructor),
            [("c", typeof(Int64)), ("b", typeof(Int32)), ("a", typeof(Int16))]
        );

        constructor
            .Should().NotBeNull();

        constructor
            .GetParameters()
            .Select(a => (a.Name, a.ParameterType))
            .Should().BeEquivalentTo([("a", typeof(Int16)), ("b", typeof(Int32)), ("c", typeof(Int64))]);
    }

    [Fact]
    public void FindCompatibleConstructor_NamesDoNotMatch_TypesMatch_ShouldReturnNull() =>
        EntityHelper.FindCompatibleConstructor(
                typeof(ItemWithConstructor),
                [("d", typeof(Int16)), ("e", typeof(Int32)), ("f", typeof(Int64))]
            )
            .Should().BeNull();

    [Fact]
    public void FindCompatibleConstructor_NamesMatch_TypesAreCompatible_ShouldReturnConstructor()
    {
        var constructor = EntityHelper.FindCompatibleConstructor(
            typeof(ItemWithConstructor),
            [("a", typeof(Int32)), ("b", typeof(Int32)), ("c", typeof(Int32))]
        );

        constructor
            .Should().NotBeNull();

        constructor
            .GetParameters()
            .Select(a => (a.Name, a.ParameterType))
            .Should().BeEquivalentTo([("a", typeof(Int16)), ("b", typeof(Int32)), ("c", typeof(Int64))]);
    }

    [Fact]
    public void FindCompatibleConstructor_NamesMatch_TypesAreIncompatible_ShouldReturnNull() =>
        EntityHelper.FindCompatibleConstructor(
                typeof(ItemWithConstructor),
                [("a", typeof(Int16)), ("b", typeof(Int32)), ("c", typeof(TimeSpan))]
            )
            .Should().BeNull();

    [Fact]
    public void FindCompatibleConstructor_NamesMatch_TypesMatch_ShouldReturnConstructor()
    {
        var constructor = EntityHelper.FindCompatibleConstructor(
            typeof(ItemWithConstructor),
            [("a", typeof(Int16)), ("b", typeof(Int32)), ("c", typeof(Int64))]
        );

        constructor
            .Should().NotBeNull();

        constructor
            .GetParameters()
            .Select(a => (a.Name, a.ParameterType))
            .Should().BeEquivalentTo([("a", typeof(Int16)), ("b", typeof(Int32)), ("c", typeof(Int64))]);
    }

    [Fact]
    public void FindCompatibleConstructor_NamesMatchWithDifferentCasing_TypesMatch_ShouldReturnConstructor()
    {
        var constructor = EntityHelper.FindCompatibleConstructor(
            typeof(ItemWithConstructor),
            [("A", typeof(Int16)), ("B", typeof(Int32)), ("C", typeof(Int64))]
        );

        constructor
            .Should().NotBeNull();

        constructor
            .GetParameters()
            .Select(a => a.ParameterType)
            .Should().BeEquivalentTo([typeof(Int16), typeof(Int32), typeof(Int64)]);
    }

    [Fact]
    public void FindCompatibleConstructor_NoMatchingConstructor_ShouldReturnNull() =>
        EntityHelper.FindCompatibleConstructor(
                typeof(ItemWithConstructor),
                [("a", typeof(Int16)), ("b", typeof(Int32)), ("c", typeof(Int64)), ("d", typeof(String))]
            )
            .Should().BeNull();

    [Fact]
    public void FindParameterlessConstructor_NoParameterlessConstructor_ShouldReturnNull()
    {
        var constructor = EntityHelper.FindParameterlessConstructor(typeof(EntityWithPublicConstructor));

        constructor
            .Should().BeNull();
    }

    [Fact]
    public void FindParameterlessConstructor_PrivateParameterlessConstructor_ShouldReturnPrivateConstructor()
    {
        var constructor = EntityHelper.FindParameterlessConstructor(typeof(ItemWithPrivateParameterlessConstructor));

        constructor
            .Should().BeSameAs(
                typeof(ItemWithPrivateParameterlessConstructor).GetConstructor(
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    Type.EmptyTypes
                )
            );
    }

    [Fact]
    public void FindParameterlessConstructor_PublicParameterlessConstructor_ShouldReturnPublicConstructor()
    {
        var constructor = EntityHelper.FindParameterlessConstructor(typeof(Entity));

        constructor
            .Should().BeSameAs(
                typeof(Entity).GetConstructor(BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes)
            );
    }

    [Theory]
    [InlineData(typeof(Entity))]
    [InlineData(typeof(EntityWithTableAttribute))]
    [InlineData(typeof(EntityWithIdentityAndComputedProperties))]
    public void GetEntityTypeMetadata_ShouldGetMetadataForEntityType(Type entityType)
    {
        var faker = new Faker();

        var fixture = new Fixture();
        fixture.Register(() => faker.Date.PastDateOnly());
        fixture.Register(() => faker.Date.RecentTimeOnly());

        var entity = specimenFactoryCreateMethod
            .MakeGenericMethod(entityType)
            .Invoke(null, [fixture]);

        var entityProperties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var metadata = EntityHelper.GetEntityTypeMetadata(entityType);

        metadata
            .Should().NotBeNull();

        metadata.EntityType
            .Should().Be(entityType);

        metadata.TableName
            .Should().Be(entityType.GetCustomAttribute<TableAttribute>()?.Name ?? entityType.Name);

        var allPropertiesMetadata = metadata.AllProperties;

        allPropertiesMetadata
            .Should().HaveSameCount(entityProperties);


        metadata.AllPropertiesByPropertyName
            .Should().BeEquivalentTo(allPropertiesMetadata.ToDictionary(a => a.PropertyName));

        metadata.MappedProperties
            .Should()
            .BeEquivalentTo(allPropertiesMetadata.Where(a => a is { IsNotMapped: false }));

        metadata.KeyProperties
            .Should()
            .BeEquivalentTo(allPropertiesMetadata.Where(a => a is { IsNotMapped: false, IsKeyProperty: true }));

        metadata.InsertProperties
            .Should().BeEquivalentTo(
                allPropertiesMetadata.Where(a => a is
                    { IsNotMapped: false, DatabaseGeneratedOption: DatabaseGeneratedOption.None }
                )
            );

        metadata.UpdateProperties
            .Should().BeEquivalentTo(
                allPropertiesMetadata.Where(a => a is
                    {
                        IsNotMapped: false,
                        IsKeyProperty: false,
                        DatabaseGeneratedOption: DatabaseGeneratedOption.None
                    }
                )
            );

        metadata.ComputedProperties
            .Should().BeEquivalentTo(
                allPropertiesMetadata.Where(a => a is
                    {
                        IsNotMapped: false,
                        DatabaseGeneratedOption: DatabaseGeneratedOption.Computed
                    }
                )
            );

        metadata.IdentityAndComputedProperties
            .Should().BeEquivalentTo(
                allPropertiesMetadata.Where(a => a is
                    {
                        IsNotMapped: false,
                        DatabaseGeneratedOption: DatabaseGeneratedOption.Identity or DatabaseGeneratedOption.Computed
                    }
                )
            );

        foreach (var property in entityProperties)
        {
            var propertyMetadata = allPropertiesMetadata.FirstOrDefault(a => a.PropertyName == property.Name);

            propertyMetadata
                .Should().NotBeNull();

            propertyMetadata.PropertyName
                .Should().Be(property.Name);

            propertyMetadata.PropertyType
                .Should().Be(property.PropertyType);

            propertyMetadata.PropertyInfo
                .Should().BeSameAs(property);

            propertyMetadata.IsNotMapped
                .Should().Be(property.GetCustomAttribute<NotMappedAttribute>() is not null);

            propertyMetadata.IsKeyProperty
                .Should().Be(property.GetCustomAttribute<KeyAttribute>() is not null);

            propertyMetadata.CanRead
                .Should().Be(property.CanRead);

            propertyMetadata.CanWrite
                .Should().Be(property.CanWrite);

            if (propertyMetadata.CanRead)
            {
                propertyMetadata.PropertyGetter
                    .Should().NotBeNull();

                propertyMetadata.PropertyGetter(entity)
                    .Should().Be(property.GetValue(entity));
            }
            else
            {
                propertyMetadata.PropertyGetter
                    .Should().BeNull();
            }

            if (propertyMetadata.CanWrite)
            {
                propertyMetadata.PropertySetter
                    .Should().NotBeNull();

                var value = specimenFactoryCreateMethod
                    .MakeGenericMethod(property.PropertyType)
                    .Invoke(null, [fixture]);

                propertyMetadata.PropertySetter(entity, value);

                property.GetValue(entity)
                    .Should().Be(value);
            }
            else
            {
                propertyMetadata.PropertySetter
                    .Should().BeNull();
            }

            propertyMetadata.DatabaseGeneratedOption
                .Should().Be(
                    property.GetCustomAttribute<DatabaseGeneratedAttribute>()?.DatabaseGeneratedOption ??
                    DatabaseGeneratedOption.None
                );
        }
    }

    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        (String Name, Type Type)[] constructorParameters =
            [("a", typeof(Int16)), ("b", typeof(Int32)), ("c", typeof(Int64))];

        ArgumentNullGuardVerifier.Verify(() =>
            EntityHelper.FindCompatibleConstructor(typeof(ItemWithConstructor), constructorParameters)
        );
        ArgumentNullGuardVerifier.Verify(() => EntityHelper.FindParameterlessConstructor(typeof(ItemWithConstructor)));
        ArgumentNullGuardVerifier.Verify(() => EntityHelper.GetEntityTypeMetadata(typeof(Entity)));
    }

    /// <summary>
    /// The <see cref="SpecimenFactory.Create{T}(AutoFixture.Kernel.ISpecimenBuilder)" /> method.
    /// </summary>
    private static readonly MethodInfo specimenFactoryCreateMethod = typeof(SpecimenFactory)
        .GetMethod(
            nameof(SpecimenFactory.Create),
            BindingFlags.Public | BindingFlags.Static,
            [typeof(ISpecimenBuilder)]
        )!;
}
