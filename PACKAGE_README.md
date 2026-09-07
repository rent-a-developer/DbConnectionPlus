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

All six packages are versioned and released together, so a release's adapter always matches its core package.

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
- [Comparison with Dapper](https://rent-a-developer.github.io/DbConnectionPlus/reference/comparison.html)
- [Benchmarks](https://rent-a-developer.github.io/DbConnectionPlus/reference/performance.html)
- [Source code](https://github.com/rent-a-developer/DbConnectionPlus)
- [Change log](https://github.com/rent-a-developer/DbConnectionPlus/blob/main/CHANGELOG.md)
- [Report an issue](https://github.com/rent-a-developer/DbConnectionPlus/issues)

Licensed under the [MIT license](https://github.com/rent-a-developer/DbConnectionPlus/blob/main/LICENSE.md).
