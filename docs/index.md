# DbConnectionPlus

A lightweight .NET ORM and extension library for `System.Data.Common.DbConnection`. It adds type-safe,
high-performance helpers — `Query<T>`, `InsertEntity`, `UpdateEntities`, temporary tables and more — as
extension methods on `DbConnection`, with per-database dialect support supplied by pluggable adapters.

Start at the [repository README](https://github.com/rent-a-developer/DbConnectionPlus#readme) for installation
and a quick start. These pages are the full documentation.

## Guides

| Guide | What it covers |
|---|---|
| [Querying](guides/querying.md) | `ExecuteNonQuery`, `ExecuteReader`, `ExecuteScalar`, `Exists`, and every `Query` overload — entities, scalars, value tuples and untyped rows |
| [Parameters and temporary tables](guides/parameters-and-temporary-tables.md) | `Parameter(value)` and `TemporaryTable(values)`, and the per-database caveats |
| [Entity mapping and CRUD](guides/entity-mapping-and-crud.md) | Attributes and the fluent API, insert/update/delete, optimistic concurrency, enums |
| [Configuration](guides/configuration.md) | `Configure`, `EnumSerializationMode`, `InterceptDbCommand` |
| [Custom database adapters](guides/custom-adapters.md) | Supporting a database that has no adapter package |
| [Native AOT and trimming](guides/native-aot.md) | What works, which providers are AOT-ready, and what you will see in your own build |

## Reference

| Page | What it covers |
|---|---|
| [API summary](reference/api-summary.md) | Every public entry point, one line each, linked to the generated reference |
| [Comparison with Dapper](reference/comparison.md) | Where the two differ, and where Dapper is still the better pick |
| [Performance](reference/performance.md) | The benchmark results, and how they were measured |
| [Design decisions](DESIGN-DECISIONS.md) | Why the library works the way it does |

## API reference

The generated reference for all six packages starts at
[`DbConnectionExtensions`](xref:RentADeveloper.DbConnectionPlus.DbConnectionExtensions) — the entry point for
nearly every operation.

## The packages

| Package | Contents |
|---|---|
| `DbConnectionPlus` | The core library. Everything else depends on it. |
| `DbConnectionPlus.DatabaseAdapters.MySql` | MySQL dialect support. |
| `DbConnectionPlus.DatabaseAdapters.Oracle` | Oracle dialect support. |
| `DbConnectionPlus.DatabaseAdapters.PostgreSql` | PostgreSQL dialect support. |
| `DbConnectionPlus.DatabaseAdapters.Sqlite` | SQLite dialect support. |
| `DbConnectionPlus.DatabaseAdapters.SqlServer` | SQL Server dialect support. |

Install the core package plus the adapter for your database, then register the adapter once at startup:

```csharp
DbConnectionExtensions.Configure(configuration => configuration.UseSqlServer());
```

All six are versioned and released together, so a given release's adapter always matches its core package.

## Project

- [Change log](../CHANGELOG.md)
- [Contributing](../CONTRIBUTING.md)
- [Code of conduct](../CODE_OF_CONDUCT.md)
- [Security policy](../SECURITY.md)
