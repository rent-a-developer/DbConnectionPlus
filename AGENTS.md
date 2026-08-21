# AGENTS.md

Guidance for any AI coding agent (Claude Code, Codex, …) working in this repository. This file is the
canonical source; `CLAUDE.md` is a pointer to it plus Claude Code-specific wiring.

## What this is

**DbConnectionPlus** — a lightweight .NET ORM / extension library for `System.Data.Common.DbConnection`. It adds
type-safe, high-performance helpers (`Query<T>`, `InsertEntity`, `UpdateEntities`, temporary tables, …) as extension
methods on `DbConnection`, with per-database dialect support supplied by pluggable adapters.

Published to NuGet as `DbConnectionPlus`; assemblies are named `RentADeveloper.DbConnectionPlus.*`.

## Layout

| Path | Contents |
|---|---|
| `src/DbConnectionPlus` | Core library. Root namespace `RentADeveloper.DbConnectionPlus`. |
| `src/DbConnectionPlus.DatabaseAdapters.{MySql,Oracle,PostgreSql,Sqlite,SqlServer}` | One adapter project per database system. |
| `tests/DbConnectionPlus.UnitTests` | xUnit v3 unit tests. No database required. |
| `tests/DbConnectionPlus.IntegrationTests` | xUnit v3 integration tests. Requires Docker (see below). |
| `tests/package-consumption/` | Console apps that consume the **packed NuGet packages** rather than project references. `AotConsumer` is published with Native AOT (`scripts/verify-package-aot.ps1`); `AllAdaptersConsumer` installs all six packages and is built by CI on the .NET 8 SDK alone. Not xUnit projects, and deliberately **not** in the solution — see [their README](tests/package-consumption/README.md). |
| `benchmarks/DbConnectionPlus.Benchmarks` | BenchmarkDotNet suite. Everything runs on the JIT; the three entity-mapping categories **also** run as a Native AOT binary. Run it via `scripts/benchmarks.ps1`, and read [its README](benchmarks/DbConnectionPlus.Benchmarks/README.md) before adding a benchmark. |
| `docs/` | docfx config, the site landing page + implementation plans. |
| `.agents/` | Canonical repository skills and review checklists shared by both agent integrations. |
| `.codex/` | Codex custom-agent metadata and PostToolUse hook wiring. |
| `.github/workflows/` | `ci.yml` (lint → build/test/analyze → package + docs → package-consumption gates → publish → release), plus `codeql.yml` and `dependency-review.yml`. |
| `scripts/` | Entry points **you** type: `preflight`, `verify-package-aot`, `benchmarks`, `update-public-api`, `clean-build-artifacts`, `extract-release-notes`. Also `format-cs` and `public-api-guard`, which the editor hooks run for you. |

Solution file is `DbConnectionPlus.slnx` (the XML `.slnx` format, not `.sln`). **New projects must be added to it.**

Build properties live in **two** `Directory.Build.props` files, not in the `.csproj` files:

| File | Applies to | Carries |
|---|---|---|
| `Directory.Build.props` (repo root) | all nine solution projects | `Authors`, `Company`, `Copyright`, `ImplicitUsings`, `LangVersion`, `Nullable`, `TieredCompilation`, and the ErrorProne.NET + Roslynator analyzers |
| `src/Directory.Build.props` | the six shipping projects | `<Version>`, `TargetFrameworks=net8.0;net10.0`, `IsAotCompatible`, `AnalysisLevel=latest-all`, `TreatWarningsAsErrors`, the AOT and public-API analyzers, the NuGet package metadata, and the files every package carries |

MSBuild uses the **nearest** `Directory.Build.props` and stops, so `src/Directory.Build.props` imports the root
one explicitly — remove that import and the six shipping projects silently lose authorship, nullability and the
style analyzers. The split exists because `tests/` and `benchmarks/` set neither `TreatWarningsAsErrors` nor
`AnalysisLevel` and would newly break under them. (The benchmarks are `net10.0` — BenchmarkDotNet's
`NativeAotToolchain.Net10_0` publishes that TFM. The **unit tests multi-target `net8.0;net10.0`**, because the
two builds of the shipping libraries are not the same code — `net8.0` carries an `IL3050` suppression
`net10.0` does not. The integration tests stay `net8.0`: they are bound by four database containers rather
than by the runtime, and doubling a ten-minute suite buys nothing the unit tests do not already cover on both.)

