// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus.DatabaseAdapters;

/// <summary>
/// Handles the disposal of a temporary database table.
/// When disposed, drops the temporary table.
/// </summary>
public sealed class TemporaryTableDisposer : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TemporaryTableDisposer" /> class.
    /// </summary>
    /// <param name="dropTableFunction">The function that drops the table.</param>
    /// <param name="dropTableAsyncFunction">The function that asynchronously drops the table.</param>
    /// <exception cref="ArgumentNullException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="dropTableFunction" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="dropTableAsyncFunction" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    public TemporaryTableDisposer(Action dropTableFunction, Func<ValueTask> dropTableAsyncFunction)
    {
        ArgumentNullException.ThrowIfNull(dropTableFunction);
        ArgumentNullException.ThrowIfNull(dropTableAsyncFunction);

        this.dropTableFunction = dropTableFunction;
        this.dropTableAsyncFunction = dropTableAsyncFunction;
    }

    /// <summary>
    /// Drops the temporary table.
    /// </summary>
    public void Dispose()
    {
        if (this.isDisposed)
        {
            return;
        }

        this.isDisposed = true;
        this.dropTableFunction();
    }

    /// <summary>
    /// Asynchronously drops the temporary table.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public ValueTask DisposeAsync()
    {
        if (this.isDisposed)
        {
            return ValueTask.CompletedTask;
        }

        this.isDisposed = true;
        return this.dropTableAsyncFunction();
    }

    private readonly Func<ValueTask> dropTableAsyncFunction;
    private readonly Action dropTableFunction;

    private Boolean isDisposed;
}
