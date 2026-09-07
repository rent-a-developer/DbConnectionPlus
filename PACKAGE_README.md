# DbConnectionPlus

A lightweight .NET ORM and extension library for
[DbConnection](https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbconnection) that adds
high-performance, type-safe helpers to reduce boilerplate code.

Write your own SQL as an interpolated string and get parameters, entity mapping and CRUD for free. No change
tracking, no LINQ provider, and nothing to opt into under Native AOT.

- **Parameters in interpolated strings** — `{Parameter(value)}` becomes a real `DbParameter`, so there is no
  SQL injection surface and no `AddWithValue` boilerplate.
- **On-the-fly temporary tables** — pass an `IEnumerable<T>` straight into a statement with
  `{TemporaryTable(values)}`, populated by the provider's bulk-copy API where one exists.
- **Entity mapping and CRUD** — `Query<T>`, `InsertEntity`, `UpdateEntities`, `DeleteEntity` and their bulk
  and `…Async` counterparts, with optimistic concurrency.
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

Install this package plus the adapter for the database you use:

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

## Getting started

Register the adapter once, at application startup, then call the extension methods on any `DbConnection`:

```csharp
using RentADeveloper.DbConnectionPlus.Configuration;
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

DbConnectionExtensions.Configure(configuration => configuration.UseSqlServer());

// Entities, scalars and value tuples
var lowStockProducts = connection.Query<Product>(
    $"SELECT * FROM Product WHERE UnitsInStock < {Parameter(threshold)}"
);

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

## Compatibility limits

- **`net8.0` or later.** `net8.0` is the supported floor, `net10.0` is recommended. There is no .NET Framework
  or .NET Standard 2.0 support.
- **No multi-mapping, no multiple result sets, no custom type-conversion handlers.**
- **`dynamic row.Id` does not work under Native AOT** — use the `row["Id"]` indexer, which works everywhere.
- **End-to-end AOT support is bounded by your ADO.NET driver.** SQLite, MySQL and SQL Server publish and run
  clean; some Npgsql type plug-ins reflect; `Oracle.ManagedDataAccess.Core` is not AOT-ready.
- **Temporary tables are off by default on Oracle**, because creating or dropping a private temporary table
  implicitly commits the caller's transaction. On MySQL they need `AllowLoadLocalInfile=true` in the
  connection string and `local_infile` on the server.
- **`Configure` can be called once per process**, at startup. The configuration is frozen afterwards.

## Documentation

- **[Documentation and guides](https://rent-a-developer.github.io/DbConnectionPlus/)** — querying, parameters
  and temporary tables, entity mapping and CRUD, configuration, custom adapters, and Native AOT
- [API reference](https://rent-a-developer.github.io/DbConnectionPlus/api/RentADeveloper.DbConnectionPlus.DbConnectionExtensions.html)
- [Benchmarks](https://rent-a-developer.github.io/DbConnectionPlus/reference/performance.html)
- [Source code](https://github.com/rent-a-developer/DbConnectionPlus)
- [Change log](https://github.com/rent-a-developer/DbConnectionPlus/blob/main/CHANGELOG.md)
- [Report an issue](https://github.com/rent-a-developer/DbConnectionPlus/issues)

Licensed under the [MIT license](https://github.com/rent-a-developer/DbConnectionPlus/blob/main/LICENSE.md).
