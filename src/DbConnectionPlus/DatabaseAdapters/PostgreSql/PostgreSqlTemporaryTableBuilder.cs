// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using FastMember;
using LinkDotNet.StringBuilder;
using Npgsql;
using RentADeveloper.DbConnectionPlus.Converters;
using RentADeveloper.DbConnectionPlus.DbCommands;
using RentADeveloper.DbConnectionPlus.Entities;
using RentADeveloper.DbConnectionPlus.Extensions;
using RentADeveloper.DbConnectionPlus.Readers;

namespace RentADeveloper.DbConnectionPlus.DatabaseAdapters.PostgreSql;

/// <summary>
/// The temporary table builder for PostgreSQL.
/// </summary>
internal class PostgreSqlTemporaryTableBuilder : ITemporaryTableBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlTemporaryTableBuilder" /> class.
    /// </summary>
    /// <param name="databaseAdapter">The database adapter to use to build temporary tables.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="databaseAdapter" /> is <see langword="null" />.
    /// </exception>
    public PostgreSqlTemporaryTableBuilder(PostgreSqlDatabaseAdapter databaseAdapter)
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

        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            return ThrowHelper.ThrowWrongConnectionTypeException<NpgsqlConnection, TemporaryTableDisposer>();
        }

        var npgsqlTransaction = transaction as NpgsqlTransaction;

        if (transaction is not null && npgsqlTransaction is null)
        {
            return ThrowHelper.ThrowWrongTransactionTypeException<NpgsqlTransaction, TemporaryTableDisposer>();
        }

        if (valuesType.IsBuiltInTypeOrNullableBuiltInType() || valuesType.IsEnumOrNullableEnumType())
        {
            using var createCommand = DbConnectionExtensions.DbCommandFactory.CreateDbCommand(
                connection,
                this.BuildCreateSingleColumnTemporaryTableSqlCode(
                    name,
                    valuesType,
                    DbConnectionPlusConfiguration.Instance.EnumSerializationMode
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
                    DbConnectionPlusConfiguration.Instance.EnumSerializationMode
                ),
                transaction
            );

            using var cancellationTokenRegistration =
                DbCommandHelper.RegisterDbCommandCancellation(createCommand, cancellationToken);

            DbConnectionExtensions.OnBeforeExecutingCommand(createCommand, []);

            createCommand.ExecuteNonQuery();
        }

        using var reader = CreateValuesDataReader(values, valuesType);

        this.PopulateTemporaryTable(npgsqlConnection, name, reader, cancellationToken);

        return new(
            () => DropTemporaryTable(name, npgsqlConnection, npgsqlTransaction),
            () => DropTemporaryTableAsync(name, npgsqlConnection, npgsqlTransaction)
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

        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            return ThrowHelper.ThrowWrongConnectionTypeException<NpgsqlConnection, TemporaryTableDisposer>();
        }

        var npgsqlTransaction = transaction as NpgsqlTransaction;

        if (transaction is not null && npgsqlTransaction is null)
        {
            return ThrowHelper.ThrowWrongTransactionTypeException<NpgsqlTransaction, TemporaryTableDisposer>();
        }

        if (valuesType.IsBuiltInTypeOrNullableBuiltInType() || valuesType.IsEnumOrNullableEnumType())
        {
#pragma warning disable CA2007
            await using var createCommand = DbConnectionExtensions.DbCommandFactory.CreateDbCommand(
                connection,
                this.BuildCreateSingleColumnTemporaryTableSqlCode(
                    name,
                    valuesType,
                    DbConnectionPlusConfiguration.Instance.EnumSerializationMode
                ),
                transaction
            );
#pragma warning restore CA2007

            await using var cancellationTokenRegistration =
                DbCommandHelper.RegisterDbCommandCancellation(createCommand, cancellationToken).ConfigureAwait(false);

            DbConnectionExtensions.OnBeforeExecutingCommand(createCommand, []);

            await createCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
#pragma warning disable CA2007
            await using var createCommand = DbConnectionExtensions.DbCommandFactory.CreateDbCommand(
                connection,
                this.BuildCreateMultiColumnTemporaryTableSqlCode(
                    name,
                    valuesType,
                    DbConnectionPlusConfiguration.Instance.EnumSerializationMode
                ),
                transaction
            );
#pragma warning restore CA2007

            await using var cancellationTokenRegistration =
                DbCommandHelper.RegisterDbCommandCancellation(createCommand, cancellationToken).ConfigureAwait(false);

            DbConnectionExtensions.OnBeforeExecutingCommand(createCommand, []);

            await createCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

#pragma warning disable CA2007
        await using var reader = CreateValuesDataReader(values, valuesType);
#pragma warning restore CA2007

        await this.PopulateTemporaryTableAsync(npgsqlConnection, name, reader, cancellationToken)
            .ConfigureAwait(false);

        return new(
            () => DropTemporaryTable(name, npgsqlConnection, npgsqlTransaction),
            () => DropTemporaryTableAsync(name, npgsqlConnection, npgsqlTransaction)
        );
    }

    /// <summary>
    /// Builds an SQL code to create a multi-column temporary table to be populated with objects of the type
    /// <paramref name="objectsType" />.
    /// </summary>
    /// <param name="tableName">The name of the table to create.</param>
    /// <param name="objectsType">The type of objects with which to populate the table.</param>
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
            sqlBuilder.Append(property.ColumnName);
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
    /// <param name="tableName">The name of the table to create.</param>
    /// <param name="valuesType">The type of values with which the table will be populated.</param>
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
        sqlBuilder.Append("(\"");
        sqlBuilder.Append(Constants.SingleColumnTemporaryTableColumnName);
        sqlBuilder.Append("\" ");
        sqlBuilder.Append(this.databaseAdapter.GetDataType(valuesType, enumSerializationMode));
        sqlBuilder.AppendLine(")");

        return sqlBuilder.ToString();
    }

    /// <summary>
    /// Populates the specified temporary table with the data from the specified data reader.
    /// </summary>
    /// <param name="connection">The database connection to use to populate the table.</param>
    /// <param name="tableName">The name of the table to populate.</param>
    /// <param name="dataReader">The data reader to use to populate the table.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    private void PopulateTemporaryTable(
        NpgsqlConnection connection,
        String tableName,
        DbDataReader dataReader,
        CancellationToken cancellationToken
    )
    {
        using var importer = connection.BeginBinaryImport($"COPY \"{tableName}\" FROM STDIN (FORMAT BINARY)");

        var npgsqlDbTypes = dataReader
            .GetFieldTypes()
            .Select(t =>
                this.databaseAdapter.GetDbType(t, DbConnectionPlusConfiguration.Instance.EnumSerializationMode)
            )
            .ToArray();

        while (dataReader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();

            importer.StartRow();

            for (var i = 0; i < dataReader.FieldCount; i++)
            {
                if (dataReader.IsDBNull(i))
                {
                    importer.WriteNull();
                }
                else
                {
                    var value = dataReader.GetValue(i);

                    if (value is Enum enumValue)
                    {
                        value = EnumSerializer.SerializeEnum(
                            enumValue,
                            DbConnectionPlusConfiguration.Instance.EnumSerializationMode
                        );
                    }

                    importer.Write(value, npgsqlDbTypes[i]);
                }
            }
        }

        importer.Complete();
        importer.Close();
    }

    /// <summary>
    /// Asynchronously populates the specified temporary table with the data from the specified data reader.
    /// </summary>
    /// <param name="connection">The database connection to use to populate the table.</param>
    /// <param name="tableName">The name of the table to populate.</param>
    /// <param name="dataReader">The data reader to use to populate the table.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task PopulateTemporaryTableAsync(
        NpgsqlConnection connection,
        String tableName,
        DbDataReader dataReader,
        CancellationToken cancellationToken
    )
    {
#pragma warning disable CA2007
        await using var importer = await connection
            .BeginBinaryImportAsync($"COPY \"{tableName}\" FROM STDIN (FORMAT BINARY)", cancellationToken)
            .ConfigureAwait(false);
#pragma warning restore CA2007

        var npgsqlDbTypes = dataReader
            .GetFieldTypes()
            .Select(a =>
                this.databaseAdapter.GetDbType(a, DbConnectionPlusConfiguration.Instance.EnumSerializationMode)
            )
            .ToArray();

        while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            await importer.StartRowAsync(cancellationToken).ConfigureAwait(false);

            for (var i = 0; i < dataReader.FieldCount; i++)
            {
                if (await dataReader.IsDBNullAsync(i, cancellationToken).ConfigureAwait(false))
                {
                    await importer.WriteNullAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var value = dataReader.GetValue(i);

                    if (value is Enum enumValue)
                    {
                        value = EnumSerializer.SerializeEnum(
                            enumValue,
                            DbConnectionPlusConfiguration.Instance.EnumSerializationMode
                        );
                    }

                    await importer.WriteAsync(value, npgsqlDbTypes[i], cancellationToken).ConfigureAwait(false);
                }
            }
        }

        await importer.CompleteAsync(cancellationToken).ConfigureAwait(false);
        await importer.CloseAsync(cancellationToken).ConfigureAwait(false);
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
            return new EnumerableReader(values, valuesType, Constants.SingleColumnTemporaryTableColumnName);
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
    /// <param name="name">The name of the table to drop.</param>
    /// <param name="connection">The connection to use to drop the table.</param>
    /// <param name="transaction">The transaction within to drop the table.</param>
    private static void DropTemporaryTable(String name, NpgsqlConnection connection, NpgsqlTransaction? transaction)
    {
        using var command = DbConnectionExtensions.DbCommandFactory.CreateDbCommand(
            connection,
            $"DROP TABLE IF EXISTS \"{name}\"",
            transaction
        );

        DbConnectionExtensions.OnBeforeExecutingCommand(command, []);

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Asynchronously drops the temporary table with the specified name.
    /// </summary>
    /// <param name="name">The name of the table to drop.</param>
    /// <param name="connection">The connection to use to drop the table.</param>
    /// <param name="transaction">The transaction within to drop the table.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async ValueTask DropTemporaryTableAsync(
        String name,
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction
    )
    {
#pragma warning disable CA2007
        await using var command = DbConnectionExtensions.DbCommandFactory.CreateDbCommand(
            connection,
            $"DROP TABLE IF EXISTS \"{name}\"",
            transaction
        );
#pragma warning restore CA2007

        DbConnectionExtensions.OnBeforeExecutingCommand(command, []);

        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    private readonly PostgreSqlDatabaseAdapter databaseAdapter;
}
