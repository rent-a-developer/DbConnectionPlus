// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using System.Reflection;
using Fasterflect;
using RentADeveloper.DbConnectionPlus.Converters;

namespace RentADeveloper.DbConnectionPlus.Entities;

/// <summary>
/// Provides helper functions for dealing with entities.
/// </summary>
public static class EntityHelper
{
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
    public static ConstructorInfo? FindCompatibleConstructor(Type type, (String Name, Type Type)[] expectedParameters)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(expectedParameters);

        var constructors = type
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
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

            var areParametersCompatible =
                expectedParameters
                    .All(expectedParameter =>
                        parameters.Any(parameter =>
                            !String.IsNullOrWhiteSpace(parameter.Name) &&
                            parameter.Name.Equals(expectedParameter.Name, StringComparison.OrdinalIgnoreCase) &&
                            ValueConverter.CanConvert(expectedParameter.Type, parameter.ParameterType)
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
    public static ConstructorInfo? FindParameterlessConstructor(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
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
    public static EntityTypeMetadata GetEntityTypeMetadata(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);

        return entityTypeMetadataPerEntityType.GetOrAdd(
            entityType,
            static entityType2 => CreateEntityTypeMetadata(entityType2)
        );
    }

    /// <summary>
    /// Resets the cached entity types metadata.
    /// </summary>
    internal static void ResetEntityTypeMetadataCache() =>
        entityTypeMetadataPerEntityType.Clear();

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
    private static EntityTypeMetadata CreateEntityTypeMetadata(Type entityType)
    {
        String tableName;

        DbConnectionPlusConfiguration.Instance.GetEntityTypeBuilders()
            .TryGetValue(entityType, out var entityTypeBuilder);

        if (entityTypeBuilder is not null)
        {
            tableName = !String.IsNullOrWhiteSpace(entityTypeBuilder.TableName)
                ? entityTypeBuilder.TableName
                : entityType.Name;
        }
        else
        {
            tableName = !String.IsNullOrWhiteSpace(entityType.GetCustomAttribute<TableAttribute>()?.Name)
                ? entityType.GetCustomAttribute<TableAttribute>()?.Name!
                : entityType.Name;
        }

        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var propertiesMetadata = new EntityPropertyMetadata[properties.Length];

        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];

            if (
                entityTypeBuilder is not null &&
                entityTypeBuilder.PropertyBuilders.TryGetValue(property.Name, out var propertyBuilder)
            )
            {
                propertiesMetadata[i] = new(
                    property.CanRead,
                    property.CanWrite,
                    !String.IsNullOrWhiteSpace(propertyBuilder.ColumnName)
                        ? propertyBuilder.ColumnName
                        : property.Name,
                    propertyBuilder.IsComputed,
                    propertyBuilder.IsConcurrencyToken,
                    propertyBuilder.IsIdentity,
                    propertyBuilder.IsIgnored,
                    propertyBuilder.IsKey,
                    propertyBuilder.IsRowVersion,
                    property.CanRead ? Reflect.PropertyGetter(property) : null,
                    property,
                    property.Name,
                    property.CanWrite ? Reflect.PropertySetter(property) : null,
                    property.PropertyType
                );
            }
            else
            {
                propertiesMetadata[i] = new(
                    property.CanRead,
                    property.CanWrite,
                    property.GetCustomAttribute<ColumnAttribute>()?.Name ?? property.Name,
                    property.GetCustomAttribute<DatabaseGeneratedAttribute>()?.DatabaseGeneratedOption is
                        DatabaseGeneratedOption.Computed,
                    property.GetCustomAttribute<ConcurrencyCheckAttribute>() is not null,
                    property.GetCustomAttribute<DatabaseGeneratedAttribute>()?.DatabaseGeneratedOption is
                        DatabaseGeneratedOption.Identity,
                    property.GetCustomAttribute<NotMappedAttribute>() is not null,
                    property.GetCustomAttribute<KeyAttribute>() is not null,
                    property.GetCustomAttribute<TimestampAttribute>() is not null,
                    property.CanRead ? Reflect.PropertyGetter(property) : null,
                    property,
                    property.Name,
                    property.CanWrite ? Reflect.PropertySetter(property) : null,
                    property.PropertyType
                );
            }
        }

        var identityProperties = propertiesMetadata.Where(a => a.IsIdentity).ToList();

        if (identityProperties.Count > 1)
        {
            throw new InvalidOperationException(
                $"There are multiple identity properties defined for the entity type {entityType}. Only one property " +
                "can be marked as an identity property per entity type."
            );
        }

        IReadOnlyList<EntityPropertyMetadata> computedProperties =
            [.. propertiesMetadata.Where(p => p is { IsIgnored: false, IsComputed: true })];

        IReadOnlyList<EntityPropertyMetadata> concurrencyTokenProperties =
            [.. propertiesMetadata.Where(p => p is { IsIgnored: false, IsConcurrencyToken: true })];

        IReadOnlyList<EntityPropertyMetadata> databaseGeneratedProperties =
            [.. propertiesMetadata.Where(p => !p.IsIgnored && (p.IsComputed || p.IsIdentity || p.IsRowVersion))];

        IReadOnlyList<EntityPropertyMetadata> insertProperties =
        [
            .. propertiesMetadata.Where(p => p is
                { IsIgnored: false, IsComputed: false, IsIdentity: false, IsRowVersion: false }
            )
        ];

        IReadOnlyList<EntityPropertyMetadata> keyProperties =
            [.. propertiesMetadata.Where(p => p is { IsIgnored: false, IsKey: true })];

        IReadOnlyList<EntityPropertyMetadata> mappedProperties =
            [.. propertiesMetadata.Where(p => !p.IsIgnored)];

        IReadOnlyList<EntityPropertyMetadata> rowVersionProperties =
            [.. propertiesMetadata.Where(p => p is { IsIgnored: false, IsRowVersion: true })];

        IReadOnlyList<EntityPropertyMetadata> updateProperties =
        [
            .. propertiesMetadata.Where(p => p is
                {
                    IsComputed: false,
                    IsConcurrencyToken: false,
                    IsIgnored: false,
                    IsIdentity: false,
                    IsKey: false,
                    IsRowVersion: false
                }
            )
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

    private static readonly
        ConcurrentDictionary<Type, EntityTypeMetadata> entityTypeMetadataPerEntityType = [];
}
