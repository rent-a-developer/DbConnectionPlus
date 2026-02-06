# Change Log
All notable changes to this project will be documented in this file.
 
The format is based on [Keep a Changelog](https://keepachangelog.com/) and
this project adheres to [Semantic Versioning](https://semver.org/).

TODO: Update date
## [1.2.0] - 2026-02-XX

### Added
- Support for Optimistic Concurrency Support via Concurrency Tokens (Fixes [issue #5](https://github.com/rent-a-developer/DbConnectionPlus/issues/5))

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
