// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using RentADeveloper.DbConnectionPlus.Converters;
using RentADeveloper.DbConnectionPlus.Extensions;
using RentADeveloper.DbConnectionPlus.Materializers;
using RentADeveloper.DbConnectionPlus.SqlStatements;
using DbCommandBuilder = RentADeveloper.DbConnectionPlus.DbCommands.DbCommandBuilder;

namespace RentADeveloper.DbConnectionPlus;

/// <summary>
/// Provides extension members for the type <see cref="DbConnection" />.
/// </summary>
public static partial class DbConnectionExtensions
{
    /// <summary>
    /// Executes the specified SQL statement and materializes the single row of the result set returned by the statement
    /// into an instance of the type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">
    /// The type of object to materialize the single row of the result set to. See remarks for details.
    /// </typeparam>
    /// <param name="connection">The database connection to use to execute the statement.</param>
    /// <param name="statement">The SQL statement to execute.</param>
    /// <param name="transaction">The database transaction within to execute the statement.</param>
    /// <param name="commandTimeout">The timeout to use for the execution of the statement.</param>
    /// <param name="commandType">A value indicating how <paramref name="statement" /> is to be interpreted.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// An instance of the type <typeparamref name="T" /> containing the data of the single row of the result set
    /// returned by the statement.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <typeparamref name="T" /> is an entity type or a <see cref="ValueTuple" /> type and the SQL
    ///                 statement returned no columns.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <typeparamref name="T" /> is an entity type or a <see cref="ValueTuple" /> type and the SQL
    ///                 statement returned a column without a name.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <typeparamref name="T" /> is an entity type or a <see cref="ValueTuple" /> type and the SQL
    ///                 statement returned a column with an unsupported data type.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <typeparamref name="T" /> is an entity type or a <see cref="ValueTuple" /> type that does not
    ///                 satisfy the conditions described in the remarks.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <exception cref="InvalidCastException">
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///             <typeparamref name="T" /> is a built-in type or a nullable built-in type and the first column of the
    ///             result set returned by the statement contains a value that could not be converted to the type
    /// <typeparamref name="T" />.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             <typeparamref name="T" /> is an entity type and a column value returned by the statement could not
    ///             be converted to the type of the corresponding constructor parameter or property of the type
    /// <typeparamref name="T" />.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             <typeparamref name="T" /> is a <see cref="ValueTuple" /> type and a column value returned by the
    ///             statement could not be converted to the type of the corresponding field of the type
    /// <typeparamref name="T" />.
    ///         </description>
    ///     </item>
    /// </list>
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///             The SQL statement did not return any rows.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             The SQL statement did return more than one row.
    ///         </description>
    ///     </item>
    /// </list>
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The statement was cancelled via <paramref name="cancellationToken" />.
    /// </exception>
    /// <remarks>
    /// <typeparamref name="T" /> can be any of the following types:
    /// <list type="number">
    ///     <item>
    ///         <term>
    ///             A built-in .NET type or a nullable built-in .NET type like <see cref="DateTime" /> or
    /// <see cref="String" />.
    ///         </term>
    ///         <description>
    ///             In this case only the first column of the result set will be read and converted to the type
    /// <typeparamref name="T" />.
    ///             Other columns in the result set will be ignored.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term>An entity type (a class or a record)</term>
    ///         <description>
    ///             In this case the single row in the result set will be materialized into an instance of the entity
    ///             type, with the constructor arguments or properties of the entity being populated from the
    ///             corresponding columns of the row.
    /// 
    ///             All columns returned by the SQL statement must have a name.
    /// 
    ///             The type <typeparamref name="T" /> must either:
    /// 
    ///             1. Have a constructor whose parameters match the columns of the result set returned by the
    ///                statement.
    ///                The names of the parameters must match the names of the columns (case-insensitive).
    ///                The types of the parameters must be compatible with the data types of the columns.
    ///                The compatibility is determined using <see cref="ValueConverter.CanConvert" />.
    ///                The parameters can be in any order.
    /// 
    ///             Or
    /// 
    ///             2. Have a parameterless constructor and properties (with public setters) that match the columns of
    ///                the result set returned by the statement.
    /// 
    ///                The names of the properties must match the names of the columns (case-insensitive).
    /// 
    ///                The types of the properties must be compatible with the data types of the columns.
    ///                The compatibility is determined using <see cref="ValueConverter.CanConvert" />.
    /// 
    ///                Columns without a matching property will be ignored.
    /// 
    ///             If neither condition is satisfied, an <see cref="ArgumentException" /> will be thrown.
    /// 
    ///             If a constructor parameter or a property cannot be set to the value of the corresponding column
    ///             due to a type mismatch, an <see cref="InvalidCastException" /> will be thrown.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term>A <see cref="ValueTuple" /> type like <see cref="ValueTuple{T1, T2, T3}" /></term>
    ///         <description>
    ///             In this case the single row in the result set will be materialized into an instance of the value
    ///             tuple type, with the fields of the value tuple being populated from the corresponding columns of the
    ///             row.
    /// 
    ///             All columns returned by the SQL statement must have a name.
    ///             The SQL statement must return the same number of columns as the value tuple has fields.
    ///             The SQL statement must return the columns in the same order as the fields in the value tuple.
    /// 
    ///             The data types of the columns must be compatible with the field types of the value tuple.
    ///             The compatibility is determined using <see cref="ValueConverter.CanConvert(Type, Type)" />.
    /// 
    ///             If those conditions are not met, an <see cref="ArgumentException" /> is thrown.
    ///         </description>
    ///     </item>
    /// </list>
    /// See <see cref="DbCommand.ExecuteReader()" /> for additional exceptions this method may throw.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;
    /// 
    /// public class Order
    /// {
    ///     [Key]
    ///     public Int64 Id { get; set; }
    ///     public DateTime OrderDate { get; set; }
    ///     public Decimal TotalAmount { get; set; }
    /// }
    /// 
    /// var order = connection.QuerySingle<Order>($"SELECT * FROM [Order] WHERE Id = {Parameter(orderId)}");
    /// ]]>
    /// </code>
    /// </example>
    public static T QuerySingle<T>(
        this DbConnection connection,
        InterpolatedSqlStatement statement,
        DbTransaction? transaction = null,
        TimeSpan? commandTimeout = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(connection);

        var databaseAdapter = DbConnectionPlusConfiguration.Instance.GetDatabaseAdapter(connection.GetType());

        var (command, commandDisposer) = DbCommandBuilder.BuildDbCommand(
            statement,
            databaseAdapter,
            connection,
            transaction,
            commandTimeout,
            commandType,
            cancellationToken
        );

        using (commandDisposer)
        {
            try
            {
                OnBeforeExecutingCommand(command, statement.TemporaryTables);
                var reader = command.ExecuteReader(CommandBehavior.SingleResult);

                using (reader)
                {
                    var isTBuiltInTypeOrEnumType = typeof(T).IsBuiltInTypeOrNullableBuiltInType() ||
                                                   typeof(T).IsEnumOrNullableEnumType();
                    var isTValueTupleType = typeof(T).IsValueTupleType();
                    var isTEntityType = !isTBuiltInTypeOrEnumType && !isTValueTupleType;

                    Func<DbDataReader, T> entityMaterializer = null!;
                    Func<DbDataReader, T> valueTupleMaterializer = null!;

                    if (isTValueTupleType)
                    {
                        valueTupleMaterializer = ValueTupleMaterializerFactory.GetMaterializer<T>(reader);
                    }
                    else if (isTEntityType)
                    {
                        entityMaterializer = EntityMaterializerFactory.GetMaterializer<T>(reader);
                    }

                    if (!reader.Read())
                    {
                        ThrowHelper.ThrowSqlStatementReturnedNoRowsException();
                    }

                    T result;

                    if (isTBuiltInTypeOrEnumType)
                    {
                        var value = reader.GetValue(0);

                        if (value is T alreadyTargetTypeValue)
                        {
                            return alreadyTargetTypeValue;
                        }

                        result = ConvertValueForQuery<T>(value);
                    }
                    else if (isTValueTupleType)
                    {
                        result = valueTupleMaterializer(reader);
                    }
                    else
                    {
                        result = entityMaterializer(reader);
                    }

                    if (reader.Read())
                    {
                        ThrowHelper.ThrowSqlStatementReturnedMoreThanOneRowException();
                    }

                    return result;
                }
            }
            catch (Exception exception) when (
                databaseAdapter.WasSqlStatementCancelledByCancellationToken(exception, cancellationToken)
            )
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }
    }

