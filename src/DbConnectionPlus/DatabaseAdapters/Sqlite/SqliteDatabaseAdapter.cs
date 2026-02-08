// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using RentADeveloper.DbConnectionPlus.Converters;

namespace RentADeveloper.DbConnectionPlus.DatabaseAdapters.Sqlite;

/// <summary>
/// The database adapter for SQLite databases.
/// </summary>
internal class SqliteDatabaseAdapter : IDatabaseAdapter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteDatabaseAdapter" /> class.
    /// </summary>
    public SqliteDatabaseAdapter()
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
            case DateTime:
                parameter.DbType = DbType.DateTime;
                parameter.Value = value;
                break;

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
                    "TEXT",

                EnumSerializationMode.Integers =>
                    "INTEGER",

                _ =>
                    ThrowHelper.ThrowInvalidEnumSerializationModeException<String>(enumSerializationMode)
            };
        }

        if (!typeToSqliteDataType.TryGetValue(effectiveType, out var result))
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                $"Could not map the type {type} to an SQLite data type."
            );
        }

        return result;
    }

    /// <inheritdoc />
    public String QuoteIdentifier(String identifier) =>
        "\"" + identifier + "\"";

    /// <inheritdoc />
    public String QuoteTemporaryTableName(String tableName, DbConnection connection) =>
        "temp.\"" + tableName + "\"";

    /// <inheritdoc />
    public Boolean SupportsTemporaryTables(DbConnection connection) =>
        true;

    /// <inheritdoc />
    public Boolean WasSqlStatementCancelledByCancellationToken(Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(exception);

        // SQLite does not support proper statement cancellation.
        return false;
    }

    private readonly SqliteEntityManipulator entityManipulator;
    private readonly SqliteTemporaryTableBuilder temporaryTableBuilder;

    private static readonly Dictionary<Type, String> typeToSqliteDataType = new()
    {
        { typeof(Boolean), "INTEGER" },
        { typeof(Byte), "INTEGER" },
        { typeof(Byte[]), "BLOB" },
        { typeof(Char), "TEXT" },
        { typeof(DateOnly), "TEXT" },
        { typeof(DateTime), "TEXT" },
        { typeof(DateTimeOffset), "TEXT" },
        { typeof(Decimal), "TEXT" },
        { typeof(Double), "REAL" },
        { typeof(Guid), "TEXT" },
        { typeof(Int16), "INTEGER" },
        { typeof(Int32), "INTEGER" },
        { typeof(Int64), "INTEGER" },
        { typeof(Single), "REAL" },
        { typeof(String), "TEXT" },
        { typeof(TimeOnly), "TEXT" },
        { typeof(TimeSpan), "TEXT" }
    };
}
