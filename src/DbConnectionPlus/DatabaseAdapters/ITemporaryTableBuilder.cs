// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus.DatabaseAdapters;

/// <summary>
/// Represents a builder that builds temporary database tables.
/// </summary>
public interface ITemporaryTableBuilder
{
    /// <summary>
    /// Builds a temporary table and populates it with the specified values.
    /// </summary>
    /// <param name="connection">The database connection to use to build the temporary table.</param>
    /// <param name="transaction">The database transaction within to build the temporary table.</param>
    /// <param name="name">The name of the temporary table to build.</param>
    /// <param name="values">The values to populate the temporary table with.</param>
    /// <param name="valuesType">The type of values in <paramref name="values" />.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// An instance of <see cref="TemporaryTableDisposer" /> that can be used to dispose the built table.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="name" /> is empty or consists only of white-space characters.
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
    ///                 <paramref name="name" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="values" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="valuesType" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="connection" /> is not of a compatible type.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="transaction" /> is not <see langword="null" /> and not of a compatible type.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <remarks>
    /// <para>
    /// If the type <paramref name="valuesType" /> is a scalar type
    /// (e.g. <see cref="String" />, <see cref="Int32" />, <see cref="DateTime" />, <see cref="Enum" /> and so on),
    /// a single-column temporary table will be built with a column named "Value" with a data type that matches the
    /// type <paramref name="valuesType" />.
    /// </para>
    /// <para>
    /// If the type <paramref name="valuesType" /> is a complex type (e.g. a class or a record), a multi-column
    /// temporary table will be built.
    /// The temporary table will contain a column for each instance property (with a public getter) of the type
    /// <paramref name="valuesType" />.
    /// The name of each column will be the name of the corresponding property.
    /// The data type of each column will match the property type of the corresponding property.
    /// </para>
    /// </remarks>
    public TemporaryTableDisposer BuildTemporaryTable(
        DbConnection connection,
        DbTransaction? transaction,
        String name,
        IEnumerable values,
        Type valuesType,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Asynchronously builds a temporary table and populates it with the specified values.
    /// </summary>
    /// <param name="connection">The database connection to use to build the temporary table.</param>
    /// <param name="transaction">The database transaction within to build the temporary table.</param>
    /// <param name="name">The name of the temporary table to build.</param>
    /// <param name="values">The values to populate the temporary table with.</param>
    /// <param name="valuesType">The type of values in <paramref name="values" />.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// <see cref="Task{TResult}.Result" /> will contain an instance of <see cref="TemporaryTableDisposer" />
    /// that can be used to dispose the built table.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="name" /> is empty or consists only of white-space characters.
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
    ///                 <paramref name="name" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="values" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="valuesType" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="connection" /> is not of a compatible type.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="transaction" /> is not <see langword="null" /> and not of a compatible type.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <remarks>
    /// <para>
    /// If the type <paramref name="valuesType" /> is a scalar type
    /// (e.g. <see cref="String" />, <see cref="Int32" />, <see cref="DateTime" />, <see cref="Enum" /> and so on),
    /// a single-column temporary table will be built with a column named "Value" with a data type that matches
    /// the type <paramref name="valuesType" />.
    /// </para>
    /// <para>
    /// If the type <paramref name="valuesType" /> is a complex type (e.g. a class or a record), a multi-column
    /// temporary table will be built.
    /// The temporary table will contain a column for each instance property (with a public getter) of the type
    /// <paramref name="valuesType" />.
    /// The name of each column will be the name of the corresponding property.
    /// The data type of each column will match the property type of the corresponding property.
    /// </para>
    /// </remarks>
    public Task<TemporaryTableDisposer> BuildTemporaryTableAsync(
        DbConnection connection,
        DbTransaction? transaction,
        String name,
        IEnumerable values,
        Type valuesType,
        CancellationToken cancellationToken = default
    );
}
