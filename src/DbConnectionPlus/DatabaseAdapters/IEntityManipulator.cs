// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using RentADeveloper.DbConnectionPlus.Converters;

namespace RentADeveloper.DbConnectionPlus.DatabaseAdapters;

/// <summary>
/// Provides CRUD database operations for entities.
/// </summary>
public interface IEntityManipulator
{
    /// <summary>
    /// Deletes the specified entities, identified by their key property/properties, from the database.
    /// </summary>
    /// <typeparam name="TEntity">The type of entities to delete.</typeparam>
    /// <param name="connection">The database connection to use to delete the entities.</param>
    /// <param name="entities">The entities to delete.</param>
    /// <param name="transaction">The database transaction within to perform the operation.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The number of rows affected by the delete operation.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="connection" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="entities" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <exception cref="ArgumentException">
    /// No instance property of the type <typeparamref name="TEntity" /> is denoted with a <see cref="KeyAttribute" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was cancelled via <paramref name="cancellationToken" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The table from which the entities will be deleted is determined by the <see cref="TableAttribute" />
    /// applied to the type <typeparamref name="TEntity" />.
    /// If this attribute is not present, the singular name of the type <typeparamref name="TEntity" /> is used.
    /// </para>
    /// <para>
    /// The type <typeparamref name="TEntity" /> must have at least one instance property denoted with a
    /// <see cref="KeyAttribute" />.
    /// </para>
    /// </remarks>
    public Int32 DeleteEntities<TEntity>(
        DbConnection connection,
        IEnumerable<TEntity> entities,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    ) =>
        0;

    /// <summary>
    /// Asynchronously deletes the specified entities, identified by their key property/properties, from the database.
    /// </summary>
    /// <typeparam name="TEntity">The type of entities to delete.</typeparam>
    /// <param name="connection">The database connection to use to delete the entities.</param>
    /// <param name="entities">The entities to delete.</param>
    /// <param name="transaction">The database transaction within to perform the operation.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// <see cref="Task{TResult}.Result" /> will contain the number of rows affected by the delete operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="connection" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="entities" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <exception cref="ArgumentException">
    /// No instance property of the type <typeparamref name="TEntity" /> is denoted with a <see cref="KeyAttribute" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was cancelled via <paramref name="cancellationToken" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The table from which the entities will be deleted is determined by the <see cref="TableAttribute" />
    /// applied to the type <typeparamref name="TEntity" />.
    /// If this attribute is not present, the singular name of the type <typeparamref name="TEntity" /> is used.
    /// </para>
    /// <para>
    /// The type <typeparamref name="TEntity" /> must have at least one instance property denoted with a
    /// <see cref="KeyAttribute" />.
    /// </para>
    /// </remarks>
    public Task<Int32> DeleteEntitiesAsync<TEntity>(
        DbConnection connection,
        IEnumerable<TEntity> entities,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    ) => Task.FromResult(0);

    /// <summary>
    /// Deletes the specified entity, identified by its key property / properties, from the database.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity to delete.</typeparam>
    /// <param name="connection">The database connection to use to delete the entity.</param>
    /// <param name="entity">The entity to delete.</param>
    /// <param name="transaction">The database transaction within to perform the operation.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The number of rows affected by the delete operation.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="connection" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="entity" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <exception cref="ArgumentException">
    /// No instance property of the type <typeparamref name="TEntity" /> is denoted with a <see cref="KeyAttribute" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was cancelled via <paramref name="cancellationToken" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The table from which the entity will be deleted is determined by the <see cref="TableAttribute" />
    /// applied to the type <typeparamref name="TEntity" />.
    /// If this attribute is not present, the singular name of the type <typeparamref name="TEntity" /> is used.
    /// </para>
    /// <para>
    /// The type <typeparamref name="TEntity" /> must have at least one instance property denoted with a
    /// <see cref="KeyAttribute" />.
    /// </para>
    /// </remarks>
    public Int32 DeleteEntity<TEntity>(
        DbConnection connection,
        TEntity entity,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    ) =>
        0;

