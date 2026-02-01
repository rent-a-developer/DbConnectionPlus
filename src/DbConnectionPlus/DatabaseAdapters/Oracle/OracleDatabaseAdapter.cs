// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using Oracle.ManagedDataAccess.Client;
using RentADeveloper.DbConnectionPlus.Converters;

namespace RentADeveloper.DbConnectionPlus.DatabaseAdapters.Oracle;

/// <summary>
/// The database adapter for Oracle databases.
/// </summary>
internal class OracleDatabaseAdapter : IDatabaseAdapter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OracleDatabaseAdapter" /> class.
    /// </summary>
    public OracleDatabaseAdapter()
    {
        this.entityManipulator = new(this);
        this.temporaryTableBuilder = new(this);
    }

    /// <inheritdoc />
    public IEntityManipulator EntityManipulator => this.entityManipulator;

    /// <inheritdoc />
    public ITemporaryTableBuilder TemporaryTableBuilder
    {
        get
        {
            if (!AllowTemporaryTables)
            {
                ThrowTemporaryTablesFeatureIsDisabledException();
            }

            return this.temporaryTableBuilder;
        }
    }

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

            case Guid guid:
                parameter.DbType = DbType.Binary;
                parameter.Value = guid;
                break;

            case DateTime:
                parameter.DbType = DbType.DateTime;
                parameter.Value = value;
                break;

            case Byte[]:
                parameter.DbType = DbType.Binary;
                parameter.Value = value;
                break;

            case DateOnly dateOnly:
                parameter.DbType = DbType.Date;
                parameter.Value = dateOnly.ToDateTime(TimeOnly.MinValue);
                break;

            case TimeOnly timeOnly:
                parameter.DbType = DbType.Time;
                (parameter as OracleParameter)?.OracleDbType = OracleDbType.IntervalDS;
                parameter.Value = timeOnly.ToTimeSpan();
                break;

            default:
                parameter.Value = value ?? DBNull.Value;
                break;
        }
    }

    /// <inheritdoc />
    public String FormatParameterName(String parameterName) =>
        ":\"" + parameterName + "\"";

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
                    "NVARCHAR2(200)", // 200 should be enough for most enum names

                EnumSerializationMode.Integers =>
                    "NUMBER(10)",

                _ =>
                    ThrowHelper.ThrowInvalidEnumSerializationModeException<String>(enumSerializationMode)
            };
        }

        if (!typeToOracleDataType.TryGetValue(effectiveType, out var result))
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                $"Could not map the type {type} to an Oracle data type."
            );
        }

        return result;
    }

    /// <summary>
    /// Gets the corresponding database specific <see cref="DbType" /> value for the type <paramref name="type" />.
    /// </summary>
    /// <param name="type">The type to get the database specific <see cref="DbType" /> value for.</param>
    /// <param name="enumSerializationMode">The mode to use to serialize <see cref="Enum" /> values.</param>
    /// <returns>
    /// The corresponding database specific <see cref="DbType" /> value for the type <paramref name="type" />.
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
    ///                 The type <paramref name="type" /> could not be mapped to a database specific
    /// <see cref="DbType" /> value.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
