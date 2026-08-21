# Change Log
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/) and
this project adheres to [Semantic Versioning](https://semver.org/).

## [4.0.0] - 2026-08-22

### Added
- **Native AOT and trimming support.** Reference the core package and the database adapter packages and
  publish — there is no companion package, no source generator and nothing to opt into. Publishing with
  `PublishAot` or `PublishTrimmed` reports **no** `IL2xxx` or `IL3xxx` diagnostic for any supported scenario,
  and the public API carries neither `[RequiresUnreferencedCode]` nor `[RequiresDynamicCode]`. Queries
  materialize the same results under Native AOT as under the just-in-time compiler — constructor injection
  (records and other immutable entities), property setters and value tuples of any size all behave
  identically, down to exception types and messages. The reasoning is recorded in
  [DESIGN-DECISIONS.md](DESIGN-DECISIONS.md#native-aot-and-trimming).
- The libraries now multi-target `net8.0` and `net10.0`. `net8.0` remains the supported floor; `net10.0` is
  recommended.
- The packages are marked trimmable (`[assembly: AssemblyMetadata("IsTrimmable", "True")]`), which opts their
  assemblies into trimming in consuming applications.
- `DataRow` implements `IDynamicMetaObjectProvider`, so a row assigned to a `dynamic` reference exposes its
  columns as members — `product.Id` reads a column, `product.Id = 1` assigns one — and constructing and using
  a row stays warning-free under Native AOT.

### Changed
- **BREAKING:** The non-generic `Query`, `QueryFirst`, `QueryFirstOrDefault`, `QuerySingle` and
  `QuerySingleOrDefault` methods now return `DataRow` instead of `dynamic`. Read columns through the string
  indexer — `row["Id"]` — which needs no cast and works under Native AOT. Dynamic member access (`row.Id`)
  still works wherever the runtime supports dynamic code generation, but the row has to be assigned to a
  `dynamic` reference first.
- **BREAKING:** Dynamic member access on a `DataRow` throws `KeyNotFoundException` for a column the row does
  not contain, matching the string indexer. It previously threw `RuntimeBinderException`.
- **BREAKING:** Through a `dynamic` reference, a *property* on a `DataRow` now always addresses a column, so
  `row.Count` reads the column named `Count` instead of the row's own `Count` property. Method calls are
  unchanged. Use a statically typed `DataRow` reference to reach `Count`, `Keys` and `Values`.
- **Behaviour change:** materializing a result set whose columns match none of the writable properties of the
  target entity type now throws `InvalidOperationException` instead of returning entities with every property
  left at its default value — a typo in a `SELECT` alias previously produced a sequence of empty objects with
  no error. Constructor injection is unaffected; it already failed loudly.
- **BREAKING:** Converting a `String` to a `TimeSpan`, `DateTimeOffset`, `DateOnly` or `TimeOnly` now parses
  with `CultureInfo.InvariantCulture` instead of the culture of the current thread. The reverse direction
  already wrote with the invariant culture, so the two halves disagreed: a `TimeSpan` this library had itself
  written as `1:2:03:04.567` did not parse back at all under a culture whose decimal separator is a comma, and
  a text column holding `03/04/2026` read as the 4th of March on an `en-US` machine but as the 3rd of April on
  a `de-DE` one — silently, with no error. Reading a value out of a database no longer depends on the locale
  of the machine that runs the code. Applications that stored date and time values in text columns in a
  culture-specific format have to convert those columns, or read them as `String` and parse them themselves.
- **BREAKING:** `EntityPropertyMetadata.PropertyGetter` and `EntityPropertyMetadata.PropertySetter` are now
  typed `Func<Object, Object?>?` and `Action<Object, Object?>?` instead of Fasterflect's `MemberGetter?` and
  `MemberSetter?`. The delegate shapes are identical, so code that only *invokes* an accessor needs no change;
  code that declares, stores or assigns one has to switch to the BCL delegate types.
- The debug representation of a value — used in conversion error messages and in the `ToString()` of an SQL
  statement — no longer goes through `System.Text.Json`. Sequences are unchanged, so an `Int32[]` still reads
  as `[1,2,3]`, but a value of any other type without a dedicated representation is now rendered via
  `ToString()`: an entity that previously rendered as `{"Enum":3,"Id":1}` now renders as, for a record,
  `EntityWithEnumStoredAsString { Enum = Value3, Id = 1 }`.

### Removed
- The `Fasterflect` and `FastMember` dependencies, both of which emitted code that does not exist under Native
  AOT. Consuming applications lose two transitive dependencies and the assembly-level `IL2104` trim warning
  that Fasterflect produced when publishing a trimmed or AOT application.

### Fixed
- Bumped `Microsoft.Data.Sqlite` to 10.0.11 in the SQLite adapter, the first release that resolves
  `SQLitePCLRaw` 2.1.12 instead of the vulnerable 2.1.11 (GHSA-2m69-gcr7-jv3q, high severity). Consumers no
  longer inherit the vulnerable native library.

### Migration from 3.x

1. **Reading columns from a non-generic query.** Replace dynamic member access with the `DataRow` string
   indexer — `row.Id` becomes `row["Id"]`. The indexer needs no cast and works under Native AOT, which is why
   it is now the documented default.

   Member access still works wherever the runtime supports dynamic code generation, but the row has to be
   assigned to a `dynamic` reference first, because the static return type is `DataRow`:

   ```csharp
   dynamic product = connection.QueryFirst($"SELECT * FROM Product WHERE Id = {Parameter(id)}");
   var name = product.Name;
   ```

   Two behaviour differences if you do: an unknown column throws `KeyNotFoundException` rather than
   `RuntimeBinderException`, and a *property* always addresses a column, so `row.Count` reads the column named
   `Count`. Use a statically typed `DataRow` reference to reach `Count`, `Keys` and `Values`.

2. **`EntityPropertyMetadata` accessors.** If you read `PropertyGetter` or `PropertySetter` — most applications
   do not — they are now `Func<Object, Object?>?` and `Action<Object, Object?>?`. Invocation is unchanged;
   remove any `using Fasterflect;` left behind.

3. **A query that maps nothing now throws.** If a result set's columns match none of the writable properties of
   the target type, materialization throws `InvalidOperationException` instead of returning entities left at
   their defaults. This usually surfaces a pre-existing bug, most often a typo in a `SELECT` alias.

4. **Publishing with Native AOT or trimming.** Nothing to install and nothing to opt into: reference the
   packages and publish.

## [3.0.0] - 2026-05-29

### Changed
- **BREAKING:** All NuGet packages have been renamed:
	- RentADeveloper.DbConnectionPlus > DbConnectionPlus
	- RentADeveloper.DbConnectionPlus.DatabaseAdapters.MySql > DbConnectionPlus.DatabaseAdapters.MySql
	- RentADeveloper.DbConnectionPlus.DatabaseAdapters.Oracle > DbConnectionPlus.DatabaseAdapters.Oracle
	- RentADeveloper.DbConnectionPlus.DatabaseAdapters.PostgreSql > DbConnectionPlus.DatabaseAdapters.PostgreSql
	- RentADeveloper.DbConnectionPlus.DatabaseAdapters.Sqlite > DbConnectionPlus.DatabaseAdapters.Sqlite
	- RentADeveloper.DbConnectionPlus.DatabaseAdapters.SqlServer > DbConnectionPlus.DatabaseAdapters.SqlServer

### Migration from 2.x

1. Remove the 'RentADeveloper.DbConnectionPlus*' packages and add the 'DbConnectionPlus*' packages.

## [2.0.0] - 2026-03-21

### Changed
- **BREAKING:** Database adapters have been extracted into separate NuGet packages. Users must now install the adapter package(s) for the database system(s) they use and explicitly register them via `UseXxx()` extension methods.
  - `RentADeveloper.DbConnectionPlus.DatabaseAdapters.SqlServer` — `UseSqlServer()`
  - `RentADeveloper.DbConnectionPlus.DatabaseAdapters.MySql` — `UseMySql()`
  - `RentADeveloper.DbConnectionPlus.DatabaseAdapters.PostgreSql` — `UsePostgreSql()`
  - `RentADeveloper.DbConnectionPlus.DatabaseAdapters.Oracle` — `UseOracle()`
  - `RentADeveloper.DbConnectionPlus.DatabaseAdapters.Sqlite` — `UseSqlite()`

### Migration from 1.x

1. Install the adapter package(s) for the database(s) you use (see list above).
2. Register adapters at application startup:
   ```csharp
   DbConnectionExtensions.Configure(config => config.UseSqlServer());
   ```

## [1.2.1] - 2026-03-07

### Fixed
- NameHelper.CreateNameFromCallerArgumentExpression stops scanning too early (Fixes [issue #7](https://github.com/rent-a-developer/DbConnectionPlus/issues/7))

## [1.2.0] - 2026-02-14

### Added
- Optimistic Concurrency Support via Concurrency Tokens (Fixes [issue #5](https://github.com/rent-a-developer/DbConnectionPlus/issues/5))

### Changed
- Switched benchmarks to SQLite for more stable results.

## [1.1.0] - 2026-02-01

### Added
- Fluent configuration API for general settings and entity mappings (Fixes [issue #3](https://github.com/rent-a-developer/DbConnectionPlus/issues/3))
- Support for column name mapping via System.ComponentModel.DataAnnotations.Schema.ColumnAttribute (Fixes [issue #1](https://github.com/rent-a-developer/DbConnectionPlus/issues/1))
- Throw helper for common exceptions

### Changed
- Updated all dependencies to latest stable versions
- Refactored unit and integration tests for better maintainability

## [1.0.0] - 2026-01-24

### Added
- Initial release with comprehensive database support
- Extension methods for `DbConnection` supporting SQL Server, PostgreSQL, MySQL, SQLite, and Oracle
- Interpolated string handler for safe SQL parameterization via `Parameter(value)`
- On-the-fly temporary table creation via `TemporaryTable(values)`
- General-purpose methods: `ExecuteNonQuery`, `ExecuteReader`, `ExecuteScalar`, `Exists`
- Dynamic Query methods: `Query`, `QueryFirst`, `QueryFirstOrDefault`, `QuerySingle`, `QuerySingleOrDefault`
- Query methods: `Query<T>`, `QueryFirst<T>`, `QueryFirstOrDefault<T>`, `QuerySingle<T>`, `QuerySingleOrDefault<T>`
- Support for mapping to entities, value tuples, scalar values, and dynamic objects
- Entity manipulation methods: `InsertEntity`, `UpdateEntity`, `DeleteEntity` and batch variants
- Enum serialization support with configurable string/integer modes
- Custom database adapter pattern via `IDatabaseAdapter` interface
- Full async/await support for all operations
