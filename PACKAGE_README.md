# DbConnectionPlus

A lightweight .NET ORM and extension library for [DbConnection](https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbconnection) that adds high-performance, type-safe helpers to reduce boilerplate code.

Write SQL as an interpolated string, get parameters and entity mapping for free:

```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

var lowStockProducts = connection.Query<Product>(
    $"SELECT * FROM Product WHERE UnitsInStock < {Parameter(threshold)}"
);
```

- **Parameters via interpolated strings** — `{Parameter(value)}` becomes a real `DbParameter`, so there is no
  SQL injection surface and no `AddWithValue` boilerplate.
- **On-the-fly temporary tables** — pass an `IEnumerable<T>` straight into a statement with
  `{TemporaryTable(values)}`, populated by the provider's bulk-copy API where one exists.
- **Entity mapping** — `Query<T>`, `InsertEntity`, `UpdateEntities`, `DeleteEntity` and their bulk and
  `…Async` counterparts, with optimistic concurrency support.
- **Native AOT and trimming ready** — no companion package, no source generator, nothing to opt into, and no
  `IL2xxx` / `IL3xxx` warnings in your publish.
- **Minimal overhead** — close to hand-written ADO.NET, and minimal allocations.

## Why not just use Dapper?

This library follows the same philosophy as Dapper: extension methods on `DbConnection`, your own SQL, no change tracking and no LINQ provider, but with the following differences and additions:

- Querying, CRUD and AOT support in one package. For dapper you need Dapper, Dapper.Contrib and Dapper.AOT.
- Pass parameters directly inside interpolated strings. SQL injection safe.
- Pass collections as on-the-fly temporary tables directly inside interpolated strings. Also SQL injection safe.
- Optimistic concurrency out-of-the-box.
- Full native AOT compatibility (including support for ValueTuples, which Dapper.AOT lacks).
- Customizable Enum serialization (string/integer).
- Great performance.

**Your values stay inside your query**

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

**Throw a whole collection at the database**

```csharp
connection.Query<Product>(
    $"SELECT * FROM Product WHERE SupplierId IN (SELECT Value FROM {TemporaryTable(retiredSupplierIds)})"
);
```

A temporary table is created, your collection is bulk-loaded into it (`SqlBulkCopy` and the equivalent elsewhere)
and it is dropped afterwards. Objects work too, so you can `JOIN` against in-memory data. Dapper expands
`IN @ids` into one parameter per element instead - a new query plan per list length, and a hard stop at the
provider's parameter limit.

**Full optimistic concurrence support**

```csharp
connection.InsertEntity(newProduct);   // database-generated values come back filled in
connection.UpdateEntity(product);      // throws if someone else changed the row first
connection.DeleteEntities(discontinuedProducts);
```

One entity or a sequence, sync or `…Async` - and all of it supports optimistic concurrency: mark a property
`[Timestamp]` or `[ConcurrencyCheck]` and a stale write raises `DbUpdateConcurrencyException` instead of quietly
matching no rows. Composite keys included. Dapper has no CRUD at all; Dapper.Contrib adds some, but its `UPDATE`
matches on the key alone - so there is **no optimistic concurrency anywhere in the Dapper family** - and it has no
composite keys, no Oracle support and no release since 2020.

**Native AOT support with no extra steps**

```xml
<PublishAot>true</PublishAot>
```

That is the whole setup - no companion package, no source generator, no attributes, no `IL2xxx` / `IL3xxx`
warnings. Entities, records, **value tuples**, temporary tables, CRUD and enums all keep working. Dapper needs the
separate Dapper.AOT package plus a `[DapperAot]` opt-in per method, still has no value-tuple support and no
Dapper.Contrib support, and silently leaves call sites it cannot generate as ordinary Dapper - so the build
passes and the failure appears in your published binary.

**Plus** the data annotations you already use (`[Table]`, `[Column]`, `[Key]`, `[NotMapped]`, … - or a fluent API),
one-line enum serialization as strings or integers, untyped rows through `row["Name"]` rather than `dynamic`, and
a single hook to log or tweak every command before it runs.

