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
    /// <paramref name="propertyExpression"/> is not a valid property access expression.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="propertyExpression" /> is <see langword="null" />.
    /// </exception>
    public EntityPropertyBuilder Property<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
    {
        ArgumentNullException.ThrowIfNull(propertyExpression);

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
    public EntityTypeBuilder<TEntity> ToTable(String tableName)
    {
        this.EnsureNotFrozen();

        this.tableName = tableName;

        return this;
    }

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
    Type IEntityTypeBuilder.EntityType => typeof(TEntity);

    /// <inheritdoc />
    IReadOnlyDictionary<String, IEntityPropertyBuilder> IEntityTypeBuilder.PropertyBuilders =>
        this.propertyBuilders;

    /// <inheritdoc />
    String? IEntityTypeBuilder.TableName => this.tableName;

    private void EnsureNotFrozen()
    {
        if (this.isFrozen)
        {
            ThrowHelper.ThrowConfigurationIsFrozenException();
        }
    }

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