`tests/package-consumption/` sits **outside** all of this on purpose: it carries its own empty
`Directory.Build.props`/`.targets` that stop MSBuild's upward search, so those projects receive the library
only from the packed packages. Do not "fix" that by deleting the empty files.

What stays in a `.csproj` is per-project identity — `AssemblyName`, `AssemblyTitle`, `RootNamespace`,
`PackageId`, `Description`, `PackageTags` — plus that project's own package references. A shipping `.csproj` is
about 20 lines. The package icon is **one** file, `assets/logo-128.png`, referenced from
`src/Directory.Build.props` for all six packages.

**Two readmes, and they are not interchangeable.** `README.md` is the repository's reference documentation and what a GitHub visitor reads. `PACKAGE_README.md` is what nuget.org renders as the package
page: a short overview for someone deciding whether to install, which links back to the long one. Both ship
from the repo root; only `PACKAGE_README.md` goes into the packages. An API change updates `README.md`; touch
`PACKAGE_README.md` only when the overview itself stops being true.

### The adapter seam

`IDatabaseAdapter` (`src/DbConnectionPlus/DatabaseAdapters/IDatabaseAdapter.cs`) exposes `IEntityManipulator` and
`ITemporaryTableBuilder`. Each of the five adapter projects implements all three:

```
{Db}DatabaseAdapter.cs           {Db}EntityManipulator.cs
{Db}TemporaryTableBuilder.cs     {Db}ConfigurationExtensions.cs
```

**A change to one adapter almost always needs mirroring into the other four.** Only the integration suite catches a
miss, and that needs Docker. Use the `adapter_parity_reviewer` Codex custom agent, walk
[its checklist](.agents/references/reviews/adapter-parity.md) over the diff, or check the other four by hand.

Core marks internals visible to all five adapters and to the test/benchmark assemblies
(`src/DbConnectionPlus/AssemblyAttributes.cs`).

## Build & test

```bash
dotnet build DbConnectionPlus.slnx -c Release
```

```bash
dotnet test --project tests/DbConnectionPlus.UnitTests/DbConnectionPlus.UnitTests.csproj
```

Both, plus the repo-hygiene checks, in one command:

```bash
pwsh -File scripts/preflight.ps1
```

The Native AOT gate — packs the six projects, publishes `tests/package-consumption/AotConsumer` natively
**from the packages**, gates its IL diagnostics and runs the binary. It is the **only** check that can see
silent trimming damage, because nothing is trimmed on the JIT. Needs a C++ toolchain: MSVC on Windows,
`clang` + `zlib1g-dev` on Linux.

```bash
pwsh -File scripts/verify-package-aot.ps1 -Pack
```

Run it when you change reflection, DAM annotations, the materializers or the temp-table readers. It is not part
of `preflight.ps1` — a pack plus a native publish takes minutes. `-Framework net8.0` checks the documented AOT
floor, which behaves differently from the `net10.0` default; CI runs both. Drop `-Pack` to reuse the packages
already in `artifacts/packages`.

It consumes packages rather than projects because the annotations, the embedded `ILLink.Descriptors.xml` and
the `IsTrimmable` marker all have to survive `dotnet pack` — a project-referenced version of this check would
stay green if packing dropped every one of them.

Integration tests need a running Docker daemon and nothing else — no container to start by hand, no connection
string to configure:

```bash
dotnet test --project tests/DbConnectionPlus.IntegrationTests/DbConnectionPlus.IntegrationTests.csproj
```

