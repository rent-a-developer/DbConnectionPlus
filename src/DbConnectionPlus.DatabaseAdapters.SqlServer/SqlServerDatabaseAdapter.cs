// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using RentADeveloper.DbConnectionPlus.Converters;

namespace RentADeveloper.DbConnectionPlus.DatabaseAdapters.SqlServer;

/// <summary>
/// The database adapter for SQL Server databases.
/// </summary>
public class SqlServerDatabaseAdapter : IDatabaseAdapter
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
    public ITemporaryTableBuilder TemporaryTableBuilder => this.temporaryTableBuilder;

    /// <inheritdoc />
    public void BindParameterValue(DbParameter parameter, object? value)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        switch (value)
        {
            case DateTime:
                parameter.DbType = DbType.DateTime2;
                parameter.Value = value;
                break;

            case Enum enumValue:
                parameter.DbType = DbConnectionPlusConfiguration.Instance.EnumSerializationMode switch
                {
                    EnumSerializationMode.Integers => DbType.Int32,

                    EnumSerializationMode.Strings => DbType.String,

                    _ => ThrowHelper.ThrowInvalidEnumSerializationModeException<DbType>(
                        DbConnectionPlusConfiguration.Instance.EnumSerializationMode
                    ),
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

            default:
                parameter.Value = value ?? DBNull.Value;
                break;
        }
    }

    /// <inheritdoc />
    public string FormatParameterName(string parameterName) => "@" + parameterName;

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
                EnumSerializationMode.Strings => "nvarchar(200)", // 200 should be enough for most enum names

                EnumSerializationMode.Integers => "int",

                _ => ThrowHelper.ThrowInvalidEnumSerializationModeException<string>(enumSerializationMode),
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
    public string QuoteIdentifier(string identifier) => "[" + identifier + "]";

    /// <inheritdoc />
    public string QuoteTemporaryTableName(string tableName, DbConnection connection) => "[#" + tableName + "]";

    /// <inheritdoc />
    public bool SupportsTemporaryTables(DbConnection connection) => true;

    /// <inheritdoc />
    public bool WasSqlStatementCancelledByCancellationToken(Exception exception, CancellationToken cancellationToken)
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

    private static readonly Dictionary<Type, string> typeToSqlDataType = new()
    {
        { typeof(bool), "bit" },
        { typeof(byte), "tinyint" },
        { typeof(byte[]), "varbinary(max)" },
        { typeof(char), "char(1)" },
        { typeof(DateOnly), "date" },
        { typeof(DateTime), "datetime2" },
        { typeof(DateTimeOffset), "datetimeoffset" },
        { typeof(decimal), "decimal(28,10)" },
        { typeof(double), "float" },
        { typeof(Guid), "uniqueidentifier" },
        { typeof(short), "smallint" },
        { typeof(int), "int" },
        { typeof(long), "bigint" },
        { typeof(object), "sql_variant" },
        { typeof(float), "real" },
        { typeof(string), "nvarchar(max)" },
        { typeof(TimeOnly), "time" },
        { typeof(TimeSpan), "time" },
    };
}
