# Comparison with Dapper

This library follows the same philosophy as Dapper: extension methods on `DbConnection`, your own SQL, no change tracking and no LINQ provider, but with the following differences and additions:

- Querying, CRUD and AOT support in one package. For dapper you need Dapper, Dapper.Contrib and Dapper.AOT.
- Pass parameters directly inside interpolated strings. SQL injection safe.
- Pass collections as on-the-fly temporary tables directly inside interpolated strings. Also SQL injection safe.
- Optimistic concurrency out-of-the-box.
- Full native AOT compatibility (including support for ValueTuples, which Dapper.AOT lacks).
- Customizable Enum serialization (string/integer).
- Great performance.

## Your values stay inside your query

```csharp
// Dapper
connection.Query<Product>(
    "SELECT * FROM Product WHERE SupplierId = @SupplierId AND UnitsInStock < @Threshold",
    new { SupplierId = supplierId, Threshold = threshold }
);

// DbConnectionPlus
connection.Query<Product>(
    $"SELECT * FROM Product WHERE SupplierId = {Parameter(supplierId)} AND UnitsInStock < {Parameter(threshold)}"
);
```

- No anonymous object to keep in sync with the SQL
- No renamed variable that compiles and then fails at run time
- Full auto-complete
- Full compiler verification

Each value is written where it is used, and still reaches the database as a real `DbParameter`. Nothing to name
twice, no anonymous object to keep in sync with the SQL, and no renamed variable that compiles happily and blows
up at run time.

## Throw a whole collection at the database

```csharp
var retiredSupplierIds = suppliers.Where(a => a.IsRetired).Select(a => a.Id);

var affectedProducts = connection.Query<Product>(
   $"""
    SELECT  *
    FROM    Product
    WHERE   SupplierId IN (SELECT Value FROM {TemporaryTable(retiredSupplierIds)})
    """
);
```

DbConnectionPlus creates a temporary table, bulk-loads your collection into it (`SqlBulkCopy` and the equivalent
for every other supported database) and drops it when the statement is done. Ten values or a million: no `IN` list
to build, no parameter limit to stay under, one query plan.

Objects work just as well - each property becomes a column - so you can `JOIN` straight against in-memory data:

```csharp
var orderedProducts = connection.Query<(long ProductId, int Quantity, decimal UnitPrice)>(
   $"""
    SELECT  TOrderItem.ProductId, TOrderItem.Quantity, Product.UnitPrice
    FROM    Product
            JOIN {TemporaryTable(orderItems)} TOrderItem ON TOrderItem.ProductId = Product.Id
    """
);
```

Dapper expands `IN @ids` into one parameter per element instead - a fresh query plan for every list length, and a
hard ceiling at the provider's parameter limit (2,100 on SQL Server). Table-valued parameters lift that ceiling,
but only on SQL Server, and only after you have created a table type in the database and hand-built a `DataTable`.

## Full optimistic concurrence support

```csharp
class Product
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Timestamp]
    public byte[] Version { get; set; }

    public decimal UnitPrice { get; set; }
}

connection.InsertEntity(newProduct);            // Id and Version come back filled in
connection.UpdateEntity(product);               // throws if someone else changed the row first
connection.DeleteEntities(discontinuedProducts);
```

Insert, update and delete - one entity or a whole sequence, synchronous or `…Async` - and **every one of them
supports optimistic concurrency**. Mark a property with `[Timestamp]` or `[ConcurrencyCheck]` and its original
value joins the `WHERE` clause, so an update or delete against a row that changed in the meantime raises
`DbUpdateConcurrencyException` instead of quietly matching nothing. Database-generated values are read back onto
your entity, and composite keys are supported.

Dapper has no CRUD at all - you write every `INSERT`, `UPDATE` and `DELETE` by hand. Dapper.Contrib is the usual
answer, and it does add `Insert` / `Update` / `Delete` - but its `UPDATE` matches on the key alone, so **there is
no optimistic concurrency anywhere in the Dapper family**. It also cannot handle composite keys, has no Oracle
support, and has not shipped a release since 2020.

## The attributes you already use

```csharp
[Table("Products")]
class Product
{
    [Key] public long Id { get; set; }
    [Column("ProductName")] public string Name { get; set; }
    [NotMapped] public decimal TotalPrice => this.UnitPrice * this.Quantity;
}
```

Plain `System.ComponentModel.DataAnnotations` - the same attributes EF Core reads, so your entities usually need
no changes at all. Prefer to keep your domain model clean? A fluent API configures the same things from one place.
Dapper.Contrib brings its own smaller attribute set and cannot even map a column name.

## Native AOT support with no extra steps

```xml
<PublishAot>true</PublishAot>
```

That is the entire setup. No companion package, no source generator, no attribute on anything, and no `IL2xxx` or
`IL3xxx` warning in your build. Everything keeps working: entities, records and other immutable types, **value
tuples**, temporary tables, CRUD and enums.

Dapper cannot run under Native AOT at all; you need the separate Dapper.AOT package and a `[DapperAot]` opt-in on
every method, type or module you want covered. Even then it does not support value tuples, and it does not cover
Dapper.Contrib's CRUD - so those simply have no Dapper answer under Native AOT. Worse, a call site Dapper.AOT
cannot handle is left as ordinary Dapper without any warning: the build stays green and the failure shows up in
your published binary.

## Enums, your way, in one line

```csharp
DbConnectionExtensions.Configure(config => config.EnumSerializationMode = EnumSerializationMode.Strings);
```

Readable strings or compact integers - the choice applies everywhere at once: entity properties, parameters and
temporary tables. Reading understands both, whichever your columns happen to hold. Dapper writes the underlying
number unless you write and register a type handler yourself - which Dapper.AOT's generated code then ignores.

## Untyped rows without `dynamic`

```csharp
var row = connection.QueryFirst($"SELECT * FROM Product WHERE Id = {Parameter(productId)}");
var name = row["Name"];
```

No cast, no `dynamic`, and it works under Native AOT - where Dapper's `dynamic` rows cannot be bound at all. If you
do want member access, one `dynamic` reference gives you `row.Name`; it is an option here rather than the only way
in.

## See every command your app sends

```csharp
DbConnectionExtensions.Configure(config =>
{
    config.InterceptDbCommand = (command, _) => logger.LogDebug("SQL: {Sql}", command.CommandText);
});
```

One hook, every statement DbConnectionPlus builds, right before it executes - for logging, a timeout, or a query
hint. Dapper has nothing comparable.

## And it costs you nothing

The [benchmark suite](performance.md) in this repository runs every feature three ways - hand-written `DbCommand`,
Dapper and DbConnectionPlus - against in-memory SQLite, the harshest possible setting because the query itself is
almost free there. The two libraries trade places from category to category, both within a small multiple of raw
ADO.NET, and DbConnectionPlus allocates less than Dapper in eleven of the seventeen categories.

## When Dapper is still the better pick

Being honest about it: Dapper has multi-mapping (`splitOn`) and `QueryMultiple` for several result sets from one
command, custom `ITypeHandler` conversions, support for .NET Framework and .NET Standard 2.0, and fifteen years of
ecosystem. DbConnectionPlus has none of the first three, requires `net8.0` or later, and needs a
[custom adapter](../guides/custom-adapters.md) for database systems beyond the [five it supports](https://github.com/rent-a-developer/DbConnectionPlus/blob/main/README.md#installation).
