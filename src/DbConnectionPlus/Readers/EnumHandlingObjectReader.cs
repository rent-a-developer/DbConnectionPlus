// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using FastMember;
using RentADeveloper.DbConnectionPlus.Converters;
using RentADeveloper.DbConnectionPlus.Extensions;

namespace RentADeveloper.DbConnectionPlus.Readers;

/// <summary>
/// A version of <see cref="ObjectReader" /> that handles enum serialization according to the setting
/// <see cref="DbConnectionExtensions.EnumSerializationMode" />.
/// 
/// For Enum fields <see cref="GetFieldType" /> returns the type that corresponds to the enum serialization mode.
/// For Enum fields <see cref="GetInt32" /> returns the enum value as <see cref="Int32" />.
/// For Enum fields <see cref="GetString" /> returns the enum value as <see cref="String" />.
/// For Enum fields <see cref="GetValues" /> serializes the enum values based on the enum serialization mode.
/// </summary>
internal class EnumHandlingObjectReader : ObjectReader
{
    /// <inheritdoc />
    public EnumHandlingObjectReader(Type type, IEnumerable source, params String[] members)
        : base(type, source, members)
    {
    }

    /// <inheritdoc />
    public override Type? GetFieldType(Int32 i)
    {
        var fieldType = base.GetFieldType(i);

        if (fieldType?.IsEnumOrNullableEnumType() == true)
        {
            return DbConnectionExtensions.EnumSerializationMode switch
            {
                EnumSerializationMode.Strings => typeof(String),
                EnumSerializationMode.Integers => typeof(Int32),
                _ =>
                    throw new NotSupportedException(
                        $"The {nameof(EnumSerializationMode)} " +
                        $"{DbConnectionExtensions.EnumSerializationMode.ToDebugString()} is not supported."
                    )
            };
        }

        if (fieldType?.IsCharOrNullableCharType() == true)
        {
            // The data readers of all major database systems return the type String for CHAR columns.
            // So we mimic the same behavior for consistency.

            return typeof(String);
        }

        return fieldType;
    }

    /// <inheritdoc />
    public override Int32 GetInt32(Int32 i)
    {
        if (base.GetFieldType(i)?.IsEnumOrNullableEnumType() == true && this.GetValue(i) is Enum enumValue)
        {
            return (Int32)(Object)enumValue;
        }

        return base.GetInt32(i);
    }

    /// <inheritdoc />
    public override String GetString(Int32 i)
    {
        if (base.GetFieldType(i)?.IsEnumOrNullableEnumType() == true && this.GetValue(i) is Enum enumValue)
        {
            return enumValue.ToString();
        }

        if (base.GetFieldType(i)?.IsCharOrNullableCharType() == true)
        {
            // The data readers of all major database systems return the type String for CHAR columns, which means that
            // GetString is called to retrieve the value.
            // This would cause ObjectReader to throw an InvalidCastException.
            // So we handle this case here and convert the Char to a String.

            var charValue = this.GetValue(i) as Char?;
            return charValue?.ToString() ?? String.Empty;
        }

        return base.GetString(i);
    }

    /// <inheritdoc />
    public override Int32 GetValues(Object[] values)
    {
        var numberOfObjects = base.GetValues(values);

        for (var i = 0; i < numberOfObjects; i++)
        {
            // ReSharper disable once ConvertSwitchStatementToSwitchExpression
            switch (values[i])
            {
                case Enum enumValue:
                    values[i] = EnumSerializer.SerializeEnum(enumValue, DbConnectionExtensions.EnumSerializationMode);
                    break;

                case Char charValue:
                    // The data readers of all major database systems return the type String for CHAR columns.
                    // So we mimic the same behavior for consistency.
                    values[i] = charValue.ToString();
                    break;
            }
        }

        return numberOfObjects;
    }
}
