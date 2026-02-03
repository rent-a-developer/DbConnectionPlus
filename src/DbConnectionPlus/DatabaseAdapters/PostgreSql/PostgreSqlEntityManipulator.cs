// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using LinkDotNet.StringBuilder;
using RentADeveloper.DbConnectionPlus.Converters;
using RentADeveloper.DbConnectionPlus.DbCommands;
using RentADeveloper.DbConnectionPlus.Entities;

namespace RentADeveloper.DbConnectionPlus.DatabaseAdapters.PostgreSql;

/// <summary>
/// The entity manipulator for PostgreSQL.
/// </summary>
internal class PostgreSqlEntityManipulator : IEntityManipulator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlEntityManipulator" /> class.
    /// </summary>
    /// <param name="databaseAdapter">The database adapter to use to manipulate entities.</param>
    public PostgreSqlEntityManipulator(PostgreSqlDatabaseAdapter databaseAdapter) =>
        this.databaseAdapter = databaseAdapter;

    /// <inheritdoc />
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

    /// <inheritdoc />
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
                sqlBuilder.Append("\"");
                sqlBuilder.Append(entityTypeMetadata.TableName);
                sqlBuilder.AppendLine("\"");

                sqlBuilder.AppendLine("WHERE");

                sqlBuilder.Append(Constants.Indent);

                var prependSeparator = false;

                foreach (var keyProperty in entityTypeMetadata.KeyProperties)
                {
                    if (prependSeparator)
                    {
                        sqlBuilder.Append(" AND ");
                    }

                    sqlBuilder.Append('"');
                    sqlBuilder.Append(keyProperty.ColumnName);
                    sqlBuilder.Append("\" = @");
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

                sqlBuilder.Append("INSERT INTO \"");
                sqlBuilder.Append(entityTypeMetadata.TableName);
                sqlBuilder.AppendLine("\"");

                sqlBuilder.Append(Constants.Indent);
                sqlBuilder.Append("(");

                var prependSeparator = false;

                foreach (var property in entityTypeMetadata.InsertProperties)
                {
                    if (prependSeparator)
                    {
                        sqlBuilder.Append(", ");
                    }

                    sqlBuilder.Append('"');
                    sqlBuilder.Append(property.ColumnName);
                    sqlBuilder.Append('"');

                    prependSeparator = true;
                }

                sqlBuilder.AppendLine(")");

                sqlBuilder.AppendLine("VALUES");

                sqlBuilder.Append(Constants.Indent);
                sqlBuilder.Append('(');

                prependSeparator = false;

                foreach (var property in entityTypeMetadata.InsertProperties)
                {
                    if (prependSeparator)
                    {
                        sqlBuilder.Append(", ");
                    }

                    sqlBuilder.Append('@');
                    sqlBuilder.Append(property.PropertyName);

                    prependSeparator = true;
                }

                sqlBuilder.AppendLine(")");

                if (entityTypeMetadata.DatabaseGeneratedProperties.Count > 0)
                {
                    sqlBuilder.AppendLine("RETURNING");

                    sqlBuilder.Append(Constants.Indent);

                    prependSeparator = false;

                    foreach (var property in entityTypeMetadata.DatabaseGeneratedProperties)
                    {
                        if (prependSeparator)
                        {
                            sqlBuilder.Append(", ");
                        }

                        sqlBuilder.Append('"');
                        sqlBuilder.Append(property.ColumnName);
                        sqlBuilder.Append('"');
                        prependSeparator = true;
                    }

                    sqlBuilder.AppendLine();
                }

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
                sqlBuilder.Append("\"");
                sqlBuilder.Append(entityTypeMetadata.TableName);
                sqlBuilder.AppendLine("\"");

                sqlBuilder.AppendLine("SET");

                sqlBuilder.Append(Constants.Indent);

                var prependSeparator = false;

                foreach (var property in entityTypeMetadata.UpdateProperties)
                {
                    if (prependSeparator)
                    {
                        sqlBuilder.Append(", ");
                    }

                    sqlBuilder.Append('"');
                    sqlBuilder.Append(property.ColumnName);
                    sqlBuilder.Append("\" =  @");
                    sqlBuilder.Append(property.PropertyName);

                    prependSeparator = true;
                }

                sqlBuilder.AppendLine();

                sqlBuilder.AppendLine("WHERE");

                sqlBuilder.Append(Constants.Indent);

                prependSeparator = false;

                foreach (var property in entityTypeMetadata.KeyProperties)
                {
                    if (prependSeparator)
                    {
                        sqlBuilder.Append(" AND ");
                    }

                    sqlBuilder.Append('"');
                    sqlBuilder.Append(property.ColumnName);
                    sqlBuilder.Append("\" = ");
                    sqlBuilder.Append('@');
                    sqlBuilder.Append(property.PropertyName);

                    prependSeparator = true;
                }

                sqlBuilder.AppendLine();

                if (entityTypeMetadata.DatabaseGeneratedProperties.Count > 0)
                {
                    sqlBuilder.AppendLine("RETURNING");

                    sqlBuilder.Append(Constants.Indent);

                    prependSeparator = false;

                    foreach (var property in entityTypeMetadata.DatabaseGeneratedProperties)
                    {
                        if (prependSeparator)
                        {
                            sqlBuilder.Append(", ");
                        }

                        sqlBuilder.Append('"');
                        sqlBuilder.Append(property.ColumnName);
                        sqlBuilder.Append('"');
                        prependSeparator = true;
                    }

                    sqlBuilder.AppendLine();
                }

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

                value = ValueConverter.ConvertValueToType(value, property.PropertyType);

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

                value = ValueConverter.ConvertValueToType(value, property.PropertyType);

                property.PropertySetter!(entity, value);
            }
        }
    }

    private readonly PostgreSqlDatabaseAdapter databaseAdapter;
    private readonly ConcurrentDictionary<Type, String> entityDeleteSqlCodePerEntityType = new();
    private readonly ConcurrentDictionary<Type, String> entityInsertSqlCodePerEntityType = new();
    private readonly ConcurrentDictionary<Type, String> entityUpdateSqlCodePerEntityType = new();
}
