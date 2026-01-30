// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus.Entities;

/// <summary>
/// The metadata of an entity type.
/// </summary>
/// <param name="EntityType">The entity type this metadata describes.</param>
/// <param name="TableName">The name of the database table where entities of the entity type are stored.</param>
/// <param name="AllProperties">The metadata of all instance properties of the entity type.</param>
/// <param name="AllPropertiesByPropertyName">
/// A dictionary containing the metadata of all instance properties of the entity type.
/// The keys of the dictionary are the property names.
/// The values of the dictionary are the corresponding property metadata.
/// </param>
/// <param name="MappedProperties">
/// The metadata of the mapped properties of the entity type.
/// </param>
/// <param name="KeyProperties">
/// The metadata of the key properties of the entity type.
/// </param>
/// <param name="ComputedProperties">
/// The metadata of the computed properties of the entity type.
/// </param>
/// <param name="IdentityProperty">
/// The metadata of the identity property of the entity type.
/// This is <see langword="null"/> if the entity type does not have an identity property.
/// </param>
/// <param name="DatabaseGeneratedProperties">
/// The metadata of the database-generated properties of the entity type.
/// </param>
/// <param name="InsertProperties">
/// The metadata of the properties needed to insert an entity of the entity type into the database.
/// </param>
/// <param name="UpdateProperties">
/// The metadata of the properties needed to update an entity of the entity type in the database.
/// </param>
public sealed record EntityTypeMetadata(
    Type EntityType,
    String TableName,
    IReadOnlyList<EntityPropertyMetadata> AllProperties,
    IReadOnlyDictionary<String, EntityPropertyMetadata> AllPropertiesByPropertyName,
    IReadOnlyList<EntityPropertyMetadata> MappedProperties,
    IReadOnlyList<EntityPropertyMetadata> KeyProperties,
    IReadOnlyList<EntityPropertyMetadata> ComputedProperties,
    EntityPropertyMetadata? IdentityProperty,
    IReadOnlyList<EntityPropertyMetadata> DatabaseGeneratedProperties,
    IReadOnlyList<EntityPropertyMetadata> InsertProperties,
    IReadOnlyList<EntityPropertyMetadata> UpdateProperties
);
