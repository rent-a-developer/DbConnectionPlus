// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using FastMember;
using LinkDotNet.StringBuilder;
using Oracle.ManagedDataAccess.Client;
using RentADeveloper.DbConnectionPlus.DbCommands;
using RentADeveloper.DbConnectionPlus.Entities;
using RentADeveloper.DbConnectionPlus.Extensions;
using RentADeveloper.DbConnectionPlus.Readers;

namespace RentADeveloper.DbConnectionPlus.DatabaseAdapters.Oracle;

/// <summary>
/// The temporary table builder for Oracle.
/// </summary>
internal class OracleTemporaryTableBuilder : ITemporaryTableBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OracleTemporaryTableBuilder" /> class.
    /// </summary>
    /// <param name="databaseAdapter">The database adapter to use to build temporary tables.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="databaseAdapter" /> is <see langword="null" />.
    /// </exception>
    public OracleTemporaryTableBuilder(OracleDatabaseAdapter databaseAdapter)
    {
        ArgumentNullException.ThrowIfNull(databaseAdapter);

        this.databaseAdapter = databaseAdapter;
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// <see cref="OracleDatabaseAdapter.AllowTemporaryTables" /> is set to <see langword="false" />.
    /// </exception>
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

        if (!OracleDatabaseAdapter.AllowTemporaryTables)
        {
            OracleDatabaseAdapter.ThrowTemporaryTablesFeatureIsDisabledException();
        }

        if (connection is not OracleConnection oracleConnection)
        {
            return ThrowHelper.ThrowWrongConnectionTypeException<OracleConnection, TemporaryTableDisposer>();
        }

        var oracleTransaction = transaction as OracleTransaction;

        if (transaction is not null && oracleTransaction is null)
        {
            return ThrowHelper.ThrowWrongTransactionTypeException<OracleTransaction, TemporaryTableDisposer>();
        }

        var quotedTableName = this.databaseAdapter.QuoteTemporaryTableName(name, connection);

        if (valuesType.IsBuiltInTypeOrNullableBuiltInType() || valuesType.IsEnumOrNullableEnumType())
        {
            using var createCommand = DbConnectionExtensions.DbCommandFactory.CreateDbCommand(
                connection,
                this.BuildCreateSingleColumnTemporaryTableSqlCode(
                    quotedTableName,
                    // ReSharper disable once PossibleMultipleEnumeration
                    values,
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
                    quotedTableName,
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

        // ReSharper disable once PossibleMultipleEnumeration
        using var reader = CreateValuesDataReader(values, valuesType);

        this.PopulateTemporaryTable(oracleConnection, oracleTransaction, quotedTableName, reader, cancellationToken);

        return new(
            () => DropTemporaryTable(quotedTableName, oracleConnection, oracleTransaction),
            () => DropTemporaryTableAsync(quotedTableName, oracleConnection, oracleTransaction)
        );
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// <see cref="OracleDatabaseAdapter.AllowTemporaryTables" /> is set to <see langword="false" />.
    /// </exception>
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

        if (!OracleDatabaseAdapter.AllowTemporaryTables)
        {
            OracleDatabaseAdapter.ThrowTemporaryTablesFeatureIsDisabledException();
        }

        if (connection is not OracleConnection oracleConnection)
        {
            return ThrowHelper.ThrowWrongConnectionTypeException<OracleConnection, TemporaryTableDisposer>();
        }

        var oracleTransaction = transaction as OracleTransaction;

        if (transaction is not null && oracleTransaction is null)
        {
            return ThrowHelper.ThrowWrongTransactionTypeException<OracleTransaction, TemporaryTableDisposer>();
        }

        var quotedTableName = this.databaseAdapter.QuoteTemporaryTableName(name, connection);

        if (valuesType.IsBuiltInTypeOrNullableBuiltInType() || valuesType.IsEnumOrNullableEnumType())
        {
            using var createCommand = DbConnectionExtensions.DbCommandFactory.CreateDbCommand(
                connection,
                this.BuildCreateSingleColumnTemporaryTableSqlCode(
                    quotedTableName,
                    // ReSharper disable once PossibleMultipleEnumeration
                    values,
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
                    quotedTableName,
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

        // ReSharper disable once PossibleMultipleEnumeration
        using var reader = CreateValuesDataReader(values, valuesType);

        await this.PopulateTemporaryTableAsync(
                oracleConnection,
                oracleTransaction,
                quotedTableName,
                reader,
                cancellationToken
            )
            .ConfigureAwait(false);

        return new(
            () => DropTemporaryTable(quotedTableName, oracleConnection, oracleTransaction),
            () => DropTemporaryTableAsync(quotedTableName, oracleConnection, oracleTransaction)
        );
    }

    /// <summary>
    /// Builds an SQL code to create a multi-column temporary table to be populated with objects of the type
    /// <paramref name="objectsType" />.
    /// </summary>
    /// <param name="quotedTableName">The quoted name of the temporary table to create.</param>
    /// <param name="objectsType">The type of objects the temporary table will be populated with.</param>
    /// <param name="enumSerializationMode">The mode to use to serialize <see cref="Enum" /> values.</param>
    /// <returns>The built SQL code.</returns>
    private String BuildCreateMultiColumnTemporaryTableSqlCode(
        String quotedTableName,
        Type objectsType,
        EnumSerializationMode enumSerializationMode
    )
    {
        using var sqlBuilder = new ValueStringBuilder(stackalloc Char[500]);

        sqlBuilder.Append("CREATE PRIVATE TEMPORARY TABLE ");
        sqlBuilder.AppendLine(quotedTableName);

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

        sqlBuilder.AppendLine(") ON COMMIT PRESERVE DEFINITION");

        return sqlBuilder.ToString();
    }

    /// <summary>
    /// Builds an SQL code to create a single-column temporary table to be populated with values of the type
    /// <paramref name="valuesType" />.
    /// </summary>
    /// <param name="quotedTableName">The quoted name of the temporary table to create.</param>
    /// <param name="values">The values to populate the temporary table with.</param>
    /// <param name="valuesType">The type of values the temporary table will be populated with.</param>
    /// <param name="enumSerializationMode">The mode to use to serialize <see cref="Enum" /> values.</param>
    /// <returns>The built SQL code.</returns>
    private String BuildCreateSingleColumnTemporaryTableSqlCode(
        String quotedTableName,
        IEnumerable values,
        Type valuesType,
        EnumSerializationMode enumSerializationMode
    )
    {
        using var sqlBuilder = new ValueStringBuilder(stackalloc Char[100]);

        sqlBuilder.Append("CREATE PRIVATE TEMPORARY TABLE");
        sqlBuilder.AppendLine(quotedTableName);

        sqlBuilder.Append(Constants.Indent);
        sqlBuilder.Append("(\"Value\" ");

        if (valuesType == typeof(String))
        {
            var maxLength = 0;

            foreach (String? value in values)
            {
                if (value?.Length > maxLength)
                {
                    maxLength = value.Length;
                }
            }

            switch (maxLength)
            {
                case 0:
                    sqlBuilder.Append("NVARCHAR2(1)");
                    break;

                case <= 2000:
                    sqlBuilder.Append("NVARCHAR2(");
                    sqlBuilder.Append(maxLength);
                    sqlBuilder.Append(')');
                    break;

                default:
                    sqlBuilder.Append("NVARCHAR2(2000)");
                    break;
            }
        }
        else
        {
            sqlBuilder.Append(this.databaseAdapter.GetDataType(valuesType, enumSerializationMode));
        }

        sqlBuilder.AppendLine(") ON COMMIT PRESERVE DEFINITION");

        return sqlBuilder.ToString();
    }

    /// <summary>
    /// Populates the specified temporary table with the data from the specified data reader.
    /// </summary>
    /// <param name="connection">The database connection to use to populate the temporary table.</param>
    /// <param name="transaction">The database transaction within to populate the temporary table.</param>
    /// <param name="quotedTableName">The quoted name of the temporary table to populate.</param>
    /// <param name="dataReader">The data reader to use to populate the temporary table.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    private void PopulateTemporaryTable(
        OracleConnection connection,
        OracleTransaction? transaction,
        String quotedTableName,
        DbDataReader dataReader,
        CancellationToken cancellationToken
    )
    {
        var insertCommand = connection.CreateCommand();
        insertCommand.Transaction = transaction;

        var (insertSqlCode, parameters) = BuildInsertSqlCode(quotedTableName, dataReader);

#pragma warning disable CA2100
        insertCommand.CommandText = insertSqlCode;
#pragma warning restore CA2100

        insertCommand.Parameters.AddRange(parameters);

        while (dataReader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var value = dataReader.GetValue(i);

                this.databaseAdapter.BindParameterValue(parameter, value);
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
    /// <param name="quotedTableName">The quoted name of the temporary table to populate.</param>
    /// <param name="dataReader">The data reader to use to populate the temporary table.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task PopulateTemporaryTableAsync(
        OracleConnection connection,
        OracleTransaction? transaction,
        String quotedTableName,
        DbDataReader dataReader,
        CancellationToken cancellationToken
    )
    {
#pragma warning disable CA2007
        await using var insertCommand = connection.CreateCommand();
#pragma warning restore CA2007

        insertCommand.Transaction = transaction;

        var (insertSqlCode, parameters) = BuildInsertSqlCode(quotedTableName, dataReader);

#pragma warning disable CA2100
        insertCommand.CommandText = insertSqlCode;
#pragma warning restore CA2100

        insertCommand.Parameters.AddRange(parameters);

        while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var value = dataReader.GetValue(i);

                this.databaseAdapter.BindParameterValue(parameter, value);
            }

            DbConnectionExtensions.OnBeforeExecutingCommand(insertCommand, []);

            await insertCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Builds an SQL code to insert data from the specified data reader into the specified temporary table.
    /// </summary>
    /// <param name="quotedTableName">The quoted name of the temporary table to insert data into.</param>
    /// <param name="dataReader">The data reader to read data from.</param>
    /// <returns>A tuple containing the insert SQL code and the parameters to use.</returns>
    private static (String SqlCode, OracleParameter[] Parameters) BuildInsertSqlCode(
        String quotedTableName,
        DbDataReader dataReader
    )
    {
        using var sqlBuilder = new ValueStringBuilder(stackalloc Char[500]);

        sqlBuilder.Append("INSERT INTO ");
        sqlBuilder.AppendLine(quotedTableName);

        sqlBuilder.Append(Constants.Indent);
        sqlBuilder.Append("(");

        var fieldCount = dataReader.FieldCount;
        var parameters = new OracleParameter[fieldCount];

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

            var parameter = new OracleParameter
            {
                ParameterName = fieldName
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

            sqlBuilder.Append(":\"" + parameters[i].ParameterName + "\"");
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
    /// <param name="quotedTableName">The quoted name of the temporary table to drop.</param>
    /// <param name="connection">The connection to use to drop the table.</param>
    /// <param name="transaction">The transaction within to drop the table.</param>
    private static void DropTemporaryTable(
        String quotedTableName,
        OracleConnection connection,
        OracleTransaction? transaction
    )
    {
        using var command = DbConnectionExtensions.DbCommandFactory.CreateDbCommand(
            connection,
            $"DROP TABLE {quotedTableName}",
            transaction
        );

        DbConnectionExtensions.OnBeforeExecutingCommand(command, []);

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Asynchronously drops the temporary table with the specified name.
    /// </summary>
    /// <param name="quotedTableName">The quoted name of the temporary table to drop.</param>
    /// <param name="connection">The connection to use to drop the table.</param>
    /// <param name="transaction">The transaction within to drop the table.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async ValueTask DropTemporaryTableAsync(
        String quotedTableName,
        OracleConnection connection,
        OracleTransaction? transaction
    )
    {
        using var command = DbConnectionExtensions.DbCommandFactory.CreateDbCommand(
            connection,
            $"DROP TABLE {quotedTableName}",
            transaction
        );

        DbConnectionExtensions.OnBeforeExecutingCommand(command, []);

        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    private readonly OracleDatabaseAdapter databaseAdapter;
}
