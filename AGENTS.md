# AGENTS.md

Guidance for any AI coding agent (Claude Code, Codex, …) working in this repository. This file is the canonical
source; `CLAUDE.md` is a pointer to it plus Claude Code-specific wiring.

## What this is

**DbConnectionPlus** — a lightweight .NET ORM / extension library for `System.Data.Common.DbConnection`. It adds
type-safe, high-performance helpers (`Query<T>`, `InsertEntity`, `UpdateEntities`, temporary tables, …) as extension
methods on `DbConnection`, with per-database dialect support from pluggable adapters. Published to NuGet as
`DbConnectionPlus`; assemblies are named `RentADeveloper.DbConnectionPlus.*`.

## Layout

| Path | Contents |
|---|---|
| `src/DbConnectionPlus` | Core library. Root namespace `RentADeveloper.DbConnectionPlus`. |
| `src/DbConnectionPlus.DatabaseAdapters.{MySql,Oracle,PostgreSql,Sqlite,SqlServer}` | One adapter project per database system. |
| `tests/DbConnectionPlus.{Unit,Integration}Tests` | The two xUnit v3 suites. The unit tests need no database; the integration tests need Docker. |
| `tests/package-consumption/` | Console apps that consume the **packed NuGet packages** instead of project references. Deliberately **not** in the solution — [their README](tests/package-consumption/README.md). |
| `benchmarks/DbConnectionPlus.Benchmarks` | BenchmarkDotNet suite, run via `scripts/benchmarks.ps1`. Read [its README](benchmarks/DbConnectionPlus.Benchmarks/README.md) before adding a benchmark. |
| `docs/` | docfx config, the site landing page and implementation plans. |
| `.agents/`, `.codex/`, `.claude/` | Canonical skills and references, plus each tool's agent metadata and hook wiring. |
| `.github/workflows/` | `ci.yml` (lint → build/test → package + docs → package-consumption gates → publish), `codeql.yml`, `dependency-review.yml`. |
| `scripts/` | The commands you type: `preflight`, `verify-package-aot`, `benchmarks`, `update-public-api`, `clean-build-artifacts`, `extract-release-notes`. Plus `tidy-code` and `public-api-guard`, which the editor hooks run for you. |

The solution file is `DbConnectionPlus.slnx` (XML `.slnx`, not `.sln`). **New projects must be added to it.**

Build settings live in shared files, not in the `.csproj` files:

| File | Covers | Carries |
|---|---|---|
| `Directory.Build.props` | all nine solution projects | shared metadata, `<Version>`, the **style gate** (`EnforceCodeStyleInBuild` + `TreatWarningsAsErrors`), `IsPackable=false`, the dependency audit |
| `Directory.Build.targets` | all nine | the files the packages carry, conditioned on `IsPackable` — which is why they cannot be in a `.props` file |
| `Directory.Packages.props` | all nine | **every dependency version**. A `PackageReference` here carries no `Version`; adding one is `NU1008` |
| `src/Directory.Build.props` | the six shipping projects | `TargetFrameworks`, `IsAotCompatible`, `AnalysisLevel=latest-all`, the AOT and public-API analyzers, the package metadata and package validation |
| `tests/Directory.Build.props` | the two test projects | `OutputType=Exe`, the xUnit v3 / Microsoft.Testing.Platform references and the coverage extension |

MSBuild stops at the nearest `Directory.Build.props`, so the `src/` and `tests/` ones **import the root
explicitly** — without that import their projects would silently lose all of it. `AnalysisLevel=latest-all` has
to stay under `src/`: CA1707 alone objects 2100 times to the test suite's `Method_ShouldDoSomething` naming.

`<Version>` is one edit for all six packages, at the repository root. `scripts/verify-package-aot.ps1` reads it
from there when `-PackageVersion` is omitted. **Packing is opt-in**: the root sets `IsPackable=false` and each of
the six shipping projects sets `IsPackable=true` itself, so a new project ships nothing by accident.

