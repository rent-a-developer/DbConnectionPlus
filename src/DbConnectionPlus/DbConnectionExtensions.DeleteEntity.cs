// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus;

/// <summary>
/// Provides extension members for the type <see cref="DbConnection" />.
/// </summary>
public static partial class DbConnectionExtensions
{
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
    /// <example>
    /// <code>
    /// using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;
    /// 
    /// class Product
    /// {
    ///     [Key]
    ///     public Int64 Id { get; set; }
    ///     public Boolean IsDiscontinued { get; set; }
    /// }
    /// 
    /// if (product.IsDiscontinued)
    /// {
    ///     connection.DeleteEntity(product);
    /// }
    /// </code>
    /// </example>
    public static Int32 DeleteEntity<TEntity>(
        this DbConnection connection,
        TEntity entity,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default
    )
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entity);

        var databaseAdapter = DatabaseAdapterRegistry.GetAdapter(connection.GetType());

        return databaseAdapter.EntityManipulator.DeleteEntity(
            connection,
            entity,
            transaction,
            cancellationToken
        );
    }

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
    /// <example>
    /// <code>
    /// using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;
    /// 
    /// class Product
    /// {
    ///     [Key]
    ///     public Int64 Id { get; set; }
    ///     public Boolean IsDiscontinued { get; set; }
    /// }
    /// 
    /// if (product.IsDiscontinued)
    /// {
    ///     await connection.DeleteEntityAsync(product);
    /// }
    /// </code>
    /// </example>
    public static Task<Int32> DeleteEntityAsync<TEntity>(
        this DbConnection connection,
        TEntity entity,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default
    )
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entity);

        var databaseAdapter = DatabaseAdapterRegistry.GetAdapter(connection.GetType());

        return databaseAdapter.EntityManipulator.DeleteEntityAsync(
            connection,
            entity,
            transaction,
            cancellationToken
        );
    }
}