    /// <summary>
    /// Asynchronously deletes the specified entity, identified by its key property / properties, from the database.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity to delete.</typeparam>
    /// <param name="connection">The database connection to use to delete the entity.</param>
    /// <param name="entity">The entity to delete.</param>
    /// <param name="transaction">The database transaction within to perform the operation.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// <see cref="Task{TResult}.Result" /> will contain the number of rows affected by the delete operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="connection" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="entity" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <exception cref="ArgumentException">
    /// No instance property of the type <typeparamref name="TEntity" /> is denoted with a <see cref="KeyAttribute" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was cancelled via <paramref name="cancellationToken" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The table from which the entity will be deleted is determined by the <see cref="TableAttribute" />
    /// applied to the type <typeparamref name="TEntity" />.
    /// If this attribute is not present, the singular name of the type <typeparamref name="TEntity" /> is used.
    /// </para>
    /// <para>
    /// The type <typeparamref name="TEntity" /> must have at least one instance property denoted with a
    /// <see cref="KeyAttribute" />.
    /// </para>
    /// </remarks>
    public Task<Int32> DeleteEntityAsync<TEntity>(
        DbConnection connection,
        TEntity entity,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    ) => Task.FromResult(0);

    /// <summary>
    /// Inserts the specified entities into the database.
    /// </summary>
    /// <typeparam name="TEntity">The type of entities to insert.</typeparam>
    /// <param name="connection">The database connection to use to insert the entities.</param>
    /// <param name="entities">The entities to insert.</param>
    /// <param name="transaction">The database transaction within to perform the operation.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The number of rows that were affected by the insert operation.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="connection" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="entities" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was cancelled via <paramref name="cancellationToken" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The table into which the entities will be inserted is determined by the <see cref="TableAttribute" />
    /// applied to the type <typeparamref name="TEntity" />.
    /// If this attribute is not present, the singular name of the type <typeparamref name="TEntity" /> is used.
    /// </para>
    /// <para>
    /// Each instance property of the type <typeparamref name="TEntity" /> is mapped to a column with the same name
    /// (case-sensitive) in the table.
    /// </para>
    /// <para>
    /// The columns must have data types that are compatible with the property types of the corresponding properties.
    /// The compatibility is determined using <see cref="ValueConverter.CanConvert" />.
    /// </para>
    /// <para>Properties denoted with the <see cref="NotMappedAttribute" /> are ignored.</para>
    /// <para>
    /// Properties denoted with a <see cref="DatabaseGeneratedAttribute" /> where the
    /// <see cref="DatabaseGeneratedOption" /> is set to <see cref="DatabaseGeneratedOption.Identity" /> or
    /// <see cref="DatabaseGeneratedOption.Computed" /> are also ignored.
    /// Once an entity is inserted, the values for these properties are retrieved from the database and the entity
    /// properties are updated accordingly.
    /// </para>
    /// </remarks>
    public Int32 InsertEntities<TEntity>(
        DbConnection connection,
        IEnumerable<TEntity> entities,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    ) =>
        0;

    /// <summary>
    /// Asynchronously inserts the specified entities into the database.
    /// </summary>
    /// <typeparam name="TEntity">The type of entities to insert.</typeparam>
    /// <param name="connection">The database connection to use to insert the entities.</param>
    /// <param name="entities">The entities to insert.</param>
    /// <param name="transaction">The database transaction within to perform the operation.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// <see cref="Task{TResult}.Result" /> will contain the number of rows that were affected by the insert operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="connection" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="entities" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was cancelled via <paramref name="cancellationToken" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The table into which the entities will be inserted is determined by the <see cref="TableAttribute" />
    /// applied to the type <typeparamref name="TEntity" />.
    /// If this attribute is not present, the singular name of the type <typeparamref name="TEntity" /> is used.
    /// </para>
    /// <para>
    /// Each instance property of the type <typeparamref name="TEntity" /> is mapped to a column with the same name
    /// (case-sensitive) in the table.
    /// </para>
    /// <para>
    /// The columns must have data types that are compatible with the property types of the corresponding properties.
    /// The compatibility is determined using <see cref="ValueConverter.CanConvert" />.
    /// </para>
    /// <para>Properties denoted with the <see cref="NotMappedAttribute" /> are ignored.</para>
    /// <para>
    /// Properties denoted with a <see cref="DatabaseGeneratedAttribute" /> where the
    /// <see cref="DatabaseGeneratedOption" /> is set to <see cref="DatabaseGeneratedOption.Identity" /> or
    /// <see cref="DatabaseGeneratedOption.Computed" /> are also ignored.
    /// Once an entity is inserted, the values for these properties are retrieved from the database and the entity
    /// properties are updated accordingly.
    /// </para>
    /// </remarks>
    public Task<Int32> InsertEntitiesAsync<TEntity>(
        DbConnection connection,
        IEnumerable<TEntity> entities,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    ) => Task.FromResult(0);

