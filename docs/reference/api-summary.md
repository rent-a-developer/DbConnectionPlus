# API summary

Every public entry point, one line each. The generated reference has the signatures, the parameters and the
exceptions; the guides have the worked examples.

Everything below is an extension method on `System.Data.Common.DbConnection`, unless it is listed under
Configuration. Every method has an `…Async` counterpart, and all of them accept an optional transaction,
command timeout, command type and cancellation token.

## Configuration

See [the configuration guide](../guides/configuration.md).

| Member | What it does |
|---|---|
| `DbConnectionExtensions.Configure` | Configures the library. Callable **once**, at application startup |
| `EnumSerializationMode` | Whether enum values are sent as strings (the default) or as integers |
| `InterceptDbCommand` | A delegate that sees every `DbCommand` this library builds, before it executes |
| `RegisterDatabaseAdapter<TConnection>` | Registers an adapter for a connection type — what `UseSqlServer()` and friends call |

## Entity mapping

See [entity mapping and CRUD](../guides/entity-mapping-and-crud.md).

| Member | What it does |
|---|---|
| `Entity<TEntity>()` | Starts configuring an entity type fluently |
| `ToTable`, `Property`, `HasColumnName` | Table and column names |
| `IsKey`, `IsIdentity`, `IsComputed`, `IsRowVersion`, `IsConcurrencyToken`, `IsIgnored` | Per-property mapping |
| `[Table]`, `[Column]`, `[Key]`, `[DatabaseGenerated]`, `[Timestamp]`, `[ConcurrencyCheck]`, `[NotMapped]` | The same, through the standard data annotations |

## General-purpose methods

See [querying](../guides/querying.md).

| Method | What it does |
|---|---|
| `ExecuteNonQuery` | Executes a statement and returns the number of rows affected |
| `ExecuteReader` | Executes a statement and returns a `DbDataReader`. **Dispose it** — that is what drops any temporary tables |
| `ExecuteScalar<T>` | Reads the first column of the first row, converted to `T` |
| `Exists` | Whether any row matches |

## Query methods

See [querying](../guides/querying.md).

| Method | Maps to |
|---|---|
| `Query` | a sequence of `DataRow` |
| `QueryFirst` | the first row, as a `DataRow`. Throws if there is none |
| `QueryFirstOrDefault` | the first row, or `null` |
| `QuerySingle` | the single row. Throws if there is none, or more than one |
| `QuerySingleOrDefault` | the single row, or `null`. Throws if there is more than one |
| `Query<T>` | a sequence of scalars, entities or value tuples |
| `QueryFirst<T>` | the first row. Throws if there is none |
| `QueryFirstOrDefault<T>` | the first row, or `default` |
| `QuerySingle<T>` | the single row. Throws if there is none, or more than one |
| `QuerySingleOrDefault<T>` | the single row, or `default`. Throws if there is more than one |

## Entity manipulation methods

See [entity mapping and CRUD](../guides/entity-mapping-and-crud.md).

| Method | What it does |
|---|---|
| `InsertEntity` / `InsertEntities` | Inserts one entity, or a sequence. Database-generated values are read back |
| `UpdateEntity` / `UpdateEntities` | Updates by key. Throws `DbUpdateConcurrencyException` if a concurrency token no longer matches |
| `DeleteEntity` / `DeleteEntities` | Deletes by key, with the same concurrency check |

## Special helpers

Usable in any interpolated statement. See
[parameters and temporary tables](../guides/parameters-and-temporary-tables.md).

| Helper | What it does |
|---|---|
| `Parameter(value)` | Adds the value as a real `DbParameter` and writes its name into the SQL |
| `TemporaryTable(values)` | Creates a temporary table from a sequence, bulk-loads it, writes its name into the SQL, and drops it afterwards |

## Types you will see

| Type | What it is |
|---|---|
| `DataRow` | An untyped row. Read columns with `row["Name"]`; `dynamic` member access works everywhere except Native AOT |
| `InterpolatedSqlStatement` | What an interpolated string becomes. A plain `string` converts implicitly |
| `DbUpdateConcurrencyException` | Thrown when an update or delete matched no row because a concurrency token changed. Carries the offending `Entity` |
| `IDatabaseAdapter`, `IEntityManipulator`, `ITemporaryTableBuilder` | The three seams a [custom adapter](../guides/custom-adapters.md) implements |
