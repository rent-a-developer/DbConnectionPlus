// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using FastMember;
using LinkDotNet.StringBuilder;
using Microsoft.Data.Sqlite;
using RentADeveloper.DbConnectionPlus.Converters;
using RentADeveloper.DbConnectionPlus.DbCommands;
using RentADeveloper.DbConnectionPlus.Entities;
using RentADeveloper.DbConnectionPlus.Extensions;
using RentADeveloper.DbConnectionPlus.Readers;

namespace RentADeveloper.DbConnectionPlus.DatabaseAdapters.Sqlite;

/// <summary>
/// The temporary table builder for SQLite.
/// </summary>
internal class SqliteTemporaryTableBuilder : ITemporaryTableBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteTemporaryTableBuilder" /> class.
    /// </summary>
    /// <param name="databaseAdapter">The database adapter to use to build temporary tables.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="databaseAdapter" /> is <see langword="null" />.
    /// </exception>
    public SqliteTemporaryTableBuilder(SqliteDatabaseAdapter databaseAdapter)
    {
        ArgumentNullException.ThrowIfNull(databaseAdapter);

        this.databaseAdapter = databaseAdapter;
    }

    /// <inheritdoc />
    public TemporaryTableDisposer BuildTemporaryTable(
        DbConnection connection,
        DbTransaction? transaction,
        String name,
        IEnumerable values,
        Type valuesType,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(valuesType);

        if (connection is not SqliteConnection sqliteConnection)
        {
            throw new ArgumentOutOfRangeException(
                nameof(connection),
                $"The provided connection is not of the type {nameof(SqliteConnection)}."
            );
        }

        var sqliteTransaction = transaction as SqliteTransaction;

        if (transaction is not null && sqliteTransaction is null)
        {
            throw new ArgumentOutOfRangeException(
                nameof(transaction),
                $"The provided transaction is not of the type {nameof(SqliteTransaction)}."
            );
        }

        if (valuesType.IsBuiltInTypeOrNullableBuiltInType() || valuesType.IsEnumOrNullableEnumType())
        {
            using var createCommand = DbConnectionExtensions.DbCommandFactory.CreateDbCommand(
                connection,
                this.BuildCreateSingleColumnTemporaryTableSqlCode(
                    name,
                    valuesType,
                    DbConnectionExtensions.EnumSerializationMode
                ),
                transaction
            );

            using var cancellationTokenRegistration =
                DbCommandHelper.RegisterDbCommandCancellation(createCommand, cancellationToken);

            DbConnectionExtensions.OnBeforeExecutingCommand(createCommand, []);

            createCommand.ExecuteNonQuery();
        }
        else
        {
            using var createCommand = DbConnectionExtensions.DbCommandFactory.CreateDbCommand(
                connection,
                this.BuildCreateMultiColumnTemporaryTableSqlCode(
                    name,
                    valuesType,
                    DbConnectionExtensions.EnumSerializationMode
                ),
                transaction
            );

            using var cancellationTokenRegistration =
                DbCommandHelper.RegisterDbCommandCancellation(createCommand, cancellationToken);

            DbConnectionExtensions.OnBeforeExecutingCommand(createCommand, []);

            createCommand.ExecuteNonQuery();
        }

        using var reader = CreateValuesDataReader(values, valuesType);

        PopulateTemporaryTable(sqliteConnection, sqliteTransaction, name, reader, cancellationToken);

        return new(
            () => DropTemporaryTable(name, sqliteConnection, sqliteTransaction),
            () => DropTemporaryTableAsync(name, sqliteConnection, sqliteTransaction)
        );
    }

    /// <inheritdoc />
    public async Task<TemporaryTableDisposer> BuildTemporaryTableAsync(
        DbConnection connection,
        DbTransaction? transaction,
        String name,
        IEnumerable values,
        Type valuesType,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(valuesType);

        if (connection is not SqliteConnection sqliteConnection)
        {
            throw new ArgumentOutOfRangeException(
                nameof(connection),
                $"The provided connection is not of the type {nameof(SqliteConnection)}."
            );
        }

        var sqliteTransaction = transaction as SqliteTransaction;

        if (transaction is not null && sqliteTransaction is null)
        {
            throw new ArgumentOutOfRangeException(
                nameof(transaction),
                $"The provided transaction is not of the type {nameof(SqliteTransaction)}."
            );
        }

        if (valuesType.IsBuiltInTypeOrNullableBuiltInType() || valuesType.IsEnumOrNullableEnumType())
        {
            using var createCommand = DbConnectionExtensions.DbCommandFactory.CreateDbCommand(
                connection,
                this.BuildCreateSingleColumnTemporaryTableSqlCode(
                    name,
                    valuesType,
                    DbConnectionExtensions.EnumSerializationMode
                ),
                transaction
            );

            using var cancellationTokenRegistration =
                DbCommandHelper.RegisterDbCommandCancellation(createCommand, cancellationToken);

            DbConnectionExtensions.OnBeforeExecutingCommand(createCommand, []);

            await createCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            using var createCommand = DbConnectionExtensions.DbCommandFactory.CreateDbCommand(
                connection,
                this.BuildCreateMultiColumnTemporaryTableSqlCode(
                    name,
                    valuesType,
                    DbConnectionExtensions.EnumSerializationMode
                ),
                transaction
            );

            using var cancellationTokenRegistration =
                DbCommandHelper.RegisterDbCommandCancellation(createCommand, cancellationToken);

            DbConnectionExtensions.OnBeforeExecutingCommand(createCommand, []);

            await createCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        using var reader = CreateValuesDataReader(values, valuesType);

        await PopulateTemporaryTableAsync(sqliteConnection, sqliteTransaction, name, reader, cancellationToken)
            .ConfigureAwait(false);

        return new(
            () => DropTemporaryTable(name, sqliteConnection, sqliteTransaction),
            () => DropTemporaryTableAsync(name, sqliteConnection, sqliteTransaction)
        );
    }

    /// <summary>
    /// Builds an SQL code to create a multi-column temporary table to be populated with objects of the type
    /// <paramref name="objectsType" />.
    /// </summary>
    /// <param name="tableName">The name of the temporary table to create.</param>
    /// <param name="objectsType">The type of objects the temporary table will be populated with.</param>
    /// <param name="enumSerializationMode">The mode to use to serialize <see cref="Enum" /> values.</param>
    /// <returns>The built SQL code.</returns>
    private String BuildCreateMultiColumnTemporaryTableSqlCode(
        String tableName,
        Type objectsType,
        EnumSerializationMode enumSerializationMode
    )
    {
        using var sqlBuilder = new ValueStringBuilder(stackalloc Char[500]);

        sqlBuilder.Append("CREATE TEMP TABLE \"");
        sqlBuilder.Append(tableName);
        sqlBuilder.AppendLine("\"");

        sqlBuilder.Append(Constants.Indent);
        sqlBuilder.Append("(");

        var properties = EntityHelper.GetEntityTypeMetadata(objectsType).MappedProperties.Where(a => a.CanRead);

        var prependSeparator = false;

        foreach (var property in properties)
        {
            if (prependSeparator)
            {
                sqlBuilder.Append(", ");
            }

            sqlBuilder.Append('"');
            sqlBuilder.Append(property.PropertyName);
            sqlBuilder.Append("\" ");

            var propertyType = property.PropertyType;

            sqlBuilder.Append(this.databaseAdapter.GetDataType(propertyType, enumSerializationMode));

            prependSeparator = true;
        }

        sqlBuilder.AppendLine(")");

        return sqlBuilder.ToString();
    }

    /// <summary>
    /// Builds an SQL code to create a single-column temporary table to be populated with values of the type
    /// <paramref name="valuesType" />.
    /// </summary>
    /// <param name="tableName">The name of the temporary table to create.</param>
    /// <param name="valuesType">The type of values the temporary table will be populated with.</param>
    /// <param name="enumSerializationMode">The mode to use to serialize <see cref="Enum" /> values.</param>
    /// <returns>The built SQL code.</returns>
    private String BuildCreateSingleColumnTemporaryTableSqlCode(
        String tableName,
        Type valuesType,
        EnumSerializationMode enumSerializationMode
    )
    {
        using var sqlBuilder = new ValueStringBuilder(stackalloc Char[100]);

        sqlBuilder.Append("CREATE TEMP TABLE \"");
        sqlBuilder.Append(tableName);
        sqlBuilder.AppendLine("\"");

        sqlBuilder.Append(Constants.Indent);
        sqlBuilder.Append("(\"Value\" ");
        sqlBuilder.Append(this.databaseAdapter.GetDataType(valuesType, enumSerializationMode));
        sqlBuilder.AppendLine(")");

        return sqlBuilder.ToString();
    }

    /// <summary>
    /// Builds an SQL code to insert data from the specified data reader into the specified temporary table.
    /// </summary>
    /// <param name="tableName">The name of the temporary table to insert data into.</param>
    /// <param name="dataReader">The data reader to read data from.</param>
    /// <returns>A tuple containing the insert SQL code and the parameters to use.</returns>
    private static (String SqlCode, SqliteParameter[] Parameters) BuildInsertSqlCode(
        String tableName,
        DbDataReader dataReader
    )
    {
        using var sqlBuilder = new ValueStringBuilder(stackalloc Char[500]);

        sqlBuilder.Append("INSERT INTO temp.\"");
        sqlBuilder.Append(tableName);
        sqlBuilder.AppendLine("\"");

        sqlBuilder.Append(Constants.Indent);
        sqlBuilder.Append("(");

        var fieldCount = dataReader.FieldCount;
        var parameters = new SqliteParameter[fieldCount];

        for (var i = 0; i < fieldCount; i++)
        {
            if (i > 0)
            {
                sqlBuilder.Append(", ");
            }

            var fieldName = dataReader.GetName(i);

            sqlBuilder.Append('"');
            sqlBuilder.Append(fieldName);
            sqlBuilder.Append('"');

            var parameter = new SqliteParameter
            {
                ParameterName = "@" + fieldName
            };

            parameters[i] = parameter;
        }

        sqlBuilder.AppendLine(")");

        sqlBuilder.AppendLine("VALUES");

        sqlBuilder.Append(Constants.Indent);
        sqlBuilder.Append("(");

        for (var i = 0; i < fieldCount; i++)
        {
            if (i > 0)
            {
                sqlBuilder.Append(", ");
            }

            sqlBuilder.Append(parameters[i].ParameterName);
        }

        sqlBuilder.AppendLine(")");

        return (sqlBuilder.ToString(), parameters);
    }

    /// <summary>
    /// Creates a <see cref="DbDataReader" /> that reads data from the specified sequence of values.
    /// </summary>
    /// <param name="values">The sequence containing the values to be read.</param>
    /// <param name="valuesType">The type of values in <paramref name="values" />.</param>
    /// <returns>A <see cref="DbDataReader" /> that provides access to the data in <paramref name="values" />.</returns>
    private static DbDataReader CreateValuesDataReader(IEnumerable values, Type valuesType)
    {
        if (valuesType.IsBuiltInTypeOrNullableBuiltInType() || valuesType.IsEnumOrNullableEnumType())
        {
            return new EnumerableReader(values, valuesType, "Value");
        }

        return new ObjectReader(
            valuesType,
            values,
            EntityHelper.GetEntityTypeMetadata(valuesType).MappedProperties.Where(a => a.CanRead)
                .Select(a => a.PropertyName)
                .ToArray()
        );
    }

    /// <summary>
    /// Drops the temporary table with the specified name.
    /// </summary>
    /// <param name="name">The name of the temporary table to drop.</param>
    /// <param name="connection">The connection to use to drop the table.</param>
    /// <param name="transaction">The transaction within to drop the table.</param>
    private static void DropTemporaryTable(String name, SqliteConnection connection, SqliteTransaction? transaction)
    {
        using var command = DbConnectionExtensions.DbCommandFactory.CreateDbCommand(
            connection,
            $"DROP TABLE IF EXISTS temp.\"{name}\"",
            transaction
        );

        DbConnectionExtensions.OnBeforeExecutingCommand(command, []);

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Asynchronously drops the temporary table with the specified name.
    /// </summary>
    /// <param name="name">The name of the temporary table to drop.</param>
    /// <param name="connection">The connection to use to drop the table.</param>
    /// <param name="transaction">The transaction within to drop the table.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async ValueTask DropTemporaryTableAsync(
        String name,
        SqliteConnection connection,
        SqliteTransaction? transaction
    )
    {
        using var command = DbConnectionExtensions.DbCommandFactory.CreateDbCommand(
            connection,
            $"DROP TABLE IF EXISTS temp.\"{name}\"",
            transaction
        );

        DbConnectionExtensions.OnBeforeExecutingCommand(command, []);

        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Populates the specified temporary table with the data from the specified data reader.
    /// </summary>
    /// <param name="connection">The database connection to use to populate the temporary table.</param>
    /// <param name="transaction">The database transaction within to populate the temporary table.</param>
    /// <param name="tableName">The name of the temporary table to populate.</param>
    /// <param name="dataReader">The data reader to use to populate the temporary table.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    private static void PopulateTemporaryTable(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        String tableName,
        DbDataReader dataReader,
        CancellationToken cancellationToken
    )
    {
        var insertCommand = connection.CreateCommand();
        insertCommand.Transaction = transaction;

        var (insertSqlCode, parameters) = BuildInsertSqlCode(tableName, dataReader);

#pragma warning disable CA2100
        insertCommand.CommandText = insertSqlCode;
#pragma warning restore CA2100

        insertCommand.Parameters.AddRange(parameters);

        while (dataReader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (var i = 0; i < parameters.Length; i++)
            {
                var value = dataReader.GetValue(i);

                if (value is Enum enumValue)
                {
                    value = EnumSerializer.SerializeEnum(enumValue, DbConnectionExtensions.EnumSerializationMode);
                }

                parameters[i].Value = value;
            }

            DbConnectionExtensions.OnBeforeExecutingCommand(insertCommand, []);

            insertCommand.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Asynchronously populates the specified temporary table with the data from the specified data reader.
    /// </summary>
    /// <param name="connection">The database connection to use to populate the temporary table.</param>
    /// <param name="transaction">The database transaction within to populate the temporary table.</param>
    /// <param name="tableName">The name of the temporary table to populate.</param>
    /// <param name="dataReader">The data reader to use to populate the temporary table.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private static async Task PopulateTemporaryTableAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        String tableName,
        DbDataReader dataReader,
        CancellationToken cancellationToken
    )
    {
#pragma warning disable CA2007
        await using var insertCommand = connection.CreateCommand();
#pragma warning restore CA2007

        insertCommand.Transaction = transaction;

        var (insertSqlCode, parameters) = BuildInsertSqlCode(tableName, dataReader);

#pragma warning disable CA2100
        insertCommand.CommandText = insertSqlCode;
#pragma warning restore CA2100

        insertCommand.Parameters.AddRange(parameters);

        while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            for (var i = 0; i < parameters.Length; i++)
            {
                var value = dataReader.GetValue(i);

                if (value is Enum enumValue)
                {
                    value = EnumSerializer.SerializeEnum(enumValue, DbConnectionExtensions.EnumSerializationMode);
                }

                parameters[i].Value = value;
            }

            DbConnectionExtensions.OnBeforeExecutingCommand(insertCommand, []);

            await insertCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private readonly SqliteDatabaseAdapter databaseAdapter;
}