[Testcontainers](https://dotnet.testcontainers.org/) owns the databases. The container definitions live in
`tests/DbConnectionPlus.IntegrationTests/TestDatabase/Containers/`, one fixture per database system, and each one
builds its own connection string from the free host port its container was published on — which is why nothing
collides with a locally installed server and why there is no port to agree on with CI. Containers start **lazily,
per database system**: a run filtered to SQLite and SQL Server never starts the MySQL, Oracle or PostgreSQL
container, and SQLite needs none at all. They are removed when the run ends, so every run starts from a clean
server and pays for the startup — 5 to 24 seconds each, Oracle being the slow one. Details, measured timings and
what to do when a container will not start: [the `integration-db` skill](.agents/skills/integration-db/SKILL.md).

**Scope the run.** The integration suite is not part of the default verification loop — `scripts/preflight.ps1`
is. Run it when a change can only be proven against a real database, and when you do, the default scope is
**SQLite + SQL Server** (~90 s, versus ~600 s for all five). Add another adapter's tests **only if you changed
that adapter's code**; run all five only for a change to `IDatabaseAdapter`, `IEntityManipulator` or
`ITemporaryTableBuilder`, which all five implement. Rules and measured timings:
[the `integration-db` skill](.agents/skills/integration-db/SKILL.md).

## Code style — the build enforces this

`TreatWarningsAsErrors=true` with `AnalysisLevel=latest-all`, `EnforceCodeStyleInBuild=true`, ErrorProne.NET, and
Roslynator's entire category at `error`. A style slip is a build break, not a warning.

- **BCL type names, PascalCase**: write `String`, `Object?`, `Int32`, `Boolean` — *not* `string`, `object`, `int`,
  `bool`. (`dotnet_style_predefined_type_for_*` is `false`.)
- **Expression-bodied members are `error`-severity** for methods, constructors, operators, properties, indexers,
  accessors, lambdas and local functions. Use `=>` wherever a member is a single expression.
- **File-scoped namespaces** and usings outside the namespace.
- **Max line length** is **120**.
- **No UTF-8 BOM.** `.editorconfig` sets `charset = utf-8` for `*.cs`. That is the whole rule: editors honour
  it when they write a file, and CI's `dotnet format whitespace --verify-no-changes` fails a BOM'd file as
  `error CHARSET`. Git cannot help — it treats a BOM as content — so that lint
  step is the gate. To fix one, run `pwsh -File scripts/format-cs.ps1 -Scope all` (or `-Scope whitespace`);
  the default `style` scope does not apply the charset fixer. It matters because a BOM read as plain UTF-8
  becomes an invisible U+FEFF on the first token, which silently breaks anchored edits.
- **Copyright header** on every new `.cs` file:
  ```csharp
  // Copyright (c) 2026 David Liebeherr
  // Licensed under the MIT License. See LICENSE.md in the project root for more information.
  ```
- **XML docs** on all public and most internal members of the **shipping** projects — `<param>`, `<returns>`,
  `<exception>`, `<remarks>`. Match the density of the file you are editing; `DbConnectionExtensions.QueryFirst.cs`
  sets the bar. Invalid XML docs break the docfx workflow. The benchmarks are the exception: nothing consumes
  them as an API, so they use plain `//` comments and switch `RCS1181` off for that reason.
- **Resolve analyzer diagnostics, don't suppress.** If a suppression is unavoidable use a scoped `#pragma` with a
  comment, matching the `#pragma warning disable CA1710` style in `Dynamic/DataRow.cs`.
  ⚠️ **An `IL2xxx` trim warning is not yours to suppress.** They are the only build-time proof that the
  `[DynamicallyAccessedMembers]` chain is complete, and an incomplete chain means silently unpopulated entities
  under trimming. The library has exactly two sanctioned `IL2xxx` suppressions — `IL2060` in
  `MaterializerFactoryHelper` and `IL2065` in `ValueTupleMaterializerFactory` — each narrow, justified in place
  and backed by a test; a third one is a finding. Never suppress one at a public API.
- Nullable and `ImplicitUsings` are enabled; common namespaces come from `GlobalUsings.cs`.

**Format edited `.cs` files before building** — it is much cheaper than discovering a style break in the build:

```bash
pwsh -File scripts/format-cs.ps1
```

With no arguments that formats every `.cs` file git reports as changed; pass paths to format specific files.
Claude Code and Codex both run it automatically on every edit via a PostToolUse hook, so under either agent it
is already done — see [.agents/README.md](.agents/README.md) for the wiring, and note that Codex only runs
project-local hooks after you have trusted them with `/hooks`.

## Tests

xUnit v3 with `[Fact]` / `[Theory]`, assertions via **AwesomeAssertions** (`.Should()`, `Invoking(…)`), fakes via
**NSubstitute** (plus `NSubstitute.Community.DbConnection` for `DbConnection`/`DbDataReader`), data via
**AutoFixture**, **Bogus** and **Mapster**, and null-guard coverage via **RentADeveloper.ArgumentNullGuards**.

- Naming: `Method_Scenario_ShouldExpectedOutcome`.
- Tests derive from `UnitTestsBase` / `IntegrationTestsBase`.
- One test file per public method, mirroring the `DbConnectionExtensions.*.cs` split.

### The declared public API

Each of the **six shipping projects** declares its public surface in two files next to its `.csproj`:

| File | Contains |
|---|---|
| `PublicAPI.Shipped.txt` | everything that shipped in the last release. Starts with `#nullable enable`, so `string!` and `string?` are different entries. |
| `PublicAPI.Unshipped.txt` | what is new since then, and `*REMOVED*<signature>` lines for what is gone. |

`Microsoft.CodeAnalysis.PublicApiAnalyzers` (wired in `src/Directory.Build.props`) enforces them **at build
time**: a public member that is not declared is `RS0016`, one that is declared but no longer exists is
`RS0017`. With `TreatWarningsAsErrors=true` both are build errors, so an accidental change to the public
surface cannot compile — in an adapter as much as in core.

A deliberate change means recording it:

```bash
pwsh -File scripts/update-public-api.ps1
```

That applies the `RS0016` fix, writing the new entries into `PublicAPI.Unshipped.txt` (and creating the two
files for a project that has none — `dotnet format` will not). **Review the diff**: it is the public-API
change, and per [CONTRIBUTING.md](CONTRIBUTING.md) it also requires a `CHANGELOG.md` entry, a `README.md`
update and a SemVer bump in `src/Directory.Build.props`. A `*REMOVED*` line is a break.

At release time, `pwsh -File scripts/update-public-api.ps1 -MarkShipped` folds `Unshipped` into `Shipped`, so
the next release's `Unshipped.txt` again means "new since the last one".

## Cross-project work: search the whole repo

Changes routinely touch the same symbol across the nine projects in the solution. A search scoped to `src/`
**will** miss call sites in `tests/` and `benchmarks/` that must be updated in the same change. For scale:
`EntityHelper.GetEntityTypeMetadata` is used in 18 files spread over eight projects, and the benchmark project
compiles against the same public surface as the tests do. If a search over a widely used symbol returns a
handful of hits, suspect the search before believing the count.

Search the whole repository with ripgrep, and let the compiler confirm: `dotnet build DbConnectionPlus.slnx -c
Release` builds every project, so a call site the search missed is a build error rather than a surprise later.

## Native AOT support

The **reflection paths are AOT-safe**: no companion package, no source generator, no consumer opt-in. AOT users
reference DbConnectionPlus and publish.

- Runtime *code generation* is what Native AOT forbids — plain reflection is fine. So accessors go through
  `System.Reflection.MethodInvoker` and complex-object temporary tables through `EnumerableReader`, both single
  path; the expression-compiled materializer is the JIT fast path only, behind a
  `RuntimeFeature.IsDynamicCodeSupported` branch that the AOT compiler folds away. Do not introduce a
  dependency that emits IL (`Fasterflect`, `FastMember` and friends) — it would be invisible to the analyzers
  and break every AOT consumer.
- ⚠️ **Correctness constraint:** under trimming, missing `[DynamicallyAccessedMembers]` annotations make
  reflection return *fewer members with no error* — measured: 6 columns in, 0 bound, no exception. Three
  layers defend this: DAM annotations, **treating an `IL2xxx` warning as a defect rather than as noise to
  suppress** (see the code-style section for the two sanctioned exceptions), and a zero-binding guard.
  The defect is invisible on the JIT, so **only `scripts/verify-package-aot.ps1` catches it** — run it after
  any change to a reflection path.
- The generic query methods carry **neither** `[RequiresUnreferencedCode]` nor `[RequiresDynamicCode]`, so a
  consumer publishing with `PublishAot` or `PublishTrimmed` sees no diagnostic at a call site. The three
  reflection sites the analyzers report inside the library are answered where they occur — the argument is in
  [No consumer-facing diagnostics](DESIGN-DECISIONS.md#4-no-consumer-facing-diagnostics). Adding either
  attribute to a public API is a regression the AOT consumer's zero-diagnostic gate fails on.

A source-generator design was prototyped, benchmarked and **rejected**. Rationale, measurements and the full
design record: [DESIGN-DECISIONS.md](DESIGN-DECISIONS.md#native-aot-and-trimming). Do not reintroduce it.

Before changing anything that reflects, use the `aot_compat_reviewer` Codex custom agent or walk
[its checklist](.agents/references/reviews/aot-compat.md).

## Conventions

- Branches: `feature/<issue#>-<slug>` or `bugfix/<issue#>-<slug>`, PR'd into `main`.
- Commits: Conventional Commits — `feat:`, `fix:`, `BREAKING CHANGE:`. Full checklist:
  [the `commit` skill](.agents/skills/commit/SKILL.md).
- `CHANGELOG.md` follows [Keep a Changelog](https://keepachangelog.com/); versioning is SemVer.

### Releases

**Never `dotnet pack` and push by hand.** A release is a pushed tag, and CI does the rest:

1. Bump `<Version>` in `src/Directory.Build.props` — the single source of truth for all six packages.
2. Give the `CHANGELOG.md` section a real date: `## [4.1.0] - 2026-08-17`. Not `TBD`, not empty — CI reads
   this section, uses it as the release notes, and refuses to publish without it.
3. Merge to `main`, then push the matching tag: `git tag v4.1.0 && git push origin v4.1.0`.

`ci.yml` then verifies the tag against the packed version, publishes all six packages to NuGet.org, and
creates the GitHub release with the changelog section and the packages attached. Nothing reaches NuGet.org
that has not passed the lint, test, Native AOT and .NET 8 consumer gates first. A tag whose version does not
match `src/Directory.Build.props` fails the publish job rather than shipping stale packages.

## Working procedures

Native Codex skills and custom agents are checked into the repository — see
[.agents/README.md](.agents/README.md) for the layout and Claude Code compatibility wrappers.

| Task | Codex | Claude Code |
|---|---|---|
| Commit changes | `$commit` | `/commit` |
| Run the integration DBs | `$integration-db` | `/integration-db` |
| Review AOT/trim safety | `aot_compat_reviewer` custom agent | `aot-compat-reviewer` subagent |
| Review adapter parity | `adapter_parity_reviewer` custom agent | `adapter-parity-reviewer` subagent |

Claude Code (`.claude/settings.json`) and Codex (`.codex/hooks.json`) both fire the same two PostToolUse hooks —
formatting and the public-API reminder — and both delegate to `scripts/`. Codex needs those hooks trusted once
per clone (`/hooks`); until then nothing fires and you run `scripts/format-cs.ps1` yourself.
`scripts/preflight.ps1` before committing is on you under every agent.

Keep reusable skills and review checklists in `.agents/`, Codex metadata and hook wiring in `.codex/`, Claude
Code metadata and hook wiring in `.claude/`, and executable checks in `scripts/`. Do not fork a procedure or
check into a tool-specific copy — both integrations must follow the same canonical instructions and scripts.