Target frameworks differ per project on purpose: benchmarks `net10.0`, unit tests `net8.0;net10.0` (the shipping
libraries' two builds are not the same code), integration tests `net8.0`.

`tests/package-consumption/` sits **outside** all of this on purpose. It carries its own empty
`Directory.Build.props`/`.targets`, plus a `Directory.Packages.props` that turns central package management back
off, all of which stop the upward search — so those projects get the library only from the packed packages, at a
version CI hands them. Do not "fix" that by deleting the three files.

**Two readmes, and they are not interchangeable.** `README.md` is the repository's reference documentation, what a
GitHub visitor reads, and what an API change updates. `PACKAGE_README.md` is the short overview nuget.org renders
as the package page: it links back to the long one, it is the only one that ships inside the packages, and you
touch it only when the overview itself stops being true.

### The adapter seam

`IDatabaseAdapter` (`src/DbConnectionPlus/DatabaseAdapters/IDatabaseAdapter.cs`) exposes `IEntityManipulator` and
`ITemporaryTableBuilder`, and each of the five adapter projects implements all three.

**A change to one adapter almost always needs mirroring into the other four.** Only the integration suite catches
a miss, and that needs Docker. Run the adapter-parity reviewer, walk
[its checklist](.agents/references/reviews/adapter-parity.md) over the diff, or check the other four by hand.

## Build & test

```bash
dotnet build DbConnectionPlus.slnx -c Release
dotnet test --project tests/DbConnectionPlus.UnitTests/DbConnectionPlus.UnitTests.csproj
pwsh -File scripts/preflight.ps1                 # the default loop: hygiene, tidiness CHECK, build, unit tests
pwsh -File scripts/preflight.ps1 -Fix            # the same, but tidy the working tree first
pwsh -File scripts/verify-package-aot.ps1 -Pack  # the Native AOT gate
```

`preflight.ps1` **writes build output and nothing else by default** — it does not edit source and it does not
touch the git index. `-Fix` is what rewrites files. `tidy-code.ps1 -Check` never writes at any scope: at
`-Scope all` it runs the pipeline on a disposable copy of the tree and prints the diff from there.

Search the **whole repository** when a change touches a shared symbol. A search scoped to `src/` will miss call
sites in `tests/` and `benchmarks/`; `EntityHelper.GetEntityTypeMetadata`, for scale, is used in 18 files across
eight projects. The Release build covers every project, so it turns a missed call site into a build error.

The Native AOT gate is the **only** check that can see silent trimming damage, because nothing is trimmed on the
JIT. Run it whenever you change reflection, DAM annotations, the materializers or the temp-table readers. It packs
the six projects, publishes `AotConsumer` natively **from the packages**, gates its IL diagnostics and runs the
binary. It needs a C++ toolchain and minutes, so it is not in `preflight.ps1`; `-Framework net8.0` checks the
documented AOT floor, and CI runs that as well as the `net10.0` default.

Integration tests need only a running Docker daemon; [Testcontainers](https://dotnet.testcontainers.org/) starts a
container per database system and removes it afterwards. **Scope the run:** the default is **SQLite + SQL Server**
(~90 s, against ~600 s for all five); add another adapter's tests **only if you changed that adapter's code**; run
all five only for a change to `IDatabaseAdapter`, `IEntityManipulator` or `ITemporaryTableBuilder`. Commands,
timings and troubleshooting: [the `integration-db` skill](.agents/skills/integration-db/SKILL.md).

## Code style, formatting and ordering

Formatting is CSharpier's, style is the Roslyn analyzers', ordering is ReSharper's and NewStyleCop's. All three are
build errors, in `tests/` and `benchmarks/` as much as in `src/`. Details, and the three cases where a tool does
something surprising: [the code-style reference](.agents/references/code-style.md).

- **C# keywords, never BCL type names**: `string`, `object?`, `int`, `bool`, `nint`, `nuint` — not `String`,
  `Object?`, `Int32`, `Boolean`, `IntPtr`, `UIntPtr`. ⚠️ **The build does not catch all of this.** `IDE0049`
  ignores `nint`/`nuint` and never looks inside `nameof(...)`, and `tests/package-consumption/` has no style gate
  at all. Apply the rule by hand in those three places — reasons in
  [the reference](.agents/references/code-style.md#where-the-build-misses-a-bcl-type-name).
- **Always `this.`** for instance fields, properties, methods and events; fields are never `_camelCase` (`SA1309`).
  A **primary constructor parameter** is assigned to a `private readonly` backing field and read through
  `this.field` — never used directly in a member body. ⚠️ **The build does not catch this.** A captured parameter
  compiles to a field with no `readonly`, so using one directly silently drops the guarantee that the value cannot
  be reassigned — [why it matters](.agents/references/code-style.md#primary-constructor-parameters).
- **Member order** is StyleCop's: constants and fields at the **top** of a type, then constructors, finalizers,
  delegates, events, enums, interfaces, properties, indexers, operators, methods, nested types; within each group
  public before private, static before instance, readonly before mutable, then alphabetical. Write a new member
  into the right place instead of relying on the fixer — [full order](.agents/references/code-style.md#member-order).
- **Resolve analyzer diagnostics, don't suppress.** If a suppression is unavoidable use a scoped `#pragma` with a
  comment, matching the `#pragma warning disable CA1710` style in `Dynamic/DataRow.cs`.
  ⚠️ **An `IL2xxx` trim warning is not yours to suppress.** They are the only build-time proof that the
  `[DynamicallyAccessedMembers]` chain is complete, and an incomplete chain means silently unpopulated entities
  under trimming. Exactly two are sanctioned, both narrow and backed by a test: `IL2060` in
  `MaterializerFactoryHelper`, `IL2065` in `ValueTupleMaterializerFactory`. A third is a finding, and never
  suppress one at a public API.
- **No C# 14 extension member syntax** until docfx [supports it](https://github.com/dotnet/docfx/issues/10808).
- **No UTF-8 BOM** (`.editorconfig` sets `charset = utf-8` for `*.cs`). A BOM read as plain UTF-8 becomes an
  invisible U+FEFF on the first token, which silently breaks anchored edits.
- **Copyright header** on every new `.cs` file — copy the `// Copyright (c) 2026 David Liebeherr` and
  `// Licensed under the MIT License. …` pair from an existing file.
- **XML docs** on all public and most internal members of the **shipping** projects; invalid ones break the docfx
  workflow, and benchmarks are exempt. [Details](.agents/references/code-style.md#xml-documentation-comments).
- Primary constructors, `=>` for single-expression members, file-scoped namespaces, the 120-column limit, nullable
  and `ImplicitUsings` — [the build fully enforces these](.agents/references/code-style.md#rules-the-build-fully-enforces).

One entry point applies all of it, in three scopes:

```bash
pwsh -File scripts/tidy-code.ps1              # ~1s   formatting, on the files git reports as changed
pwsh -File scripts/tidy-code.ps1 -Scope style # ~15s  + the code-style fixers
pwsh -File scripts/tidy-code.ps1 -Scope all   # ~3min + member ordering, whole solution
```

**Before you commit, run `-Scope all`** — or `scripts/preflight.ps1 -Fix`, which does it for you. That is the
only scope that reorders members, because ReSharper loads the whole solution either way. Claude Code and Codex
run the **default scope** on the file each edit touched, through a PostToolUse hook; style and ordering are not
run there, because they are too slow for a single edit and the build catches them.

## Tests

xUnit v3 with `[Fact]` / `[Theory]`, assertions via **AwesomeAssertions**, fakes via **NSubstitute** (plus
`NSubstitute.Community.DbConnection`), data via **AutoFixture**, **Bogus** and **Mapster**, null-guard coverage via
**RentADeveloper.ArgumentNullGuards**. Name tests `Method_Scenario_ShouldExpectedOutcome`, derive them from
`UnitTestsBase` / `IntegrationTestsBase`, one test file per public method, mirroring `DbConnectionExtensions.*.cs`.

## The declared public API

Each of the six shipping projects declares its public surface next to its `.csproj`, in `PublicAPI.Shipped.txt`
(what shipped in the last release) and `PublicAPI.Unshipped.txt` (what is new since, plus `*REMOVED*<signature>`
lines for what is gone). `Microsoft.CodeAnalysis.PublicApiAnalyzers` enforces them **at build time**: `RS0016` for
an undeclared public member, `RS0017` for a declared one that is gone, both errors, so an accidental change to the
public surface cannot compile. Record a deliberate one with `pwsh -File scripts/update-public-api.ps1`, then
**review the diff**: it is the public-API change. Per
[CONTRIBUTING.md](CONTRIBUTING.md) it also needs a `CHANGELOG.md` entry, a `README.md` update and a SemVer bump in
the repository-root `Directory.Build.props`. A `*REMOVED*` line is a break. `-MarkShipped` folds `Unshipped` into `Shipped` at
release time.

## Native AOT support

The **reflection paths are AOT-safe**: no companion package, no source generator, no consumer opt-in. Rationale and
measurements: [docs/DESIGN-DECISIONS.md](docs/DESIGN-DECISIONS.md#native-aot-and-trimming). Before changing anything that
reflects, run the AOT/trim reviewer or walk [its checklist](.agents/references/reviews/aot-compat.md). Three
constraints must not be broken:

- **Nothing may generate code at run time** — that, not reflection, is what Native AOT forbids. Accessors use
  `System.Reflection.MethodInvoker`, complex-object temporary tables use `EnumerableReader`, and the
  expression-compiled materializer sits behind a `RuntimeFeature.IsDynamicCodeSupported` branch the AOT compiler
  folds away. Add no IL-emitting dependency, and do not reintroduce the prototyped-and-rejected source generator.
- ⚠️ **The `[DynamicallyAccessedMembers]` chain must stay complete.** Under trimming a missing annotation makes
  reflection return *fewer members with no error* — measured: 6 columns in, 0 bound, no exception. It is invisible
  on the JIT, so **only `scripts/verify-package-aot.ps1` catches it**. That is why an `IL2xxx` warning is never
  suppressed; the code-style rules name the two sanctioned exceptions.
- **No consumer-facing diagnostics.** The generic query methods carry neither `[RequiresUnreferencedCode]` nor
  `[RequiresDynamicCode]`; adding either to a public API is a regression the AOT consumer's zero-diagnostic gate
  fails on. The argument: [docs/DESIGN-DECISIONS.md](docs/DESIGN-DECISIONS.md#4-no-consumer-facing-diagnostics).

## Conventions

- Branches: [Conventional Branch](https://conventionalbranch.org/) — `<type>/issue-<number>-<slug>` off
  `main`, or `<type>/<slug>` when there is no issue. Types: `feature bugfix hotfix release chore`. **The same
  rule applies to you.** There is no `claude/` or `codex/` prefix — a branch is named after what it does, not
  after who typed it.
- Commits: [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) with a **lowercase,
  imperative** summary — `build: standardize repository tooling`. A breaking change is `feat!:` or `fix!:`
  plus a `BREAKING CHANGE:` **footer**; `BREAKING CHANGE` is never a type. Full checklist:
  [the `commit` skill](.agents/skills/commit/SKILL.md).
- `CHANGELOG.md` follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/): user-visible changes go
  under `## [Unreleased]`. Internal formatting and tooling work needs no entry. **Never bump a version** —
  the version, the release date, the API-snapshot promotion and the tag are the maintainer's.
- Pull request process: [CONTRIBUTING.md](CONTRIBUTING.md#opening-the-pull-request).
- Line endings are LF everywhere; `.gitattributes` and `.editorconfig` enforce this and CI verifies it.
  Never hand-convert line endings, and never compare a multi-line source literal against
  `Environment.NewLine` - the literal carries the file's bytes, `Environment.NewLine` carries the host's.

### Releases

**Never `dotnet pack` and push by hand.** A release is a pushed tag and CI does the rest: it checks the tag against
the packed version, publishes all six packages to NuGet.org, and creates the GitHub release from the `CHANGELOG.md`
section. The three steps: [CONTRIBUTING.md](CONTRIBUTING.md#releasing).

## Working procedures

Two skills and two review agents are checked in, each under a Codex and a Claude name: `$commit` / `/commit`,
`$integration-db` / `/integration-db`, `aot_compat_reviewer` / `aot-compat-reviewer`, `adapter_parity_reviewer` /
`adapter-parity-reviewer`.

Claude Code (`.claude/settings.json`) and Codex (`.codex/hooks.json`) fire the same two PostToolUse hooks —
formatting and the public-API reminder — and both delegate to `scripts/`. Each one is **scoped to the file the
edit touched**: no fallback to every dirty file, no path outside the repository, and they never fail an edit.
Codex needs those hooks trusted once per clone (`/hooks`), and reviewed again whenever a pull request changes
one; until then nothing fires and you run `scripts/tidy-code.ps1` yourself. Details, including what to do when
no hook covers the edit: [.agents/README.md](.agents/README.md).

Reusable skills and reference material belong in `.agents/`, executable checks in `scripts/`, and only metadata and
hook wiring in `.codex/` and `.claude/`. Never fork a procedure or a check into a tool-specific copy — both
integrations follow the same canonical files. Layout: [.agents/README.md](.agents/README.md).
