# Contributing

When contributing to this repository, please first discuss the change you wish to make via issue,
email, or any other method with the owners of this repository before making a change. 

Please note we have a code of conduct, please follow it in all your interactions with the project.

## Pull Request Process

1. Branch from `main` following [Conventional Branch](https://conventionalbranch.org/):
   `<type>/<description>`, or `<type>/issue-<issue#>-<slug>` when there is an issue — for example
   `feature/issue-42-bulk-insert`. The types are `feature/`, `bugfix/`, `hotfix/`, `release/` and
   `chore/`; use the long forms, not `feat/` or `fix/`. Descriptions are lowercase letters, digits and
   hyphens, with no hyphen at the start or end and never two in a row. An AI agent working on its own
   branch may use `claude/` or `codex/` in place of a type.

   Use [Conventional Commits](https://www.conventionalcommits.org/) for the messages.
2. Make the change, with tests. New behavior and fixed bugs need coverage; a change to one database adapter
   almost always has to be mirrored into the other four.
3. Run the pre-commit gate. It applies style, formatting and member ordering, then builds and runs the
   unit suite:
   ```shell
   pwsh -File scripts/preflight.ps1
   ```
   It rewrites files — review what it changed and include it in your commit. To run just the tidying:
   `pwsh -File scripts/tidy-cs.ps1 -Scope all`.

   `TreatWarningsAsErrors` is on for **every** project, so the build is also the style, member-ordering,
   trim-analyzer and public-API gate, and `CSharpier.MsBuild` makes an unformatted file a build error too.
   The build never rewrites your files — it fails and names them; `scripts/tidy-cs.ps1` is what fixes them.
   **Never suppress an `IL2xxx` warning to get a green build** — it is the only
   build-time evidence that the trimming annotations are complete.
4. If you touched a reflection path, also run the Native AOT gate. Nothing else in the repository can see
   silent trimming damage:
   ```shell
   pwsh -File scripts/verify-package-aot.ps1 -Pack
   ```
5. If you changed a database adapter, run that adapter's integration tests against a real database — see
   [.agents/skills/integration-db/SKILL.md](.agents/skills/integration-db/SKILL.md).
6. Update the companion files: `CHANGELOG.md` under the upcoming version following
   [Keep a Changelog](https://keepachangelog.com/), `README.md` for any public API change, and the affected
   project's `PublicAPI.Unshipped.txt` via `pwsh -File scripts/update-public-api.ps1`.
7. Open the pull request and work through the checklist in its template. CI runs the same gates plus CodeQL
   and a dependency review; all of them must be green.
8. Your Pull Request will be reviewed by project maintainers. Address any feedback provided.
9. Once approved by the maintainers, your Pull Request will be merged.

The full working guide — layout, the adapter seam, code style, the declared public API and Native AOT — is
[AGENTS.md](AGENTS.md). It is written for AI coding agents, but everything in it applies to people too.

## Setting up a clone

Two things to do once, after cloning:

```shell
dotnet tool restore
git config blame.ignoreRevsFile .git-blame-ignore-revs
```

The first installs CSharpier, the ReSharper command line tools and docfx, which `scripts/tidy-cs.ps1` and the
documentation build need. The second makes `git blame` skip the commits listed in `.git-blame-ignore-revs`,
which reformatted and reordered the whole repository, so blame points at whoever wrote the logic rather than
at the tool that moved it. GitHub already does this on its own.

If you use Rider, two settings make the tooling invisible: install the **CSharpier** plugin and switch on
Settings | Tools | CSharpier | Run on Save, and use the shared **ReorderMembers** cleanup profile (from
`DbConnectionPlus.slnx.DotSettings`) when you want members put back in order.

## Releasing

Releases are cut by CI from a pushed tag; nothing is packed or pushed by hand. The versioning scheme is
[SemVer](https://semver.org/).

1. Bump `<Version>` in `src/Directory.Build.props` — one edit for all six packages.
2. Give the `CHANGELOG.md` section for that version a real date (`## [4.1.0] - 2026-08-17`). CI reads this
   section, uses it as the release notes, and refuses to publish if it is missing, undated or empty.
3. Merge to `main`, then push the tag:
   ```shell
   git tag v4.1.0 && git push origin v4.1.0
   ```

CI verifies the tag against the packed version, runs every gate, then pushes all six packages to NuGet.org and
creates the GitHub release. Details: [AGENTS.md](AGENTS.md#releases).

## Code of Conduct

### Our Pledge

In the interest of fostering an open and welcoming environment, we as
contributors and maintainers pledge to making participation in our project and
our community a harassment-free experience for everyone, regardless of age, body
size, disability, ethnicity, gender identity and expression, level of experience,
nationality, personal appearance, race, religion, or sexual identity and
orientation.

### Our Standards

Examples of behavior that contributes to creating a positive environment
include:

* Using welcoming and inclusive language
* Being respectful of differing viewpoints and experiences
* Gracefully accepting constructive criticism
* Focusing on what is best for the community
* Showing empathy towards other community members

Examples of unacceptable behavior by participants include:

* The use of sexualized language or imagery and unwelcome sexual attention or advances
* Trolling, insulting/derogatory comments, and personal or political attacks
* Public or private harassment
* Publishing others' private information, such as a physical or electronic address, without explicit permission
* Other conduct which could reasonably be considered inappropriate in a professional setting

### Our Responsibilities

Project maintainers are responsible for clarifying the standards of acceptable
behavior and are expected to take appropriate and fair corrective action in
response to any instances of unacceptable behavior.

Project maintainers have the right and responsibility to remove, edit, or
reject comments, commits, code, wiki edits, issues, and other contributions
that are not aligned to this Code of Conduct, or to ban temporarily or
permanently any contributor for other behaviors that they deem inappropriate,
threatening, offensive, or harmful.

### Scope

This Code of Conduct applies both within project spaces and in public spaces
when an individual is representing the project or its community. Examples of
representing a project or community include using an official project e-mail
address, posting via an official social media account, or acting as an appointed
representative at an online or offline event. Representation of a project may be
further defined and clarified by project maintainers.

### Enforcement

Instances of abusive, harassing, or otherwise unacceptable behavior may be
reported by contacting the project owner. All complaints will be reviewed and
investigated and will result in a response that is deemed necessary and appropriate
to the circumstances. The project team is obligated to maintain confidentiality
with regard to the reporter of an incident. Further details of specific enforcement
policies may be posted separately.

Project maintainers who do not follow or enforce the Code of Conduct in good
faith may face temporary or permanent repercussions as determined by other
members of the project's leadership.

### Attribution

This Code of Conduct is adapted from the [Contributor Covenant][homepage], version 1.4,
available at [https://contributor-covenant.org/version/1/4][version]

[homepage]: https://contributor-covenant.org
[version]: https://contributor-covenant.org/version/1/4/
