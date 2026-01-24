// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus.DbCommands;

/// <summary>
/// Handles the disposal of a <see cref="DbCommand" /> and its associated resources.
/// When disposed, disposes the command, any temporary tables created for the command, and the cancellation token
/// registration associated with the command.
/// </summary>
internal sealed class DbCommandDisposer : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DbCommandDisposer" /> class.
    /// </summary>
    /// <param name="command">The database command to dispose.</param>
    /// <param name="temporaryTableDisposers">The disposers of the temporary tables created for the command.</param>
    /// <param name="cancellationTokenRegistration">
    /// The registration of the cancellation token associated with the command.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="command" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="temporaryTableDisposers" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    public DbCommandDisposer(
        DbCommand command,
        TemporaryTableDisposer[] temporaryTableDisposers,
        CancellationTokenRegistration cancellationTokenRegistration
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(temporaryTableDisposers);

        this.command = command;
        this.temporaryTableDisposers = temporaryTableDisposers;
        this.cancellationTokenRegistration = cancellationTokenRegistration;
    }

    /// <summary>
    /// Disposes the database command and its associated resources.
    /// </summary>
    public void Dispose()
    {
        if (this.isDisposed)
        {
            return;
        }

        this.isDisposed = true;

        this.cancellationTokenRegistration.Dispose();

        this.command.Dispose();

        foreach (var tableDisposer in this.temporaryTableDisposers)
        {
            tableDisposer.Dispose();
        }
    }

    /// <summary>
    /// Asynchronously disposes the database command and its associated resources.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (this.isDisposed)
        {
            return;
        }

        this.isDisposed = true;

        await this.cancellationTokenRegistration.DisposeAsync().ConfigureAwait(false);

        await this.command.DisposeAsync().ConfigureAwait(false);

        foreach (var tableDisposer in this.temporaryTableDisposers)
        {
            await tableDisposer.DisposeAsync().ConfigureAwait(false);
        }
    }

    private readonly CancellationTokenRegistration cancellationTokenRegistration;
    private readonly DbCommand command;
    private readonly TemporaryTableDisposer[] temporaryTableDisposers;

    private Boolean isDisposed;
}
