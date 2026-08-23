// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using System.Dynamic;
using System.Linq.Expressions;

namespace RentADeveloper.DbConnectionPlus.Dynamic;

/// <summary>
/// The data of a single row returned by an SQL query.
/// </summary>
/// <remarks>
/// <para>
/// Columns are always available through the string indexer — <c>row["Id"]</c>. This is the form of access to use in
/// an application published with Native AOT, and it needs no cast.
/// </para>
/// <para>
/// Where the runtime supports dynamic code generation, a row can additionally be assigned to a
/// <see langword="dynamic" /> variable to read and write its columns as members — <c>row.Id</c>. Member access is
/// bound by the Dynamic Language Runtime and behaves exactly like the indexer, including throwing
/// <see cref="KeyNotFoundException" /> for a column the row does not contain. It is not available in an application
/// published with Native AOT, where the compiler reports the <c>dynamic</c> usage at the call site that wrote it.
/// </para>
/// <para>
/// Through a <see langword="dynamic" /> reference, reading or writing a <em>property</em> always addresses a column:
/// <c>row.Count</c> reads the column named <c>Count</c> and throws <see cref="KeyNotFoundException" /> if the row
/// has no such column — it does not read <see cref="Count" />. Calling a <em>method</em> still resolves against this
/// type, so <c>row.ContainsKey("Id")</c> and <c>row.ToString()</c> behave normally. Use a statically typed
/// <see cref="DataRow" /> reference to reach <see cref="Count" />, <see cref="Keys" /> and <see cref="Values" />.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Works everywhere, including Native AOT.
/// var id = row["Id"];
///
/// // Additionally available where run-time code generation is supported.
/// dynamic product = row;
/// var name = product.Name;
/// </code>
/// </example>
/// <param name="columns">
/// The columns of the data row.
/// The keys are expected to be the column names, and the values are expected to be the corresponding column values.
/// </param>
#pragma warning disable CA1710
public class DataRow(IDictionary<string, object?> columns) : IDictionary<string, object?>, IDynamicMetaObjectProvider
#pragma warning restore CA1710
{
    /// <summary>
    /// Reads the value of a column, used as the target of a bound dynamic member read.
    /// </summary>
    private static readonly Func<DataRow, string, object?> readColumn = static (row, columnName) => row[columnName];

    /// <summary>
    /// Writes the value of a column and returns it, used as the target of a bound dynamic member write.
    /// </summary>
    private static readonly Func<DataRow, string, object?, object?> writeColumn = static (row, columnName, value) =>
        row[columnName] = value;

    private readonly IDictionary<string, object?> columns = columns;

    /// <inheritdoc />
    public int Count => this.columns.Count;

    /// <inheritdoc />
    public bool IsReadOnly => this.columns.IsReadOnly;

    /// <inheritdoc />
    public ICollection<string> Keys => this.columns.Keys;

    /// <inheritdoc />
    public ICollection<object?> Values => this.columns.Values;

    /// <inheritdoc />
    public object? this[string key]
    {
        get => this.columns[key];
        set => this.columns[key] = value;
    }

    /// <inheritdoc />
    public void Add(KeyValuePair<string, object?> item) => this.columns.Add(item);

    /// <inheritdoc />
    public void Add(string key, object? value) => this.columns.Add(key, value);

    /// <inheritdoc />
    public void Clear() => this.columns.Clear();

    /// <inheritdoc />
    public bool Contains(KeyValuePair<string, object?> item) => this.columns.Contains(item);

    /// <inheritdoc />
    public bool ContainsKey(string key) => this.columns.ContainsKey(key);

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex) => this.columns.CopyTo(array, arrayIndex);

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => this.columns.GetEnumerator();

    /// <inheritdoc />
    public bool Remove(KeyValuePair<string, object?> item) => this.columns.Remove(item);

    /// <inheritdoc />
    public bool Remove(string key) => this.columns.Remove(key);

    /// <inheritdoc />
    public bool TryGetValue(string key, out object? value) => this.columns.TryGetValue(key, out value);

    /// <summary>
    /// Returns the <see cref="DynamicMetaObject" /> that binds member access on this row to its columns.
    /// </summary>
    /// <param name="parameter">The expression representing this row at the dynamic call site.</param>
    /// <returns>The <see cref="DynamicMetaObject" /> that binds member access on this row to its columns.</returns>
    /// <remarks>
    /// This is Dynamic Language Runtime plumbing that supports <c>row.ColumnName</c> on a
    /// <see langword="dynamic" /> reference. It is not meant to be called directly. Override it to change how member
    /// access on a derived row is bound.
    /// </remarks>
    protected virtual DynamicMetaObject GetMetaObject(Expression parameter) => new DataRowMetaObject(parameter, this);

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    /// <inheritdoc />
    DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => this.GetMetaObject(parameter);

    /// <summary>
    /// Binds member access on a <see cref="DataRow" /> to the columns of the row, so that <c>row.Id</c> resolves to
    /// the same column as <c>row["Id"]</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="DataRow" /> implements <see cref="IDynamicMetaObjectProvider" /> rather than deriving from
    /// <see cref="DynamicObject" /> deliberately. As of .NET 10 the <see cref="DynamicObject" /> constructor is
    /// annotated with <see cref="RequiresDynamicCodeAttribute" />, which would make merely constructing a row — and
    /// therefore every non-generic query method — report an AOT incompatibility, even for callers that only ever use
    /// the string indexer. The interface carries no such annotation.
    /// </para>
    /// <para>
    /// The expressions built here must therefore never go through <c>Expression.Lambda</c> or
    /// <c>LambdaExpression.Compile</c>, which are annotated with <see cref="RequiresDynamicCodeAttribute" /> and
    /// would reintroduce exactly that problem. Binding to a delegate that reads or writes the column is all this type
    /// does; the Dynamic Language Runtime owns the call site, so the run-time code generation is attributed to the
    /// consumer that wrote <see langword="dynamic" />.
    /// </para>
    /// </remarks>
    private sealed class DataRowMetaObject : DynamicMetaObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataRowMetaObject" /> class.
        /// </summary>
        /// <param name="expression">The expression representing the <see cref="DataRow" /> at the call site.</param>
        /// <param name="row">The <see cref="DataRow" /> the member access is bound against.</param>
        internal DataRowMetaObject(Expression expression, DataRow row)
            : base(expression, BindingRestrictions.Empty, row) { }

        /// <inheritdoc />
        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            ArgumentNullException.ThrowIfNull(binder);

            if (!this.HasValue)
            {
                return binder.Defer(this);
            }

            return new(
                Expression.Invoke(
                    Expression.Constant(readColumn),
                    this.GetRowExpression(),
                    Expression.Constant(binder.Name)
                ),
                this.GetTypeRestriction()
            );
        }

        /// <inheritdoc />
        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
        {
            ArgumentNullException.ThrowIfNull(binder);
            ArgumentNullException.ThrowIfNull(value);

            if (!this.HasValue || !value.HasValue)
            {
                return binder.Defer(this, value);
            }

            return new(
                Expression.Invoke(
                    Expression.Constant(writeColumn),
                    this.GetRowExpression(),
                    Expression.Constant(binder.Name),
                    Expression.Convert(value.Expression, typeof(object))
                ),
                this.GetTypeRestriction().Merge(value.Restrictions)
            );
        }

        /// <inheritdoc />
        public override IEnumerable<string> GetDynamicMemberNames() => ((DataRow)this.Value!).Keys;

        /// <summary>
        /// Gets the call-site expression converted to <see cref="DataRow" />.
        /// </summary>
        /// <returns>The call-site expression converted to <see cref="DataRow" />.</returns>
        private UnaryExpression GetRowExpression() => Expression.Convert(this.Expression, typeof(DataRow));

        /// <summary>
        /// Gets the binding restriction that limits the bound call site to the runtime type of the row.
        /// </summary>
        /// <returns>The binding restriction that limits the bound call site to the runtime type of the row.</returns>
        private BindingRestrictions GetTypeRestriction() =>
            BindingRestrictions.GetTypeRestriction(this.Expression, this.LimitType);
    }
}