Dapper is still the better pick for multi-mapping and `QueryMultiple`, custom `ITypeHandler` conversions, .NET
Framework / .NET Standard 2.0, and databases without an adapter here. The
[full comparison](https://github.com/rent-a-developer/DbConnectionPlus#why-not-just-use-dapper), with benchmark
numbers, is in the repository README.

## Installation

Install the core package plus the adapter for the database you use:

```shell
dotnet add package DbConnectionPlus
dotnet add package DbConnectionPlus.DatabaseAdapters.SqlServer
```

| Database | Adapter package | Provider |
|---|---|---|
| SQL Server | `DbConnectionPlus.DatabaseAdapters.SqlServer` | `Microsoft.Data.SqlClient` |
| MySQL | `DbConnectionPlus.DatabaseAdapters.MySql` | `MySqlConnector` |
| PostgreSQL | `DbConnectionPlus.DatabaseAdapters.PostgreSql` | `Npgsql` |
| SQLite | `DbConnectionPlus.DatabaseAdapters.Sqlite` | `Microsoft.Data.Sqlite` |
| Oracle | `DbConnectionPlus.DatabaseAdapters.Oracle` | `Oracle.ManagedDataAccess.Core` |

Any other database system can be supported by implementing a custom adapter.

## Getting started

Register the adapter(s) once at application startup:

```csharp
using RentADeveloper.DbConnectionPlus.Configuration;

DbConnectionExtensions.Configure(config => config.UseSqlServer());
```

Then use the extension methods on any `DbConnection`:

```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

// Query entities, scalars and value tuples
var product = connection.QuerySingle<Product>($"SELECT * FROM Product WHERE Id = {Parameter(id)}");
var count = connection.ExecuteScalar<Int32>($"SELECT COUNT(*) FROM Product");
var (id, name) = connection.QueryFirst<(Int64 Id, String Name)>($"SELECT Id, Name FROM Product");

// Rows without a type - read columns with the indexer or via dynamic member access
var row = connection.QueryFirst($"SELECT * FROM Product WHERE Id = {Parameter(id)}");
var unitsInStock = row["UnitsInStock"];

dynamic row = connection.QueryFirst($"SELECT * FROM Product WHERE Id = {Parameter(id)}");
var unitsInStock = row.UnitsInStock;

// CRUD
connection.InsertEntity(product);
connection.UpdateEntities(products);
connection.DeleteEntity(product);

// A collection as a temporary table, joined in SQL
var retired = connection.Query<Product>(
    $"""
     SELECT  *
     FROM    Product
     WHERE   SupplierId IN (SELECT Value FROM {TemporaryTable(retiredSupplierIds)})
     """
);
```

Every method has an `…Async` counterpart, and all of them accept an optional transaction, command timeout,
command type and cancellation token.

## Native AOT

Reference the packages and publish — there is nothing to install and nothing to opt into. Publishing with
`PublishAot` or `PublishTrimmed` reports no `IL2xxx` or `IL3xxx` diagnostic for any supported scenario, on
`net8.0` and `net10.0` alike.

The one API that cannot work under Native AOT is `dynamic` member access on a row (`row.Id`), because the
Dynamic Language Runtime needs run-time code generation; use the `row["Id"]` indexer instead. End-to-end
support is also bounded by your ADO.NET provider — see the AOT section of the full documentation for the
per-provider matrix.

## Documentation

- **[Full documentation and examples](https://github.com/rent-a-developer/DbConnectionPlus#readme)**
- [API reference](https://rent-a-developer.github.io/DbConnectionPlus/)
- [Change log](https://github.com/rent-a-developer/DbConnectionPlus/blob/main/CHANGELOG.md)
- [Report an issue](https://github.com/rent-a-developer/DbConnectionPlus/issues)

Licensed under the [MIT license](https://github.com/rent-a-developer/DbConnectionPlus/blob/main/LICENSE.md).
