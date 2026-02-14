// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using LinkDotNet.StringBuilder;
using RentADeveloper.DbConnectionPlus.Extensions;

namespace RentADeveloper.DbConnectionPlus.SqlStatements;

/// <summary>
/// Represents an SQL statement constructed using interpolated string syntax.
/// </summary>
/// <remarks>
/// This type enables passing values as parameters and sequences of values as temporary tables to SQL statements via
/// expressions inside interpolated strings.
/// Therefore, this type implements the C# interpolated string handler pattern
/// (see https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/tutorials/interpolated-string-handler).
/// </remarks>
[InterpolatedStringHandler]
[DebuggerTypeProxy(typeof(InterpolatedSqlStatementDebugView))]
// ReSharper disable once StructCanBeMadeReadOnly
public struct InterpolatedSqlStatement : IEquatable<InterpolatedSqlStatement>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InterpolatedSqlStatement" /> class.
    /// </summary>
    /// <param name="literalLength">The length of the interpolated string.</param>
    /// <param name="formattedCount">The number of expressions used in the interpolated string.</param>
    /// <remarks>
    /// This constructor is part of the interpolated string handler pattern.
    /// It is not intended to be called by user code.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    // ReSharper disable once UnusedParameter.Local
#pragma warning disable RCS1163 // Unused parameter
    public InterpolatedSqlStatement(Int32 literalLength, Int32 formattedCount)
