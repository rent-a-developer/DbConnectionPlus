// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus.Helpers;

/// <summary>
/// Provides helper functions for name inference.
/// </summary>
internal static class NameHelper
{
    /// <summary>
    /// Creates a name suitable for a parameter or temporary table from the specified expression (obtained through
    /// <see cref="CallerArgumentExpressionAttribute" />) and truncates the name to the specified maximum length.
    /// </summary>
    /// <param name="expression">The expression from which to create a name.</param>
    /// <param name="maximumLength">The maximum length of the name to return.</param>
    /// <returns>The name created from <paramref name="expression" />.</returns>
    /// <remarks>
    /// This method removes common prefixes such as "this.", "new", and "Get" from the expression. It then constructs
    /// a name by replacing any remaining non-alphanumeric characters with underscores and truncating the result
    /// to the specified maximum length.
    /// The first character of the resulting name is converted to uppercase if it is a lowercase letter.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static String CreateNameFromCallerArgumentExpression(ReadOnlySpan<Char> expression, Int32 maximumLength)
    {
        // Remove "this.":
        if (
            expression.Length >= 5 &&
            expression[0] == 't' && expression[1] == 'h' &&
            expression[2] == 'i' && expression[3] == 's' &&
            expression[4] == '.'
        )
        {
            expression = expression[5..];
        }

        // Remove "new":
        if (
            expression.Length >= 3 &&
            expression[0] == 'n' && expression[1] == 'e' && expression[2] == 'w'
        )
        {
            expression = expression[3..];
        }

        // Remove "Get":
        if (
            expression.Length >= 3 &&
            expression[0] == 'G' && expression[1] == 'e' && expression[2] == 't'
        )
        {
            expression = expression[3..];
        }

        var length = Math.Min(expression.Length, maximumLength);
        
        var buffer = length <= 512 ? stackalloc Char[length] : new Char[length];
        
        var count = 0;

        for (var i = 0; i < expression.Length && count < maximumLength; i++)
        {
            var c = expression[i];

            // Only allow letters, digits, and underscores in the name.
            if (
                (UInt32)(c - 'A') <= 'Z' - 'A' || // Uppercase letters
                (UInt32)(c - 'a') <= 'z' - 'a' || // Lowercase letters
                (UInt32)(c - '0') <= '9' - '0' || // Digits
                c == '_' // Underscores
            )
            {
                buffer[count++] = c;
            }
        }

        // Convert the first character to uppercase if it is a lowercase letter.
        if (count > 0 && (UInt32)(buffer[0] - 'a') <= 'z' - 'a')
        {
            buffer[0] = (Char)(buffer[0] - 32);
        }

        return new(buffer[..count]);
    }
}
