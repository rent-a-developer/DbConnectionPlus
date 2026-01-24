using RentADeveloper.DbConnectionPlus.SqlStatements;

namespace RentADeveloper.DbConnectionPlus;

/// <summary>
/// A delegate for intercepting database commands executed via DbConnectionPlus.
/// </summary>
/// <param name="dbCommand">The database command being executed.</param>
/// <param name="temporaryTables">The temporary tables created for the command.</param>
public delegate void InterceptDbCommand(DbCommand dbCommand, IReadOnlyList<InterpolatedTemporaryTable> temporaryTables);