    /// <summary>
    /// Inserts the specified entity into the database.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity to insert.</typeparam>
    /// <param name="connection">The database connection to use to insert the entity.</param>
    /// <param name="entity">The entity to insert.</param>
    /// <param name="transaction">The database transaction within to perform the operation.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The number of rows that were affected by the insert operation.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="connection" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="entity" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was cancelled via <paramref name="cancellationToken" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The table into which the entity will be inserted is determined by the <see cref="TableAttribute" />
    /// applied to the type <typeparamref name="TEntity" />.
    /// If this attribute is not present, the singular name of the type <typeparamref name="TEntity" /> is used.
    /// </para>
    /// <para>
    /// Each instance property of the type <typeparamref name="TEntity" /> is mapped to a column with the same name
    /// (case-sensitive) in the table.
    /// </para>
    /// <para>
    /// The columns must have data types that are compatible with the property types of the corresponding properties.
    /// The compatibility is determined using <see cref="ValueConverter.CanConvert" />.
    /// </para>
    /// <para>Properties denoted with the <see cref="NotMappedAttribute" /> are ignored.</para>
    /// <para>
    /// Properties denoted with a <see cref="DatabaseGeneratedAttribute" /> where the
    /// <see cref="DatabaseGeneratedOption" /> is set to <see cref="DatabaseGeneratedOption.Identity" /> or
    /// <see cref="DatabaseGeneratedOption.Computed" /> are also ignored.
    /// Once an entity is inserted, the values for these properties are retrieved from the database and the entity
    /// properties are updated accordingly.
    /// </para>
    /// </remarks>
    public Int32 InsertEntity<TEntity>(
        DbConnection connection,
        TEntity entity,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    ) =>
        0;

    /// <summary>
    /// Asynchronously inserts the specified entity into the database.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity to insert.</typeparam>
    /// <param name="connection">The database connection to use to insert the entity.</param>
    /// <param name="entity">The entity to insert.</param>
    /// <param name="transaction">The database transaction within to perform the operation.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// <see cref="Task{TResult}.Result" /> will contain the number of rows that were affected by the insert operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="connection" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="entity" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was cancelled via <paramref name="cancellationToken" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The table into which the entity will be inserted is determined by the <see cref="TableAttribute" />
    /// applied to the type <typeparamref name="TEntity" />.
    /// If this attribute is not present, the singular name of the type <typeparamref name="TEntity" /> is used.
    /// </para>
    /// <para>
    /// Each instance property of the type <typeparamref name="TEntity" /> is mapped to a column with the same name
    /// (case-sensitive) in the table.
    /// </para>
    /// <para>
    /// The columns must have data types that are compatible with the property types of the corresponding properties.
    /// The compatibility is determined using <see cref="ValueConverter.CanConvert" />.
    /// </para>
    /// <para>Properties denoted with the <see cref="NotMappedAttribute" /> are ignored.</para>
    /// <para>
    /// Properties denoted with a <see cref="DatabaseGeneratedAttribute" /> where the
    /// <see cref="DatabaseGeneratedOption" /> is set to <see cref="DatabaseGeneratedOption.Identity" /> or
    /// <see cref="DatabaseGeneratedOption.Computed" /> are also ignored.
    /// Once an entity is inserted, the values for these properties are retrieved from the database and the entity
    /// properties are updated accordingly.
    /// </para>
    /// </remarks>
    public Task<Int32> InsertEntityAsync<TEntity>(
        DbConnection connection,
        TEntity entity,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    ) => Task.FromResult(0);

