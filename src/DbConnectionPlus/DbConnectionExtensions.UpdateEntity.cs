// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using RentADeveloper.DbConnectionPlus.Converters;
using RentADeveloper.DbConnectionPlus.Exceptions;

namespace RentADeveloper.DbConnectionPlus;

/// <summary>
/// Provides extension members for the type <see cref="DbConnection" />.
/// </summary>
public static partial class DbConnectionExtensions
{
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
    /// No instance property of the type <typeparamref name="TEntity" /> is configured as a key property.
    /// </exception>
    /// <exception cref="DbUpdateConcurrencyException">
    /// A concurrency violation was encountered while updating an entity. A concurrency violation occurs when an
    /// unexpected number of rows are affected by an update operation. This is usually because the data in the database
    /// has been modified since the entity has been loaded.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was cancelled via <paramref name="cancellationToken" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The table in which the entity will be updated can be configured via <see cref="TableAttribute" /> or
    /// <see cref="Configure" />. Per default, the singular name of the type <typeparamref name="TEntity" /> is used
    /// as the table name.
    /// </para>
    /// <para>
    /// The type <typeparamref name="TEntity" /> must have at least one instance property configured as key property.
    /// Use <see cref="KeyAttribute" /> or <see cref="Configure" /> to configure key properties.
    /// </para>
    /// <para>
    /// Per default, each instance property of the type <typeparamref name="TEntity" /> is mapped to a column with the
    /// same name (case-sensitive) in the table. This can be configured via <see cref="ColumnAttribute" /> or
    /// <see cref="Configure" />.
    /// </para>
    /// <para>
    /// The columns must have data types that are compatible with the property types of the corresponding properties.
    /// The compatibility is determined using <see cref="ValueConverter.CanConvert" />.
    /// </para>
    /// <para>
    /// Properties configured as ignored properties (via <see cref="NotMappedAttribute" /> or <see cref="Configure" />)
    /// are not updated.
    /// </para>
    /// <para>
    /// Properties configured as identity or computed properties (via <see cref="DatabaseGeneratedAttribute" /> or
    /// <see cref="Configure" />) are also not updated.
    /// Once an entity is updated, the values for these properties are retrieved from the database and the entity
    /// properties are updated accordingly.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;
    /// 
    /// class User
    /// {
    ///     [Key]
    ///     public Int64 Id { get; set; }
    ///     public DateTime LastLoginDate { get; set; }
    ///     public UserState State { get; set; }
    /// }
    /// 
    /// if (user.LastLoginDate < DateTime.UtcNow.AddYears(-1))
    /// {
    ///     user.State = UserState.Inactive;
    ///     connection.UpdateEntity(user);
    /// }
    /// ]]>
    /// </code>
    /// </example>
    public static Int32 UpdateEntity<TEntity>(
        this DbConnection connection,
        TEntity entity,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default
    )
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entity);

        var databaseAdapter = DbConnectionPlusConfiguration.Instance.GetDatabaseAdapter(connection.GetType());

        return databaseAdapter.EntityManipulator.UpdateEntity(
            connection,
            entity,
            transaction,
            cancellationToken
        );
    }

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
    /// No instance property of the type <typeparamref name="TEntity" /> is configured as a key property.
    /// </exception>
    /// <exception cref="DbUpdateConcurrencyException">
    /// A concurrency violation was encountered while updating an entity. A concurrency violation occurs when an
    /// unexpected number of rows are affected by an update operation. This is usually because the data in the database
    /// has been modified since the entity has been loaded.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was cancelled via <paramref name="cancellationToken" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The table in which the entity will be updated can be configured via <see cref="TableAttribute" /> or
    /// <see cref="Configure" />. Per default, the singular name of the type <typeparamref name="TEntity" /> is used
    /// as the table name.
    /// </para>
    /// <para>
    /// The type <typeparamref name="TEntity" /> must have at least one instance property configured as key property.
    /// Use <see cref="KeyAttribute" /> or <see cref="Configure" /> to configure key properties.
    /// </para>
    /// <para>
    /// Per default, each instance property of the type <typeparamref name="TEntity" /> is mapped to a column with the
    /// same name (case-sensitive) in the table. This can be configured via <see cref="ColumnAttribute" /> or
    /// <see cref="Configure" />.
    /// </para>
    /// <para>
    /// The columns must have data types that are compatible with the property types of the corresponding properties.
    /// The compatibility is determined using <see cref="ValueConverter.CanConvert" />.
    /// </para>
    /// <para>
    /// Properties configured as ignored properties (via <see cref="NotMappedAttribute" /> or <see cref="Configure" />)
    /// are not updated.
    /// </para>
    /// <para>
    /// Properties configured as identity or computed properties (via <see cref="DatabaseGeneratedAttribute" /> or
    /// <see cref="Configure" />) are also not updated.
    /// Once an entity is updated, the values for these properties are retrieved from the database and the entity
    /// properties are updated accordingly.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;
    /// 
    /// class User
    /// {
    ///     [Key]
    ///     public Int64 Id { get; set; }
    ///     public DateTime LastLoginDate { get; set; }
    ///     public UserState State { get; set; }
    /// }
    /// 
    /// if (user.LastLoginDate < DateTime.UtcNow.AddYears(-1))
    /// {
    ///     user.State = UserState.Inactive;
    ///     await connection.UpdateEntityAsync(user);
    /// }
    /// ]]>
    /// </code>
    /// </example>
    public static Task<Int32> UpdateEntityAsync<TEntity>(
        this DbConnection connection,
        TEntity entity,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default
    )
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entity);

        var databaseAdapter = DbConnectionPlusConfiguration.Instance.GetDatabaseAdapter(connection.GetType());

        return databaseAdapter.EntityManipulator.UpdateEntityAsync(
            connection,
            entity,
            transaction,
            cancellationToken
        );
    }
}
