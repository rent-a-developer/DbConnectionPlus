// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using RentADeveloper.DbConnectionPlus.DbCommands;
using RentADeveloper.DbConnectionPlus.SqlStatements;

namespace RentADeveloper.DbConnectionPlus;

/// <summary>
/// Provides extension members for the type <see cref="DbConnection" />.
/// </summary>
public static partial class DbConnectionExtensions
{
    /// <summary>
    /// Controls how <see cref="Enum" /> values are serialized when they are sent to a database using one of the
    /// following methods:
    /// 
    /// 1. When an entity containing an enum property is inserted via
    /// <see cref="InsertEntities{TEntity}" />, <see cref="InsertEntitiesAsync{TEntity}" />,
    /// <see cref="InsertEntity{TEntity}" /> or <see cref="InsertEntityAsync{TEntity}" />.
    /// 
    /// 2. When an entity containing an enum property is updated via
    /// <see cref="UpdateEntities{TEntity}" />, <see cref="UpdateEntitiesAsync{TEntity}" />,
    /// <see cref="UpdateEntity{TEntity}" /> or <see cref="UpdateEntityAsync{TEntity}" />.
    /// 
    /// 3. When an enum value is passed as a parameter to an SQL statement via <see cref="Parameter" />.
    /// 
    /// 4. When a sequence of enum values is passed as a temporary table to an SQL statement via
    /// <see cref="TemporaryTable{T}" />.
    /// 
    /// 5. When objects containing an enum property are passed as a temporary table to an SQL statement via
    /// <see cref="TemporaryTable{T}" />.
    /// 
    /// The default is <see cref="DbConnectionPlus.EnumSerializationMode.Strings" />.
    /// </summary>
    /// <remarks>
    /// <strong>Thread Safety:</strong>
    /// This is a static mutable property. To avoid race conditions in multi-threaded applications, set this property
    /// during application initialization before any database operations are performed, and do not change it afterward.
    /// Changing this value while database operations are in progress from multiple threads may lead to inconsistent
    /// behavior.
    /// </remarks>
    public static EnumSerializationMode EnumSerializationMode { get; set; } = EnumSerializationMode.Strings;

    /// <summary>
    /// A function that can be used to intercept database commands executed via DbConnectionPlus.
    /// Can be used for logging or modifying commands before execution.
    /// </summary>
    /// <remarks>
    /// <strong>Thread Safety:</strong>
    /// This is a static mutable property. To avoid race conditions in multi-threaded applications, set this property
    /// during application initialization before any database operations are performed, and do not change it afterward.
    /// Changing this value while database operations are in progress from multiple threads may lead to inconsistent
    /// behavior.
    /// </remarks>
    public static InterceptDbCommand? InterceptDbCommand { get; set; }

    /// <summary>
    /// The factory to use to create instances of <see cref="DbCommand" />.
    /// </summary>
    /// <remarks>
    /// This property is mainly used to test the cancellation of SQL statements in integration tests.
    /// </remarks>
    internal static IDbCommandFactory DbCommandFactory { get; set; } = new DefaultDbCommandFactory();

    /// <summary>
    /// A function to be called before executing a database command via DbConnectionPlus.
    /// </summary>
    /// <param name="command">The database command being executed.</param>
    /// <param name="temporaryTables">The temporary tables created for the command.</param>
    internal static void OnBeforeExecutingCommand(
        DbCommand command,
        IReadOnlyList<InterpolatedTemporaryTable> temporaryTables
    ) =>
        InterceptDbCommand?.Invoke(command, temporaryTables);
}
