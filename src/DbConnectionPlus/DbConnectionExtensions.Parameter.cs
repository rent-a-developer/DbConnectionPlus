// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using RentADeveloper.DbConnectionPlus.Helpers;
using RentADeveloper.DbConnectionPlus.SqlStatements;

namespace RentADeveloper.DbConnectionPlus;

/// <summary>
/// Provides extension members for the type <see cref="DbConnection" />.
/// </summary>
public static partial class DbConnectionExtensions
{
    /// <summary>
    /// Wraps <paramref name="parameterValue" /> in an instance of <see cref="InterpolatedParameter" /> to indicate
    /// that this value should be passed as a parameter to an SQL statement.
    /// 
    /// Use this method to pass a value in an interpolated string as a parameter to an SQL statement.
    /// </summary>
    /// <param name="parameterValue">The value to pass as a parameter.</param>
    /// <param name="parameterValueExpression">
    /// The expression from which <paramref name="parameterValue" /> was obtained.
    /// Used to infer the name for the parameter.
    /// This parameter is optional and is automatically provided by the compiler.
    /// </param>
    /// <returns>
    /// An instance of <see cref="InterpolatedParameter" /> indicating that <paramref name="parameterValue" /> should
    /// be passed as a parameter to an SQL statement.
    /// </returns>
    /// <remarks>
    /// To use this method, import <see cref="DbConnectionExtensions" /> with a using directive with the static
    /// modifier:
    /// <code>
    /// using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;
    /// </code>
    /// Example:
    /// <code>
    /// <![CDATA[
    /// using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;
    /// 
    /// var lowStockThreshold = configuration.Thresholds.LowStock;
    /// 
    /// var lowStockProductsReader = connection.ExecuteReader(
    ///    $"""
    ///     SELECT  *
    ///     FROM    Product
    ///     WHERE   UnitsInStock < {Parameter(lowStockThreshold)}
    ///     """
    /// );
    /// ]]>
    /// </code>
    /// This will add a parameter with the name "LowStockThreshold" and the value of the variable "lowStockThreshold"
    /// to the SQL statement.
    /// 
    /// The name of the parameter will be inferred from the expression from which <paramref name="parameterValue" />
    /// was obtained.
    /// If the name cannot be inferred from the expression a generic name like "Parameter_1", "Parameter_2", and so
    /// on will be used.
    /// 
    /// If you pass an <see cref="Enum" /> value as a parameter, the enum value is serialized according to the setting
    /// <see cref="DbConnectionExtensions.EnumSerializationMode" />.
    /// </remarks>
    public static InterpolatedParameter Parameter(
        Object? parameterValue,
        [CallerArgumentExpression(nameof(parameterValue))]
        String? parameterValueExpression = null
    )
    {
        String? inferredParameterName = null;

        if (!String.IsNullOrWhiteSpace(parameterValueExpression))
        {
            var nameFromCallerArgumentExpression = NameHelper.CreateNameFromCallerArgumentExpression(
                parameterValueExpression,
                MaximumParameterNameLength
            );

            if (!String.IsNullOrWhiteSpace(nameFromCallerArgumentExpression))
            {
                inferredParameterName = nameFromCallerArgumentExpression;
            }
        }

        return new(inferredParameterName, parameterValue);
    }

    /// <summary>
    /// The maximum length for inferred parameter names. This length is supported by all major database systems.
    /// </summary>
    private const Int32 MaximumParameterNameLength = 60;
}
