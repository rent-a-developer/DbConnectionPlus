// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using LinkDotNet.StringBuilder;
using RentADeveloper.DbConnectionPlus.DbCommands;
using RentADeveloper.DbConnectionPlus.Entities;

namespace RentADeveloper.DbConnectionPlus.DatabaseAdapters.SqlServer;

/// <summary>
/// The entity manipulator for SQL Server.
/// </summary>
internal class SqlServerEntityManipulator : IEntityManipulator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerEntityManipulator" /> class.
    /// </summary>
    /// <param name="databaseAdapter">The database adapter to use to manipulate entities.</param>
    public SqlServerEntityManipulator(SqlServerDatabaseAdapter databaseAdapter) =>
        this.databaseAdapter = databaseAdapter;

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="connection" /> is not a <see cref="SqlConnection" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="transaction" /> is not <see langword="null" /> and not a
    /// <see cref="SqlTransaction" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    public Int32 DeleteEntities<TEntity>(
        DbConnection connection,
        IEnumerable<TEntity> entities,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entities);

        var entitiesList = entities.ToList();

        // For a small number of entities deleting them one by one is more efficient than creating a temp table.
        if (entitiesList.Count < BulkDeleteThreshold)
        {
            var totalNumberOfAffectedRows = 0;

            foreach (var entity in entitiesList)
            {
                if (entity is null)
                {
                    continue;
                }

                totalNumberOfAffectedRows += this.DeleteEntity(connection, entity, transaction, cancellationToken);
            }

            return totalNumberOfAffectedRows;
        }

        if (connection is not SqlConnection sqlConnection)
        {
            return ThrowHelper.ThrowWrongConnectionTypeException<SqlConnection, Int32>();
        }

        var sqlTransaction = transaction as SqlTransaction;

        if (transaction is not null && sqlTransaction is null)
        {
            return ThrowHelper.ThrowWrongTransactionTypeException<SqlTransaction, Int32>();
        }

        var entityTypeMetadata = EntityHelper.GetEntityTypeMetadata(typeof(TEntity));

        var onClause = String.Join(
            " AND ",
            entityTypeMetadata.KeyProperties.Select(p => $"TKeys.[{p.PropertyName}] = TEntities.[{p.PropertyName}]")
        );

        try
        {
            var keysTableName = "Keys_" + Guid.NewGuid().ToString("N");

            this.BuildEntityKeysTemporaryTable(
                sqlConnection,
                keysTableName,
                entitiesList,
                entityTypeMetadata,
                sqlTransaction,
                cancellationToken
            );

            var numberOfAffectedRows = connection.ExecuteNonQuery(
                $"""
                 DELETE
                 {Constants.Indent}[{entityTypeMetadata.TableName}]
                 FROM
                 {Constants.Indent}[{entityTypeMetadata.TableName}] AS TEntities
                 INNER JOIN
                 {Constants.Indent}[#{keysTableName}] AS TKeys
                 ON
                 {Constants.Indent}{onClause}
                 """,
                transaction,
                cancellationToken: cancellationToken
            );

#pragma warning disable CA2016
            connection.ExecuteNonQuery($"DROP TABLE [#{keysTableName}]", transaction);
#pragma warning restore CA2016

            return numberOfAffectedRows;
        }
        catch (Exception exception) when (this.databaseAdapter.WasSqlStatementCancelledByCancellationToken(
                exception,
                cancellationToken
            )
        )
        {
            throw new OperationCanceledException(cancellationToken);
        }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="connection" /> is not a <see cref="SqlConnection" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="transaction" /> is not <see langword="null" /> and not a
    /// <see cref="SqlTransaction" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    public async Task<Int32> DeleteEntitiesAsync<TEntity>(
        DbConnection connection,
        IEnumerable<TEntity> entities,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entities);

        var entitiesList = entities.ToList();

        // For a small number of entities deleting them one by one is more efficient than creating a temp table.
        if (entitiesList.Count < BulkDeleteThreshold)
        {
            var totalNumberOfAffectedRows = 0;

            foreach (var entity in entitiesList)
            {
                if (entity is null)
                {
                    continue;
                }

                totalNumberOfAffectedRows += await this
                    .DeleteEntityAsync(connection, entity, transaction, cancellationToken).ConfigureAwait(false);
            }

            return totalNumberOfAffectedRows;
        }

        if (connection is not SqlConnection sqlConnection)
        {
            return ThrowHelper.ThrowWrongConnectionTypeException<SqlConnection, Int32>();
        }

        var sqlTransaction = transaction as SqlTransaction;

        if (transaction is not null && sqlTransaction is null)
        {
            return ThrowHelper.ThrowWrongTransactionTypeException<SqlTransaction, Int32>();
        }

        var entityTypeMetadata = EntityHelper.GetEntityTypeMetadata(typeof(TEntity));

        var onClause = String.Join(
            " AND ",
            entityTypeMetadata.KeyProperties.Select(p => $"TKeys.[{p.PropertyName}] = TEntities.[{p.PropertyName}]")
        );

        try
        {
            var keysTableName = "Keys_" + Guid.NewGuid().ToString("N");

            await this.BuildEntityKeysTemporaryTableAsync(
                sqlConnection,
                keysTableName,
                entitiesList,
                entityTypeMetadata,
                sqlTransaction,
                cancellationToken
            ).ConfigureAwait(false);

            var numberOfAffectedRows = await connection.ExecuteNonQueryAsync(
                $"""
                 DELETE
                 {Constants.Indent}[{entityTypeMetadata.TableName}]
                 FROM
                 {Constants.Indent}[{entityTypeMetadata.TableName}] AS TEntities
                 INNER JOIN
                 {Constants.Indent}[#{keysTableName}] AS TKeys
                 ON
                 {Constants.Indent}{onClause}
                 """,
                transaction,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

#pragma warning disable CA2016
            // ReSharper disable once MethodSupportsCancellation
            await connection.ExecuteNonQueryAsync($"DROP TABLE [#{keysTableName}]", transaction).ConfigureAwait(false);
#pragma warning restore CA2016

            return numberOfAffectedRows;
        }
        catch (Exception exception) when (this.databaseAdapter.WasSqlStatementCancelledByCancellationToken(
                exception,
                cancellationToken
            )
        )
        {
            throw new OperationCanceledException(cancellationToken);
        }
    }

    /// <inheritdoc />
    public Int32 DeleteEntity<TEntity>(
        DbConnection connection,
        TEntity entity,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entity);

        var entityTypeMetadata = EntityHelper.GetEntityTypeMetadata(typeof(TEntity));

        var (command, parameters) = this.CreateDeleteEntityCommand(connection, transaction, entityTypeMetadata);
        var cancellationTokenRegistration = DbCommandHelper.RegisterDbCommandCancellation(command, cancellationToken);

        this.PopulateParametersFromEntityProperties(entityTypeMetadata, parameters, entity);

        using (command)
        using (cancellationTokenRegistration)
        {
            try
            {
                DbConnectionExtensions.OnBeforeExecutingCommand(command, []);

                return command.ExecuteNonQuery();
            }
            catch (Exception exception) when (this.databaseAdapter.WasSqlStatementCancelledByCancellationToken(
                    exception,
                    cancellationToken
                )
            )
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public async Task<Int32> DeleteEntityAsync<TEntity>(
        DbConnection connection,
        TEntity entity,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entity);

        var entityTypeMetadata = EntityHelper.GetEntityTypeMetadata(typeof(TEntity));

        var (command, parameters) = this.CreateDeleteEntityCommand(connection, transaction, entityTypeMetadata);
        var cancellationTokenRegistration = DbCommandHelper.RegisterDbCommandCancellation(command, cancellationToken);

        this.PopulateParametersFromEntityProperties(entityTypeMetadata, parameters, entity);

        using (command)
        using (cancellationTokenRegistration)
        {
            try
            {
                DbConnectionExtensions.OnBeforeExecutingCommand(command, []);

                return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (this.databaseAdapter.WasSqlStatementCancelledByCancellationToken(
                    exception,
                    cancellationToken
                )
            )
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public Int32 InsertEntities<TEntity>(
        DbConnection connection,
        IEnumerable<TEntity> entities,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entities);

        var entityTypeMetadata = EntityHelper.GetEntityTypeMetadata(typeof(TEntity));

        var (command, parameters) = this.CreateInsertEntityCommand(
            connection,
            transaction,
            entityTypeMetadata
        );
        var cancellationTokenRegistration = DbCommandHelper.RegisterDbCommandCancellation(command, cancellationToken);

        using (command)
        using (cancellationTokenRegistration)
        {
            var totalNumberOfAffectedRows = 0;

            try
            {
                foreach (var entity in entities)
                {
                    if (entity is null)
                    {
                        continue;
                    }

                    this.PopulateParametersFromEntityProperties(entityTypeMetadata, parameters, entity);

                    DbConnectionExtensions.OnBeforeExecutingCommand(command, []);

                    using var reader = command.ExecuteReader(CommandBehavior.SequentialAccess);

                    UpdateDatabaseGeneratedProperties(entityTypeMetadata, reader, entity, cancellationToken);

                    totalNumberOfAffectedRows += reader.RecordsAffected;
                }
            }
            catch (Exception exception) when (
                this.databaseAdapter.WasSqlStatementCancelledByCancellationToken(exception, cancellationToken)
            )
            {
                throw new OperationCanceledException(cancellationToken);
            }

            return totalNumberOfAffectedRows;
        }
    }

    /// <inheritdoc />
    public async Task<Int32> InsertEntitiesAsync<TEntity>(
        DbConnection connection,
        IEnumerable<TEntity> entities,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entities);

        var entityTypeMetadata = EntityHelper.GetEntityTypeMetadata(typeof(TEntity));

        var (command, parameters) = this.CreateInsertEntityCommand(
            connection,
            transaction,
            entityTypeMetadata
        );
        var cancellationTokenRegistration = DbCommandHelper.RegisterDbCommandCancellation(command, cancellationToken);

        using (command)
        using (cancellationTokenRegistration)
        {
            var totalNumberOfAffectedRows = 0;

            try
            {
                foreach (var entity in entities)
                {
                    if (entity is null)
                    {
                        continue;
                    }

                    this.PopulateParametersFromEntityProperties(entityTypeMetadata, parameters, entity);

                    DbConnectionExtensions.OnBeforeExecutingCommand(command, []);

#pragma warning disable CA2007
                    await using var reader = await command
                        .ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
#pragma warning restore CA2007

                    await UpdateDatabaseGeneratedPropertiesAsync(
                        entityTypeMetadata,
                        reader,
                        entity,
                        cancellationToken
                    ).ConfigureAwait(false);

                    totalNumberOfAffectedRows += reader.RecordsAffected;
                }
            }
            catch (Exception exception) when (
                this.databaseAdapter.WasSqlStatementCancelledByCancellationToken(exception, cancellationToken)
            )
            {
                throw new OperationCanceledException(cancellationToken);
            }

            return totalNumberOfAffectedRows;
        }
    }

    /// <inheritdoc />
    public Int32 InsertEntity<TEntity>(
        DbConnection connection,
        TEntity entity,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entity);

        var entityTypeMetadata = EntityHelper.GetEntityTypeMetadata(typeof(TEntity));

        var (command, parameters) = this.CreateInsertEntityCommand(
            connection,
            transaction,
            entityTypeMetadata
        );
        var cancellationTokenRegistration = DbCommandHelper.RegisterDbCommandCancellation(command, cancellationToken);

        using (command)
        using (cancellationTokenRegistration)
        {
            try
            {
                this.PopulateParametersFromEntityProperties(entityTypeMetadata, parameters, entity);

                DbConnectionExtensions.OnBeforeExecutingCommand(command, []);

                using var reader = command.ExecuteReader(CommandBehavior.SequentialAccess);

                UpdateDatabaseGeneratedProperties(entityTypeMetadata, reader, entity, cancellationToken);

                return reader.RecordsAffected;
            }
            catch (Exception exception) when (
                this.databaseAdapter.WasSqlStatementCancelledByCancellationToken(exception, cancellationToken)
            )
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public async Task<Int32> InsertEntityAsync<TEntity>(
        DbConnection connection,
        TEntity entity,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entity);

        var entityTypeMetadata = EntityHelper.GetEntityTypeMetadata(typeof(TEntity));

        var (command, parameters) = this.CreateInsertEntityCommand(
            connection,
            transaction,
            entityTypeMetadata
        );
        var cancellationTokenRegistration = DbCommandHelper.RegisterDbCommandCancellation(command, cancellationToken);

        using (command)
        using (cancellationTokenRegistration)
        {
            try
            {
                this.PopulateParametersFromEntityProperties(entityTypeMetadata, parameters, entity);

                DbConnectionExtensions.OnBeforeExecutingCommand(command, []);

#pragma warning disable CA2007
                await using var reader = await command
                    .ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
#pragma warning restore CA2007

                await UpdateDatabaseGeneratedPropertiesAsync(entityTypeMetadata, reader, entity, cancellationToken)
                    .ConfigureAwait(false);

                return reader.RecordsAffected;
            }
            catch (Exception exception) when (
                this.databaseAdapter.WasSqlStatementCancelledByCancellationToken(exception, cancellationToken)
            )
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public Int32 UpdateEntities<TEntity>(
        DbConnection connection,
        IEnumerable<TEntity> entities,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entities);

        var entityTypeMetadata = EntityHelper.GetEntityTypeMetadata(typeof(TEntity));

        var (command, parameters) = this.CreateUpdateEntityCommand(
            connection,
            transaction,
            entityTypeMetadata
        );
        var cancellationTokenRegistration = DbCommandHelper.RegisterDbCommandCancellation(command, cancellationToken);

        using (command)
        using (cancellationTokenRegistration)
        {
            var totalNumberOfAffectedRows = 0;

            try
            {
                foreach (var entity in entities)
                {
                    if (entity is null)
                    {
                        continue;
                    }

                    this.PopulateParametersFromEntityProperties(entityTypeMetadata, parameters, entity);

                    DbConnectionExtensions.OnBeforeExecutingCommand(command, []);

                    using var reader = command.ExecuteReader(CommandBehavior.SequentialAccess);

                    UpdateDatabaseGeneratedProperties(entityTypeMetadata, reader, entity, cancellationToken);

                    totalNumberOfAffectedRows += reader.RecordsAffected;
                }
            }
            catch (Exception exception) when (
                this.databaseAdapter.WasSqlStatementCancelledByCancellationToken(exception, cancellationToken)
            )
            {
                throw new OperationCanceledException(cancellationToken);
            }

            return totalNumberOfAffectedRows;
        }
    }

    /// <inheritdoc />
    public async Task<Int32> UpdateEntitiesAsync<TEntity>(
        DbConnection connection,
        IEnumerable<TEntity> entities,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entities);

        var entityTypeMetadata = EntityHelper.GetEntityTypeMetadata(typeof(TEntity));

        var (command, parameters) = this.CreateUpdateEntityCommand(
            connection,
            transaction,
            entityTypeMetadata
        );
        var cancellationTokenRegistration = DbCommandHelper.RegisterDbCommandCancellation(command, cancellationToken);

        using (command)
        using (cancellationTokenRegistration)
        {
            var totalNumberOfAffectedRows = 0;

            try
            {
                foreach (var entity in entities)
                {
                    if (entity is null)
                    {
                        continue;
                    }

                    this.PopulateParametersFromEntityProperties(entityTypeMetadata, parameters, entity);

                    DbConnectionExtensions.OnBeforeExecutingCommand(command, []);

#pragma warning disable CA2007
                    await using var reader = await command
                        .ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
#pragma warning restore CA2007

                    await UpdateDatabaseGeneratedPropertiesAsync(
                        entityTypeMetadata,
                        reader,
                        entity,
                        cancellationToken
                    ).ConfigureAwait(false);

                    totalNumberOfAffectedRows += reader.RecordsAffected;
                }
            }
            catch (Exception exception) when (
                this.databaseAdapter.WasSqlStatementCancelledByCancellationToken(exception, cancellationToken)
            )
            {
                throw new OperationCanceledException(cancellationToken);
            }

            return totalNumberOfAffectedRows;
        }
    }

    /// <inheritdoc />
    public Int32 UpdateEntity<TEntity>(
        DbConnection connection,
        TEntity entity,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entity);

        var entityTypeMetadata = EntityHelper.GetEntityTypeMetadata(typeof(TEntity));

        var (command, parameters) = this.CreateUpdateEntityCommand(
            connection,
            transaction,
            entityTypeMetadata
        );
        var cancellationTokenRegistration = DbCommandHelper.RegisterDbCommandCancellation(command, cancellationToken);

        using (command)
        using (cancellationTokenRegistration)
        {
            try
            {
                this.PopulateParametersFromEntityProperties(entityTypeMetadata, parameters, entity);

                DbConnectionExtensions.OnBeforeExecutingCommand(command, []);

                using var reader = command.ExecuteReader(CommandBehavior.SequentialAccess);

                UpdateDatabaseGeneratedProperties(entityTypeMetadata, reader, entity, cancellationToken);

                return reader.RecordsAffected;
            }
            catch (Exception exception) when (
                this.databaseAdapter.WasSqlStatementCancelledByCancellationToken(exception, cancellationToken)
            )
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public async Task<Int32> UpdateEntityAsync<TEntity>(
        DbConnection connection,
        TEntity entity,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entity);

        var entityTypeMetadata = EntityHelper.GetEntityTypeMetadata(typeof(TEntity));

        var (command, parameters) = this.CreateUpdateEntityCommand(
            connection,
            transaction,
            entityTypeMetadata
        );
        var cancellationTokenRegistration = DbCommandHelper.RegisterDbCommandCancellation(command, cancellationToken);

        using (command)
        using (cancellationTokenRegistration)
        {
            try
            {
                this.PopulateParametersFromEntityProperties(entityTypeMetadata, parameters, entity);

                DbConnectionExtensions.OnBeforeExecutingCommand(command, []);

#pragma warning disable CA2007
                await using var reader = await command
                    .ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken)
                    .ConfigureAwait(false);
#pragma warning restore CA2007

                await UpdateDatabaseGeneratedPropertiesAsync(entityTypeMetadata, reader, entity, cancellationToken)
                    .ConfigureAwait(false);

                return reader.RecordsAffected;
            }
            catch (Exception exception) when (
                this.databaseAdapter.WasSqlStatementCancelledByCancellationToken(exception, cancellationToken)
            )
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }
    }

    /// <summary>
    /// Builds a temporary table containing the keys of the provided entities.
    /// </summary>
    /// <typeparam name="TEntity">The type of entities for which the table is built.</typeparam>
    /// <param name="connection">The database connection to use to build the table.</param>
    /// <param name="keysTableName">The name of the table to build.</param>
    /// <param name="entities">The entities whose keys should be stored in the table.</param>
    /// <param name="entityTypeMetadata">The metadata for the entity type.</param>
    /// <param name="transaction">The database transaction within to perform the operation.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    private void BuildEntityKeysTemporaryTable<TEntity>(
        SqlConnection connection,
        String keysTableName,
        List<TEntity> entities,
        EntityTypeMetadata entityTypeMetadata,
        SqlTransaction? transaction,
        CancellationToken cancellationToken
    )
    {
        connection.ExecuteNonQuery(
            this.GetCreateEntityKeysTemporaryTableSqlCode(entityTypeMetadata).Replace(
                "###__________EntityKeys__________###",
                $"[#{keysTableName}]",
                StringComparison.Ordinal
            ),
            transaction,
            cancellationToken: cancellationToken
        );

        using var keysTable = new DataTable();

        foreach (var property in entityTypeMetadata.KeyProperties)
        {
            var columnType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            keysTable.Columns.Add(property.PropertyName, columnType);
        }

        foreach (var entity in entities)
        {
            if (entity is null)
            {
                continue;
            }

            var keysRow = keysTable.NewRow();

            foreach (var keyProperty in entityTypeMetadata.KeyProperties)
            {
                keysRow[keyProperty.PropertyName] = keyProperty.PropertyGetter!(entity);
            }

            keysTable.Rows.Add(keysRow);
        }

        using var sqlBulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction);
        sqlBulkCopy.BatchSize = 0;
        sqlBulkCopy.DestinationTableName = "#" + keysTableName;
        sqlBulkCopy.WriteToServer(keysTable);
    }

    /// <summary>
    /// Asynchronously builds a temporary table containing the keys of the provided entities.
    /// </summary>
    /// <typeparam name="TEntity">The type of entities for which the table is built.</typeparam>
    /// <param name="connection">The database connection to use to build the table.</param>
    /// <param name="keysTableName">The name of the table to build.</param>
    /// <param name="entities">The entities whose keys should be stored in the table.</param>
    /// <param name="entityTypeMetadata">The metadata for the entity type.</param>
    /// <param name="transaction">The database transaction within to perform the operation.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task BuildEntityKeysTemporaryTableAsync<TEntity>(
        SqlConnection connection,
        String keysTableName,
        List<TEntity> entities,
        EntityTypeMetadata entityTypeMetadata,
        SqlTransaction? transaction,
        CancellationToken cancellationToken
    )
    {
        await connection.ExecuteNonQueryAsync(
            this.GetCreateEntityKeysTemporaryTableSqlCode(entityTypeMetadata).Replace(
                "###__________EntityKeys__________###",
                $"[#{keysTableName}]",
                StringComparison.Ordinal
            ),
            transaction,
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        using var keysTable = new DataTable();

        foreach (var property in entityTypeMetadata.KeyProperties)
        {
            var columnType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            keysTable.Columns.Add(property.PropertyName, columnType);
        }

        foreach (var entity in entities)
        {
            if (entity is null)
            {
                continue;
            }

            var keysRow = keysTable.NewRow();

            foreach (var keyProperty in entityTypeMetadata.KeyProperties)
            {
                keysRow[keyProperty.PropertyName] = keyProperty.PropertyGetter!(entity);
            }

            keysTable.Rows.Add(keysRow);
        }

        using var sqlBulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction);
        sqlBulkCopy.BatchSize = 0;
        sqlBulkCopy.DestinationTableName = "#" + keysTableName;
        await sqlBulkCopy.WriteToServerAsync(keysTable, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a command to delete an entity.
    /// </summary>
    /// <param name="connection">The connection to use to create the command.</param>
    /// <param name="transaction">The transaction to assign to the command.</param>
    /// <param name="entityTypeMetadata">The metadata for the entity type.</param>
    /// <returns>
    /// A tuple containing the created command and the parameters for the key property values of the entity to delete.
    /// </returns>
    private (DbCommand Command, List<DbParameter> Parameters) CreateDeleteEntityCommand(
        DbConnection connection,
        DbTransaction? transaction,
        EntityTypeMetadata entityTypeMetadata
    )
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entityTypeMetadata);

        var command = DbConnectionExtensions.DbCommandFactory.CreateDbCommand(
            connection,
            this.GetDeleteEntitySqlCode(entityTypeMetadata),
            transaction
        );

        var parameters = new List<DbParameter>();

        foreach (var property in entityTypeMetadata.KeyProperties)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = property.PropertyName;
            parameters.Add(parameter);
            command.Parameters.Add(parameter);
        }

        return (command, parameters);
    }

    /// <summary>
    /// Creates a command to insert an entity.
    /// </summary>
    /// <param name="connection">The connection to use to create the command.</param>
    /// <param name="transaction">The transaction to assign to the command.</param>
    /// <param name="entityTypeMetadata">The metadata for the entity type.</param>
    /// <returns>
    /// A tuple containing the created command and the parameters for the property values of the entity to insert.
    /// </returns>
    private (DbCommand Command, List<DbParameter> Parameters) CreateInsertEntityCommand(
        DbConnection connection,
        DbTransaction? transaction,
        EntityTypeMetadata entityTypeMetadata
    )
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entityTypeMetadata);

        var command = DbConnectionExtensions.DbCommandFactory.CreateDbCommand(
            connection,
            this.GetInsertEntitySqlCode(entityTypeMetadata),
            transaction
        );

        var parameters = new List<DbParameter>();

        foreach (var property in entityTypeMetadata.MappedProperties)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = property.PropertyName;
            parameters.Add(parameter);
            command.Parameters.Add(parameter);
        }

        return (command, parameters);
    }

    /// <summary>
    /// Creates a command to update an entity.
    /// </summary>
    /// <param name="connection">The connection to use to create the command.</param>
    /// <param name="transaction">The transaction to assign to the command.</param>
    /// <param name="entityTypeMetadata">The metadata for the entity type.</param>
    /// <returns>
    /// A tuple containing the created command and the parameters for the property values of the entity to update.
    /// </returns>
    private (DbCommand Command, List<DbParameter> Parameters) CreateUpdateEntityCommand(
        DbConnection connection,
        DbTransaction? transaction,
        EntityTypeMetadata entityTypeMetadata
    )
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entityTypeMetadata);

        var command = DbConnectionExtensions.DbCommandFactory.CreateDbCommand(
            connection,
            this.GetUpdateEntitySqlCode(entityTypeMetadata),
            transaction
        );

        var parameters = new List<DbParameter>();

        foreach (var property in entityTypeMetadata.MappedProperties)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = property.PropertyName;
            parameters.Add(parameter);
            command.Parameters.Add(parameter);
        }

        return (command, parameters);
    }

    /// <summary>
    /// Gets the SQL code to create a temporary table for the keys of the provided entity type.
    /// </summary>
    /// <param name="entityTypeMetadata">The metadata for the entity type to create the temporary table for.</param>
    /// <returns>The SQL code to create the temporary table.</returns>
    private String GetCreateEntityKeysTemporaryTableSqlCode(EntityTypeMetadata entityTypeMetadata) =>
        this.createEntityKeysTemporaryTableSqlCodePerEntityType.GetOrAdd(
            entityTypeMetadata.EntityType,
            _ =>
            {
                if (entityTypeMetadata.KeyProperties.Count == 0)
                {
                    ThrowHelper.ThrowEntityTypeHasNoKeyPropertyException(entityTypeMetadata.EntityType);
                }

                using var createKeysTableSqlBuilder = new ValueStringBuilder(stackalloc Char[200]);

                createKeysTableSqlBuilder.AppendLine("CREATE TABLE ###__________EntityKeys__________###");

                createKeysTableSqlBuilder.Append(Constants.Indent);
                createKeysTableSqlBuilder.Append("(");

                var prependSeparator = false;

                foreach (var property in entityTypeMetadata.KeyProperties)
                {
                    if (prependSeparator)
                    {
                        createKeysTableSqlBuilder.Append(", ");
                    }

                    createKeysTableSqlBuilder.Append('[');
                    createKeysTableSqlBuilder.Append(property.PropertyName);
                    createKeysTableSqlBuilder.Append("] ");
                    createKeysTableSqlBuilder.Append(
                        this.databaseAdapter.GetDataType(
                            property.PropertyType,
                            DbConnectionExtensions.EnumSerializationMode
                        )
                    );

                    prependSeparator = true;
                }

                createKeysTableSqlBuilder.AppendLine(")");

                return createKeysTableSqlBuilder.ToString();
            }
        );

    /// <summary>
    /// Gets the SQL code to delete an entity of the provided entity type.
    /// </summary>
    /// <param name="entityTypeMetadata">The metadata for the entity type to delete.</param>
    /// <returns>The SQL code to delete an entity of the specified type.</returns>
    private String GetDeleteEntitySqlCode(EntityTypeMetadata entityTypeMetadata) =>
        this.entityDeleteSqlCodePerEntityType.GetOrAdd(
            entityTypeMetadata.EntityType,
            _ =>
            {
                if (entityTypeMetadata.KeyProperties.Count == 0)
                {
                    ThrowHelper.ThrowEntityTypeHasNoKeyPropertyException(entityTypeMetadata.EntityType);
                }

                using var sqlBuilder = new ValueStringBuilder(stackalloc Char[500]);

                sqlBuilder.AppendLine("DELETE FROM");

                sqlBuilder.Append(Constants.Indent);
                sqlBuilder.Append("[");
                sqlBuilder.Append(entityTypeMetadata.TableName);
                sqlBuilder.AppendLine("]");

                sqlBuilder.AppendLine("WHERE");

                sqlBuilder.Append(Constants.Indent);

                var prependSeparator = false;

                foreach (var keyProperty in entityTypeMetadata.KeyProperties)
                {
                    if (prependSeparator)
                    {
                        sqlBuilder.Append(" AND ");
                    }

                    sqlBuilder.Append('[');
                    sqlBuilder.Append(keyProperty.PropertyName);
                    sqlBuilder.Append("] = @");
                    sqlBuilder.Append(keyProperty.PropertyName);

                    prependSeparator = true;
                }

                sqlBuilder.AppendLine();

                return sqlBuilder.ToString();
            }
        );

    /// <summary>
    /// Gets the SQL code to insert an entity of the provided entity type.
    /// </summary>
    /// <param name="entityTypeMetadata">The metadata for the entity type to insert.</param>
    /// <returns>The SQL code to insert an entity of the specified type.</returns>
    private String GetInsertEntitySqlCode(EntityTypeMetadata entityTypeMetadata) =>
        this.entityInsertSqlCodePerEntityType.GetOrAdd(
            entityTypeMetadata.EntityType,
            _ =>
            {
                using var sqlBuilder = new ValueStringBuilder(stackalloc Char[500]);

                sqlBuilder.Append("INSERT INTO [");
                sqlBuilder.Append(entityTypeMetadata.TableName);
                sqlBuilder.AppendLine("]");

                sqlBuilder.Append(Constants.Indent);
                sqlBuilder.Append("(");

                var prependSeparator = false;

                foreach (var property in entityTypeMetadata.InsertProperties)
                {
                    if (prependSeparator)
                    {
                        sqlBuilder.Append(", ");
                    }

                    sqlBuilder.Append('[');
                    sqlBuilder.Append(property.PropertyName);
                    sqlBuilder.Append(']');

                    prependSeparator = true;
                }

                sqlBuilder.AppendLine(")");

                if (entityTypeMetadata.DatabaseGeneratedProperties.Count > 0)
                {
                    sqlBuilder.AppendLine("OUTPUT");

                    sqlBuilder.Append(Constants.Indent);

                    prependSeparator = false;

                    foreach (var property in entityTypeMetadata.DatabaseGeneratedProperties)
                    {
                        if (prependSeparator)
                        {
                            sqlBuilder.Append(", ");
                        }

                        sqlBuilder.Append("INSERTED.[");
                        sqlBuilder.Append(property.PropertyName);
                        sqlBuilder.Append(']');
                        prependSeparator = true;
                    }

                    sqlBuilder.AppendLine();
                }

                sqlBuilder.AppendLine("VALUES");

                sqlBuilder.Append(Constants.Indent);
                sqlBuilder.Append("(");

                prependSeparator = false;

                foreach (var property in entityTypeMetadata.InsertProperties)
                {
                    if (prependSeparator)
                    {
                        sqlBuilder.Append(", ");
                    }

                    sqlBuilder.Append("@");
                    sqlBuilder.Append(property.PropertyName);

                    prependSeparator = true;
                }

                sqlBuilder.AppendLine(")");

                return sqlBuilder.ToString();
            }
        );

    /// <summary>
    /// Gets the SQL code to update an entity of the provided entity type.
    /// </summary>
    /// <param name="entityTypeMetadata">The metadata for the entity type to update.</param>
    /// <returns>The SQL code to update an entity of the specified type.</returns>
    private String GetUpdateEntitySqlCode(EntityTypeMetadata entityTypeMetadata) =>
        this.entityUpdateSqlCodePerEntityType.GetOrAdd(
            entityTypeMetadata.EntityType,
            _ =>
            {
                if (entityTypeMetadata.KeyProperties.Count == 0)
                {
                    ThrowHelper.ThrowEntityTypeHasNoKeyPropertyException(entityTypeMetadata.EntityType);
                }

                using var sqlBuilder = new ValueStringBuilder(stackalloc Char[500]);

                sqlBuilder.AppendLine("UPDATE");

                sqlBuilder.Append(Constants.Indent);
                sqlBuilder.Append("[");
                sqlBuilder.Append(entityTypeMetadata.TableName);
                sqlBuilder.AppendLine("]");

                sqlBuilder.AppendLine("SET");

                sqlBuilder.Append(Constants.Indent);

                var prependSeparator = false;

                foreach (var property in entityTypeMetadata.UpdateProperties)
                {
                    if (prependSeparator)
                    {
                        sqlBuilder.Append(", ");
                    }

                    sqlBuilder.Append('[');
                    sqlBuilder.Append(property.PropertyName);
                    sqlBuilder.Append("] = @");
                    sqlBuilder.Append(property.PropertyName);

                    prependSeparator = true;
                }

                sqlBuilder.AppendLine();

                if (entityTypeMetadata.DatabaseGeneratedProperties.Count > 0)
                {
                    sqlBuilder.AppendLine("OUTPUT");

                    sqlBuilder.Append(Constants.Indent);

                    prependSeparator = false;

                    foreach (var property in entityTypeMetadata.DatabaseGeneratedProperties)
                    {
                        if (prependSeparator)
                        {
                            sqlBuilder.Append(", ");
                        }

                        sqlBuilder.Append("INSERTED.[");
                        sqlBuilder.Append(property.PropertyName);
                        sqlBuilder.Append(']');
                        prependSeparator = true;
                    }

                    sqlBuilder.AppendLine();
                }

                sqlBuilder.AppendLine("WHERE");

                sqlBuilder.Append(Constants.Indent);

                prependSeparator = false;

                foreach (var property in entityTypeMetadata.KeyProperties)
                {
                    if (prependSeparator)
                    {
                        sqlBuilder.Append(" AND ");
                    }

                    sqlBuilder.Append('[');
                    sqlBuilder.Append(property.PropertyName);
                    sqlBuilder.Append("] = ");
                    sqlBuilder.Append('@');
                    sqlBuilder.Append(property.PropertyName);

                    prependSeparator = true;
                }

                sqlBuilder.AppendLine();

                return sqlBuilder.ToString();
            }
        );

    /// <summary>
    /// Populates the provided parameters with the property values of the provided entity.
    /// </summary>
    /// <param name="entityTypeMetadata">The metadata for the entity type.</param>
    /// <param name="parameters">The parameters to populate.</param>
    /// <param name="entity">The entity from which to get the property values.</param>
    private void PopulateParametersFromEntityProperties(
        EntityTypeMetadata entityTypeMetadata,
        List<DbParameter> parameters,
        Object entity
    )
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(entity);

        foreach (var parameter in parameters)
        {
            var property = entityTypeMetadata.AllPropertiesByPropertyName[parameter.ParameterName];
            var propertyValue = property.PropertyGetter!(entity);
            this.databaseAdapter.BindParameterValue(parameter, propertyValue);
        }
    }

    /// <summary>
    /// Updates the database generated properties of the provided entity from the provided data reader.
    /// </summary>
    /// <param name="entityTypeMetadata">The metadata for the entity type.</param>
    /// <param name="reader">The data reader from which to read the values for the properties.</param>
    /// <param name="entity">The entity to update.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    private static void UpdateDatabaseGeneratedProperties(
        EntityTypeMetadata entityTypeMetadata,
        DbDataReader reader,
        Object entity,
        CancellationToken cancellationToken
    )
    {
        if (entityTypeMetadata.DatabaseGeneratedProperties.Count > 0 && reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (var i = 0; i < entityTypeMetadata.DatabaseGeneratedProperties.Count; i++)
            {
                var property = entityTypeMetadata.DatabaseGeneratedProperties[i];

                if (!property.CanWrite)
                {
                    continue;
                }

                var value = reader.GetValue(i);

                property.PropertySetter!(entity, value);
            }
        }
    }

    /// <summary>
    /// Asynchronously updates the database generated properties of the provided entity from the provided data
    /// reader.
    /// </summary>
    /// <param name="entityTypeMetadata">The metadata for the entity type.</param>
    /// <param name="reader">The data reader from which to read the values for the properties.</param>
    /// <param name="entity">The entity to update.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private static async Task UpdateDatabaseGeneratedPropertiesAsync(
        EntityTypeMetadata entityTypeMetadata,
        DbDataReader reader,
        Object entity,
        CancellationToken cancellationToken
    )
    {
        if (
            entityTypeMetadata.DatabaseGeneratedProperties.Count > 0 &&
            await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
        )
        {
            for (var i = 0; i < entityTypeMetadata.DatabaseGeneratedProperties.Count; i++)
            {
                var property = entityTypeMetadata.DatabaseGeneratedProperties[i];

                if (!property.CanWrite)
                {
                    continue;
                }

                var value = reader.GetValue(i);

                property.PropertySetter!(entity, value);
            }
        }
    }

    private readonly ConcurrentDictionary<Type, String> createEntityKeysTemporaryTableSqlCodePerEntityType = new();
    private readonly SqlServerDatabaseAdapter databaseAdapter;
    private readonly ConcurrentDictionary<Type, String> entityDeleteSqlCodePerEntityType = new();
    private readonly ConcurrentDictionary<Type, String> entityInsertSqlCodePerEntityType = new();
    private readonly ConcurrentDictionary<Type, String> entityUpdateSqlCodePerEntityType = new();
    private const Int32 BulkDeleteThreshold = 10;
}
