// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using RentADeveloper.DbConnectionPlus.Entities;
using RentADeveloper.DbConnectionPlus.SqlStatements;

namespace RentADeveloper.DbConnectionPlus;

/// <summary>
/// Provides extension members for the type <see cref="DbConnection" />.
/// </summary>
public static partial class DbConnectionExtensions
{
    /// <summary>
    /// Configures DbConnectionPlus.
    /// </summary>
    /// <param name="configureAction">The action that configures DbConnectionPlus.</param>
    public static void Configure(Action<DbConnectionPlusConfiguration> configureAction)
    {
        ArgumentNullException.ThrowIfNull(configureAction);

        configureAction(DbConnectionPlusConfiguration.Instance);

        ((IFreezable)DbConnectionPlusConfiguration.Instance).Freeze();

        // We need to reset the entity type metadata cache, because the configuration may have changed how entities
        // are mapped that were previously mapped via data annotation attributes or conventions.
        EntityHelper.ResetEntityTypeMetadataCache();
    }

    /// <summary>
    /// A function to be called before executing a database command via DbConnectionPlus.
    /// </summary>
    /// <param name="command">The database command being executed.</param>
    /// <param name="temporaryTables">The temporary tables created for the command.</param>
    internal static void OnBeforeExecutingCommand(
        DbCommand command,
        IReadOnlyList<InterpolatedTemporaryTable> temporaryTables
    ) =>
        DbConnectionPlusConfiguration.Instance.InterceptDbCommand?.Invoke(command, temporaryTables);
}
