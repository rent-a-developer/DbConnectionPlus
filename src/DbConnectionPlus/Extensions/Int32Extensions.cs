// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using Humanizer;

namespace RentADeveloper.DbConnectionPlus.Extensions;

/// <summary>
/// Provides extension methods for the type <see cref="Int32" />.
/// </summary>
internal static class Int32Extensions
{
    /// <summary>
    /// Turns this number into an ordinal number in english notation, used to denote the position in an ordered sequence
    /// (e.g. 1st, 2nd, 3rd, 4th).
    /// </summary>
    /// <param name="value">The number to ordinalize.</param>
    /// <returns>The ordinalized number in english notation.</returns>
    internal static String OrdinalizeEnglish(this Int32 value) =>
        value.Ordinalize(englishCulture);

    private static readonly CultureInfo englishCulture = new("en-US");
}
