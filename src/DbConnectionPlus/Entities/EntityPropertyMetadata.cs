// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using System.Reflection;
using Fasterflect;

namespace RentADeveloper.DbConnectionPlus.Entities;

/// <summary>
/// Metadata of an entity property.
/// </summary>
/// <param name="PropertyName">The name of the property.</param>
/// <param name="PropertyType">The property type of the property.</param>
/// <param name="PropertyInfo">The property info of the property.</param>
/// <param name="IsNotMapped">Determines whether the property is not mapped to a database column.</param>
/// <param name="IsKeyProperty">Determines whether the property is a key property.</param>
/// <param name="CanRead">Determines whether the property can be read.</param>
/// <param name="CanWrite">Determines whether the property can be written to.</param>
/// <param name="PropertyGetter">
/// The getter function for the property.
/// This is <see langword="null" /> if the property has no getter.
/// </param>
/// <param name="PropertySetter">
/// The setter function for the property.
/// This is <see langword="null" /> if the property has no setter.
/// </param>
/// <param name="DatabaseGeneratedOption">The database generated option for the property.</param>
public sealed record EntityPropertyMetadata(
    String PropertyName,
    Type PropertyType,
    PropertyInfo PropertyInfo,
    Boolean IsNotMapped,
    Boolean IsKeyProperty,
    Boolean CanRead,
    Boolean CanWrite,
    MemberGetter? PropertyGetter,
    MemberSetter? PropertySetter,
    DatabaseGeneratedOption DatabaseGeneratedOption
);
