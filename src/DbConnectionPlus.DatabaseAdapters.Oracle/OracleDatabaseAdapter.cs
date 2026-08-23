// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using Oracle.ManagedDataAccess.Client;
using RentADeveloper.DbConnectionPlus.Converters;

namespace RentADeveloper.DbConnectionPlus.DatabaseAdapters.Oracle;

/// <summary>
/// The database adapter for Oracle databases.
/// </summary>
public class OracleDatabaseAdapter : IDatabaseAdapter
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
    public void BindParameterValue(DbParameter parameter, object? value)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        switch (value)
        {
            case DateTime:
                parameter.DbType = DbType.DateTime;
                parameter.Value = value;
                break;

            case Guid guid:
                parameter.DbType = DbType.Binary;
                parameter.Value = guid;
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

            case byte[]:
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
    public string FormatParameterName(string parameterName) =>
        ":\"" + parameterName + "\"";

    /// <inheritdoc />
    public string GetDataType(Type type, EnumSerializationMode enumSerializationMode)
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
                    ThrowHelper.ThrowInvalidEnumSerializationModeException<string>(enumSerializationMode)
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
    public string QuoteIdentifier(string identifier) =>
        "\"" + identifier + "\"";

    /// <inheritdoc />
    public string QuoteTemporaryTableName(string tableName, DbConnection connection)
    {
        var prefix = connection.ExecuteScalar<string>(
            "SELECT VALUE FROM v$parameter WHERE NAME = 'private_temp_table_prefix'"
        );

        return "\"" + prefix + tableName + "\"";
    }

    /// <inheritdoc />
    public bool SupportsTemporaryTables(DbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        return this.supportsTemporaryTablesPerConnectionString.GetOrAdd(
            connection.ConnectionString,
            // Oracle 18c added support for private temporary tables.
            _ => connection.Exists("SELECT 1 FROM v$instance WHERE version >= '18'")
        );
    }

    /// <inheritdoc />
    public bool WasSqlStatementCancelledByCancellationToken(
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
    public static bool AllowTemporaryTables { get; set; }

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
    private readonly ConcurrentDictionary<string, bool> supportsTemporaryTablesPerConnectionString = [];
    private readonly OracleTemporaryTableBuilder temporaryTableBuilder;

    private static readonly Dictionary<Type, DbType> typeToDbType = new()
    {
        { typeof(bool), DbType.Boolean },
        { typeof(byte), DbType.Byte },
        { typeof(byte[]), DbType.Binary },
        { typeof(char), DbType.StringFixedLength },
        { typeof(DateOnly), DbType.Date },
        { typeof(DateTime), DbType.DateTime },
        { typeof(DateTimeOffset), DbType.DateTimeOffset },
        { typeof(decimal), DbType.Decimal },
        { typeof(double), DbType.Double },
        { typeof(Guid), DbType.Guid },
        { typeof(short), DbType.Int16 },
        { typeof(int), DbType.Int32 },
        { typeof(long), DbType.Int64 },
        { typeof(float), DbType.Single },
        { typeof(string), DbType.String },
        { typeof(TimeOnly), DbType.Time },
        { typeof(TimeSpan), DbType.Time }
    };

    private static readonly Dictionary<Type, string> typeToOracleDataType = new()
    {
        { typeof(bool), "NUMBER(1)" },
        { typeof(byte), "NUMBER(3)" },
        { typeof(byte[]), "RAW(2000)" },
        { typeof(char), "CHAR(1)" },
        { typeof(DateOnly), "DATE" },
        { typeof(DateTime), "TIMESTAMP" },
        { typeof(DateTimeOffset), "TIMESTAMP WITH TIME ZONE" },
        { typeof(decimal), "NUMBER(28,10)" },
        { typeof(double), "BINARY_DOUBLE" },
        { typeof(Guid), "RAW(16)" },
        { typeof(short), "NUMBER(5)" },
        { typeof(int), "NUMBER(10)" },
        { typeof(long), "NUMBER(19)" },
        { typeof(float), "BINARY_FLOAT" },
        { typeof(string), "NVARCHAR2(2000)" },
        { typeof(TimeOnly), "INTERVAL DAY TO SECOND" },
        { typeof(TimeSpan), "INTERVAL DAY TO SECOND" }
    };
}
