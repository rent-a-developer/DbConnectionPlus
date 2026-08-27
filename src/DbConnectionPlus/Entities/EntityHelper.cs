// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using System.Reflection;
using RentADeveloper.DbConnectionPlus.Converters;
using RentADeveloper.DbConnectionPlus.Materializers;
using RentADeveloper.DbConnectionPlus.Readers;

namespace RentADeveloper.DbConnectionPlus.Entities;

/// <summary>
/// Provides helper functions for dealing with entities.
/// </summary>
public static class EntityHelper
{
    /// <summary>
    /// The members of an entity type that this library reflects over, and which therefore must survive trimming.
    /// </summary>
    /// <remarks>
    /// Constructors cover <see cref="FindCompatibleConstructor" /> and <see cref="FindParameterlessConstructor" />
    /// (both of which pass <see cref="BindingFlags.Public" /> and <see cref="BindingFlags.NonPublic" />), and
    /// public properties cover the property scan in <see cref="CreateEntityTypeMetadata"/>.
    /// Every entry point that ends up reflecting over an entity type annotates its type parameter or
    /// <see cref="Type" /> parameter with this exact set — an incomplete annotation does not fail loudly, it
    /// silently binds fewer columns.
    /// </remarks>
    internal const DynamicallyAccessedMemberTypes EntityMemberTypes =
        DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors
        | DynamicallyAccessedMemberTypes.PublicProperties;

    /// <summary>
    /// The members that must survive trimming for a type used as the result type of a query.
    /// </summary>
    /// <remarks>
    /// The generic query methods accept an entity type, a value tuple type or a built-in type, and only decide
    /// which one it is at run time. Their type parameter therefore has to preserve the union of what both
    /// materializers reflect over — <see cref="EntityMemberTypes" /> plus the value tuple's public fields.
    /// </remarks>
    internal const DynamicallyAccessedMemberTypes QueryResultMemberTypes =
        EntityMemberTypes | ValueTupleMaterializerFactory.ValueTupleMemberTypes;

    /// <summary>
    /// The members that must survive trimming for a type whose values are written to a temporary table.
    /// </summary>
    /// <remarks>
    /// A temporary table is filled either from entities, whose metadata is read through
    /// <see cref="GetEntityTypeMetadata" />, or from scalar values, which are streamed through
    /// <see cref="EnumerableReader"/> — and that reader reports the value type from
    /// <see cref="DbDataReader.GetFieldType" />, whose contract requires the type's public fields and properties.
    /// </remarks>
    internal const DynamicallyAccessedMemberTypes TemporaryTableValueMemberTypes =
        EntityMemberTypes | DynamicallyAccessedMemberTypes.PublicFields;

    private static readonly ConcurrentDictionary<Type, EntityTypeMetadata> entityTypeMetadataPerEntityType = [];