#pragma warning disable CA1822
    public DbType GetDbType(Type type, EnumSerializationMode enumSerializationMode)
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
                    DbType.String,

                EnumSerializationMode.Integers =>
                    DbType.Int32,

                _ =>
                    ThrowHelper.ThrowInvalidEnumSerializationModeException<DbType>(enumSerializationMode)
            };
        }

        if (!typeToDbType.TryGetValue(effectiveType, out var result))
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                $"Could not map the type {type} to a {typeof(DbType)} value."
            );
        }

        return result;
    }

    /// <inheritdoc />
    public String QuoteIdentifier(String identifier) =>
        "\"" + identifier + "\"";

    /// <inheritdoc />
    public String QuoteTemporaryTableName(String tableName, DbConnection connection)
    {
        var prefix = connection.ExecuteScalar<String>(
            "SELECT VALUE FROM v$parameter WHERE NAME = 'private_temp_table_prefix'"
        );

        return "\"" + prefix + tableName + "\"";
    }

    /// <inheritdoc />
    public Boolean SupportsTemporaryTables(DbConnection connection) =>
        this.supportsTemporaryTablesPerConnectionString.GetOrAdd(
            connection.ConnectionString,
            // Oracle 18c added support for private temporary tables.
            _ => connection.Exists("SELECT 1 FROM v$instance WHERE version >= '18'")
        );

    /// <inheritdoc />
    public Boolean WasSqlStatementCancelledByCancellationToken(
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is not OracleException oracleException)
        {
            return false;
        }

        if (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        foreach (OracleError error in oracleException.Errors)
        {
            if (error.Number == 1013)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// <para>
    /// Determines whether the temporary tables feature of DbConnectionPlus
    /// (<see cref="DbConnectionExtensions.TemporaryTable{T}" />) is allowed to be used with Oracle databases.
    /// Disabled by default.
    /// </para>
    /// <para>
    /// WARNING:
    /// Before enabling this feature, read the following note:
    /// When using the temporary tables feature of DbConnectionPlus with an Oracle database, please be aware of the
    /// following implications:
    /// The temporary tables feature of DbConnectionPlus creates private temporary tables and drops them after use.
    /// Unfortunately DDL statements (like creating and dropping a private temporary table) cause an implicit commit of
    /// the current transaction in an Oracle database.
    /// That means if you use the temporary tables feature inside an explicit transaction, the transaction will be
    /// committed when the temporary table is created and again when it is dropped!
    /// </para>
    /// <para>
    /// Therefore, when using DbConnectionPlus with Oracle databases, avoid using the temporary tables feature inside
    /// explicit transactions or at least be aware of the implications.
    /// You have been warned!
    /// </para>
    /// </summary>
    /// <remarks>
    /// If set to <see langword="false" />, attempting to use the temporary tables feature will throw an exception.
    /// </remarks>
    public static Boolean AllowTemporaryTables { get; set; }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException" /> indicating that the temporary tables feature of
    /// DbConnectionPlus is disabled for Oracle databases.
    /// </summary>
    /// <exception cref="InvalidOperationException">Always thrown.</exception>
    internal static void ThrowTemporaryTablesFeatureIsDisabledException() =>
        throw new InvalidOperationException(
            "The temporary tables feature of DbConnectionPlus is currently disabled for Oracle databases. " +
            $"To enable it set {typeof(OracleDatabaseAdapter)}.{nameof(AllowTemporaryTables)} " +
            "to true, but be sure to read the documentation first, because enabling this feature has implications " +
            "for transaction management."
        );

    private readonly OracleEntityManipulator entityManipulator;
    private readonly ConcurrentDictionary<String, Boolean> supportsTemporaryTablesPerConnectionString = [];
    private readonly OracleTemporaryTableBuilder temporaryTableBuilder;

    private static readonly Dictionary<Type, DbType> typeToDbType = new()
    {
        { typeof(Boolean), DbType.Boolean },
        { typeof(Byte), DbType.Byte },
        { typeof(Byte[]), DbType.Binary },
        { typeof(Char), DbType.StringFixedLength },
        { typeof(DateOnly), DbType.Date },
        { typeof(DateTime), DbType.DateTime },
        { typeof(DateTimeOffset), DbType.DateTimeOffset },
        { typeof(Decimal), DbType.Decimal },
        { typeof(Double), DbType.Double },
        { typeof(Guid), DbType.Guid },
        { typeof(Int16), DbType.Int16 },
        { typeof(Int32), DbType.Int32 },
        { typeof(Int64), DbType.Int64 },
        { typeof(Single), DbType.Single },
        { typeof(String), DbType.String },
        { typeof(TimeOnly), DbType.Time },
        { typeof(TimeSpan), DbType.Time }
    };

    private static readonly Dictionary<Type, String> typeToOracleDataType = new()
    {
        { typeof(Boolean), "NUMBER(1)" },
        { typeof(Byte), "NUMBER(3)" },
        { typeof(Byte[]), "RAW(2000)" },
        { typeof(Char), "CHAR(1)" },
        { typeof(DateOnly), "DATE" },
        { typeof(DateTime), "TIMESTAMP" },
        { typeof(DateTimeOffset), "TIMESTAMP WITH TIME ZONE" },
        { typeof(Decimal), "NUMBER(28,10)" },
        { typeof(Double), "BINARY_DOUBLE" },
        { typeof(Guid), "RAW(16)" },
        { typeof(Int16), "NUMBER(5)" },
        { typeof(Int32), "NUMBER(10)" },
        { typeof(Int64), "NUMBER(19)" },
        { typeof(Single), "BINARY_FLOAT" },
        { typeof(String), "NVARCHAR2(2000)" },
        { typeof(TimeOnly), "INTERVAL DAY TO SECOND" },
        { typeof(TimeSpan), "INTERVAL DAY TO SECOND" }
    };
}
