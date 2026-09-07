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

All six packages are versioned and released together, so a release's adapter always matches its core package.

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
[the reference section](docs/reference/) carries the
[comparison with Dapper](docs/reference/comparison.md) and the
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

---

## Moved sections

Four sections used to live in this file, and links to their anchors are already published — the
`PACKAGE_README.md` inside the 4.0.0 packages on nuget.org points at one of them. The headings below keep
those anchors working. Nothing else is here.

### Why not just use Dapper?

Moved to [docs/reference/comparison.md](docs/reference/comparison.md).

### API summary

Moved to [docs/reference/api-summary.md](docs/reference/api-summary.md).

### Benchmarks

Moved to [docs/reference/performance.md](docs/reference/performance.md).

### Custom database adapter

Moved to [docs/guides/custom-adapters.md](docs/guides/custom-adapters.md).

### Native AOT and trimming

Moved to [docs/guides/native-aot.md](docs/guides/native-aot.md).