    /// <summary>
    /// Tries to find a constructor of the type <paramref name="type" /> that has parameters compatible to the
    /// specified expected parameters.
    /// Public constructors are preferred over non-public ones.
    /// </summary>
    /// <param name="type">The type of which to find the constructor.</param>
    /// <param name="expectedParameters">
    /// The expected parameters of the constructor to find.
    ///
    /// The constructor to find must have parameters with the same names (case-insensitive) and compatible types.
    /// A parameter type is considered compatible if a value of the expected parameter type can be converted to
    /// the actual parameter type.
    /// The parameters do not need to be in the same order as specified here.
    /// </param>
    /// <returns>A compatible constructor, or <see langword="null" /> if none was found.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="type" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="expectedParameters" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    public static ConstructorInfo? FindCompatibleConstructor(
        [DynamicallyAccessedMembers(EntityMemberTypes)] Type type,
        (string Name, Type Type)[] expectedParameters
    )
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(expectedParameters);

        var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .OrderByDescending(c => c.IsPublic)
            .ThenBy(c => c.IsPrivate)
            .ThenBy(c => c.GetParameters().Length);

        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();

            if (parameters.Length != expectedParameters.Length)
            {
                continue;
            }

            var areParametersCompatible = expectedParameters.All(expectedParameter =>
                parameters.Any(parameter =>
                    !string.IsNullOrWhiteSpace(parameter.Name)
                    && parameter.Name.Equals(expectedParameter.Name, StringComparison.OrdinalIgnoreCase)
                    && ValueConverter.CanConvert(expectedParameter.Type, parameter.ParameterType)
                )
            );

            if (areParametersCompatible)
            {
                return constructor;
            }
        }

        return null;
    }

    /// <summary>
    /// Tries to find the parameterless constructor of the type <paramref name="type" />.
    /// Public constructors are preferred over non-public ones.
    /// </summary>
    /// <returns>
    /// The parameterless constructor of the type <paramref name="type" />, or <see langword="null" /> if none was
    /// found.
    /// </returns>
    /// <param name="type">The type of which to find the parameterless constructor.</param>
    /// <exception cref="ArgumentNullException"><paramref name="type" /> is <see langword="null" />.</exception>
    public static ConstructorInfo? FindParameterlessConstructor(
        [DynamicallyAccessedMembers(EntityMemberTypes)] Type type
    )
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .OrderByDescending(c => c.IsPublic)
            .ThenBy(c => c.IsPrivate)
            .FirstOrDefault(c => c.GetParameters().Length == 0);
    }

    /// <summary>
    /// Gets the metadata for the entity type <paramref name="entityType" />.
    /// </summary>
    /// <param name="entityType">The entity type for which to get the metadata.</param>
    /// <returns>
    /// An instance of <see cref="EntityTypeMetadata" /> containing the metadata for the entity type
    /// <paramref name="entityType" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="entityType" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// There is more than one identity property defined for the entity type <paramref name="entityType" />.
    /// </exception>
    public static EntityTypeMetadata GetEntityTypeMetadata(
        [DynamicallyAccessedMembers(EntityMemberTypes)] Type entityType
    )
    {
        ArgumentNullException.ThrowIfNull(entityType);

        // The cache is deliberately not populated through the GetOrAdd factory overload:
        // [DynamicallyAccessedMembers] does not flow into a lambda, so the annotation on entityType would be
        // lost on the way to CreateEntityTypeMetadata and the trimmer would drop the entity's members.
        // Reading through TryGetValue first keeps the hot path allocation- and reflection-free.
        if (entityTypeMetadataPerEntityType.TryGetValue(entityType, out var entityTypeMetadata))
        {
            return entityTypeMetadata;
        }

        return entityTypeMetadataPerEntityType.GetOrAdd(entityType, CreateEntityTypeMetadata(entityType));
    }

    /// <summary>
    /// Resets the cached entity types metadata.
    /// </summary>
    internal static void ResetEntityTypeMetadataCache() => entityTypeMetadataPerEntityType.Clear();

    /// <summary>
    /// Creates the metadata for the entity type <paramref name="entityType" />.
    /// </summary>
    /// <param name="entityType">The entity type for which to create the metadata.</param>
    /// <returns>
    /// An instance of <see cref="EntityTypeMetadata" /> containing the created metadata.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// There is more than one identity property defined for the entity type <paramref name="entityType" />.
    /// </exception>
    private static EntityTypeMetadata CreateEntityTypeMetadata(
        [DynamicallyAccessedMembers(EntityMemberTypes)] Type entityType
    )
    {
        string tableName;

        DbConnectionPlusConfiguration
            .Instance.GetEntityTypeBuilders()
            .TryGetValue(entityType, out var entityTypeBuilder);

        if (entityTypeBuilder is not null)
        {
            tableName = !string.IsNullOrWhiteSpace(entityTypeBuilder.TableName)
                ? entityTypeBuilder.TableName
                : entityType.Name;
        }
        else
        {
            tableName = !string.IsNullOrWhiteSpace(entityType.GetCustomAttribute<TableAttribute>()?.Name)
                ? entityType.GetCustomAttribute<TableAttribute>()?.Name!
                : entityType.Name;
        }

        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var propertiesMetadata = new EntityPropertyMetadata[properties.Length];

        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];

            if (
                entityTypeBuilder is not null
                && entityTypeBuilder.PropertyBuilders.TryGetValue(property.Name, out var propertyBuilder)
            )
            {
                propertiesMetadata[i] = new(
                    property.CanRead,
                    property.CanWrite,
                    !string.IsNullOrWhiteSpace(propertyBuilder.ColumnName) ? propertyBuilder.ColumnName : property.Name,
                    propertyBuilder.IsComputed,
                    propertyBuilder.IsConcurrencyToken,
                    propertyBuilder.IsIdentity,
                    propertyBuilder.IsIgnored,
                    propertyBuilder.IsKey,
                    propertyBuilder.IsRowVersion,
                    property.CanRead ? CreatePropertyGetter(property) : null,
                    property,
                    property.Name,
                    property.CanWrite ? CreatePropertySetter(property) : null,
                    property.PropertyType
                );
            }
            else
            {
                propertiesMetadata[i] = new(
                    property.CanRead,
                    property.CanWrite,
                    property.GetCustomAttribute<ColumnAttribute>()?.Name ?? property.Name,
                    property.GetCustomAttribute<DatabaseGeneratedAttribute>()?.DatabaseGeneratedOption
                        is DatabaseGeneratedOption.Computed,
                    property.GetCustomAttribute<ConcurrencyCheckAttribute>() is not null,
                    property.GetCustomAttribute<DatabaseGeneratedAttribute>()?.DatabaseGeneratedOption
                        is DatabaseGeneratedOption.Identity,
                    property.GetCustomAttribute<NotMappedAttribute>() is not null,
                    property.GetCustomAttribute<KeyAttribute>() is not null,
                    property.GetCustomAttribute<TimestampAttribute>() is not null,
                    property.CanRead ? CreatePropertyGetter(property) : null,
                    property,
                    property.Name,
                    property.CanWrite ? CreatePropertySetter(property) : null,
                    property.PropertyType
                );
            }
        }

        var identityProperties = propertiesMetadata.Where(a => a.IsIdentity).ToList();

        if (identityProperties.Count > 1)
        {
            throw new InvalidOperationException(
                $"There are multiple identity properties defined for the entity type {entityType}. Only one property "
                    + "can be marked as an identity property per entity type."
            );
        }

        IReadOnlyList<EntityPropertyMetadata> computedProperties =
        [
            .. propertiesMetadata.Where(p => p is { IsIgnored: false, IsComputed: true }),
        ];

        IReadOnlyList<EntityPropertyMetadata> concurrencyTokenProperties =
        [
            .. propertiesMetadata.Where(p => p is { IsIgnored: false, IsConcurrencyToken: true }),
        ];

        IReadOnlyList<EntityPropertyMetadata> databaseGeneratedProperties =
        [
            .. propertiesMetadata.Where(p => !p.IsIgnored && (p.IsComputed || p.IsIdentity || p.IsRowVersion)),
        ];

        IReadOnlyList<EntityPropertyMetadata> insertProperties =
        [
            .. propertiesMetadata.Where(p =>
                p is { IsIgnored: false, IsComputed: false, IsIdentity: false, IsRowVersion: false }
            ),
        ];

        IReadOnlyList<EntityPropertyMetadata> keyProperties =
        [
            .. propertiesMetadata.Where(p => p is { IsIgnored: false, IsKey: true }),
        ];

        IReadOnlyList<EntityPropertyMetadata> mappedProperties = [.. propertiesMetadata.Where(p => !p.IsIgnored)];

        IReadOnlyList<EntityPropertyMetadata> rowVersionProperties =
        [
            .. propertiesMetadata.Where(p => p is { IsIgnored: false, IsRowVersion: true }),
        ];

        IReadOnlyList<EntityPropertyMetadata> updateProperties =
        [
            .. propertiesMetadata.Where(p =>
                p
                    is {
                        IsComputed: false,
                        IsConcurrencyToken: false,
                        IsIgnored: false,
                        IsIdentity: false,
                        IsKey: false,
                        IsRowVersion: false
                    }
            ),
        ];

        return new(
            entityType,
            tableName,
            propertiesMetadata,
            propertiesMetadata.ToDictionary(p => p.PropertyName),
            computedProperties,
            concurrencyTokenProperties,
            databaseGeneratedProperties,
            identityProperties.FirstOrDefault(),
            insertProperties,
            keyProperties,
            mappedProperties,
            rowVersionProperties,
            updateProperties
        );
    }

    /// <summary>
    /// Creates the getter function for the property <paramref name="property" />.
    /// </summary>
    /// <param name="property">The property for which to create the getter function.</param>
    /// <returns>A function taking an entity and returning the value of <paramref name="property" />.</returns>
    /// <remarks>
    /// The underlying <see cref="MethodInvoker" /> is resolved on the first call and then kept in the closure, so
    /// building the metadata of an entity type costs nothing per property until an accessor is actually used. The
    /// unsynchronized assignment is deliberate: two threads racing here produce two equivalent invokers, and either
    /// one is correct.
    /// </remarks>
    private static Func<object, object?> CreatePropertyGetter(PropertyInfo property)
    {
        MethodInvoker? getMethodInvoker = null;

        return entity =>
        {
            getMethodInvoker ??= MethodInvoker.Create(property.GetMethod!);

            return getMethodInvoker.Invoke(entity);
        };
    }

    /// <summary>
    /// Creates the setter function for the property <paramref name="property" />.
    /// </summary>
    /// <param name="property">The property for which to create the setter function.</param>
    /// <returns>An action taking an entity and the value to assign to <paramref name="property" />.</returns>
    /// <remarks>
    /// The underlying <see cref="MethodInvoker" /> is resolved on the first call and then kept in the closure, so
    /// building the metadata of an entity type costs nothing per property until an accessor is actually used. The
    /// unsynchronized assignment is deliberate: two threads racing here produce two equivalent invokers, and either
    /// one is correct.
    /// </remarks>
    private static Action<object, object?> CreatePropertySetter(PropertyInfo property)
    {
        MethodInvoker? setMethodInvoker = null;

        return (entity, value) =>
        {
            setMethodInvoker ??= MethodInvoker.Create(property.SetMethod!);

            setMethodInvoker.Invoke(entity, value);
        };
    }
}
