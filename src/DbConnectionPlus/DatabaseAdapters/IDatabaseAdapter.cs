// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus.DatabaseAdapters;

/// <summary>
/// Represents an adapter that adapts DbConnectionPlus to a specific database system.
/// </summary>
public interface IDatabaseAdapter
{
    /// <summary>
    /// The entity manipulator for the database system this adapter supports.
    /// </summary>
    public IEntityManipulator EntityManipulator { get; }

    /// <summary>
    /// The temporary table builder for the database system this adapter supports.
    /// </summary>
    public ITemporaryTableBuilder TemporaryTableBuilder { get; }

    /// <summary>
    /// <para>
    /// Binds <paramref name="value" /> to <paramref name="parameter" />.
    /// </para>
    /// <para>
    /// If <paramref name="value" /> is an <see cref="Enum" /> value, it is serialized according to the setting
    /// <see cref="DbConnectionPlusConfiguration.EnumSerializationMode" /> before being assigned to the parameter.
    /// </para>
    /// </summary>
    /// <param name="parameter">The parameter to bind <paramref name="value" /> to.</param>
    /// <param name="value">The value to bind to <paramref name="parameter" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="parameter" /> is <see langword="null" />.</exception>
    public void BindParameterValue(DbParameter parameter, Object? value);

    /// <summary>
    /// Returns <paramref name="parameterName" /> with the appropriate prefix
    /// (e.g. "@" for SQL Server or ":" for Oracle) for use in SQL statements.
    /// </summary>
    /// <param name="parameterName">The parameter name to format.</param>
    /// <returns>
    /// <paramref name="parameterName" /> formatted with the appropriate prefix, suitable for inclusion in SQL
    /// statements.
    /// </returns>
    public String FormatParameterName(String parameterName);

    /// <summary>
    /// Gets the corresponding database specific data type for the type <paramref name="type" />.
    /// </summary>
    /// <param name="type">The type to get the database specific data type for.</param>
    /// <param name="enumSerializationMode">The mode to use to serialize <see cref="Enum" /> values.</param>
    /// <returns>
    /// The corresponding database specific data type for the type <paramref name="type" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="type" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="enumSerializationMode" /> is not a valid <see cref="EnumSerializationMode" />
    /// value.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 The type <paramref name="type" /> could not be mapped to a database specific data type.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    public String GetDataType(Type type, EnumSerializationMode enumSerializationMode);

    /// <summary>
    /// Returns <paramref name="identifier" /> properly quoted for use in SQL statements.
    /// </summary>
    /// <param name="identifier">The identifier to be quoted.</param>
    /// <returns>
    /// A string containing the quoted version of <paramref name="identifier" />, suitable for use in SQL statements.
    /// </returns>
    public String QuoteIdentifier(String identifier);

    /// <summary>
    /// Returns the specified name of a temporary table properly quoted for use in SQL statements.
    /// </summary>
    /// <param name="tableName">The temporary table name to quote.</param>
    /// <param name="connection">The database connection to use for quoting.</param>
    /// <returns>
    /// A string containing the quoted version of <paramref name="tableName" />, suitable for use in SQL statements.
    /// </returns>
    public String QuoteTemporaryTableName(String tableName, DbConnection connection);

    /// <summary>
    /// Determines whether the database system this adapter supports has support for (local/session scoped) temporary
    /// tables.
    /// </summary>
    /// <param name="connection">The database connection to use to check for the support.</param>
    /// <returns>
    /// <see langword="true" /> if the database system supports temporary tables; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// A database adapter returning <see langword="true" /> for this method must implement an
    /// <see cref="ITemporaryTableBuilder" /> and provide an instance of it via the <see cref="TemporaryTableBuilder" />
    /// property.
    /// </para>
    /// <para>
    /// If a database adapter returns <see langword="false" /> for this method, it must throw a
    /// <see cref="NotSupportedException" /> from the <see cref="TemporaryTableBuilder" /> property.
    /// </para>
    /// </remarks>
    public Boolean SupportsTemporaryTables(DbConnection connection);

    /// <summary>
    /// Determines whether <paramref name="exception" /> was thrown because an SQL statement was cancelled via
    /// <paramref name="cancellationToken" />.
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <param name="cancellationToken">The token via the SQL statement may have been cancelled.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="exception" /> was thrown because an SQL statement was cancelled via
    /// <paramref name="cancellationToken" />; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception" /> is <see langword="null" />.</exception>
    public Boolean WasSqlStatementCancelledByCancellationToken(
        Exception exception,
        CancellationToken cancellationToken
    );
}
