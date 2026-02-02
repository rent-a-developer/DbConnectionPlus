using System.Reflection;
using AutoFixture;
using AutoFixture.Kernel;
using Bogus;
using RentADeveloper.DbConnectionPlus.Entities;

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

    [Fact]
    public void GetEntityTypeMetadata_Mapping_FluentApi_ShouldGetMetadataBasedOnFluentApiMapping()
    {
        MappingTestEntityFluentApi.Configure();

        var metadata = EntityHelper.GetEntityTypeMetadata(typeof(MappingTestEntityFluentApi));

        metadata
            .Should().NotBeNull();

        metadata.EntityType
            .Should().Be(typeof(MappingTestEntityFluentApi));

        metadata.TableName
            .Should().Be("MappingTestEntity");

        metadata.AllProperties
            .Should().HaveCount(8);

        metadata.AllPropertiesByPropertyName
            .Should().BeEquivalentTo(metadata.AllProperties.ToDictionary(a => a.PropertyName));

        var computedProperty = metadata.AllPropertiesByPropertyName["Computed_"];

        computedProperty.ColumnName
            .Should().Be("Computed");

        computedProperty.IsComputed
            .Should().BeTrue();

        var concurrencyTokenProperty = metadata.AllPropertiesByPropertyName["ConcurrencyToken_"];

        concurrencyTokenProperty.ColumnName
            .Should().Be("ConcurrencyToken");

        concurrencyTokenProperty.IsConcurrencyToken
            .Should().BeTrue();

        var identityProperty = metadata.AllPropertiesByPropertyName["Identity_"];
        
        identityProperty.ColumnName
            .Should().Be("Identity");

        identityProperty.IsIdentity
            .Should().BeTrue();

        var key1Property = metadata.AllPropertiesByPropertyName["Key1_"];
        
        key1Property.ColumnName
            .Should().Be("Key1");

        key1Property.IsKey
            .Should().BeTrue();

        var key2Property = metadata.AllPropertiesByPropertyName["Key2_"];
        
        key2Property.ColumnName
            .Should().Be("Key2");

        key2Property.IsKey
            .Should().BeTrue();

        var nameProperty = metadata.AllPropertiesByPropertyName["Name_"];

        nameProperty.ColumnName
            .Should().Be("Name");

        var notMappedProperty = metadata.AllPropertiesByPropertyName["NotMapped"];

        notMappedProperty.IsIgnored
            .Should().BeTrue();

        var rowVersionProperty = metadata.AllPropertiesByPropertyName["RowVersion_"];

        rowVersionProperty.IsRowVersion
            .Should().BeTrue();

        metadata.ComputedProperties
            .Should().BeEquivalentTo([computedProperty]);

        metadata.ConcurrencyTokenProperties
            .Should().BeEquivalentTo([concurrencyTokenProperty]);

        metadata.DatabaseGeneratedProperties
            .Should().BeEquivalentTo([computedProperty, identityProperty, rowVersionProperty]);

        metadata.IdentityProperty
            .Should().Be(identityProperty);

        metadata.InsertProperties
            .Should().BeEquivalentTo([concurrencyTokenProperty, key1Property, key2Property, nameProperty]);

        metadata.KeyProperties
            .Should().BeEquivalentTo([key1Property, key2Property]);

        metadata.MappedProperties
            .Should().BeEquivalentTo(
                [computedProperty, concurrencyTokenProperty, identityProperty, key1Property, key2Property, nameProperty, rowVersionProperty]
            );

        metadata.RowVersionProperties
            .Should().BeEquivalentTo(
                [rowVersionProperty]
            );

        metadata.UpdateProperties
            .Should().BeEquivalentTo([nameProperty]);
    }

    [Fact]
    public void GetEntityTypeMetadata_MoreThanOneIdentityProperty_ShouldThrow() =>
        Invoking(() => EntityHelper.GetEntityTypeMetadata(typeof(EntityWithMultipleIdentityProperties)))
            .Should().Throw<InvalidOperationException>()
            .WithMessage(
                "There are multiple identity properties defined for the entity type " +
                $"{typeof(EntityWithMultipleIdentityProperties)}. Only one property can be marked as an identity " +
                "property per entity type."
            );

    [Fact]
    public void GetEntityTypeMetadata_Mapping_Attributes_ShouldGetMetadataBasedOnAttributes()
    {
        var faker = new Faker();

        var fixture = new Fixture();
        fixture.Register(() => faker.Date.PastDateOnly());
        fixture.Register(() => faker.Date.RecentTimeOnly());

        var entityType = typeof(MappingTestEntityAttributes);

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

        metadata.ComputedProperties
            .Should()
            .BeEquivalentTo(allPropertiesMetadata.Where(a => a is { IsIgnored: false, IsComputed: true }));

        metadata.ConcurrencyTokenProperties
            .Should()
            .BeEquivalentTo(allPropertiesMetadata.Where(a => a is { IsIgnored: false, IsConcurrencyToken: true }));

        metadata.DatabaseGeneratedProperties
            .Should()
            .BeEquivalentTo(allPropertiesMetadata.Where(a => !a.IsIgnored && (a.IsComputed || a.IsIdentity || a.IsRowVersion)));

        metadata.IdentityProperty
            .Should()
            .Be(allPropertiesMetadata.FirstOrDefault(a => a is { IsIgnored: false, IsIdentity: true }));

        metadata.InsertProperties
            .Should().BeEquivalentTo(
                allPropertiesMetadata.Where(a => a is
                { IsIgnored: false, IsComputed: false, IsIdentity: false, IsRowVersion:false }
                )
            );

        metadata.KeyProperties
            .Should()
            .BeEquivalentTo(allPropertiesMetadata.Where(a => a is { IsIgnored: false, IsKey: true }));

        metadata.MappedProperties
            .Should()
            .BeEquivalentTo(allPropertiesMetadata.Where(a => a is { IsIgnored: false }));

        metadata.RowVersionProperties
            .Should()
            .BeEquivalentTo(allPropertiesMetadata.Where(a => a is { IsRowVersion: true }));

        metadata.UpdateProperties
            .Should().BeEquivalentTo(
                allPropertiesMetadata.Where(a => a is
                    {
                        IsComputed: false,
                        IsConcurrencyToken: false,
                        IsIgnored: false,
                        IsIdentity: false,
                        IsKey: false,
                        IsRowVersion: false
                    }
                )
            );

        foreach (var property in entityProperties)
        {
            var propertyMetadata = allPropertiesMetadata.FirstOrDefault(a => a.PropertyName == property.Name);

            propertyMetadata
                .Should().NotBeNull();

            propertyMetadata.CanRead
                .Should().Be(property.CanRead);

            propertyMetadata.CanWrite
                .Should().Be(property.CanWrite);

            propertyMetadata.ColumnName
                .Should().Be(property.GetCustomAttribute<ColumnAttribute>()?.Name ?? property.Name);

            propertyMetadata.IsComputed
                .Should().Be(
                    property.GetCustomAttribute<DatabaseGeneratedAttribute>()?.DatabaseGeneratedOption is
                        DatabaseGeneratedOption.Computed
                );

            propertyMetadata.IsConcurrencyToken
                .Should().Be(property.GetCustomAttribute<ConcurrencyCheckAttribute>() is not null);

            propertyMetadata.IsIdentity
                .Should().Be(
                    property.GetCustomAttribute<DatabaseGeneratedAttribute>()?.DatabaseGeneratedOption is
                        DatabaseGeneratedOption.Identity
                );

            propertyMetadata.IsIgnored
                .Should().Be(property.GetCustomAttribute<NotMappedAttribute>() is not null);

            propertyMetadata.IsKey
                .Should().Be(property.GetCustomAttribute<KeyAttribute>() is not null);

            propertyMetadata.IsRowVersion
                .Should().Be(property.GetCustomAttribute<TimestampAttribute>() is not null);

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

            propertyMetadata.PropertyInfo
                .Should().BeSameAs(property);

            propertyMetadata.PropertyName
                .Should().Be(property.Name);

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

            propertyMetadata.PropertyType
                .Should().Be(property.PropertyType);
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
