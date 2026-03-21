// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using LinkDotNet.StringBuilder;
using RentADeveloper.DbConnectionPlus.Converters;
using RentADeveloper.DbConnectionPlus.DbCommands;
using RentADeveloper.DbConnectionPlus.Entities;

namespace RentADeveloper.DbConnectionPlus.DatabaseAdapters.Oracle;

/// <summary>
/// The entity manipulator for PostgreSQL.
/// </summary>
internal class OracleEntityManipulator : IEntityManipulator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OracleEntityManipulator" /> class.
    /// </summary>
    /// <param name="databaseAdapter">The database adapter to use to manipulate entities.</param>
    public OracleEntityManipulator(OracleDatabaseAdapter databaseAdapter) =>
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

        var entityTypeMetadata = EntityHelper.GetEntityTypeMetadata(typeof(TEntity));

        var (command, parameters) = this.CreateDeleteEntityCommand(connection, transaction, entityTypeMetadata);
        var cancellationTokenRegistration = DbCommandHelper.RegisterDbCommandCancellation(command, cancellationToken);

        var totalNumberOfAffectedRows = 0;

        using (command)
        using (cancellationTokenRegistration)
        {
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

                    var numberOfAffectedRows = command.ExecuteNonQuery();

                    if (numberOfAffectedRows != 1)
                    {
                        ThrowHelper.ThrowDatabaseOperationAffectedUnexpectedNumberOfRowsException(
                            1,
                            numberOfAffectedRows,
                            entity
                        );
                    }

                    totalNumberOfAffectedRows += numberOfAffectedRows;
                }
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

        var entityTypeMetadata = EntityHelper.GetEntityTypeMetadata(typeof(TEntity));

        var (command, parameters) = this.CreateDeleteEntityCommand(connection, transaction, entityTypeMetadata);
        var cancellationTokenRegistration = DbCommandHelper.RegisterDbCommandCancellation(command, cancellationToken);

        var totalNumberOfAffectedRows = 0;

        using (command)
        using (cancellationTokenRegistration)
        {
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

                    var numberOfAffectedRows =
                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                    if (numberOfAffectedRows != 1)
                    {
                        ThrowHelper.ThrowDatabaseOperationAffectedUnexpectedNumberOfRowsException(
                            1,
                            numberOfAffectedRows,
                            entity
                        );
                    }

                    totalNumberOfAffectedRows += numberOfAffectedRows;
                }
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

                var numberOfAffectedRows = command.ExecuteNonQuery();

                if (numberOfAffectedRows != 1)
                {
                    ThrowHelper.ThrowDatabaseOperationAffectedUnexpectedNumberOfRowsException(
                        1,
                        numberOfAffectedRows,
                        entity
                    );
                }

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

                var numberOfAffectedRows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                if (numberOfAffectedRows != 1)
                {
                    ThrowHelper.ThrowDatabaseOperationAffectedUnexpectedNumberOfRowsException(
                        1,
                        numberOfAffectedRows,
                        entity
                    );
                }

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

                    totalNumberOfAffectedRows += command.ExecuteNonQuery();

                    var outputParameters = parameters.Where(a => a.Direction == ParameterDirection.Output).ToArray();

                    UpdateDatabaseGeneratedProperties(entityTypeMetadata, outputParameters, entity);
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

                    totalNumberOfAffectedRows +=
                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                    var outputParameters = parameters.Where(a => a.Direction == ParameterDirection.Output).ToArray();

                    UpdateDatabaseGeneratedProperties(entityTypeMetadata, outputParameters, entity);
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

                var numberOfAffectedRows = command.ExecuteNonQuery();

                var outputParameters = parameters.Where(a => a.Direction == ParameterDirection.Output).ToArray();

                UpdateDatabaseGeneratedProperties(entityTypeMetadata, outputParameters, entity);

                return numberOfAffectedRows;
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

                var numberOfAffectedRows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                var outputParameters = parameters.Where(a => a.Direction == ParameterDirection.Output).ToArray();

                UpdateDatabaseGeneratedProperties(entityTypeMetadata, outputParameters, entity);

                return numberOfAffectedRows;
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

                    var numberOfAffectedRows = command.ExecuteNonQuery();

                    if (numberOfAffectedRows != 1)
                    {
                        ThrowHelper.ThrowDatabaseOperationAffectedUnexpectedNumberOfRowsException(
                            1,
                            numberOfAffectedRows,
                            entity
                        );
                    }

                    totalNumberOfAffectedRows += numberOfAffectedRows;

                    var outputParameters = parameters.Where(a => a.Direction == ParameterDirection.Output).ToArray();

                    UpdateDatabaseGeneratedProperties(entityTypeMetadata, outputParameters, entity);
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

                    var numberOfAffectedRows =
                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                    if (numberOfAffectedRows != 1)
                    {
                        ThrowHelper.ThrowDatabaseOperationAffectedUnexpectedNumberOfRowsException(
                            1,
                            numberOfAffectedRows,
                            entity
                        );
                    }

                    totalNumberOfAffectedRows += numberOfAffectedRows;

                    var outputParameters = parameters.Where(a => a.Direction == ParameterDirection.Output).ToArray();

                    UpdateDatabaseGeneratedProperties(entityTypeMetadata, outputParameters, entity);
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

                var numberOfAffectedRows = command.ExecuteNonQuery();

                if (numberOfAffectedRows != 1)
                {
                    ThrowHelper.ThrowDatabaseOperationAffectedUnexpectedNumberOfRowsException(
                        1,
                        numberOfAffectedRows,
                        entity
                    );
                }

                var outputParameters = parameters.Where(a => a.Direction == ParameterDirection.Output).ToArray();

                UpdateDatabaseGeneratedProperties(entityTypeMetadata, outputParameters, entity);

                return numberOfAffectedRows;
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

                var numberOfAffectedRows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                if (numberOfAffectedRows != 1)
                {
                    ThrowHelper.ThrowDatabaseOperationAffectedUnexpectedNumberOfRowsException(
                        1,
                        numberOfAffectedRows,
                        entity
                    );
                }

                var outputParameters = parameters.Where(a => a.Direction == ParameterDirection.Output).ToArray();

                UpdateDatabaseGeneratedProperties(entityTypeMetadata, outputParameters, entity);

                return numberOfAffectedRows;
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

        var command = connection.CreateCommand();

        command.CommandText = this.GetDeleteEntitySqlCode(entityTypeMetadata);
        command.Transaction = transaction;

        var parameters = new List<DbParameter>();

        var whereProperties = entityTypeMetadata.KeyProperties
            .Concat(entityTypeMetadata.ConcurrencyTokenProperties)
            .Concat(entityTypeMetadata.RowVersionProperties);

        foreach (var property in whereProperties)
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

        var command = connection.CreateCommand();

        command.CommandText = this.GetInsertEntitySqlCode(entityTypeMetadata);
        command.Transaction = transaction;

        var parameters = new List<DbParameter>();

        foreach (var property in entityTypeMetadata.InsertProperties)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = property.PropertyName;
            parameters.Add(parameter);
            command.Parameters.Add(parameter);
        }

        // Add output parameters for the database generated properties to retrieve their values after the insert:
        foreach (var property in entityTypeMetadata.DatabaseGeneratedProperties)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = "return_" + property.ColumnName;

            parameter.DbType = this.databaseAdapter.GetDbType(
                property.PropertyType,
                DbConnectionPlusConfiguration.Instance.EnumSerializationMode
            );

            parameter.Direction = ParameterDirection.Output;

            if (property.PropertyType == typeof(Byte[]))
            {
                // Use max size for byte arrays to actually retrieve the full value:
                parameter.Size = 32767;
            }

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

        var command = connection.CreateCommand();

        command.CommandText = this.GetUpdateEntitySqlCode(entityTypeMetadata);
        command.Transaction = transaction;

        var parameters = new List<DbParameter>();

        var whereProperties = entityTypeMetadata.KeyProperties
            .Concat(entityTypeMetadata.ConcurrencyTokenProperties)
            .Concat(entityTypeMetadata.RowVersionProperties);

        foreach (var property in entityTypeMetadata.UpdateProperties.Concat(whereProperties))
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = property.PropertyName;
            parameters.Add(parameter);
            command.Parameters.Add(parameter);
        }

        // Add output parameters for the database generated properties to retrieve their values after the update:
        foreach (var property in entityTypeMetadata.DatabaseGeneratedProperties)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = "return_" + property.ColumnName;

            parameter.DbType = this.databaseAdapter.GetDbType(
                property.PropertyType,
                DbConnectionPlusConfiguration.Instance.EnumSerializationMode
            );

            parameter.Direction = ParameterDirection.Output;

            if (property.PropertyType == typeof(Byte[]))
            {
                // Use max size for byte arrays to actually retrieve the full value:
                parameter.Size = 32767;
            }

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

                var whereProperties = entityTypeMetadata.KeyProperties
                    .Concat(entityTypeMetadata.ConcurrencyTokenProperties)
                    .Concat(entityTypeMetadata.RowVersionProperties);

                foreach (var keyProperty in whereProperties)
                {
                    if (prependSeparator)
                    {
                        sqlBuilder.Append(" AND ");
                    }

                    sqlBuilder.Append('"');
                    sqlBuilder.Append(keyProperty.ColumnName);
                    sqlBuilder.Append("\" =:\"");
                    sqlBuilder.Append(keyProperty.PropertyName);
                    sqlBuilder.Append('"');

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
                sqlBuilder.Append("(");

                prependSeparator = false;

                foreach (var property in entityTypeMetadata.InsertProperties)
                {
                    if (prependSeparator)
                    {
                        sqlBuilder.Append(", ");
                    }

                    sqlBuilder.Append(":\"");
                    sqlBuilder.Append(property.PropertyName);
                    sqlBuilder.Append('"');

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

                    sqlBuilder.AppendLine("INTO");

                    sqlBuilder.Append(Constants.Indent);

                    prependSeparator = false;

                    foreach (var property in entityTypeMetadata.DatabaseGeneratedProperties)
                    {
                        if (prependSeparator)
                        {
                            sqlBuilder.Append(", ");
                        }

                        sqlBuilder.Append(":\"return_");
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
                    sqlBuilder.Append("\" = :\"");
                    sqlBuilder.Append(property.PropertyName);
                    sqlBuilder.Append('"');

                    prependSeparator = true;
                }

                sqlBuilder.AppendLine();

                sqlBuilder.AppendLine("WHERE");

                sqlBuilder.Append(Constants.Indent);

                prependSeparator = false;

                var whereProperties = entityTypeMetadata.KeyProperties
                    .Concat(entityTypeMetadata.ConcurrencyTokenProperties)
                    .Concat(entityTypeMetadata.RowVersionProperties);

                foreach (var property in whereProperties)
                {
                    if (prependSeparator)
                    {
                        sqlBuilder.Append(" AND ");
                    }

                    sqlBuilder.Append('"');
                    sqlBuilder.Append(property.ColumnName);
                    sqlBuilder.Append("\" = ");
                    sqlBuilder.Append(":\"");
                    sqlBuilder.Append(property.PropertyName);
                    sqlBuilder.Append('"');

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

                    sqlBuilder.AppendLine("INTO");

                    sqlBuilder.Append(Constants.Indent);

                    prependSeparator = false;

                    foreach (var property in entityTypeMetadata.DatabaseGeneratedProperties)
                    {
                        if (prependSeparator)
                        {
                            sqlBuilder.Append(", ");
                        }

                        sqlBuilder.Append(":\"return_");
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

        foreach (var parameter in parameters.Where(a => a.Direction == ParameterDirection.Input))
        {
            var property = entityTypeMetadata.AllPropertiesByPropertyName[parameter.ParameterName];
            var propertyValue = property.PropertyGetter!(entity);
            this.databaseAdapter.BindParameterValue(parameter, propertyValue);
        }
    }

    /// <summary>
    /// Updates the database generated properties of the provided entity from the provided output parameters.
    /// </summary>
    /// <param name="entityTypeMetadata">The metadata for the entity type.</param>
    /// <param name="outputParameters">The output parameters from which to read the values for the properties.</param>
    /// <param name="entity">The entity to update.</param>
    private static void UpdateDatabaseGeneratedProperties(
        EntityTypeMetadata entityTypeMetadata,
        DbParameter[] outputParameters,
        Object entity
    )
    {
        if (entityTypeMetadata.DatabaseGeneratedProperties.Count > 0)
        {
            for (var i = 0; i < entityTypeMetadata.DatabaseGeneratedProperties.Count; i++)
            {
                var property = entityTypeMetadata.DatabaseGeneratedProperties[i];
                if (!property.CanWrite)
                {
                    continue;
                }

                var value = outputParameters[i].Value;

                value = ValueConverter.ConvertValueToType(value, property.PropertyType);

                property.PropertySetter!(entity, value);
            }
        }
    }

    private readonly OracleDatabaseAdapter databaseAdapter;
    private readonly ConcurrentDictionary<Type, String> entityDeleteSqlCodePerEntityType = new();
    private readonly ConcurrentDictionary<Type, String> entityInsertSqlCodePerEntityType = new();
    private readonly ConcurrentDictionary<Type, String> entityUpdateSqlCodePerEntityType = new();
}
