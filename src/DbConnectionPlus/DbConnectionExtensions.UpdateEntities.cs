// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using RentADeveloper.DbConnectionPlus.Converters;

namespace RentADeveloper.DbConnectionPlus;

/// <summary>
/// Provides extension members for the type <see cref="DbConnection" />.
/// </summary>
public static partial class DbConnectionExtensions
{
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
    /// If a property is denoted with the <see cref="ColumnAttribute" />, the name specified in the attribute is used
    /// as the column name.
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
    /// var usersWithoutLoginInPastYear = connection.Query<User>(
    ///     """
    ///     SELECT  *
    ///     FROM    Users
    ///     WHERE   LastLoginDate < DATEADD(YEAR, -1, GETUTCDATE())
    ///     """
    /// );
    /// 
    /// foreach (var user in usersWithoutLoginInPastYear)
    /// {
    ///     user.State = UserState.Inactive;
    /// }
    /// 
    /// connection.UpdateEntities(usersWithoutLoginInPastYear);
    /// ]]>
    /// </code>
    /// </example>
    public static Int32 UpdateEntities<TEntity>(
        this DbConnection connection,
        IEnumerable<TEntity> entities,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default
    )
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entities);

        var databaseAdapter = DatabaseAdapterRegistry.GetAdapter(connection.GetType());

        return databaseAdapter.EntityManipulator.UpdateEntities(
            connection,
            entities,
            transaction,
            cancellationToken
        );
    }

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
    /// If a property is denoted with the <see cref="ColumnAttribute" />, the name specified in the attribute is used
    /// as the column name.
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
    /// var usersWithoutLoginInPastYear = connection.QueryAsync<User>(
    ///     """
    ///     SELECT  *
    ///     FROM    Users
    ///     WHERE   LastLoginDate < DATEADD(YEAR, -1, GETUTCDATE())
    ///     """
    /// );
    /// 
    /// await foreach (var user in usersWithoutLoginInPastYear)
    /// {
    ///     user.State = UserState.Inactive;
    /// }
    /// 
    /// await connection.UpdateEntitiesAsync(usersWithoutLoginInPastYear);
    /// ]]>
    /// </code>
    /// </example>
    public static Task<Int32> UpdateEntitiesAsync<TEntity>(
        this DbConnection connection,
        IEnumerable<TEntity> entities,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default
    )
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entities);

        var databaseAdapter = DatabaseAdapterRegistry.GetAdapter(connection.GetType());

        return databaseAdapter.EntityManipulator
            .UpdateEntitiesAsync(connection, entities, transaction, cancellationToken);
    }
}
