// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using RentADeveloper.DbConnectionPlus.Converters;

namespace RentADeveloper.DbConnectionPlus.DatabaseAdapters.MySql;

/// <summary>
/// The database adapter for MySQL databases.
/// </summary>
public class MySqlDatabaseAdapter : IDatabaseAdapter
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
    public ITemporaryTableBuilder TemporaryTableBuilder => this.temporaryTableBuilder;

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
                EnumSerializationMode.Strings => "VARCHAR(200)", // 200 should be enough for most enum names

                EnumSerializationMode.Integers => "INT",

                _ => ThrowHelper.ThrowInvalidEnumSerializationModeException<string>(enumSerializationMode),
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
    public string QuoteIdentifier(string identifier) => "`" + identifier + "`";

    /// <inheritdoc />
    public string QuoteTemporaryTableName(string tableName, DbConnection connection) => "`" + tableName + "`";

    /// <inheritdoc />
    public bool SupportsTemporaryTables(DbConnection connection) => true;

    /// <inheritdoc />
    public bool WasSqlStatementCancelledByCancellationToken(Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(exception);

        // MySqlConnector does not support proper statement cancellation.
        return false;
    }

    private readonly MySqlEntityManipulator entityManipulator;
    private readonly MySqlTemporaryTableBuilder temporaryTableBuilder;

    private static readonly Dictionary<Type, string> typeToMySqlDataType = new()
    {
        { typeof(bool), "TINYINT(1)" },
        { typeof(byte), "TINYINT UNSIGNED" },
        { typeof(byte[]), "BLOB" },
        { typeof(char), "CHAR(1)" },
        { typeof(DateOnly), "DATE" },
        { typeof(DateTime), "DATETIME" },
        { typeof(decimal), "DECIMAL(65,30)" },
        { typeof(double), "DOUBLE" },
        { typeof(Guid), "CHAR(36)" },
        { typeof(short), "SMALLINT" },
        { typeof(int), "INT" },
        { typeof(long), "BIGINT" },
        { typeof(float), "FLOAT" },
        { typeof(string), "TEXT" },
        { typeof(TimeOnly), "TIME" },
        { typeof(TimeSpan), "TIME" },
    };
}
