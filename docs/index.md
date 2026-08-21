# DbConnectionPlus

A lightweight .NET ORM and extension library for `System.Data.Common.DbConnection`. It adds type-safe,
high-performance helpers — `Query<T>`, `InsertEntity`, `UpdateEntities`, temporary tables and more — as
extension methods on `DbConnection`, with per-database dialect support supplied by pluggable adapters.

These pages are the generated **API reference** for all six packages. The narrative documentation — getting
started, the full feature reference and the design record — lives in the repository:

- [README](https://github.com/rent-a-developer/DbConnectionPlus#readme) — the reference documentation.
- [CHANGELOG](https://github.com/rent-a-developer/DbConnectionPlus/blob/main/CHANGELOG.md) — what changed, per release.
- [DESIGN-DECISIONS](https://github.com/rent-a-developer/DbConnectionPlus/blob/main/DESIGN-DECISIONS.md) — why it works the way it does.

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

## Native AOT

The reflection paths are AOT-safe: no companion package, no source generator, no consumer opt-in. Publishing
an application with `PublishAot` or `PublishTrimmed` reports no `IL2xxx` or `IL3xxx` diagnostics for any
supported scenario, on `net8.0` and `net10.0` alike.

Start at [`DbConnectionExtensions`](xref:RentADeveloper.DbConnectionPlus.DbConnectionExtensions) — it is the entry point for nearly every operation.