#pragma warning restore RCS1163 // Unused parameter
    {
        this.fragments = new(formattedCount);
        this.temporaryTables = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InterpolatedSqlStatement" /> class from the specified SQL code
    /// and parameters.
    /// </summary>
    /// <param name="code">The code of the SQL statement.</param>
    /// <param name="parameters">The parameters of the SQL statement.</param>
    /// <exception cref="ArgumentNullException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <paramref name="code" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="parameters" /> is <see langword="null" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="parameters" /> contains a duplicate parameter.</exception>
    /// <remarks>
    /// If a parameter value is an <see cref="Enum" />, it is serialized according to
    /// <see cref="DbConnectionPlusConfiguration.EnumSerializationMode" />.
    /// </remarks>
    public InterpolatedSqlStatement(String code, params (String Name, Object? Value)[] parameters)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(parameters);

        this.fragments = new(1 /* fragment for the code */ + parameters.Length /* fragments for the parameters */);
        this.temporaryTables = [];

        this.fragments.Add(new Literal(code));

        var duplicateParameters = parameters
            .GroupBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicateParameters.Count > 0)
        {
            var duplicateParameterNames = duplicateParameters.SelectMany(a => a.Select(b => $"'{b.Name}'")).ToList();

            throw new ArgumentException(
                "The specified parameters have the following duplicate parameter names: " +
                $"{String.Join(", ", duplicateParameterNames)}. Make sure each parameter name is only used once.",
                nameof(parameters)
            );
        }

        foreach (var parameter in parameters)
        {
            this.fragments.Add(new Parameter(parameter.Name, parameter.Value));
        }
    }

    /// <summary>
    /// Appends the specified value to this instance.
    /// </summary>
    /// <typeparam name="T">The type of value to append.</typeparam>
    /// <param name="value">The value to append.</param>
    /// <param name="alignment">
    /// The minimum number of characters that should be written for <paramref name="value" />.
    /// A negative value indicates that the value should be left-aligned and the required minimum whitespace characters
    /// to add is the absolute value.
    /// </param>
    /// <param name="format">The string to use to format <paramref name="value" />.</param>
    /// <remarks>
    /// This method is part of the interpolated string handler pattern.
    /// It is not intended to be called by user code.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted<T>(T? value, Int32 alignment = 0, String? format = null)
    {
        switch (value)
        {
            case InterpolatedParameter interpolatedParameter:
                this.fragments.Add(interpolatedParameter);
                break;

            case InterpolatedTemporaryTable interpolatedTemporaryTable:
                this.fragments.Add(interpolatedTemporaryTable);
                this.temporaryTables.Add(interpolatedTemporaryTable);
                break;

            default:
                var formattedValue =
                    value switch
                    {
                        String stringValue => stringValue,
                        IFormattable formattable => formattable.ToString(format, CultureInfo.InvariantCulture),
                        null => String.Empty,
                        _ => value.ToString() ?? String.Empty
                    };

                if (alignment != 0)
                {
                    var paddingWidth = Math.Abs(alignment);
                    var padding = paddingWidth - formattedValue.Length;

                    if (padding > 0)
                    {
                        if (alignment > 0)
                        {
                            // Right-align:
                            this.fragments.Add(new Literal(new String(' ', padding) + formattedValue));
                        }
                        else
                        {
                            // Left-align:
                            this.fragments.Add(new Literal(formattedValue + new String(' ', padding)));
                        }

                        break;
                    }
                }

                this.fragments.Add(new Literal(formattedValue));
                break;
        }
    }

    /// <summary>
    /// Appends the specified literal value to this instance.
    /// </summary>
    /// <param name="value">The literal value to append to this instance.</param>
    /// <remarks>
    /// This method is part of the interpolated string handler pattern.
    /// It is not intended to be called by user code.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendLiteral(String? value)
    {
        if (value is not null)
        {
            this.fragments.Add(new Literal(value));
        }
    }

    /// <inheritdoc />
    public readonly Boolean Equals(InterpolatedSqlStatement other) =>
        this.fragments.SequenceEqual(other.Fragments);

    /// <inheritdoc />
    public readonly override Boolean Equals(Object? obj) =>
        obj is InterpolatedSqlStatement other && this.Equals(other);

    /// <inheritdoc />
    public readonly override Int32 GetHashCode()
    {
        var hashCode = new HashCode();

        foreach (var fragment in this.fragments)
        {
            hashCode.Add(fragment);
        }

        return hashCode.ToHashCode();
    }

    /// <inheritdoc />
    public readonly override String ToString()
    {
        using var stringBuilder = new ValueStringBuilder(stackalloc Char[500]);

        stringBuilder.AppendLine("SQL Statement");
        stringBuilder.AppendLine("");

        stringBuilder.AppendLine("Statement Code");
        stringBuilder.AppendLine("--------------");

        var parameters = new Dictionary<String, Object?>(StringComparer.Ordinal);
        var interpolatedTemporaryTables = new List<InterpolatedTemporaryTable>();

        foreach (var fragment in this.fragments)
        {
            switch (fragment)
            {
                case Literal literal:
                    stringBuilder.Append(literal.Value);
                    break;

                case InterpolatedParameter interpolatedParameter:
                    var parameterName = interpolatedParameter.InferredName;

                    if (String.IsNullOrWhiteSpace(parameterName))
                    {
                        parameterName = "Parameter_" + (parameters.Count + 1).ToString(CultureInfo.InvariantCulture);
                    }

                    if (parameters.ContainsKey(parameterName))
                    {
                        var suffix = 2;

                        var newParameterName = parameterName + suffix.ToString(CultureInfo.InvariantCulture);

                        while (parameters.ContainsKey(newParameterName))
                        {
                            suffix++;
                            newParameterName = parameterName + suffix.ToString(CultureInfo.InvariantCulture);
                        }

                        parameterName = newParameterName;
                    }

                    parameters.Add(parameterName, interpolatedParameter.Value);

                    stringBuilder.Append('@');
                    stringBuilder.Append(parameterName);
                    break;

                case InterpolatedTemporaryTable interpolatedTemporaryTable:
                    stringBuilder.Append(interpolatedTemporaryTable.Name);
                    interpolatedTemporaryTables.Add(interpolatedTemporaryTable);
                    break;

                case Parameter parameter:
                    parameters.Add(parameter.Name, parameter.Value);
                    break;
            }
        }

        stringBuilder.AppendLine();
        stringBuilder.AppendLine("--------------");

        stringBuilder.AppendLine();
        stringBuilder.AppendLine("Statement Parameters");
        stringBuilder.AppendLine("--------------------");

        foreach (var (name, value) in parameters)
        {
            stringBuilder.Append(name);
            stringBuilder.Append(" = ");
            stringBuilder.AppendLine(value.ToDebugString());
        }

        stringBuilder.AppendLine();
        stringBuilder.AppendLine("Statement Temporary Tables");
        stringBuilder.AppendLine("--------------------------");

        foreach (var temporaryTable in interpolatedTemporaryTables)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(temporaryTable.Name);
            stringBuilder.AppendLine(new String('-', temporaryTable.Name.Length));

            foreach (var value in temporaryTable.Values)
            {
                stringBuilder.AppendLine(value.ToDebugString());
            }
        }

        return stringBuilder.ToString();
    }

    /// <summary>
    /// Creates a new instance of <see cref="InterpolatedSqlStatement" /> from the specified string.
    /// </summary>
    /// <param name="value">
    /// The string from which to create an instance of <see cref="InterpolatedSqlStatement" />.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    public static InterpolatedSqlStatement FromString(String value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new(value);
    }

    /// <summary>
    /// Determines whether the two specified instances of <see cref="InterpolatedSqlStatement" /> are equal.
    /// </summary>
    /// <param name="left">The first instance to compare.</param>
    /// <param name="right">The second instance to compare.</param>
    /// <returns>
    /// <see langword="true" /> if the two specified instances of <see cref="InterpolatedSqlStatement" /> are
    /// equal; otherwise, <see langword="false" />.
    /// </returns>
    public static Boolean operator ==(InterpolatedSqlStatement left, InterpolatedSqlStatement right) =>
        left.Equals(right);

    /// <summary>
    /// Implicitly converts a string to an instance of <see cref="InterpolatedSqlStatement" />.
    /// </summary>
    /// <param name="value">The string to convert to an instance of <see cref="InterpolatedSqlStatement" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    public static implicit operator InterpolatedSqlStatement(String value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new(value);
    }

    /// <summary>
    /// Determines whether the two specified instances of <see cref="InterpolatedSqlStatement" /> are unequal.
    /// </summary>
    /// <param name="left">The first instance to compare.</param>
    /// <param name="right">The second instance to compare.</param>
    /// <returns>
    /// <see langword="true" /> if the two the specified instances of <see cref="InterpolatedSqlStatement" /> are
    /// unequal; otherwise, <see langword="false" />.
    /// </returns>
    public static Boolean operator !=(InterpolatedSqlStatement left, InterpolatedSqlStatement right) =>
        !(left == right);

    /// <summary>
    /// The fragments that make up this SQL statement.
    /// </summary>
    internal IReadOnlyList<IInterpolatedSqlStatementFragment> Fragments => this.fragments;

    /// <summary>
    /// The temporary tables used in this SQL statement.
    /// </summary>
    internal readonly IReadOnlyList<InterpolatedTemporaryTable> TemporaryTables => this.temporaryTables;

    private readonly List<IInterpolatedSqlStatementFragment> fragments;
    private readonly List<InterpolatedTemporaryTable> temporaryTables;
}