    /// <summary>
    /// Updates the specified entities, identified by their key property / properties, in the database.
    /// </summary>
    /// <typeparam name="TEntity">The type of entities to update.</typeparam>
    /// <param name="connection">The database connection to use to update the entities.</param>
    /// <param name="entities">The entities to update.</param>
    /// <param name="transaction">The database transaction within to perform the operation.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The number of rows that were affected by the update operation.</returns>
    /// <exception cref="ArgumentException">
    /// No instance property of the type <typeparamref name="TEntity" /> is denoted with a <see cref="KeyAttribute" />.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="connection" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="entities" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was cancelled via <paramref name="cancellationToken" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The table where the entities will be updated is determined by the <see cref="TableAttribute" /> applied to the
    /// type <typeparamref name="TEntity" />.
    /// If this attribute is not present, the singular name of the type <typeparamref name="TEntity" /> is used.
    /// </para>
    /// <para>
    /// The type <typeparamref name="TEntity" /> must have at least one instance property denoted with a
    /// <see cref="KeyAttribute" />.
    /// </para>
    /// <para>
    /// Each instance property of the type <typeparamref name="TEntity" /> is mapped to a column with the same name
    /// (case-sensitive) in the table.
    /// </para>
    /// <para>
    /// The columns must have data types that are compatible with the property types of the corresponding properties.
    /// The compatibility is determined using <see cref="ValueConverter.CanConvert" />.
    /// </para>
    /// <para>Properties denoted with the <see cref="NotMappedAttribute" /> are ignored.</para>
    /// <para>
    /// Properties denoted with a <see cref="DatabaseGeneratedAttribute" /> where the
    /// <see cref="DatabaseGeneratedOption" /> is set to <see cref="DatabaseGeneratedOption.Identity" /> or
    /// <see cref="DatabaseGeneratedOption.Computed" /> are also ignored.
    /// Once an entity is updated, the values for these properties are retrieved from the database and the entity
    /// properties are updated accordingly.
    /// </para>
    /// </remarks>
    public Int32 UpdateEntities<TEntity>(
        DbConnection connection,
        IEnumerable<TEntity> entities,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    ) =>
        0;

    /// <summary>
    /// Asynchronously updates the specified entities, identified by their key property / properties, in the database.
    /// </summary>
    /// <typeparam name="TEntity">The type of entities to update.</typeparam>
    /// <param name="connection">The database connection to use to update the entities.</param>
    /// <param name="entities">The entities to update.</param>
    /// <param name="transaction">The database transaction within to perform the operation.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// <see cref="Task{TResult}.Result" /> will contain the number of rows that were affected by the update operation.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// No instance property (with a public getter) of the type <typeparamref name="TEntity" /> is denoted with a
    /// <see cref="KeyAttribute" />.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="connection" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="entities" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was cancelled via <paramref name="cancellationToken" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The table where the entities will be updated is determined by the <see cref="TableAttribute" /> applied to the
    /// type <typeparamref name="TEntity" />.
    /// If this attribute is not present, the singular name of the type <typeparamref name="TEntity" /> is used.
    /// </para>
    /// <para>
    /// The type <typeparamref name="TEntity" /> must have at least one instance property denoted with a
    /// <see cref="KeyAttribute" />.
    /// </para>
    /// <para>
    /// Each instance property of the type <typeparamref name="TEntity" /> is mapped to a column with the same name
    /// (case-sensitive) in the table.
    /// </para>
    /// <para>
    /// The columns must have data types that are compatible with the property types of the corresponding properties.
    /// The compatibility is determined using <see cref="ValueConverter.CanConvert" />.
    /// </para>
    /// <para>Properties denoted with the <see cref="NotMappedAttribute" /> are ignored.</para>
    /// <para>
    /// Properties denoted with a <see cref="DatabaseGeneratedAttribute" /> where the
    /// <see cref="DatabaseGeneratedOption" /> is set to <see cref="DatabaseGeneratedOption.Identity" /> or
    /// <see cref="DatabaseGeneratedOption.Computed" /> are also ignored.
    /// Once an entity is updated, the values for these properties are retrieved from the database and the entity
    /// properties are updated accordingly.
    /// </para>
    /// </remarks>
    public Task<Int32> UpdateEntitiesAsync<TEntity>(
        DbConnection connection,
        IEnumerable<TEntity> entities,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    ) => Task.FromResult(0);

