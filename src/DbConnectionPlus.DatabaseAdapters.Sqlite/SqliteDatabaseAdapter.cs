// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using RentADeveloper.DbConnectionPlus.Converters;

namespace RentADeveloper.DbConnectionPlus.DatabaseAdapters.Sqlite;

/// <summary>
/// The database adapter for SQLite databases.
/// </summary>
public class SqliteDatabaseAdapter : IDatabaseAdapter
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
                EnumSerializationMode.Strings => "TEXT",

                EnumSerializationMode.Integers => "INTEGER",

                _ => ThrowHelper.ThrowInvalidEnumSerializationModeException<string>(enumSerializationMode),
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
    public string QuoteIdentifier(string identifier) => "\"" + identifier + "\"";

    /// <inheritdoc />
    public string QuoteTemporaryTableName(string tableName, DbConnection connection) => "temp.\"" + tableName + "\"";

    /// <inheritdoc />
    public bool SupportsTemporaryTables(DbConnection connection) => true;

    /// <inheritdoc />
    public bool WasSqlStatementCancelledByCancellationToken(Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(exception);

        // SQLite does not support proper statement cancellation.
        return false;
    }

    private readonly SqliteEntityManipulator entityManipulator;
    private readonly SqliteTemporaryTableBuilder temporaryTableBuilder;

    private static readonly Dictionary<Type, string> typeToSqliteDataType = new()
    {
        { typeof(bool), "INTEGER" },
        { typeof(byte), "INTEGER" },
        { typeof(byte[]), "BLOB" },
        { typeof(char), "TEXT" },
        { typeof(DateOnly), "TEXT" },
        { typeof(DateTime), "TEXT" },
        { typeof(DateTimeOffset), "TEXT" },
        { typeof(decimal), "TEXT" },
        { typeof(double), "REAL" },
        { typeof(Guid), "TEXT" },
        { typeof(short), "INTEGER" },
        { typeof(int), "INTEGER" },
        { typeof(long), "INTEGER" },
        { typeof(float), "REAL" },
        { typeof(string), "TEXT" },
        { typeof(TimeOnly), "TEXT" },
        { typeof(TimeSpan), "TEXT" },
    };
}
