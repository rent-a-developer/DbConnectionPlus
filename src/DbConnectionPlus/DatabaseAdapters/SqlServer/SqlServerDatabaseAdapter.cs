// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using RentADeveloper.DbConnectionPlus.Converters;

namespace RentADeveloper.DbConnectionPlus.DatabaseAdapters.SqlServer;

/// <summary>
/// The database adapter for SQL Server databases.
/// </summary>
internal class SqlServerDatabaseAdapter : IDatabaseAdapter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerDatabaseAdapter" /> class.
    /// </summary>
    public SqlServerDatabaseAdapter()
    {
        this.temporaryTableBuilder = new(this);
        this.entityManipulator = new(this);
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
                parameter.DbType = DbConnectionExtensions.EnumSerializationMode switch
                {
                    EnumSerializationMode.Integers =>
                        DbType.Int32,

                    EnumSerializationMode.Strings =>
                        DbType.String,

                    _ =>
                        ThrowHelper.ThrowInvalidEnumSerializationModeException<DbType>(
                            DbConnectionExtensions.EnumSerializationMode
                        )
                };

                parameter.Value = EnumSerializer.SerializeEnum(enumValue, DbConnectionExtensions.EnumSerializationMode);
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
                    "nvarchar(200)", // 200 should be enough for most enum names

                EnumSerializationMode.Integers =>
                    "int",

                _ =>
                    ThrowHelper.ThrowInvalidEnumSerializationModeException<String>(enumSerializationMode)
            };
        }

        if (!typeToSqlDataType.TryGetValue(effectiveType, out var result))
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                $"Could not map the type {type} to an SQL Server data type."
            );
        }

        return result;
    }

    /// <inheritdoc />
    public String QuoteIdentifier(String identifier) =>
        "[" + identifier + "]";

    /// <inheritdoc />
    public String QuoteTemporaryTableName(String tableName, DbConnection connection) =>
        "[#" + tableName + "]";

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

        if (exception is not SqlException sqlException)
        {
            return false;
        }

        // Unfortunately SQL Server does not raise a specific error when a statement is being cancelled by the user.
        // However, if a cancellation was requested via the specified cancellation token and
        // SQL Server raised an error with class 11, number 0 and state 0, then we can be pretty sure the error was
        // raised because of the cancellation.

        if (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        foreach (SqlError error in sqlException.Errors)
        {
            if (error is { Class: 11, Number: 0, State: 0 })
            {
                return true;
            }
        }

        return false;
    }

    private readonly SqlServerEntityManipulator entityManipulator;
    private readonly SqlServerTemporaryTableBuilder temporaryTableBuilder;

    private static readonly Dictionary<Type, String> typeToSqlDataType = new()
    {
        { typeof(Boolean), "bit" },
        { typeof(Byte), "tinyint" },
        { typeof(Byte[]), "varbinary(max)" },
        { typeof(Char), "char(1)" },
        { typeof(DateOnly), "date" },
        { typeof(DateTime), "datetime2" },
        { typeof(DateTimeOffset), "datetimeoffset" },
        { typeof(Decimal), "decimal(28,10)" },
        { typeof(Double), "float" },
        { typeof(Guid), "uniqueidentifier" },
        { typeof(Int16), "smallint" },
        { typeof(Int32), "int" },
        { typeof(Int64), "bigint" },
        { typeof(Object), "sql_variant" },
        { typeof(Single), "real" },
        { typeof(String), "nvarchar(max)" },
        { typeof(TimeOnly), "time" },
        { typeof(TimeSpan), "time" }
    };
}
