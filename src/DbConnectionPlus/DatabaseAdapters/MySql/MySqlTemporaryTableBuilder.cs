// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using LinkDotNet.StringBuilder;
using Microsoft.Data;
using MySqlConnector;
using RentADeveloper.DbConnectionPlus.Converters;
using RentADeveloper.DbConnectionPlus.DbCommands;
using RentADeveloper.DbConnectionPlus.Entities;
using RentADeveloper.DbConnectionPlus.Extensions;
using RentADeveloper.DbConnectionPlus.Readers;

namespace RentADeveloper.DbConnectionPlus.DatabaseAdapters.MySql;

/// <summary>
/// The temporary table builder for MySQL.
/// </summary>
internal class MySqlTemporaryTableBuilder : ITemporaryTableBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlTemporaryTableBuilder" /> class.
    /// </summary>
    /// <param name="databaseAdapter">The database adapter to use to build temporary tables.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="databaseAdapter" /> is <see langword="null" />.
    /// </exception>
    public MySqlTemporaryTableBuilder(MySqlDatabaseAdapter databaseAdapter)
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

        if (connection is not MySqlConnection mySqlConnection)
        {
            return ThrowHelper.ThrowWrongConnectionTypeException<MySqlConnection, TemporaryTableDisposer>();
        }

        var mySqlTransaction = transaction as MySqlTransaction;

        if (transaction is not null && mySqlTransaction is null)
        {
            return ThrowHelper.ThrowWrongTransactionTypeException<MySqlTransaction, TemporaryTableDisposer>();
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

        var mySqlBulkCopy = new MySqlBulkCopy(mySqlConnection, mySqlTransaction)
        {
            BulkCopyTimeout = 0,
            DestinationTableName = $"`{name}`"
        };

        mySqlBulkCopy.ColumnMappings.Clear();

        for (var fieldOrdinal = 0; fieldOrdinal < reader.FieldCount; fieldOrdinal++)
        {
            var fieldName = reader.GetName(fieldOrdinal);
            mySqlBulkCopy.ColumnMappings.Add(new(fieldOrdinal, fieldName));
        }

        mySqlBulkCopy.WriteToServer(reader);

        return new(
            () => DropTemporaryTable(name, mySqlConnection, mySqlTransaction),
            () => DropTemporaryTableAsync(name, mySqlConnection, mySqlTransaction)
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

        if (connection is not MySqlConnection mySqlConnection)
        {
            return ThrowHelper.ThrowWrongConnectionTypeException<MySqlConnection, TemporaryTableDisposer>();
        }

        var mySqlTransaction = transaction as MySqlTransaction;

        if (transaction is not null && mySqlTransaction is null)
        {
            return ThrowHelper.ThrowWrongTransactionTypeException<MySqlTransaction, TemporaryTableDisposer>();
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

        var mySqlBulkCopy = new MySqlBulkCopy(mySqlConnection, mySqlTransaction)
        {
            BulkCopyTimeout = 0,
            DestinationTableName = $"`{name}`"
        };

        mySqlBulkCopy.ColumnMappings.Clear();

        for (var fieldOrdinal = 0; fieldOrdinal < reader.FieldCount; fieldOrdinal++)
        {
            var fieldName = reader.GetName(fieldOrdinal);
            mySqlBulkCopy.ColumnMappings.Add(new(fieldOrdinal, fieldName));
        }

        try
        {
            await mySqlBulkCopy.WriteToServerAsync(reader, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationAbortedException)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        return new(
            () => DropTemporaryTable(name, mySqlConnection, mySqlTransaction),
            () => DropTemporaryTableAsync(name, mySqlConnection, mySqlTransaction)
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

        sqlBuilder.Append("CREATE TEMPORARY TABLE `");
        sqlBuilder.Append(tableName);
        sqlBuilder.AppendLine("`");

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

            sqlBuilder.Append("`");
            sqlBuilder.Append(property.PropertyName);
            sqlBuilder.Append("` ");

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

        sqlBuilder.Append("CREATE TEMPORARY TABLE `");
        sqlBuilder.Append(tableName);
        sqlBuilder.AppendLine("`");

        sqlBuilder.Append(Constants.Indent);
        sqlBuilder.Append("(`Value` ");
        sqlBuilder.Append(this.databaseAdapter.GetDataType(valuesType, enumSerializationMode));
        sqlBuilder.AppendLine(")");

        return sqlBuilder.ToString();
    }

    /// <summary>
    /// Creates a <see cref="DbDataReader" /> that reads data from the specified sequence of values.
    /// </summary>
    /// <param name="values">The sequence containing the values to be read.</param>
    /// <param name="valuesType">The type of values in <paramref name="values" />.</param>
    /// <returns>
    /// A <see cref="DbDataReader" /> that provides access to the data in <paramref name="values" />.
    /// </returns>
    private static DbDataReader CreateValuesDataReader(IEnumerable values, Type valuesType)
    {
        if (valuesType.IsBuiltInTypeOrNullableBuiltInType() || valuesType.IsEnumOrNullableEnumType())
        {
            if (valuesType.IsEnumOrNullableEnumType())
            {
                var enumValues = new List<Object>();

                foreach (var value in values)
                {
                    if (value is Enum enumValue)
                    {
                        enumValues.Add(
                            EnumSerializer.SerializeEnum(enumValue, DbConnectionExtensions.EnumSerializationMode)
                        );
                    }
                    else
                    {
                        enumValues.Add(value);
                    }
                }

                var newValuesType = DbConnectionExtensions.EnumSerializationMode switch
                {
                    EnumSerializationMode.Integers =>
                        typeof(Int32?),

                    EnumSerializationMode.Strings =>
                        typeof(String),

                    _ =>
                        ThrowHelper.ThrowInvalidEnumSerializationModeException<Type>(
                            DbConnectionExtensions.EnumSerializationMode
                        )
                };

                return new EnumerableReader(enumValues, newValuesType, "Value");
            }

            return new EnumerableReader(values, valuesType, "Value");
        }

        return new EnumHandlingObjectReader(
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
    private static void DropTemporaryTable(String name, MySqlConnection connection, MySqlTransaction? transaction)
    {
        using var command = DbConnectionExtensions.DbCommandFactory.CreateDbCommand(
            connection,
            $"DROP TEMPORARY TABLE IF EXISTS `{name}`",
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
        MySqlConnection connection,
        MySqlTransaction? transaction
    )
    {
        using var command = DbConnectionExtensions.DbCommandFactory.CreateDbCommand(
            connection,
            $"DROP TEMPORARY TABLE IF EXISTS `{name}`",
            transaction
        );

        DbConnectionExtensions.OnBeforeExecutingCommand(command, []);

        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    private readonly MySqlDatabaseAdapter databaseAdapter;
}