    /// <summary>
    /// Updates the specified entity, identified by its key property / properties, in the database.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity to update.</typeparam>
    /// <param name="connection">The database connection to use to update the entity.</param>
    /// <param name="entity">The entity to update.</param>
    /// <param name="transaction">The database transaction within to perform the operation.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The number of rows that were affected by the update operation.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="connection" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="entity" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <exception cref="ArgumentException">
    /// No instance property of the type <typeparamref name="TEntity" /> is denoted with a <see cref="KeyAttribute" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was cancelled via <paramref name="cancellationToken" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The table where the entity will be updated is determined by the <see cref="TableAttribute" /> applied to the
    /// type <typeparamref name="TEntity" />.
    /// If this attribute is not present, the singular name of the type <typeparamref name="TEntity" /> is used.
    /// </para>
    /// <para>
    /// The type <typeparamref name="TEntity" /> must have at least one instance property denoted with a
    /// <see cref="KeyAttribute" />.
    /// </para>
    /// <para>
    /// Each instance property of the type <typeparamref name="TEntity" /> is mapped to a column with the same name
    /// (case-sensitive) in the table.
    /// </para>
    /// <para>
    /// The columns must have data types that are compatible with the property types of the corresponding properties.
    /// The compatibility is determined using <see cref="ValueConverter.CanConvert" />.
    /// </para>
    /// <para>Properties denoted with the <see cref="NotMappedAttribute" /> are ignored.</para>
    /// <para>
    /// Properties denoted with a <see cref="DatabaseGeneratedAttribute" /> where the
    /// <see cref="DatabaseGeneratedOption" /> is set to <see cref="DatabaseGeneratedOption.Identity" /> or
    /// <see cref="DatabaseGeneratedOption.Computed" /> are also ignored.
    /// Once an entity is updated, the values for these properties are retrieved from the database and the entity
    /// properties are updated accordingly.
    /// </para>
    /// </remarks>
    public Int32 UpdateEntity<TEntity>(
        DbConnection connection,
        TEntity entity,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    ) =>
        0;

    /// <summary>
    /// Asynchronously updates the specified entity, identified by its key property / properties, in the database.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity to update.</typeparam>
    /// <param name="connection">The database connection to use to update the entity.</param>
    /// <param name="entity">The entity to update.</param>
    /// <param name="transaction">The database transaction within to perform the operation.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// <see cref="Task{TResult}.Result" /> will contain the number of rows that were affected by the update operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="connection" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="entity" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <exception cref="ArgumentException">
    /// No instance property of the type <typeparamref name="TEntity" /> is denoted with a <see cref="KeyAttribute" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was cancelled via <paramref name="cancellationToken" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The table where the entity will be updated is determined by the <see cref="TableAttribute" /> applied to the
    /// type <typeparamref name="TEntity" />.
    /// If this attribute is not present, the singular name of the type <typeparamref name="TEntity" /> is used.
    /// </para>
    /// <para>
    /// The type <typeparamref name="TEntity" /> must have at least one instance property denoted with a
    /// <see cref="KeyAttribute" />.
    /// </para>
    /// <para>
    /// Each instance property of the type <typeparamref name="TEntity" /> is mapped to a column with the same name
    /// (case-sensitive) in the table.
    /// </para>
    /// <para>
    /// The columns must have data types that are compatible with the property types of the corresponding properties.
    /// The compatibility is determined using <see cref="ValueConverter.CanConvert" />.
    /// </para>
    /// <para>Properties denoted with the <see cref="NotMappedAttribute" /> are ignored.</para>
    /// <para>
    /// Properties denoted with a <see cref="DatabaseGeneratedAttribute" /> where the
    /// <see cref="DatabaseGeneratedOption" /> is set to <see cref="DatabaseGeneratedOption.Identity" /> or
    /// <see cref="DatabaseGeneratedOption.Computed" /> are also ignored.
    /// Once an entity is updated, the values for these properties are retrieved from the database and the entity
    /// properties are updated accordingly.
    /// </para>
    /// </remarks>
    public Task<Int32> UpdateEntityAsync<TEntity>(
        DbConnection connection,
        TEntity entity,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    ) => Task.FromResult(0);
}
