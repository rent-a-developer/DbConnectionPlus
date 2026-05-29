# Change Log
All notable changes to this project will be documented in this file.
 
The format is based on [Keep a Changelog](https://keepachangelog.com/) and
this project adheres to [Semantic Versioning](https://semver.org/).

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
