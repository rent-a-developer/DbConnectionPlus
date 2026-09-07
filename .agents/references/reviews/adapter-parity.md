# Database adapter parity review

Use this after editing anything under `src/DbConnectionPlus.DatabaseAdapters.*`, or after changing
`IDatabaseAdapter`, `IEntityManipulator` or `ITemporaryTableBuilder` in core.

This repo has five adapter projects implementing the same three seams with per-dialect SQL:

```
src/DbConnectionPlus.DatabaseAdapters.MySql
src/DbConnectionPlus.DatabaseAdapters.Oracle
src/DbConnectionPlus.DatabaseAdapters.PostgreSql
src/DbConnectionPlus.DatabaseAdapters.Sqlite
src/DbConnectionPlus.DatabaseAdapters.SqlServer
```

Each contains `{Db}DatabaseAdapter.cs`, `{Db}EntityManipulator.cs`, `{Db}TemporaryTableBuilder.cs` and
`{Db}ConfigurationExtensions.cs`.

A change to one adapter almost always has to be mirrored into the others. The only thing that catches a
miss is the integration suite, which needs Docker and about ten minutes for the full matrix — so it usually is
not run. The job here is to catch it statically.

This is a **review**: report findings, do not edit files.

## What to do

1. **Establish the change set.** Run `git diff main...HEAD --stat` and `git diff main...HEAD` (fall back to
   `git diff HEAD` / `git status` if the branch has no upstream diff). Identify which adapter files changed and
   whether any core seam (`IDatabaseAdapter`, `IEntityManipulator`, `ITemporaryTableBuilder`,
   `DatabaseAdapters/Constants.cs`) changed.

2. **For each changed adapter member**, read the corresponding member in every other adapter and classify:

   - **Missing** — the other adapters were not updated at all, and they should have been.
   - **Diverged** — they were updated, but the logic differs in a way that is not explained by dialect
     differences.
   - **Correct** — either mirrored properly, or deliberately different for a real dialect reason.

3. **For each core-seam change**, enumerate every implementation of the changed interface member and
   confirm each compiles against the new contract. Search the whole repository for the member name — the
   implementations are named `{Db}DatabaseAdapter`, `{Db}EntityManipulator` and `{Db}TemporaryTableBuilder`, so
   a hit count below the number of adapter projects is a missed mirror — and trust the compiler over the search.

4. **Check test parity.** Adapter behaviour is covered in
   `tests/DbConnectionPlus.IntegrationTests/DatabaseAdapters/{MySql,Oracle,PostgreSql,Sqlite,SqlServer}/`.
   If a behaviour change gained a test in one adapter's file, the others normally need the same test.

## Dialect differences that are legitimately asymmetric

Do not report these as divergence unless the change actually gets them wrong:

- **Identifier quoting** — `[…]` (SQL Server), `` `…` `` (MySQL), `"…"` (PostgreSQL, Oracle, SQLite).
- **Parameter prefixes** — `@` (SQL Server, MySQL, SQLite), `:` (Oracle), `$`/`@` (PostgreSQL).
- **Temporary table syntax** — `#temp` (SQL Server), `CREATE TEMPORARY TABLE` (MySQL, PostgreSQL, SQLite),
  `CREATE GLOBAL TEMPORARY TABLE` / private temp tables (Oracle).
- **Data type mapping** in `GetDataType` — entirely per-dialect by design.
- **Generated-key readback** — `SCOPE_IDENTITY()`, `LAST_INSERT_ID()`, `RETURNING`, `last_insert_rowid()`.
- **Bulk insert paths** — MySQL's `AllowLoadLocalInfile`, SQL Server's `SqlBulkCopy`, and Oracle array binding
  have no equivalent elsewhere.
- MySQL's separate enum-handling behaviour in the temp-table reader path. `EnumerableReader` preserves this
  asymmetry deliberately — do not "fix" it as a side effect.
- Oracle's entity manipulator genuinely has **two** `PropertyGetter`/`PropertySetter` call sites where the
  others have three. That is not a missing mirror.

## Reporting

Report only findings you have verified by reading the other adapters' code. For each:

- The adapter(s) that are missing or diverged from the change.
- File and line.
- What specifically is inconsistent.
- Whether you believe it is a genuine bug or an intentional dialect difference, and why.

If every adapter is consistent, say so in one line and stop. Do not pad the report.
