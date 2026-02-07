// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using FastMember;
using LinkDotNet.StringBuilder;
using Microsoft.Data;
using RentADeveloper.DbConnectionPlus.DbCommands;
using RentADeveloper.DbConnectionPlus.Entities;
using RentADeveloper.DbConnectionPlus.Extensions;
using RentADeveloper.DbConnectionPlus.Readers;

namespace RentADeveloper.DbConnectionPlus.DatabaseAdapters.SqlServer;

/// <summary>
/// The temporary table builder for SQL Server.
/// </summary>
internal class SqlServerTemporaryTableBuilder : ITemporaryTableBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerTemporaryTableBuilder" /> class.
    /// </summary>
    /// <param name="databaseAdapter">The database adapter to use to build temporary tables.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="databaseAdapter" /> is <see langword="null" />.
    /// </exception>
    public SqlServerTemporaryTableBuilder(SqlServerDatabaseAdapter databaseAdapter)
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

        if (connection is not SqlConnection sqlConnection)
        {
            return ThrowHelper.ThrowWrongConnectionTypeException<SqlConnection, TemporaryTableDisposer>();
        }

        var sqlTransaction = transaction as SqlTransaction;

        if (transaction is not null && sqlTransaction is null)
        {
            return ThrowHelper.ThrowWrongTransactionTypeException<SqlTransaction, TemporaryTableDisposer>();
        }

        // For text columns, we must use the collation of the database the connection is currently connected to,
        // because the tempDB might have a different collation. Otherwise, we could run into collation conflict errors
        // when joining the temporary table with other tables.
        var databaseCollation = GetCurrentDatabaseCollation(sqlConnection, sqlTransaction);

        if (valuesType.IsBuiltInTypeOrNullableBuiltInType() || valuesType.IsEnumOrNullableEnumType())
        {
            using var createCommand = connection.CreateCommand();

            createCommand.CommandText = this.BuildCreateSingleColumnTemporaryTableSqlCode(
                name,
                // ReSharper disable once PossibleMultipleEnumeration
                values,
                valuesType,
                databaseCollation,
                DbConnectionPlusConfiguration.Instance.EnumSerializationMode
            );
            createCommand.Transaction = transaction;

            using var cancellationTokenRegistration =
                DbCommandHelper.RegisterDbCommandCancellation(createCommand, cancellationToken);

            DbConnectionExtensions.OnBeforeExecutingCommand(createCommand, []);

            createCommand.ExecuteNonQuery();
        }
        else
        {
            using var createCommand = connection.CreateCommand();

            createCommand.CommandText = this.BuildCreateMultiColumnTemporaryTableSqlCode(
                name,
                valuesType,
                databaseCollation,
                DbConnectionPlusConfiguration.Instance.EnumSerializationMode
            );
            createCommand.Transaction = transaction;

            using var cancellationTokenRegistration =
                DbCommandHelper.RegisterDbCommandCancellation(createCommand, cancellationToken);

            DbConnectionExtensions.OnBeforeExecutingCommand(createCommand, []);

            createCommand.ExecuteNonQuery();
        }

        // ReSharper disable once PossibleMultipleEnumeration
        using var reader = CreateValuesDataReader(values, valuesType);

        using var sqlBulkCopy = new SqlBulkCopy(sqlConnection, SqlBulkCopyOptions.TableLock, sqlTransaction);

        sqlBulkCopy.BulkCopyTimeout = 0;
        sqlBulkCopy.BatchSize = 0;
        sqlBulkCopy.DestinationTableName = "#" + name;

        sqlBulkCopy.ColumnMappings.Clear();

        if (valuesType.IsBuiltInTypeOrNullableBuiltInType() || valuesType.IsEnumOrNullableEnumType())
        {
            sqlBulkCopy.ColumnMappings.Add(
                Constants.SingleColumnTemporaryTableColumnName,
                Constants.SingleColumnTemporaryTableColumnName
            );
        }
        else
        {
            var properties = EntityHelper.GetEntityTypeMetadata(valuesType).MappedProperties.Where(a => a.CanRead);

            foreach (var property in properties)
            {
                sqlBulkCopy.ColumnMappings.Add(property.PropertyName, property.ColumnName);
            }
        }

        sqlBulkCopy.WriteToServer(reader);

        return new(
            () => DropTemporaryTable(name, sqlConnection, sqlTransaction),
            () => DropTemporaryTableAsync(name, sqlConnection, sqlTransaction)
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

        if (connection is not SqlConnection sqlConnection)
        {
            return ThrowHelper.ThrowWrongConnectionTypeException<SqlConnection, TemporaryTableDisposer>();
        }

        var sqlTransaction = transaction as SqlTransaction;

        if (transaction is not null && sqlTransaction is null)
        {
            return ThrowHelper.ThrowWrongTransactionTypeException<SqlTransaction, TemporaryTableDisposer>();
        }

        // For text columns, we must use the collation of the database the connection is currently connected to,
        // because the tempDB might have a different collation. Otherwise, we could run into collation conflict errors
        // when joining the temporary table with other tables.
        var databaseCollation = await GetCurrentDatabaseCollationAsync(sqlConnection, sqlTransaction)
            .ConfigureAwait(false);

        if (valuesType.IsBuiltInTypeOrNullableBuiltInType() || valuesType.IsEnumOrNullableEnumType())
        {
#pragma warning disable CA2007
            await using var createCommand = connection.CreateCommand();
            createCommand.CommandText = this.BuildCreateSingleColumnTemporaryTableSqlCode(
                name,
                // ReSharper disable once PossibleMultipleEnumeration
                values,
                valuesType,
                databaseCollation,
                DbConnectionPlusConfiguration.Instance.EnumSerializationMode
            );
            createCommand.Transaction = transaction;
#pragma warning restore CA2007

            await using var cancellationTokenRegistration =
                DbCommandHelper.RegisterDbCommandCancellation(createCommand, cancellationToken).ConfigureAwait(false);

            DbConnectionExtensions.OnBeforeExecutingCommand(createCommand, []);

            await createCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
#pragma warning disable CA2007
            await using var createCommand = connection.CreateCommand();
#pragma warning restore CA2007

            createCommand.CommandText = this.BuildCreateMultiColumnTemporaryTableSqlCode(
                name,
                valuesType,
                databaseCollation,
                DbConnectionPlusConfiguration.Instance.EnumSerializationMode
            );
            createCommand.Transaction = transaction;

            await using var cancellationTokenRegistration =
                DbCommandHelper.RegisterDbCommandCancellation(createCommand, cancellationToken).ConfigureAwait(false);

            DbConnectionExtensions.OnBeforeExecutingCommand(createCommand, []);

            await createCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        // ReSharper disable once PossibleMultipleEnumeration
#pragma warning disable CA2007
        await using var reader = CreateValuesDataReader(values, valuesType);
#pragma warning restore CA2007

        using var sqlBulkCopy = new SqlBulkCopy(sqlConnection, SqlBulkCopyOptions.TableLock, sqlTransaction);

        sqlBulkCopy.BulkCopyTimeout = 0;
        sqlBulkCopy.BatchSize = 0;
        sqlBulkCopy.DestinationTableName = "#" + name;

        sqlBulkCopy.ColumnMappings.Clear();

        if (valuesType.IsBuiltInTypeOrNullableBuiltInType() || valuesType.IsEnumOrNullableEnumType())
        {
            sqlBulkCopy.ColumnMappings.Add(
                Constants.SingleColumnTemporaryTableColumnName,
                Constants.SingleColumnTemporaryTableColumnName
            );
        }
        else
        {
            var properties = EntityHelper.GetEntityTypeMetadata(valuesType).MappedProperties.Where(a => a.CanRead);

            foreach (var property in properties)
            {
                sqlBulkCopy.ColumnMappings.Add(property.PropertyName, property.ColumnName);
            }
        }

        try
        {
            await sqlBulkCopy.WriteToServerAsync(reader, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationAbortedException)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        return new(
            () => DropTemporaryTable(name, sqlConnection, sqlTransaction),
            () => DropTemporaryTableAsync(name, sqlConnection, sqlTransaction)
        );
    }


    /// <summary>
    /// Builds an SQL code to create a multi-column temporary table to be populated with objects of the type
    /// <paramref name="objectsType" />.
    /// </summary>
    /// <param name="tableName">The name of the table to create.</param>
    /// <param name="objectsType">The type of objects with which to populate the table.</param>
    /// <param name="collation">The collation to use for text columns.</param>
    /// <param name="enumSerializationMode">The mode to use to serialize <see cref="Enum" /> values.</param>
    /// <returns>The built SQL code.</returns>
    private String BuildCreateMultiColumnTemporaryTableSqlCode(
        String tableName,
        Type objectsType,
        String collation,
        EnumSerializationMode enumSerializationMode
    )
    {
        using var sqlBuilder = new ValueStringBuilder(stackalloc Char[500]);

        sqlBuilder.Append("CREATE TABLE [#");
        sqlBuilder.Append(tableName);
        sqlBuilder.AppendLine("]");

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

            sqlBuilder.Append('[');
            sqlBuilder.Append(property.ColumnName);
            sqlBuilder.Append("] ");

            var propertyType = property.PropertyType;

            sqlBuilder.Append(this.databaseAdapter.GetDataType(propertyType, enumSerializationMode));

            if (
                propertyType == typeof(String)
                ||
                (
                    propertyType.IsEnumOrNullableEnumType() &&
                    enumSerializationMode == EnumSerializationMode.Strings
                )
            )
            {
                sqlBuilder.Append(" COLLATE ");
                sqlBuilder.Append(collation);
            }

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
    /// <param name="values">The values with which to populate the table.</param>
    /// <param name="valuesType">The type of values with which the table will be populated.</param>
    /// <param name="collation">The collation to use for text columns.</param>
    /// <param name="enumSerializationMode">The mode to use to serialize <see cref="Enum" /> values.</param>
    /// <returns>The built SQL code.</returns>
    private String BuildCreateSingleColumnTemporaryTableSqlCode(
        String tableName,
        IEnumerable values,
        Type valuesType,
        String collation,
        EnumSerializationMode enumSerializationMode
    )
    {
        using var sqlBuilder = new ValueStringBuilder(stackalloc Char[100]);

        sqlBuilder.Append("CREATE TABLE [#");
        sqlBuilder.Append(tableName);
        sqlBuilder.AppendLine("]");

        sqlBuilder.Append(Constants.Indent);
        sqlBuilder.Append("([");
        sqlBuilder.Append(Constants.SingleColumnTemporaryTableColumnName);
        sqlBuilder.Append("] ");

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
                    sqlBuilder.Append("NVARCHAR(1)");
                    break;

                case <= 4000:
                    sqlBuilder.Append("NVARCHAR(");
                    sqlBuilder.Append(maxLength);
                    sqlBuilder.Append(')');
                    break;

                default:
                    sqlBuilder.Append("NVARCHAR(MAX)");
                    break;
            }
        }
        else
        {
            sqlBuilder.Append(this.databaseAdapter.GetDataType(valuesType, enumSerializationMode));
        }

        if (
            valuesType == typeof(String)
            ||
            (
                valuesType.IsEnumOrNullableEnumType() &&
                enumSerializationMode == EnumSerializationMode.Strings
            )
        )
        {
            sqlBuilder.Append(" COLLATE ");
            sqlBuilder.Append(collation);
        }

        sqlBuilder.AppendLine(")");

        return sqlBuilder.ToString();
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
    private static void DropTemporaryTable(String name, SqlConnection connection, SqlTransaction? transaction)
    {
        using var command = connection.CreateCommand();
        
        command.CommandText = $"IF OBJECT_ID('tempdb..#{name}', 'U') IS NOT NULL DROP TABLE [#{name}]";
        command.Transaction = transaction;

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
        SqlConnection connection,
        SqlTransaction? transaction
    )
    {
#pragma warning disable CA2007
        await using var command = connection.CreateCommand();
#pragma warning restore CA2007

        command.CommandText = $"IF OBJECT_ID('tempdb..#{name}', 'U') IS NOT NULL DROP TABLE [#{name}]";
        command.Transaction = transaction;

        DbConnectionExtensions.OnBeforeExecutingCommand(command, []);

        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the collation of the database the specified connection is currently connected to.
    /// </summary>
    /// <param name="connection">The connection to the database of which to get the collation.</param>
    /// <param name="transaction">The database transaction within to perform the operation.</param>
    /// <returns>The collation of the database the specified connection is currently connected to.</returns>
    private static String GetCurrentDatabaseCollation(
        SqlConnection connection,
        SqlTransaction? transaction = null
    ) =>
        databaseCollationPerDatabase.GetOrAdd(
            (connection.DataSource, connection.Database),
            static (_, args) =>
            {
                using var command = args.connection.CreateCommand();
                
                command.CommandText = GetCurrentDatabaseCollationQuery;
                command.Transaction = args.transaction;

                DbConnectionExtensions.OnBeforeExecutingCommand(command, []);

                return (String)command.ExecuteScalar()!;
            },
            (connection, transaction)
        );

    /// <summary>
    /// Asynchronously gets the collation of the database the specified connection is currently connected to.
    /// </summary>
    /// <param name="connection">The connection to the database of which to get the collation.</param>
    /// <param name="transaction">The database transaction within to perform the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// <see cref="ValueTask{TResult}.Result" /> will contain the collation of the database the specified connection is
    /// currently connected to.
    /// </returns>
    private static async ValueTask<String> GetCurrentDatabaseCollationAsync(
        SqlConnection connection,
        SqlTransaction? transaction = null
    )
    {
        if (databaseCollationPerDatabase.TryGetValue((connection.DataSource, connection.Database), out var collation))
        {
            return collation;
        }

#pragma warning disable CA2007
        await using var command = connection.CreateCommand();
#pragma warning restore CA2007

        command.CommandText = GetCurrentDatabaseCollationQuery;
        command.Transaction = transaction;

        DbConnectionExtensions.OnBeforeExecutingCommand(command, []);

        collation = (String)(await command.ExecuteScalarAsync().ConfigureAwait(false))!;

        return databaseCollationPerDatabase.GetOrAdd((connection.DataSource, connection.Database), collation);
    }

    private readonly SqlServerDatabaseAdapter databaseAdapter;

    private const String GetCurrentDatabaseCollationQuery =
        "SELECT CONVERT (VARCHAR(256), DATABASEPROPERTYEX(DB_NAME(), 'collation'))";

    private static readonly ConcurrentDictionary<(String DataSource, String Database), String>
        databaseCollationPerDatabase = [];
}
