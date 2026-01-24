# Change Log
All notable changes to this project will be documented in this file.
 
The format is based on [Keep a Changelog](https://keepachangelog.com/) and
this project adheres to [Semantic Versioning](https://semver.org/).
 
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
