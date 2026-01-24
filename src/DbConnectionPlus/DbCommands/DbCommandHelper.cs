// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus.DbCommands;

/// <summary>
/// Provides helper functions for working with <see cref="DbCommand" />.
/// </summary>
internal static class DbCommandHelper
{
    /// <summary>
    /// Registers a callback to cancel <paramref name="command" /> if <paramref name="cancellationToken" /> is
    /// triggered.
    /// </summary>
    /// <param name="command">
    /// The <see cref="DbCommand" /> to be canceled when <paramref name="cancellationToken" /> is triggered.
    /// </param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken" /> to monitor for cancellation requests.
    /// If the token cannot be canceled, no callback will be registered.
    /// </param>
    /// <returns>
    /// A <see cref="CancellationTokenRegistration" /> representing the callback registration.
    /// If the token cannot be canceled, the default of <see cref="CancellationTokenRegistration" /> is returned.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="command" /> is <see langword="null" />.</exception>
    internal static CancellationTokenRegistration RegisterDbCommandCancellation(
        DbCommand command,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        return cancellationToken.CanBeCanceled
            ? cancellationToken.UnsafeRegister(static state => ((DbCommand)state!).Cancel(), command)
            : default;
    }
}
