<!-- Thank you for contributing! Please read CONTRIBUTING.md first. -->

## What does this change?

<!-- A short description of the change and, for behavior changes, the motivation. Link the related issue: Fixes #123 -->

## Checklist

Everything under **Always** applies to every pull request. The rest applies only when its trigger does —
tick it, or leave it and say why in the description. A box that does not apply is not a box to tick.

### Always

- [ ] `pwsh -File scripts/preflight.ps1` passes. It runs the public-API reminder, checks style, formatting
      and member ordering, builds Release and runs the unit suite on both target frameworks. Use
      `-Fix` to have it apply the tidying rather than only report it.
- [ ] The Release build produces **zero warnings**. `TreatWarningsAsErrors` is on, so this also covers style,
      member ordering, trim and public-API diagnostics.
- [ ] Branch name follows [Conventional Branch](https://conventionalbranch.org/):
      `<type>/issue-<number>-<slug>`, or `<type>/<slug>` when there is no issue.

### If the change adds behaviour or fixes a bug

- [ ] It is covered by tests.
- [ ] `CHANGELOG.md` has an entry under `## [Unreleased]`. Do **not** bump a version — the version, the
      release date and the tag are the maintainer's, at release time.

### If the change touches a database adapter, or the shared adapter seam

- [ ] It is mirrored into the other four adapters, or it genuinely does not apply to them — see
      [CONTRIBUTING.md](../CONTRIBUTING.md#database-adapters).
- [ ] The integration tests ran for every adapter the change touches, and the description says which.

### If the change touches a reflection path

Reflection, the `[DynamicallyAccessedMembers]` annotations, the materializers, or the temporary-table
readers. Nothing is trimmed on the just-in-time compiler, so no other check in this repository can see the
damage a mistake here does.

- [ ] `pwsh -File scripts/verify-package-aot.ps1 -Pack` passes for **both** frameworks
      (`-Framework net8.0` and the `net10.0` default).
- [ ] No new `IL2xxx` / `IL3050` diagnostics, and none suppressed.

### If the change touches the public API

- [ ] The affected `PublicAPI.Unshipped.txt` is updated with `pwsh -File scripts/update-public-api.ps1`, and
      the diff was reviewed line by line. A `*REMOVED*` entry is a breaking change.
- [ ] The XML documentation and the affected pages under `docs/` are updated.
