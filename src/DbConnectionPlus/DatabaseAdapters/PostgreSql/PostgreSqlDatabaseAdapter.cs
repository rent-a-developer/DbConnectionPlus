// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using NpgsqlTypes;
using RentADeveloper.DbConnectionPlus.Converters;

namespace RentADeveloper.DbConnectionPlus.DatabaseAdapters.PostgreSql;

/// <summary>
/// The database adapter for PostgreSQL databases.
/// </summary>
internal class PostgreSqlDatabaseAdapter : IDatabaseAdapter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlDatabaseAdapter" /> class.
    /// </summary>
    public PostgreSqlDatabaseAdapter()
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
                parameter.DbType = DbType.DateTime2;
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
                    "character varying(200)", // 200 should be enough for most enum names

                EnumSerializationMode.Integers =>
                    "integer",

                _ =>
                    ThrowHelper.ThrowInvalidEnumSerializationModeException<String>(enumSerializationMode)
            };
        }

        if (!typeToPostgreSqlDataType.TryGetValue(effectiveType, out var result))
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                $"Could not map the type {type} to a PostgreSQL data type."
            );
        }

        return result;
    }

    /// <summary>
    /// Gets the corresponding <see cref="NpgsqlDbType" /> value for the specified type.
    /// </summary>
    /// <param name="type">The type to get the <see cref="NpgsqlDbType" /> value for.</param>
    /// <param name="enumSerializationMode">The mode to use to serialize <see cref="Enum" /> values.</param>
    /// <returns>
    /// The corresponding <see cref="NpgsqlDbType" /> value for the specified type.
    /// If <paramref name="type" /> is an <see cref="Enum" /> or a nullable <see cref="Enum" />, the returned
    /// <see cref="NpgsqlDbType" /> will be either <see cref="NpgsqlDbType.Varchar" /> or
    /// <see cref="NpgsqlDbType.Integer" />, depending on the value of <paramref name="enumSerializationMode" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="type" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="enumSerializationMode" /> is not a valid <see cref="EnumSerializationMode" />
    /// value.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 The type <paramref name="type" /> could not be mapped to a <see cref="NpgsqlDbType" /> value.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
#pragma warning disable CA1822
    public NpgsqlDbType GetDbType(Type type, EnumSerializationMode enumSerializationMode)
#pragma warning restore CA1822
    {
        ArgumentNullException.ThrowIfNull(type);

        // Unwrap Nullable<T> types:
        var effectiveType = Nullable.GetUnderlyingType(type) ?? type;

        if (effectiveType.IsEnum)
        {
            return enumSerializationMode switch
            {
                EnumSerializationMode.Strings =>
                    NpgsqlDbType.Varchar,

                EnumSerializationMode.Integers =>
                    NpgsqlDbType.Integer,

                _ =>
                    ThrowHelper.ThrowInvalidEnumSerializationModeException<NpgsqlDbType>(enumSerializationMode)
            };
        }

        if (!typeToNpgsqlDbType.TryGetValue(effectiveType, out var result))
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                $"Could not map the type {type} to a {typeof(NpgsqlDbType)} value."
            );
        }

        return result;
    }

    /// <inheritdoc />
    public String QuoteIdentifier(String identifier) =>
        "\"" + identifier + "\"";

    /// <inheritdoc />
    public String QuoteTemporaryTableName(String tableName, DbConnection connection) =>
        "\"" + tableName + "\"";

    /// <inheritdoc />
    public Boolean SupportsTemporaryTables(DbConnection connection) =>
        true;

    /// <inheritdoc />
    public Boolean WasSqlStatementCancelledByCancellationToken(
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(exception);

        return cancellationToken.IsCancellationRequested && exception is OperationCanceledException;
    }

    private readonly PostgreSqlEntityManipulator entityManipulator;
    private readonly PostgreSqlTemporaryTableBuilder temporaryTableBuilder;

    private static readonly Dictionary<Type, NpgsqlDbType> typeToNpgsqlDbType = new()
    {
        { typeof(Boolean), NpgsqlDbType.Boolean },
        { typeof(Byte), NpgsqlDbType.Smallint },
        { typeof(Byte[]), NpgsqlDbType.Bytea },
        { typeof(Char), NpgsqlDbType.Char },
        { typeof(DateOnly), NpgsqlDbType.Date },
        { typeof(DateTime), NpgsqlDbType.Timestamp },
        { typeof(Decimal), NpgsqlDbType.Numeric },
        { typeof(Double), NpgsqlDbType.Double },
        { typeof(Guid), NpgsqlDbType.Uuid },
        { typeof(Int16), NpgsqlDbType.Smallint },
        { typeof(Int32), NpgsqlDbType.Integer },
        { typeof(Int64), NpgsqlDbType.Bigint },
        { typeof(Single), NpgsqlDbType.Real },
        { typeof(String), NpgsqlDbType.Text },
        { typeof(TimeOnly), NpgsqlDbType.Time },
        { typeof(TimeSpan), NpgsqlDbType.Interval }
    };

    private static readonly Dictionary<Type, String> typeToPostgreSqlDataType = new()
    {
        { typeof(Boolean), "boolean" },
        { typeof(Byte), "smallint" },
        { typeof(Byte[]), "bytea" },
        { typeof(Char), "char(1)" },
        { typeof(DateOnly), "date" },
        { typeof(DateTime), "timestamp without time zone" },
        { typeof(Decimal), "decimal" },
        { typeof(Double), "double precision" },
        { typeof(Guid), "uuid" },
        { typeof(Int16), "smallint" },
        { typeof(Int32), "integer" },
        { typeof(Int64), "bigint" },
        { typeof(Single), "real" },
        { typeof(String), "text" },
        { typeof(TimeOnly), "time" },
        { typeof(TimeSpan), "interval" }
    };
}
