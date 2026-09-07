---
name: integration-db
description: Run the correctly scoped integration suite against the test databases, and troubleshoot the containers Testcontainers starts for it. Use when a code change requires verification against SQLite, SQL Server, MySQL, Oracle or PostgreSQL, or when a database container fails to start.
---

# Integration test databases

The integration suite talks to four real databases. **It starts them itself** — there is nothing to bring up
first, and nothing to shut down afterwards.

**Optional argument:**
- no argument — run the whole suite.
- anything else — treated as a `--filter-query` expression for xUnit.

## How the databases get there

[Testcontainers](https://dotnet.testcontainers.org/) starts one container per database system, waits until the
server inside it accepts connections, and removes it when the run ends. The container definitions are the
fixtures in `tests/DbConnectionPlus.IntegrationTests/TestDatabase/Containers/` — image, password and connection
string all live in that C# code, not in a compose file or an environment variable.

| Database system | Image | Started for |
|---|---|---|
| MySQL | `mysql:latest` | `*MySql*` test classes |
| Oracle | `gvenzl/oracle-free:23-slim-faststart` | `*Oracle*` test classes |
| PostgreSQL | `postgres:latest` | `*PostgreSql*` test classes |
| SQL Server | `mcr.microsoft.com/mssql/server:2022-latest` | `*SqlServer*` test classes |

Three consequences worth knowing before you debug anything:

- **Ports are not fixed.** Every container publishes its port to a free host port, and the fixture builds the
  connection string from whatever it got. Nothing can collide with a database server installed locally, and
  there is no port to look up.
- **Containers start lazily, per database system.** A run filtered to SQLite and SQL Server never starts the
  MySQL, Oracle or PostgreSQL container. SQLite runs in-process and needs none at all. So the cost of a scoped
  run really is only the systems it covers.
- **Every run starts from a fresh server and pays its startup.** Measured from container creation to the first
  accepted connection: PostgreSQL 4.7 s, SQL Server 11.1 s, MySQL 19.8 s, Oracle 23.9 s. Nothing survives
  between runs, so no run can be polluted by the last one.

The only prerequisite is a running Docker daemon. Check it with `docker version` before blaming the suite.

## Run

```bash
dotnet test --project tests/DbConnectionPlus.IntegrationTests/DbConnectionPlus.IntegrationTests.csproj -c Release
```

To scope to one database system, add a class filter — e.g. `--filter-class "*SqlServer*"`.

## Scoping the run — don't pay 10 minutes for every iteration

**First decide whether you need database tests at all.** They are not part of the default verification loop —
`scripts/preflight.ps1` (hygiene, tidiness, Release build and the unit suite) is. Reach for the integration suite when the
change can only be proven against a real database: SQL generation, an adapter, the CRUD or temp-table paths,
type mapping, or anything a substituted `DbDataReader` cannot exercise honestly. A refactor whose behaviour the
unit tests already pin down does not need them.

Two rules, in order:

1. **If you need database tests, run SQLite + SQL Server.** That is the default scope.
2. **Run another adapter's tests only if you actually changed that adapter's code.**

Measured with `-c Release --no-build` and the images already pulled. Container startup is **included**, because
you now always pay it:

| Scope | Wall time | Executed | Skipped |
|---|---:|---:|---:|
| SQLite only | **15 s** | 869 | 104 |
| PostgreSQL only | 24 s | 895 | 80 |
| **SQLite + SQL Server** | **90 s** | 1844 | 104 |
| SQL Server only | 92 s | **975** | **0** |
| Oracle only | 142 s | 863 | 100 |
| MySQL only | 353 s | 855 | 118 |
| Full matrix (every adapter) | **597 s** | 4457 | 402 |

SQLite is free next to SQL Server's container start, which is why the pair costs no more than SQL Server alone -
the two numbers are the same measurement within noise.

**Default to SQLite + SQL Server** — it keeps what matters for a fraction of the time:

- They are the **field-type contrast** that matters — SQLite hands back `Int64` where SQL Server hands back
  `Int32`, which is exactly what drives the materializer's per-column "needs conversion?" decision. Materializers
  are cached per result-set shape, so this pair exercises both branches.
- SQL Server **skips nothing**. PostgreSQL is faster but skips 80 tests, so it is less coverage for the money.
  Use PostgreSQL as the second provider only if SQL Server's minute and a half is genuinely blocking.
- SQLite alone is the right inner-loop check while iterating on core code, and the only scope that needs no
  Docker at all. It is not sufficient to call anything verified.

🔴 **When to go beyond the default pair — changed adapter code, and nothing else:**

- **A change under `src/DbConnectionPlus.DatabaseAdapters.{MySql,Oracle,PostgreSql}`** — add exactly those
  adapters to the default pair, and no others. Touching the MySQL adapter buys a MySQL run, not a full matrix.
- **A change to the shared adapter seam** — `IDatabaseAdapter`, `IEntityManipulator` or
  `ITemporaryTableBuilder` — obliges you to run **the full matrix**, because every adapter implements it. They are
  genuinely divergent code, and at least one asymmetry is deliberate: MySQL's temp-table reader applies
  enum/`Char` handling that the others do not. A two-provider run cannot see that.

**A core-only change does not earn the full matrix.** The five-provider run is repeatedly byte-identical to the
previous baseline for changes that touch no adapter, which is ten minutes for no signal. If you want the extra
confidence anyway, say you are running it and why — do not present it as required.

## Keep the output to one line

`dotnet test` prints every skipped test by name — hundreds of lines of pure noise that will eat a session's
context for no benefit. **Always filter:**

```bash
dotnet test ... 2>&1 | grep -Ei "^\s*(Failed|Passed)!|error"
```

Include filter results and per-provider counts only when you actually need them. To confirm which providers a run
covered without re-running it, `--xunit-list classes --ignore-exit-code 8` piped through
`grep -oiE "(MySql|Oracle|PostgreSql|Sqlite|SqlServer)" | sort | uniq -c` is fast and does not execute anything.

## Stop

Nothing to do. The containers are removed when the run ends — by an assembly fixture, and by Testcontainers'
resource reaper if the process is killed before it gets there. If a run was interrupted hard enough to leave
something behind, `docker ps` shows it with a `testcontainers` label:

```bash
docker ps --filter "label=org.testcontainers=true"
```

## Troubleshooting

- **"Docker is either not running or misconfigured"** — the daemon is down. Start Docker Desktop; nothing about
  the suite can work without it.
- **A test class fails in its fixture with a Docker error, but the tests themselves look fine** — that is the
  container failing to start. The message from the daemon is in the failure; read it before re-running.
- **Oracle exits or restarts repeatedly** — usually memory. `gvenzl/oracle-free` wants ~2 GB; check Docker
  Desktop's resource limits.
- **The first Oracle or SQL Server run is far slower than the table above** — the image is being pulled. The
  first Oracle run measured 341 s against 142 s warm. `docker images` tells you whether it is there yet.
- **You want to watch a container start** — the messages go to xUnit's diagnostics, which `dotnet test` does not
  print. The test project is an executable, so run it directly instead:

  ```bash
  tests/DbConnectionPlus.IntegrationTests/bin/Release/net8.0/RentADeveloper.DbConnectionPlus.IntegrationTests -class "*PostgreSql*" -diagnostics
  ```

  That prints Testcontainers' own log — image pull, container id, every readiness probe — plus the connection
  string each fixture built and how long the container took.
- **Containers pile up after crashed runs** — the reaper container (`testcontainers/ryuk`) removes them once the
  session ends. If Ryuk itself is blocked, remove them with the label filter shown above.

Report test results faithfully, including which providers ran and which were skipped or filtered out.
