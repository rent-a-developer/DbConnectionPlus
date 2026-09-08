<div align="center">

![logo](assets/logo-40.png)

# DbConnectionPlus

**A lightweight .NET ORM and extension library for
[DbConnection](https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbconnection)
that adds high-performance, type-safe helpers to reduce boilerplate code, boost productivity, and make working with
SQL databases in C# more enjoyable.**

[![CI](https://github.com/rent-a-developer/DbConnectionPlus/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/rent-a-developer/DbConnectionPlus/actions/workflows/ci.yml)
[![Coverage](https://codecov.io/gh/rent-a-developer/DbConnectionPlus/branch/main/graph/badge.svg)](https://codecov.io/gh/rent-a-developer/DbConnectionPlus)
[![NuGet Version](https://img.shields.io/nuget/v/DbConnectionPlus)](https://www.nuget.org/packages/DbConnectionPlus/)
[![.NET 8 | 10](https://img.shields.io/badge/.NET-8.0%20%7C%2010.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![API docs](https://img.shields.io/badge/API%20docs-GitHub%20Pages-2b6cb0)](https://rent-a-developer.github.io/DbConnectionPlus/)
[![license](https://img.shields.io/badge/License-MIT-purple.svg)](LICENSE.md)
![semver](https://img.shields.io/badge/semver-4.0.0-blue)

</div>

Write your own SQL as an interpolated string and get parameters, entity mapping and CRUD for free. No change
tracking, no LINQ provider, no `DataTable`, and nothing to opt into under Native AOT.

```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

var lowStockProducts = connection.Query<Product>(
    $"SELECT * FROM Product WHERE UnitsInStock < {Parameter(threshold)}"
);
```

## Why not just use Dapper?

This library follows the same philosophy as Dapper: extension methods on `DbConnection`, your own SQL, no change tracking and no LINQ provider, but with the following differences and additions:

- Querying, CRUD and AOT support in one package. For dapper you need Dapper, Dapper.Contrib and Dapper.AOT.
- Pass parameters directly inside interpolated strings. SQL injection safe.
- Pass collections as on-the-fly temporary tables directly inside interpolated strings. Also SQL injection safe.
- Optimistic concurrency out-of-the-box.
- Full native AOT compatibility (including support for ValueTuples, which Dapper.AOT lacks).
- Customizable Enum serialization (string/integer).
- Great performance.

### Your values stay inside your query

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

### Throw a whole collection at the database

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

### Full optimistic concurrence support

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

### The attributes you already use

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

### Native AOT support with no extra steps

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

### Enums, your way, in one line

```csharp
DbConnectionExtensions.Configure(config => config.EnumSerializationMode = EnumSerializationMode.Strings);
```

Readable strings or compact integers - the choice applies everywhere at once: entity properties, parameters and
temporary tables. Reading understands both, whichever your columns happen to hold. Dapper writes the underlying
number unless you write and register a type handler yourself - which Dapper.AOT's generated code then ignores.

### Untyped rows without `dynamic`

```csharp
var row = connection.QueryFirst($"SELECT * FROM Product WHERE Id = {Parameter(productId)}");
var name = row["Name"];
```

No cast, no `dynamic`, and it works under Native AOT - where Dapper's `dynamic` rows cannot be bound at all. If you
do want member access, one `dynamic` reference gives you `row.Name`; it is an option here rather than the only way
in.

### See every command your app sends

```csharp
DbConnectionExtensions.Configure(config =>
{
    config.InterceptDbCommand = (command, _) => logger.LogDebug("SQL: {Sql}", command.CommandText);
});
```

One hook, every statement DbConnectionPlus builds, right before it executes - for logging, a timeout, or a query
hint. Dapper has nothing comparable.

### And it costs you nothing

The [benchmark suite](docs/reference/performance.md) in this repository runs every feature three ways - hand-written `DbCommand`,
Dapper and DbConnectionPlus - against in-memory SQLite, the harshest possible setting because the query itself is
almost free there. The two libraries trade places from category to category, both within a small multiple of raw
ADO.NET, and DbConnectionPlus allocates less than Dapper in eleven of the seventeen categories.

### When Dapper is still the better pick

Being honest about it: Dapper has multi-mapping (`splitOn`) and `QueryMultiple` for several result sets from one
command, custom `ITypeHandler` conversions, support for .NET Framework and .NET Standard 2.0, and fifteen years of
ecosystem. DbConnectionPlus has none of the first three, requires `net8.0` or later, and needs a
[custom adapter](docs/guides/custom-adapters.md) for database systems beyond the [database systems it supports](#installation).

## Requirements

| | |
|---|---|
| Framework | `net8.0` or later. `net8.0` is the supported floor; `net10.0` is recommended |
| Database | MySQL, Oracle, PostgreSQL, SQLite or SQL Server — or [your own adapter](docs/guides/custom-adapters.md) |
| Dependencies | two, in the core package: `LinkDotNet.StringBuilder` and `Humanizer.Core`. Each adapter package adds its ADO.NET driver and nothing else |

There is no support for .NET Framework or .NET Standard 2.0.

## Installation

Install the core package plus the adapter for the database you use:

```shell
dotnet add package DbConnectionPlus
dotnet add package DbConnectionPlus.DatabaseAdapters.SqlServer
```

| Database | Adapter package | Driver it brings |
|---|---|---|
| SQL Server | `DbConnectionPlus.DatabaseAdapters.SqlServer` | `Microsoft.Data.SqlClient` |
| MySQL | `DbConnectionPlus.DatabaseAdapters.MySql` | `MySqlConnector` |
| PostgreSQL | `DbConnectionPlus.DatabaseAdapters.PostgreSql` | `Npgsql` |
| SQLite | `DbConnectionPlus.DatabaseAdapters.Sqlite` | `Microsoft.Data.Sqlite` |
| Oracle | `DbConnectionPlus.DatabaseAdapters.Oracle` | `Oracle.ManagedDataAccess.Core` |

The packages are versioned and released together, so a release's adapter always matches its core package.

## Quick start

Register the adapter once, at application startup:

```csharp
using RentADeveloper.DbConnectionPlus.Configuration;

DbConnectionExtensions.Configure(configuration => configuration.UseSqlServer());
```

Then call the extension methods on any `DbConnection`:

```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

class Product
{
    [Key]
    public long Id { get; set; }
    public int UnitsInStock { get; set; }
}

// Entities, scalars and value tuples
var lowStockProducts = connection.Query<Product>(
    $"SELECT * FROM Product WHERE UnitsInStock < {Parameter(threshold)}"
);
var numberOfProducts = connection.ExecuteScalar<int>($"SELECT COUNT(*) FROM Product");

// A row without a type - read columns through the indexer
var row = connection.QueryFirst($"SELECT * FROM Product WHERE Id = {Parameter(productId)}");
var unitsInStock = row["UnitsInStock"];

// A whole collection, as a temporary table the database can join against
var affected = connection.Query<Product>(
    $"SELECT * FROM Product WHERE SupplierId IN (SELECT Value FROM {TemporaryTable(retiredSupplierIds)})"
);

// CRUD, with optimistic concurrency
connection.InsertEntity(newProduct);
connection.UpdateEntities(lowStockProducts);
connection.DeleteEntity(discontinuedProduct);
```

Every method has an `…Async` counterpart, and all of them accept an optional transaction, command timeout,
command type and cancellation token.

## What it does

| | |
|---|---|
| [Parameters in interpolated strings](docs/guides/parameters-and-temporary-tables.md) | `{Parameter(value)}` becomes a real `DbParameter`. The value is never concatenated into the SQL |
| [On-the-fly temporary tables](docs/guides/parameters-and-temporary-tables.md#on-the-fly-temporary-tables-via-interpolated-strings) | `{TemporaryTable(values)}` bulk-loads an `IEnumerable<T>` into a temporary table and drops it afterwards |
| [Querying](docs/guides/querying.md) | `Query<T>` and friends map to entities, records, scalars and value tuples — or to an untyped `DataRow` |
| [Entity mapping and CRUD](docs/guides/entity-mapping-and-crud.md) | `[Table]`, `[Column]`, `[Key]` and the rest of `System.ComponentModel.DataAnnotations`, or a fluent API. Insert, update and delete with optimistic concurrency |
| [Configuration](docs/guides/configuration.md) | One `Configure` call: adapters, enum serialization, and a hook that sees every command before it runs |
| [Native AOT](docs/guides/native-aot.md) | Reference the package and publish. No companion package, no source generator, no `IL2xxx` warnings |

The detail lives in [the guides](docs/guides/), the API reference is
[published from the XML docs](https://rent-a-developer.github.io/DbConnectionPlus/), and
[the reference section](docs/reference/) carries the [API summary](docs/reference/api-summary.md) and the
[benchmark results](docs/reference/performance.md).

## Support limits

Read these before adopting it; each one is a thing this library does **not** do.

- **No multi-mapping and no multiple result sets.** There is no `splitOn` and no `QueryMultiple`.
- **No custom type-conversion handlers.** The conversions are the ones in the box.
- **`dynamic row.Id` does not work under Native AOT.** The Dynamic Language Runtime binds by generating code.
  Use the `row["Id"]` indexer, which works everywhere. See [Native AOT](docs/guides/native-aot.md).
- **End-to-end AOT support is bounded by your ADO.NET driver.** SQLite, MySQL and SQL Server publish and run
  clean; some Npgsql type plug-ins reflect; `Oracle.ManagedDataAccess.Core` is not AOT-ready, and this library
  cannot fix that. The [per-provider matrix](docs/guides/native-aot.md#supported-providers) has the detail.
- **Temporary tables are off by default on Oracle**, because creating or dropping a private temporary table
  implicitly commits the caller's transaction. [Why, and how to enable them](docs/guides/parameters-and-temporary-tables.md#on-the-fly-temporary-tables-via-interpolated-strings).
- **MySQL temporary tables need `AllowLoadLocalInfile=true`** in the connection string and `local_infile` on
  the server, because they are populated with `MySqlBulkCopy`.
- **`Configure` can be called once per process**, at startup. The configuration is frozen afterwards.

## Links

- [Documentation](docs/) — guides, reference, and the design record
- [API reference](https://rent-a-developer.github.io/DbConnectionPlus/)
- [Change log](CHANGELOG.md)
- [Design decisions](docs/DESIGN-DECISIONS.md) — why it works the way it does
- [Contributing](CONTRIBUTING.md) · [Code of conduct](CODE_OF_CONDUCT.md) · [Security policy](SECURITY.md)
- Licensed under the [MIT license](LICENSE.md)

## Contributors

- David Liebeherr ([info@rent-a-developer.de](mailto:info@rent-a-developer.de))
