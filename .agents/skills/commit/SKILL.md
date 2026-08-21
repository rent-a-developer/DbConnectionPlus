---
name: commit
description: Review, verify, deliberately stage, and commit the current DbConnectionPlus repository changes. Use only when the user explicitly asks you to create a Git commit; do not use for ordinary code changes, reviews, or status checks.
---

# Commit

Write a commit that matches how `main` is written, and check the repo's own release-hygiene rules first.

## 1. Look at what changed

```bash
git status
```

```bash
git diff HEAD
```

Read the actual diff. Do not write a message from file names alone.

## 2. Apply the CONTRIBUTING.md checklist

Before committing, check whether the change requires companion edits and raise anything missing with the user:

- **Public API changed?** The build already told you — an undeclared public member is `RS0016` and a vanished
  one `RS0017`. Record it with `pwsh -File scripts/update-public-api.ps1` and review the
  `PublicAPI.Unshipped.txt` diff; a `*REMOVED*` line is a break.
- **User-facing change?** `CHANGELOG.md` needs an entry under `## [Unreleased]` or the next version heading,
  Keep-a-Changelog format (`### Added` / `### Changed` / `### Fixed`). Breaking changes are written
  `- **BREAKING:** …` and a `### Migration from Nx` section is added for a major.
- **Interface or behaviour change?** `README.md` — the "API summary" section and any affected examples. The
  README also carries version numbers in its examples. `PACKAGE_README.md` (the NuGet package page) only needs
  touching if the change makes its short overview wrong.
- **SemVer bump?** `<Version>` in `src/Directory.Build.props` — one edit, applied to all six shipping projects.
- **Adapter change?** Was it mirrored into the other four adapters? Delegate the check to the
  `adapter_parity_reviewer` custom agent when the change meets that agent's scope.
- **Touched a reflection path?** `pwsh -File scripts/verify-package-aot.ps1 -Pack` — the unit and integration
  suites cannot see silent trimming damage. Delegate the review to the `aot_compat_reviewer` custom agent.

## 3. Verify it builds

`TreatWarningsAsErrors=true` means a style slip is a build break, and CONTRIBUTING.md requires "all tests pass
and the build succeeds with no warnings".

```bash
pwsh -File scripts/preflight.ps1
```

That script runs the public-API reminder, the Release build and the unit tests. Equivalent by hand:

```bash
dotnet build DbConnectionPlus.slnx -c Release
```

```bash
dotnet test --project tests/DbConnectionPlus.UnitTests/DbConnectionPlus.UnitTests.csproj
```

If either fails, report the failure and stop — don't commit over it.

## 4. Write the message

Conventional Commits, matching `main`'s history:

```
feat: Implement feature Optimistic Concurrency Support via Concurrency Tokens
fix: NameHelper.CreateNameFromCallerArgumentExpression stops scanning to early
BREAKING CHANGE: Rename NuGet packages
```

- Subject line: type prefix, then a capitalised, descriptive summary. Imperative or descriptive both appear in
  this history — match the surrounding style.
- Types in use: `feat`, `fix`, `BREAKING CHANGE`. Use `docs`, `test`, `refactor`, `chore`, `build` where they
  genuinely fit.
- Add a body when the *why* is not obvious from the subject.
- If the branch is `feature/<n>-<slug>` or `bugfix/<n>-<slug>`, reference the issue number in the body.

Stage deliberately — `git add` the relevant paths rather than `git add -A`, and confirm nothing unintended
(build output, local scratch files) is included. A `PublicAPI.*.txt` change belongs in the same commit as
the code that caused it.

## 5. Commit

Commit only. Do not push and do not open a PR unless the user asks.
