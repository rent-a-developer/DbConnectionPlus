// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using LinkDotNet.StringBuilder;
using MySqlConnector;
using RentADeveloper.DbConnectionPlus.Converters;
using RentADeveloper.DbConnectionPlus.DbCommands;
using RentADeveloper.DbConnectionPlus.Extensions;
using RentADeveloper.DbConnectionPlus.Readers;

namespace RentADeveloper.DbConnectionPlus.DatabaseAdapters.MySql;

/// <summary>
/// The temporary table builder for MySQL.
/// </summary>
internal class MySqlTemporaryTableBuilder : ITemporaryTableBuilder
{
    private readonly MySqlDatabaseAdapter databaseAdapter;

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
        string name,
        IEnumerable values,
        [DynamicallyAccessedMembers(EntityHelper.TemporaryTableValueMemberTypes)] Type valuesType,
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
            using var createCommand = connection.CreateCommand();

            createCommand.CommandText = this.BuildCreateSingleColumnTemporaryTableSqlCode(
                name,
                valuesType,
                DbConnectionPlusConfiguration.Instance.EnumSerializationMode
            );
            createCommand.Transaction = transaction;

            using var cancellationTokenRegistration = DbCommandHelper.RegisterDbCommandCancellation(
                createCommand,
                cancellationToken
            );

            DbConnectionExtensions.OnBeforeExecutingCommand(createCommand, []);

            createCommand.ExecuteNonQuery();
        }
        else
        {
            using var createCommand = connection.CreateCommand();

            createCommand.CommandText = this.BuildCreateMultiColumnTemporaryTableSqlCode(
                name,
                valuesType,
                DbConnectionPlusConfiguration.Instance.EnumSerializationMode
            );
            createCommand.Transaction = transaction;

            using var cancellationTokenRegistration = DbCommandHelper.RegisterDbCommandCancellation(
                createCommand,
                cancellationToken
            );

            DbConnectionExtensions.OnBeforeExecutingCommand(createCommand, []);

            createCommand.ExecuteNonQuery();
        }

        using var reader = CreateValuesDataReader(values, valuesType);

        var mySqlBulkCopy = new MySqlBulkCopy(mySqlConnection, mySqlTransaction)
        {
            BulkCopyTimeout = 0,
            DestinationTableName = $"`{name}`",
        };

        mySqlBulkCopy.ColumnMappings.Clear();

        if (valuesType.IsBuiltInTypeOrNullableBuiltInType() || valuesType.IsEnumOrNullableEnumType())
        {
            mySqlBulkCopy.ColumnMappings.Add(new(0, Constants.SingleColumnTemporaryTableColumnName));
        }
        else
        {
            var properties = EntityHelper
                .GetEntityTypeMetadata(valuesType)
                .MappedProperties.Where(a => a.CanRead)
                .ToList();

            for (var i = 0; i < properties.Count; i++)
            {
                var property = properties[i];

                mySqlBulkCopy.ColumnMappings.Add(new(i, property.ColumnName));
            }
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
        string name,
        IEnumerable values,
        [DynamicallyAccessedMembers(EntityHelper.TemporaryTableValueMemberTypes)] Type valuesType,
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
#pragma warning disable CA2007
            await using var createCommand = connection.CreateCommand();
#pragma warning restore CA2007

            createCommand.CommandText = this.BuildCreateSingleColumnTemporaryTableSqlCode(
                name,
                valuesType,
                DbConnectionPlusConfiguration.Instance.EnumSerializationMode
            );
            createCommand.Transaction = transaction;

            await using var cancellationTokenRegistration =
#pragma warning disable CA2007
            DbCommandHelper.RegisterDbCommandCancellation(createCommand, cancellationToken);
#pragma warning restore CA2007

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
                DbConnectionPlusConfiguration.Instance.EnumSerializationMode
            );

            createCommand.Transaction = transaction;

            await using var cancellationTokenRegistration = DbCommandHelper
                .RegisterDbCommandCancellation(createCommand, cancellationToken)
                .ConfigureAwait(false);

            DbConnectionExtensions.OnBeforeExecutingCommand(createCommand, []);

            await createCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

#pragma warning disable CA2007
        await using var reader = CreateValuesDataReader(values, valuesType);
