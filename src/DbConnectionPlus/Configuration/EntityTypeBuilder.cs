using System.Linq.Expressions;
using System.Reflection;

namespace RentADeveloper.DbConnectionPlus.Configuration;

/// <summary>
/// A builder for configuring an entity type.
/// </summary>
/// <typeparam name="TEntity">The type of the entity being configured.</typeparam>
public sealed class EntityTypeBuilder<TEntity> : IEntityTypeBuilder
{
    /// <summary>
    /// Gets a builder for configuring the specified property.
    /// </summary>
    /// <typeparam name="TProperty">The property type of the property to configure.</typeparam>
    /// <param name="propertyExpression">
    /// A lambda expression representing the property to be configured (<c>blog => blog.Url</c>).
    /// </param>
    /// <returns>The builder for the specified property.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="propertyExpression" /> is not a valid property access expression.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="propertyExpression" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The configuration of DbConnectionPlus is already frozen and can no longer be modified.
    /// </exception>
    public EntityPropertyBuilder Property<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
    {
        ArgumentNullException.ThrowIfNull(propertyExpression);

        this.EnsureNotFrozen();

        var propertyName = GetPropertyNameFromPropertyExpression(propertyExpression);

        return (EntityPropertyBuilder)this.propertyBuilders.GetOrAdd(
            propertyName,
            static (propertyName2, self) => new EntityPropertyBuilder(self, propertyName2),
            this
        );
    }

    /// <summary>
    /// Maps the entity to the specified table name.
    /// </summary>
    /// <param name="tableName">The name of the table to map the entity to.</param>
    /// <returns>This builder instance for further configuration.</returns>
    /// <exception cref="InvalidOperationException">
    /// The configuration of DbConnectionPlus is already frozen and can no longer be modified.
    /// </exception>
    // ReSharper disable once ParameterHidesMember
    public EntityTypeBuilder<TEntity> ToTable(String tableName)
    {
        this.EnsureNotFrozen();

        this.tableName = tableName;

        return this;
    }

    /// <inheritdoc />
    Type IEntityTypeBuilder.EntityType => typeof(TEntity);

    /// <inheritdoc />
    void IFreezable.Freeze()
    {
        this.isFrozen = true;

        foreach (var propertyBuilder in this.propertyBuilders.Values)
        {
            propertyBuilder.Freeze();
        }
    }

    /// <inheritdoc />
    IReadOnlyDictionary<String, IEntityPropertyBuilder> IEntityTypeBuilder.PropertyBuilders =>
        this.propertyBuilders;

    /// <inheritdoc />
    String? IEntityTypeBuilder.TableName => this.tableName;

    /// <summary>
    /// Ensures this instance is not frozen.
    /// </summary>
    /// <exception cref="InvalidOperationException">This object is already frozen.</exception>
    private void EnsureNotFrozen()
    {
        if (this.isFrozen)
        {
            ThrowHelper.ThrowConfigurationIsFrozenException();
        }
    }

    /// <summary>
    /// Gets the name of the property accessed in the specified property access expression.
    /// </summary>
    /// <param name="propertyExpression">The property access expression to get the property name from.</param>
    /// <returns>The name of the property accessed in <paramref name="propertyExpression" />.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="propertyExpression" /> is not a valid property access expression.
    /// </exception>
    private static String GetPropertyNameFromPropertyExpression(LambdaExpression propertyExpression)
    {
        if (propertyExpression.Body is MemberExpression { Member: PropertyInfo propertyInfo }) return propertyInfo.Name;

        throw new ArgumentException(
            $"The expression '{propertyExpression}' is not a valid property access expression. The expression should " +
            "represent a simple property access: 'a => a.MyProperty'.",
            nameof(propertyExpression)
        );
    }

    private readonly ConcurrentDictionary<String, IEntityPropertyBuilder> propertyBuilders = new();
    private Boolean isFrozen;
    private String? tableName;
}
