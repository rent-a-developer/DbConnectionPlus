// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using DataRow = RentADeveloper.DbConnectionPlus.Dynamic.DataRow;

namespace RentADeveloper.DbConnectionPlus.Materializers;

/// <summary>
/// Materialize instances of <see cref="DbDataReader" /> to instances of <see cref="DataRow" />.
/// </summary>
internal static class DataRowMaterializer
{
    /// <summary>
    /// Materializes the data in <paramref name="dataReader" /> to an instance of <see cref="DataRow" />.
    /// </summary>
    /// <param name="dataReader">The <see cref="DbDataReader" /> to materialize.</param>
    /// <returns>An instance of <see cref="DataRow" /> containing the data of <paramref name="dataReader" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dataReader" /> is <see langword="null" />.</exception>
    internal static DataRow Materialize(DbDataReader dataReader)
    {
        ArgumentNullException.ThrowIfNull(dataReader);

        var values = new Object[dataReader.FieldCount];
        dataReader.GetValues(values);

        var columns = new Dictionary<String, Object?>(dataReader.FieldCount);

        for (var ordinal = 0; ordinal < dataReader.FieldCount; ordinal++)
        {
            columns[dataReader.GetName(ordinal)] = values[ordinal];
        }

        return new(columns);
    }
}
