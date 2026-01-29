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
    public static EntityTypeMetadata GetEntityTypeMetadata(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);

        return entityTypeMetadataPerEntityType.GetOrAdd(
            entityType,
            static entityType2 => CreateEntityTypeMetadata(entityType2)
        );
    }

    /// <summary>
    /// Creates the metadata for the entity type <paramref name="entityType" />.
    /// </summary>
    /// <param name="entityType">The entity type for which to create the metadata.</param>
    /// <returns>
    /// An instance of <see cref="EntityTypeMetadata" /> containing the created metadata.
    /// </returns>
    private static EntityTypeMetadata CreateEntityTypeMetadata(Type entityType)
    {
        var tableName = entityType.GetCustomAttribute<TableAttribute>()?.Name ?? entityType.Name;
        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var propertiesMetadata = new EntityPropertyMetadata[properties.Length];

        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];

            propertiesMetadata[i] = new(
                property.Name,
                property.PropertyType,
                property,
                property.GetCustomAttribute<NotMappedAttribute>() is not null,
                property.GetCustomAttribute<KeyAttribute>() is not null,
                property.CanRead,
                property.CanWrite,
                property.CanRead ? Reflect.PropertyGetter(property) : null,
                property.CanWrite ? Reflect.PropertySetter(property) : null,
                property.GetCustomAttribute<DatabaseGeneratedAttribute>()?.DatabaseGeneratedOption ??
                DatabaseGeneratedOption.None
            );
        }

        return new(
            entityType,
            tableName,
            AllProperties: propertiesMetadata,
            AllPropertiesByPropertyName: propertiesMetadata
                .ToDictionary(p => p.PropertyName),
            MappedProperties: propertiesMetadata
                .Where(p => !p.IsNotMapped)
                .ToList(),
            KeyProperties: propertiesMetadata
                .Where(p => p is
                {
                    IsNotMapped: false, 
                    IsKeyProperty: true
                })
                .ToList(),
            InsertProperties: propertiesMetadata
                .Where(p => p is
                {
                    IsNotMapped: false, 
                    DatabaseGeneratedOption: DatabaseGeneratedOption.None
                })
                .ToList(),
            UpdateProperties: propertiesMetadata
                .Where(p => p is
                {
                    IsNotMapped: false,
                    IsKeyProperty: false,
                    DatabaseGeneratedOption: DatabaseGeneratedOption.None
                })
                .ToList(),
            DatabaseGeneratedProperties: propertiesMetadata
                .Where(p => p is
                    {
                        IsNotMapped: false,
                        DatabaseGeneratedOption: DatabaseGeneratedOption.Identity or DatabaseGeneratedOption.Computed
                    }
                )
                .ToList()
        );
    }

    private static readonly
        ConcurrentDictionary<Type, EntityTypeMetadata> entityTypeMetadataPerEntityType = [];
}
