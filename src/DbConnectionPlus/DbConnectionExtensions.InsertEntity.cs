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
    /// The table into which the entity will be inserted can be configured via <see cref="TableAttribute" /> or
    /// <see cref="Configure"/>. Per default, the singular name of the type <typeparamref name="TEntity" /> is used
    /// as the table name.
    /// </para>
    /// <para>
    /// Per default, each instance property of the type <typeparamref name="TEntity" /> is mapped to a column with the
    /// same name (case-sensitive) in the table. This can be configured via <see cref="ColumnAttribute" /> or
    /// <see cref="Configure"/>.
    /// </para>
    /// <para>
    /// The columns must have data types that are compatible with the property types of the corresponding properties.
    /// The compatibility is determined using <see cref="ValueConverter.CanConvert" />.
    /// </para>
    /// <para>
    /// Properties configured as ignored properties (via <see cref="NotMappedAttribute" /> or <see cref="Configure" />)
    /// are not inserted.
    /// </para>
    /// <para>
    /// Properties configured as identity or computed properties (via <see cref="DatabaseGeneratedAttribute" /> or
    /// <see cref="Configure"/>) are also not inserted.
    /// Once an entity is inserted, the values for these properties are retrieved from the database and the entity
    /// properties are updated accordingly.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;
    /// 
    /// class Product
    /// {
    ///     public Int64 Id { get; set; }
    ///     public Int64 SupplierId { get; set; }
    ///     public String Name { get; set; }
    ///     public Decimal UnitPrice { get; set; }
    ///     public Int32 UnitsInStock { get; set; }
    /// }
    /// 
    /// var newProduct = GetNewProduct();
    /// 
    /// connection.InsertEntity(newProduct);
    /// </code>
    /// </example>
    public static Int32 InsertEntity<TEntity>(
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

        return databaseAdapter.EntityManipulator.InsertEntity(
            connection,
            entity,
            transaction,
            cancellationToken
        );
    }

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
    /// The table into which the entity will be inserted can be configured via <see cref="TableAttribute" /> or
    /// <see cref="Configure"/>. Per default, the singular name of the type <typeparamref name="TEntity" /> is used
    /// as the table name.
    /// </para>
    /// <para>
    /// Per default, each instance property of the type <typeparamref name="TEntity" /> is mapped to a column with the
    /// same name (case-sensitive) in the table. This can be configured via <see cref="ColumnAttribute" /> or
    /// <see cref="Configure"/>.
    /// </para>
    /// <para>
    /// The columns must have data types that are compatible with the property types of the corresponding properties.
    /// The compatibility is determined using <see cref="ValueConverter.CanConvert" />.
    /// </para>
    /// <para>
    /// Properties configured as ignored properties (via <see cref="NotMappedAttribute" /> or <see cref="Configure" />)
    /// are not inserted.
    /// </para>
    /// <para>
    /// Properties configured as identity or computed properties (via <see cref="DatabaseGeneratedAttribute" /> or
    /// <see cref="Configure"/>) are also not inserted.
    /// Once an entity is inserted, the values for these properties are retrieved from the database and the entity
    /// properties are updated accordingly.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;
    /// 
    /// class Product
    /// {
    ///     public Int64 Id { get; set; }
    ///     public Int64 SupplierId { get; set; }
    ///     public String Name { get; set; }
    ///     public Decimal UnitPrice { get; set; }
    ///     public Int32 UnitsInStock { get; set; }
    /// }
    /// 
    /// var newProduct = await GetNewProductAsync();
    /// 
    /// await connection.InsertEntityAsync(newProduct);
    /// </code>
    /// </example>
    public static Task<Int32> InsertEntityAsync<TEntity>(
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

        return databaseAdapter.EntityManipulator.InsertEntityAsync(
            connection,
            entity,
            transaction,
            cancellationToken
        );
    }
}
