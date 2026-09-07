# Contributing

Contributions and bug reports are welcome. For anything larger than a fix, please open an issue first so the
approach can be agreed before you write it.

Please note we have a [code of conduct](CODE_OF_CONDUCT.md), and it applies to every interaction with the
project.

This page is self-contained: everything you need to build, check and submit a change is here.

## Setting up a clone

You need the **.NET 10 SDK** (`global.json` pins `10.0.100` with `rollForward: latestFeature`) and
**PowerShell 7** (`pwsh`), which every script in `scripts/` requires. `git` must be on `PATH`.

Two things to do once, after cloning:

```shell
dotnet tool restore
git config --local blame.ignoreRevsFile .git-blame-ignore-revs
```

The first installs the pinned local tools — CSharpier, the ReSharper command-line tools and docfx — which the
tidying script and the documentation build need. **No script ever installs a tool for you**; they fail and
tell you to run this instead.

The second is optional and per-clone. It makes `git blame` skip the commits listed in
`.git-blame-ignore-revs`, which reformatted and reordered the whole repository without changing what the code
does, so blame points at whoever wrote the logic rather than at the tool that moved it. GitHub already does
this on its own; this is only for your local `git blame`.

If you use Rider, two settings make the tooling invisible: install the **CSharpier** plugin and switch on
Settings | Tools | CSharpier | Run on Save, and use the shared **ReorderMembers** cleanup profile (from
`DbConnectionPlus.slnx.DotSettings`) when you want members put back in order.

### Database prerequisites

