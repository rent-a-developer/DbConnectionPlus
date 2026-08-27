<!-- Thank you for contributing! Please read CONTRIBUTING.md first. -->

## What does this change?

<!-- A short description of the change and, for behavior changes, the motivation. Link the related issue: Fixes #123 -->

## Checklist

- [ ] `dotnet build DbConnectionPlus.slnx -c Release` succeeds with **zero warnings** (`TreatWarningsAsErrors` is on, so this also covers style, trim and public-API diagnostics).
- [ ] `pwsh -File scripts/preflight.ps1` passes.
- [ ] New behavior and fixed bugs are covered by tests.
- [ ] **Adapter parity**: a change to one adapter is mirrored into the other four, or does not apply to them (see `.agents/references/reviews/adapter-parity.md`).
- [ ] Integration tests run for every adapter the change touches (`.agents/skills/integration-db/SKILL.md`).
- [ ] No new `IL2xxx`/`IL3050` diagnostics. If a reflection path changed: `pwsh -File scripts/verify-package-aot.ps1 -Pack` passes for **both** frameworks.
- [ ] Public API changes are declared in the affected `PublicAPI.Unshipped.txt` (`scripts/update-public-api.ps1`).
- [ ] XML docs and `README.md` updated for public API changes.
- [ ] `CHANGELOG.md` updated, and the version in `src/Directory.Build.props` bumped if this release-bound change needs it.
- [ ] Style, formatting and member ordering applied (`pwsh -File scripts/tidy-cs.ps1 -Scope all`, which
      `preflight.ps1` also runs).
- [ ] Branch name follows [Conventional Branch](https://conventionalbranch.org/): `<type>/issue-<issue#>-<slug>`.
