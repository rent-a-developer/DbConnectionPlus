// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using System.Runtime.InteropServices;

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
        // Remove common prefixes that are not relevant for the name.

        if (expression.StartsWith("this.", StringComparison.Ordinal))
        {
            expression = expression[5..];
        }

        if (expression.StartsWith("new", StringComparison.Ordinal))
        {
            expression = expression[3..];
        }

        if (expression.StartsWith("Get", StringComparison.Ordinal))
        {
            expression = expression[3..];
        }

        var scanLength = Math.Min(expression.Length, maximumLength);
        
        var buffer = scanLength <= 512 ? stackalloc Char[scanLength] : new Char[scanLength];

        ref var src = ref MemoryMarshal.GetReference(expression);
        ref var dst = ref MemoryMarshal.GetReference(buffer);

        var count = 0;
        
        for (var i = 0; i < scanLength; i++)
        {
            var character = Unsafe.Add(ref src, i);
            
            if (
                (UInt32)(character - '0') <= 9 || // Digits
                (UInt32)(character - 'A') <= 25 || // Uppercase letters
                (UInt32)(character - 'a') <= 25 || // Lowercase letters
                character == '_'
            )
            {
                Unsafe.Add(ref dst, count++) = character;
            }
        }

        // Convert the first character to uppercase if it is a lowercase letter.
        if (count != 0 && (UInt32)(buffer[0] - 'a') <= 25)
        {
            buffer[0] = (Char)(buffer[0] - 32);
        }

        return new(buffer[..count]);
    }
}