The **unit tests need nothing but the SDK**. The integration tests need a running
[Docker](https://www.docker.com/) daemon and nothing else: [Testcontainers](https://dotnet.testcontainers.org/)
starts one container per database system, waits until it accepts connections, and removes it when the run
ends. There is no compose file to bring up, no connection string to configure, and no port to look up — every
container publishes to a free host port and the fixture builds its connection string from that.

Oracle wants about 2 GB of memory for its container; if it exits or restarts repeatedly, check Docker's
resource limits.

### Native prerequisites

The Native AOT gate and the benchmarks' AOT job publish a native binary, which needs a C++ toolchain:

| Platform | What you need |
|---|---|
| Windows | MSVC, and `vswhere.exe` resolvable — the scripts put the Visual Studio Installer directory on `PATH` for you, without which the link step fails with a misleading `MSB3073` |
| Linux | `clang` and `zlib1g-dev` |

## The change

### Branches

Branch from `main` following [Conventional Branch](https://conventionalbranch.org/):

```text
<type>/issue-<number>-<slug>      feature/issue-42-bulk-insert
<type>/<slug>                     chore/tidy-the-release-notes      (when there is no issue)
```

The types are `feature`, `bugfix`, `hotfix`, `release` and `chore` — the long forms, not `feat` or `fix`.
Slugs are lowercase letters, digits and hyphens, with no hyphen at the start or end and never two in a row.

**The same rule applies to work done by an AI agent.** There is no `claude/` or `codex/` prefix: a branch is
named after what it does, not after who typed it.

### Commits

[Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/), with a **lowercase, imperative**
summary:

```text
build: standardize repository tooling
feat: add bulk insert for value tuples
fix: stop NameHelper scanning past the closing bracket
docs: move the guides out of the README
```

Types in use: `feat`, `fix`, `docs`, `test`, `refactor`, `chore`, `build`, `perf`, `ci`.

A **breaking change** is `feat!:` or `fix!:` plus a `BREAKING CHANGE:` footer explaining what breaks and what
to do about it. `BREAKING CHANGE` is a footer, never a type — `BREAKING CHANGE: …` as a subject line is wrong.

Add a body whenever the *why* is not obvious from the subject, and reference the issue number there when the
branch has one.

### Style, formatting and member ordering

Three tools own three concerns, and all three are build errors:

| Concern | Tool |
|---|---|
| Formatting — whitespace, line breaks, wrapping | CSharpier |
| Style — `var`, `=>`, `this.`, null checks, usings | the Roslyn analyzers |
| Ordering — types and their members | ReSharper applies it, NewStyleCop checks most of it |

One command applies all three:

```shell
pwsh -File scripts/tidy-code.ps1 -Scope all
```

The narrower scopes are `-Scope style` (adds the code-style fixers to the formatter, about 15 seconds) and the
default (`format`, CSharpier only, about a second, on the files git reports as changed). `-Scope all` covers
the whole solution, because ReSharper loads all of it either way.

`-Check` reports instead of fixing, and **never writes to your working tree** — at `-Scope all` it runs the
pipeline on a disposable copy and prints the diff it produced there.

### Verifying it

```shell
pwsh -File scripts/preflight.ps1
```

That is the gate to run before every commit. It checks the public API, checks style, formatting and ordering,
builds Release and runs the unit suite on both target frameworks. **By default it writes build output and
nothing else** — it does not edit your files and it does not touch the git index. Pass `-Fix` to have it apply
the tidying first:

```shell
pwsh -File scripts/preflight.ps1 -Fix
```

`TreatWarningsAsErrors` is on for **every** project, so the build is also the style, member-ordering,
trim-analyzer and public-API gate, and `CSharpier.MsBuild` makes an unformatted file a build error too. The
build never rewrites your files — it fails and names them.

⚠️ **Never suppress an `IL2xxx` warning to get a green build.** It is the only build-time evidence that the
trimming annotations are complete, and an incomplete chain means entities that come back silently empty under
trimming.

Two gates are deliberately outside preflight, because each takes minutes and neither applies to every change:

| Gate | Run it when | Command |
|---|---|---|
| Integration tests | the change can only be proven against a real database: SQL generation, an adapter, CRUD, temporary tables, type mapping | `dotnet test --project tests/DbConnectionPlus.IntegrationTests/DbConnectionPlus.IntegrationTests.csproj -c Release` |
| Native AOT | the change touches reflection, the `[DynamicallyAccessedMembers]` annotations, the materializers or the temporary-table readers | `pwsh -File scripts/verify-package-aot.ps1 -Pack` |

**Scope the integration run.** The default scope is SQLite + SQL Server (about 90 seconds, against about 10
minutes for all five); add `--filter-class "*MySql*"` and the like only for an adapter you actually changed.
A change to `IDatabaseAdapter`, `IEntityManipulator` or `ITemporaryTableBuilder` obliges all five.

The AOT gate runs both frameworks — `-Framework net8.0` for the documented floor, `net10.0` by default — and
CI runs both, on Linux and Windows, against the exact packages it will publish.

### Database adapters

There are five adapter projects under `src/DbConnectionPlus.DatabaseAdapters.*`, each implementing
`IDatabaseAdapter`, `IEntityManipulator` and `ITemporaryTableBuilder` with per-dialect SQL.

**A change to one adapter almost always has to be mirrored into the other four**, and only the integration
suite catches a miss. Some asymmetries are legitimate and should be left alone: identifier quoting, parameter
prefixes, temporary-table syntax, `GetDataType` mapping, generated-key readback, the bulk-insert paths, and
MySQL's separate enum handling in the temporary-table reader.

### Companion edits

| Change | What else it needs |
|---|---|
| New behaviour, or a fixed bug | tests |
| Anything user-visible | an entry under `## [Unreleased]` in `CHANGELOG.md`, in [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) format. Write a breaking change as `- **BREAKING:** …` |
| A public API change | `pwsh -File scripts/update-public-api.ps1`, then **review the diff** — it *is* the API change, and a `*REMOVED*` line is a break. Update the XML docs and the affected pages under `docs/` |

Internal formatting and tooling work needs no changelog entry: the changelog is for what a consumer sees.

**Do not bump a version.** The version, the release date, the promotion of `PublicAPI.Unshipped.txt` to
`Shipped`, and the tag are the maintainer's, at release time. Describing your change accurately under
`## [Unreleased]` is what lets them choose the number.

### Opening the pull request

Work through the checklist in the template. Everything under **Always** applies; the rest applies only when
its trigger does. CI runs the same gates plus CodeQL and a dependency review, and all of them must be green.

A maintainer will review it, and may ask for changes.

## Line endings

Every text file is LF, in the repository and in the working tree, on every OS. `.gitattributes` enforces this
whatever your `core.autocrlf` is set to, so there is nothing to configure, and CI fails if a wrongly stored
file lands anyway.

`.editorconfig` also asks editors and formatters to write LF. If one does not, git still stores LF, but
`git status` lists the file as modified while `git diff` shows nothing. Run
`pwsh -File scripts/tidy-code.ps1` to fix it, or `git checkout -- <path>` to discard it.

If you have set `git config core.safecrlf true`, git refuses to add such a file with "CRLF would be replaced
by LF". Run the tidy script first, or use `core.safecrlf warn`.

## Releasing

Releases are cut by CI from a pushed tag; nothing is packed or pushed by hand. The versioning scheme is
[SemVer](https://semver.org/). **This is the maintainer's procedure**, not a contributor's.

1. Bump `<Version>` in the repository-root `Directory.Build.props` — one edit for all six packages — and move
   `PackageValidationBaselineVersion` in `src/Directory.Build.props` to the version being replaced.
2. Fold the accumulated public-API entries into the shipped snapshots:
   `pwsh -File scripts/update-public-api.ps1 -MarkShipped`.
3. Turn `## [Unreleased]` in `CHANGELOG.md` into a dated section for that version (`## [4.1.0] - 2026-08-17`),
   and open a fresh empty `## [Unreleased]` above it. CI reads the dated section, uses it as the release
   notes, and refuses to publish if it is missing, undated or empty.
4. Merge to `main`, then push the tag:
   ```shell
   git tag v4.1.0 && git push origin v4.1.0
   ```

CI verifies the tag against the packed version, runs every gate — including the package-consumption and Native
AOT jobs, which nothing can bypass — then pushes all six packages to NuGet.org and creates the GitHub release.