    /// <summary>
    /// Asynchronously executes the specified SQL statement and materializes the single row of the result set returned
    /// by the statement into an instance of the type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">
    /// The type of object to materialize the single row of the result set to. See remarks for details.
    /// </typeparam>
    /// <param name="connection">The database connection to use to execute the statement.</param>
    /// <param name="statement">The SQL statement to execute.</param>
    /// <param name="transaction">The database transaction within to execute the statement.</param>
    /// <param name="commandTimeout">The timeout to use for the execution of the statement.</param>
    /// <param name="commandType">A value indicating how <paramref name="statement" /> is to be interpreted.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// <see cref="Task{TResult}.Result" /> will contain an instance of the type <typeparamref name="T" /> containing
    /// the data of the single row of the result set returned by the statement.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <typeparamref name="T" /> is an entity type or a <see cref="ValueTuple" /> type and the SQL
    ///                 statement returned no columns.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <typeparamref name="T" /> is an entity type or a <see cref="ValueTuple" /> type and the SQL
    ///                 statement returned a column without a name.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <typeparamref name="T" /> is an entity type or a <see cref="ValueTuple" /> type and the SQL
    ///                 statement returned a column with an unsupported data type.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <typeparamref name="T" /> is an entity type or a <see cref="ValueTuple" /> type that does not
    ///                 satisfy the conditions described in the remarks.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <exception cref="InvalidCastException">
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///             <typeparamref name="T" /> is a built-in type or a nullable built-in type and the first column of the
    ///             result set returned by the statement contains a value that could not be converted to the type
    /// <typeparamref name="T" />.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             <typeparamref name="T" /> is an entity type and a column value returned by the statement could not
    ///             be converted to the type of the corresponding constructor parameter or property of the type
    /// <typeparamref name="T" />.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             <typeparamref name="T" /> is a <see cref="ValueTuple" /> type and a column value returned by the
    ///             statement could not be converted to the type of the corresponding field of the type
    /// <typeparamref name="T" />.
    ///         </description>
    ///     </item>
    /// </list>
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///             The SQL statement did not return any rows.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             The SQL statement did return more than one row.
    ///         </description>
    ///     </item>
    /// </list>
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The statement was cancelled via <paramref name="cancellationToken" />.
    /// </exception>
    /// <remarks>
    /// <typeparamref name="T" /> can be any of the following types:
    /// <list type="number">
    ///     <item>
    ///         <term>
    ///             A built-in .NET type or a nullable built-in .NET type like <see cref="DateTime" /> or
    /// <see cref="String" />.
    ///         </term>
    ///         <description>
    ///             In this case only the first column of the result set will be read and converted to the type
    /// <typeparamref name="T" />.
    ///             Other columns in the result set will be ignored.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term>An entity type (a class or a record)</term>
    ///         <description>
    ///             In this case the single row in the result set will be materialized into an instance of the entity
    ///             type, with the constructor arguments or properties of the entity being populated from the
    ///             corresponding columns of the row.
    /// 
    ///             All columns returned by the SQL statement must have a name.
    /// 
    ///             The type <typeparamref name="T" /> must either:
    /// 
    ///             1. Have a constructor whose parameters match the columns of the result set returned by the
    ///                statement.
    ///                The names of the parameters must match the names of the columns (case-insensitive).
    ///                The types of the parameters must be compatible with the data types of the columns.
    ///                The compatibility is determined using <see cref="ValueConverter.CanConvert" />.
    ///                The parameters can be in any order.
    /// 
    ///             Or
    /// 
    ///             2. Have a parameterless constructor and properties (with public setters) that match the columns of
    ///                the result set returned by the statement.
    /// 
    ///                The names of the properties must match the names of the columns (case-insensitive).
    /// 
    ///                The types of the properties must be compatible with the data types of the columns.
    ///                The compatibility is determined using <see cref="ValueConverter.CanConvert" />.
    /// 
    ///                Columns without a matching property will be ignored.
    /// 
    ///             If neither condition is satisfied, an <see cref="ArgumentException" /> will be thrown.
    /// 
    ///             If a constructor parameter or a property cannot be set to the value of the corresponding column
    ///             due to a type mismatch, an <see cref="InvalidCastException" /> will be thrown.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term>A <see cref="ValueTuple" /> type like <see cref="ValueTuple{T1, T2, T3}" /></term>
    ///         <description>
    ///             In this case the single row in the result set will be materialized into an instance of the value
    ///             tuple type, with the fields of the value tuple being populated from the corresponding columns of the
    ///             row.
    /// 
    ///             All columns returned by the SQL statement must have a name.
    ///             The SQL statement must return the same number of columns as the value tuple has fields.
    ///             The SQL statement must return the columns in the same order as the fields in the value tuple.
    /// 
    ///             The data types of the columns must be compatible with the field types of the value tuple.
    ///             The compatibility is determined using <see cref="ValueConverter.CanConvert(Type, Type)" />.
    /// 
    ///             If those conditions are not met, an <see cref="ArgumentException" /> is thrown.
    ///         </description>
    ///     </item>
    /// </list>
    /// See <see cref="DbCommand.ExecuteReader()" /> for additional exceptions this method may throw.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;
    /// 
    /// public class Order
    /// {
    ///     [Key]
    ///     public Int64 Id { get; set; }
    ///     public DateTime OrderDate { get; set; }
    ///     public Decimal TotalAmount { get; set; }
    /// }
    /// 
    /// var order = await connection.QuerySingleAsync<Order>($"SELECT * FROM [Order] WHERE Id = {Parameter(orderId)}");
    /// ]]>
    /// </code>
    /// </example>
    public static async Task<T> QuerySingleAsync<T>(
        this DbConnection connection,
        InterpolatedSqlStatement statement,
        DbTransaction? transaction = null,
        TimeSpan? commandTimeout = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(connection);

        var databaseAdapter = DbConnectionPlusConfiguration.Instance.GetDatabaseAdapter(connection.GetType());

        var (command, commandDisposer) = await DbCommandBuilder.BuildDbCommandAsync(
            statement,
            databaseAdapter,
            connection,
            transaction,
            commandTimeout,
            commandType,
            cancellationToken
        ).ConfigureAwait(false);

        using (commandDisposer)
        {
            try
            {
                OnBeforeExecutingCommand(command, statement.TemporaryTables);
                var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken)
                    .ConfigureAwait(false);

                await using (reader)
                {
                    var isTBuiltInTypeOrEnumType = typeof(T).IsBuiltInTypeOrNullableBuiltInType() ||
                                                   typeof(T).IsEnumOrNullableEnumType();
                    var isTValueTupleType = typeof(T).IsValueTupleType();
                    var isTEntityType = !isTBuiltInTypeOrEnumType && !isTValueTupleType;

                    Func<DbDataReader, T> entityMaterializer = null!;
                    Func<DbDataReader, T> valueTupleMaterializer = null!;

                    if (isTValueTupleType)
                    {
                        valueTupleMaterializer = ValueTupleMaterializerFactory.GetMaterializer<T>(reader);
                    }
                    else if (isTEntityType)
                    {
                        entityMaterializer = EntityMaterializerFactory.GetMaterializer<T>(reader);
                    }

                    if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        ThrowHelper.ThrowSqlStatementReturnedNoRowsException();
                    }

                    T result;

                    if (isTBuiltInTypeOrEnumType)
                    {
                        var value = reader.GetValue(0);

                        if (value is T alreadyTargetTypeValue)
                        {
                            return alreadyTargetTypeValue;
                        }

                        result = ConvertValueForQuery<T>(value);
                    }
                    else if (isTValueTupleType)
                    {
                        result = valueTupleMaterializer(reader);
                    }
                    else
                    {
                        result = entityMaterializer(reader);
                    }

                    if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        ThrowHelper.ThrowSqlStatementReturnedMoreThanOneRowException();
                    }

                    return result;
                }
            }
            catch (Exception exception) when (
                databaseAdapter.WasSqlStatementCancelledByCancellationToken(exception, cancellationToken)
            )
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }
    }
}
