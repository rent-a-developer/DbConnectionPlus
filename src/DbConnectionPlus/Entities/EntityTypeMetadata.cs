// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus.Entities;

/// <summary>
/// Metadata of an entity type.
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
/// The metadata of all instance properties of the entity type not denoted with the <see cref="NotMappedAttribute" />.
/// </param>
/// <param name="KeyProperties">
/// The metadata of all public instance properties of the entity type that are denoted with the
/// <see cref="KeyAttribute" /> and not denoted with the <see cref="NotMappedAttribute" />.
/// </param>
/// <param name="InsertProperties">
/// The metadata of all public instance properties needed to insert an entity of the entity type into the database.
/// </param>
/// <param name="UpdateProperties">
/// The metadata of all public instance properties needed to update an entity of the entity type in the database.
/// </param>
/// <param name="ComputedProperties">
/// The metadata of all public instance properties of the entity type that are not denoted with the
/// <see cref="NotMappedAttribute" /> and are denoted with the <see cref="DatabaseGeneratedAttribute" />
/// where <see cref="DatabaseGeneratedAttribute.DatabaseGeneratedOption" /> is set to
/// <see cref="DatabaseGeneratedOption.Computed" />.
/// </param>
/// <param name="IdentityAndComputedProperties">
/// The metadata of all public instance properties of the entity type that are not denoted with the
/// <see cref="NotMappedAttribute" /> and are denoted with the <see cref="DatabaseGeneratedAttribute" />
/// where <see cref="DatabaseGeneratedAttribute.DatabaseGeneratedOption" /> is set to
/// <see cref="DatabaseGeneratedOption.Identity" /> or <see cref="DatabaseGeneratedOption.Computed" />.
/// </param>
public sealed record EntityTypeMetadata(
    Type EntityType,
    String TableName,
    IReadOnlyList<EntityPropertyMetadata> AllProperties,
    IReadOnlyDictionary<String, EntityPropertyMetadata> AllPropertiesByPropertyName,
    IReadOnlyList<EntityPropertyMetadata> MappedProperties,
    IReadOnlyList<EntityPropertyMetadata> KeyProperties,
    IReadOnlyList<EntityPropertyMetadata> InsertProperties,
    IReadOnlyList<EntityPropertyMetadata> UpdateProperties,
    IReadOnlyList<EntityPropertyMetadata> ComputedProperties,
    IReadOnlyList<EntityPropertyMetadata> IdentityAndComputedProperties
);