#pragma warning restore CA2007

        var mySqlBulkCopy = new MySqlBulkCopy(mySqlConnection, mySqlTransaction)
        {
            BulkCopyTimeout = 0,
            DestinationTableName = $"`{name}`",
        };

        mySqlBulkCopy.ColumnMappings.Clear();

        if (valuesType.IsBuiltInTypeOrNullableBuiltInType() || valuesType.IsEnumOrNullableEnumType())
        {
            mySqlBulkCopy.ColumnMappings.Add(new(0, Constants.SingleColumnTemporaryTableColumnName));
        }
        else
        {
            var properties = EntityHelper
                .GetEntityTypeMetadata(valuesType)
                .MappedProperties.Where(a => a.CanRead)
                .ToList();

            for (var i = 0; i < properties.Count; i++)
            {
                var property = properties[i];

                mySqlBulkCopy.ColumnMappings.Add(new(i, property.ColumnName));
            }
        }

        await mySqlBulkCopy.WriteToServerAsync(reader, cancellationToken).ConfigureAwait(false);

        return new(
            () => DropTemporaryTable(name, mySqlConnection, mySqlTransaction),
            () => DropTemporaryTableAsync(name, mySqlConnection, mySqlTransaction)
        );
    }

    /// <summary>
    /// Creates a <see cref="DbDataReader" /> that reads data from the specified sequence of values.
    /// </summary>
    /// <param name="values">The sequence containing the values to be read.</param>
    /// <param name="valuesType">The type of values in <paramref name="values" />.</param>
    /// <returns>
    /// A <see cref="DbDataReader" /> that provides access to the data in <paramref name="values" />.
    /// </returns>
    private static EnumerableReader CreateValuesDataReader(
        IEnumerable values,
        [DynamicallyAccessedMembers(EntityHelper.TemporaryTableValueMemberTypes)] Type valuesType
    )
    {
        if (valuesType.IsBuiltInTypeOrNullableBuiltInType() || valuesType.IsEnumOrNullableEnumType())
        {
            if (valuesType.IsEnumOrNullableEnumType())
            {
                var enumValues = new List<object>();

                foreach (var value in values)
                {
                    if (value is Enum enumValue)
                    {
                        enumValues.Add(
                            EnumSerializer.SerializeEnum(
                                enumValue,
                                DbConnectionPlusConfiguration.Instance.EnumSerializationMode
                            )
                        );
                    }
                    else
                    {
                        enumValues.Add(value);
                    }
                }

                // The reader is built per branch instead of first resolving the type into a local: that keeps the
                // fully known typeof(...) values flowing straight into EnumerableReader, whose valuesType
                // parameter is annotated. Routing them through a switch expression would launder the annotation
                // away, because the throw helper's generic return value carries none.
                switch (DbConnectionPlusConfiguration.Instance.EnumSerializationMode)
                {
                    case EnumSerializationMode.Integers:
                        return new(enumValues, typeof(int?), Constants.SingleColumnTemporaryTableColumnName);

                    case EnumSerializationMode.Strings:
                        return new(enumValues, typeof(string), Constants.SingleColumnTemporaryTableColumnName);

                    default:
                        return ThrowHelper.ThrowInvalidEnumSerializationModeException<EnumerableReader>(
                            DbConnectionPlusConfiguration.Instance.EnumSerializationMode
                        );
                }
            }

            return new(values, valuesType, Constants.SingleColumnTemporaryTableColumnName);
        }

        return new(
            values,
            [.. EntityHelper.GetEntityTypeMetadata(valuesType).MappedProperties.Where(a => a.CanRead)],
            EnumerableReaderOptions.SerializeEnums | EnumerableReaderOptions.ReadCharsAsStrings
        );
    }

    /// <summary>
    /// Drops the temporary table with the specified name.
    /// </summary>
    /// <param name="name">The name of the table to drop.</param>
    /// <param name="connection">The connection to use to drop the table.</param>
    /// <param name="transaction">The transaction within to drop the table.</param>
    private static void DropTemporaryTable(string name, MySqlConnection connection, MySqlTransaction? transaction)
    {
        using var command = connection.CreateCommand();

        command.CommandText = $"DROP TEMPORARY TABLE IF EXISTS `{name}`";
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
        string name,
        MySqlConnection connection,
        MySqlTransaction? transaction
    )
    {
#pragma warning disable CA2007
        await using var command = connection.CreateCommand();
#pragma warning restore CA2007

        command.CommandText = $"DROP TEMPORARY TABLE IF EXISTS `{name}`";
        command.Transaction = transaction;

        DbConnectionExtensions.OnBeforeExecutingCommand(command, []);

        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Builds an SQL code to create a multi-column temporary table to be populated with objects of the type
    /// <paramref name="objectsType" />.
    /// </summary>
    /// <param name="tableName">The name of the table to create.</param>
    /// <param name="objectsType">The type of objects with which to populate the table.</param>
    /// <param name="enumSerializationMode">The mode to use to serialize <see cref="Enum" /> values.</param>
    /// <returns>The built SQL code.</returns>
    private string BuildCreateMultiColumnTemporaryTableSqlCode(
        string tableName,
        [DynamicallyAccessedMembers(EntityHelper.TemporaryTableValueMemberTypes)] Type objectsType,
        EnumSerializationMode enumSerializationMode
    )
    {
        using var sqlBuilder = new ValueStringBuilder(stackalloc char[500]);

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
            sqlBuilder.Append(property.ColumnName);
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
    /// <param name="tableName">The name of the table to create.</param>
    /// <param name="valuesType">The type of values with which the table will be populated.</param>
    /// <param name="enumSerializationMode">The mode to use to serialize <see cref="Enum" /> values.</param>
    /// <returns>The built SQL code.</returns>
    private string BuildCreateSingleColumnTemporaryTableSqlCode(
        string tableName,
        [DynamicallyAccessedMembers(EntityHelper.TemporaryTableValueMemberTypes)] Type valuesType,
        EnumSerializationMode enumSerializationMode
    )
    {
        using var sqlBuilder = new ValueStringBuilder(stackalloc char[100]);

        sqlBuilder.Append("CREATE TEMPORARY TABLE `");
        sqlBuilder.Append(tableName);
        sqlBuilder.AppendLine("`");

        sqlBuilder.Append(Constants.Indent);
        sqlBuilder.Append("(`");
        sqlBuilder.Append(Constants.SingleColumnTemporaryTableColumnName);
        sqlBuilder.Append("` ");
        sqlBuilder.Append(this.databaseAdapter.GetDataType(valuesType, enumSerializationMode));
        sqlBuilder.AppendLine(")");

        return sqlBuilder.ToString();
    }
}
