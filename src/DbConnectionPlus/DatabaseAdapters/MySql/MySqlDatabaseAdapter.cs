// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using RentADeveloper.DbConnectionPlus.Converters;

namespace RentADeveloper.DbConnectionPlus.DatabaseAdapters.MySql;

/// <summary>
/// The database adapter for MySQL databases.
/// </summary>
internal class MySqlDatabaseAdapter : IDatabaseAdapter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlDatabaseAdapter" /> class.
    /// </summary>
    public MySqlDatabaseAdapter()
    {
        this.entityManipulator = new(this);
        this.temporaryTableBuilder = new(this);
    }

    /// <inheritdoc />
    public IEntityManipulator EntityManipulator => this.entityManipulator;

    /// <inheritdoc />
    public ITemporaryTableBuilder TemporaryTableBuilder =>
        this.temporaryTableBuilder;

    /// <inheritdoc />
    public void BindParameterValue(DbParameter parameter, Object? value)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        switch (value)
        {
            case Enum enumValue:
                parameter.DbType = DbConnectionPlusConfiguration.Instance.EnumSerializationMode switch
                {
                    EnumSerializationMode.Integers =>
                        DbType.Int32,

                    EnumSerializationMode.Strings =>
                        DbType.String,

                    _ =>
                        ThrowHelper.ThrowInvalidEnumSerializationModeException<DbType>(
                            DbConnectionPlusConfiguration.Instance.EnumSerializationMode
                        )
                };

                parameter.Value = EnumSerializer.SerializeEnum(
                    enumValue,
                    DbConnectionPlusConfiguration.Instance.EnumSerializationMode
                );
                break;

            case DateTime:
                parameter.DbType = DbType.DateTime;
                parameter.Value = value;
                break;

            case Byte[]:
                parameter.DbType = DbType.Binary;
                parameter.Value = value;
                break;

            default:
                parameter.Value = value ?? DBNull.Value;
                break;
        }
    }

    /// <inheritdoc />
    public String FormatParameterName(String parameterName) =>
        "@" + parameterName;

    /// <inheritdoc />
    public String GetDataType(Type type, EnumSerializationMode enumSerializationMode)
    {
        ArgumentNullException.ThrowIfNull(type);

        // Unwrap Nullable<T> types:
        var effectiveType = Nullable.GetUnderlyingType(type) ?? type;

        if (effectiveType.IsEnum)
        {
            return enumSerializationMode switch
            {
                EnumSerializationMode.Strings =>
                    "VARCHAR(200)", // 200 should be enough for most enum names

                EnumSerializationMode.Integers =>
                    "INT",

                _ => ThrowHelper.ThrowInvalidEnumSerializationModeException<String>(enumSerializationMode)
            };
        }

        if (!typeToMySqlDataType.TryGetValue(effectiveType, out var result))
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                $"Could not map the type {type} to a MySQL data type."
            );
        }

        return result;
    }

    /// <inheritdoc />
    public String QuoteIdentifier(String identifier) =>
        "`" + identifier + "`";

    /// <inheritdoc />
    public String QuoteTemporaryTableName(String tableName, DbConnection connection) =>
        "`" + tableName + "`";

    /// <inheritdoc />
    public Boolean SupportsTemporaryTables(DbConnection connection) =>
        true;

    /// <inheritdoc />
    public Boolean WasSqlStatementCancelledByCancellationToken(Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(exception);

        // MySqlConnector does not support proper statement cancellation.
        return false;
    }

    private readonly MySqlEntityManipulator entityManipulator;
    private readonly MySqlTemporaryTableBuilder temporaryTableBuilder;

    private static readonly Dictionary<Type, String> typeToMySqlDataType = new()
    {
        { typeof(Boolean), "TINYINT(1)" },
        { typeof(Byte), "TINYINT UNSIGNED" },
        { typeof(Byte[]), "BLOB" },
        { typeof(Char), "CHAR(1)" },
        { typeof(DateOnly), "DATE" },
        { typeof(DateTime), "DATETIME" },
        { typeof(Decimal), "DECIMAL(65,30)" },
        { typeof(Double), "DOUBLE" },
        { typeof(Guid), "CHAR(36)" },
        { typeof(Int16), "SMALLINT" },
        { typeof(Int32), "INT" },
        { typeof(Int64), "BIGINT" },
        { typeof(Single), "FLOAT" },
        { typeof(String), "TEXT" },
        { typeof(TimeOnly), "TIME" },
        { typeof(TimeSpan), "TIME" }
    };
}
