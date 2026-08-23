// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using System.Globalization;

namespace RentADeveloper.DbConnectionPlus.PackageConsumption;

/// <summary>
/// A minimal assertion harness, shared by every package consumer.
/// </summary>
/// <remarks>
/// <para>
/// The consumers cannot use the repository's usual test stack: xUnit, NSubstitute and AwesomeAssertions all
/// need run-time code generation themselves, which is precisely what a Native AOT binary does not have. Every
/// assertion therefore has to be hand-written, and a failure is reported through the process exit code so a CI
/// job can gate on it.
/// </para>
/// <para>
/// This file is <c>&lt;Compile Include&gt;</c>-linked into each consumer rather than shared through a project
/// reference: a project reference would introduce an assembly that did not come out of a package, which is
/// the one thing these projects exist to avoid.
/// </para>
/// </remarks>
public static class Check
{
    /// <summary>Gets the number of assertions that failed so far.</summary>
    public static Int32 FailureCount { get; private set; }

    /// <summary>Writes a section header, so the console output stays readable in a CI log.</summary>
    /// <param name="title">The title of the section.</param>
    public static void Section(String title)
    {
        Console.WriteLine();
        Console.WriteLine(new String('=', 100));
        Console.WriteLine(title);
        Console.WriteLine();
    }

    /// <summary>Asserts that <paramref name="actual" /> equals <paramref name="expected" />.</summary>
    /// <typeparam name="T">The type of the values to compare.</typeparam>
    /// <param name="label">A description of what is being asserted.</param>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The actual value.</param>
    public static void Equal<T>(String label, T expected, T actual)
    {
        if (EqualityComparer<T>.Default.Equals(expected, actual))
        {
            Pass(label);

            return;
        }

        Fail(label, $"expected [{Render(expected)}], got [{Render(actual)}]");
    }

    /// <summary>Asserts that <paramref name="condition" /> is <see langword="true" />.</summary>
    /// <param name="label">A description of what is being asserted.</param>
    /// <param name="condition">The condition that must hold.</param>
    public static void True(String label, Boolean condition)
    {
        if (condition)
        {
            Pass(label);

            return;
        }

        Fail(label, "expected true, got false");
    }

    /// <summary>
    /// Asserts that <paramref name="action" /> throws a <typeparamref name="TException" /> whose message
    /// contains <paramref name="expectedMessageFragment" />.
    /// </summary>
    /// <typeparam name="TException">The type of exception that is expected.</typeparam>
    /// <param name="label">A description of what is being asserted.</param>
    /// <param name="expectedMessageFragment">A fragment the exception message must contain.</param>
    /// <param name="action">The action that must throw.</param>
    public static void Throws<TException>(String label, String expectedMessageFragment, Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException exception)
        {
            if (exception.Message.Contains(expectedMessageFragment, StringComparison.Ordinal))
            {
                Pass(label);
            }
            else
            {
                Fail(label, $"threw {typeof(TException).Name}, but its message was [{exception.Message}]");
            }

            return;
        }
        catch (Exception exception)
        {
            // The whole exception, not just its message: under Native AOT a surprise here is usually a
            // trimming or reflection failure, and the stack trace is what identifies it.
            Fail(label, $"expected {typeof(TException).Name}, got {exception}");

            return;
        }

        Fail(label, $"expected {typeof(TException).Name}, but nothing was thrown");
    }

    private static void Pass(String label) => Console.WriteLine($"  PASS  {label}");

    private static void Fail(String label, String detail)
    {
        FailureCount++;

        Console.WriteLine($"  FAIL  {label}");
        Console.WriteLine($"        {detail}");
    }

    private static String Render(Object? value) =>
        value switch
        {
            null => "null",
            Byte[] bytes => Convert.ToHexString(bytes),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? String.Empty,
        };
}
