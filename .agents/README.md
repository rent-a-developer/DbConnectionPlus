# AI-agent integration

Reusable AI-agent instructions have one canonical location:

```text
AGENTS.md                              Repository guidance
.agents/skills/*/SKILL.md              Workflow procedures
.agents/references/*.md                Reference material AGENTS.md links to
.agents/references/reviews/*.md        Review checklists
scripts/*.ps1                          Executable checks and workflows
```

Tool-specific directories contain only discovery metadata or protocol adapters:

```text
.claude/skills/*/SKILL.md              Claude skill metadata plus a shared-procedure reference
.claude/agents/*.md                    Claude agent metadata plus a shared-checklist reference
.claude/settings.json                  Claude hook wiring
.claude/hooks/*.ps1                    Claude hook protocol adapters
.codex/agents/*.toml                   Codex agent metadata plus a shared-checklist reference
.codex/hooks.json                      Codex hook wiring
.codex/hooks/*.ps1                     Codex hook protocol adapters
```

Codex discovers the canonical skills directly from `.agents/skills/`. Their `agents/openai.yaml` files contain
Codex-only UI metadata and explicit-invocation policy. Claude needs thin skill wrappers because its
`disable-model-invocation` policy lives in `SKILL.md` frontmatter.

The hook adapters differ because Claude and Codex use different payload and response contracts. Both delegate
all substantive behavior to the same scripts: `scripts/tidy-code.ps1` and `scripts/public-api-guard.ps1`.

## What the hooks do, and what they will not do

Both `PostToolUse` hooks are **scoped to the file the triggering edit touched**. Claude reports it as
`tool_input.file_path`; Codex reports an `apply_patch` whose patch text is in `tool_input.command`, so the
adapter reads the `*** Add File:`, `*** Update File:` and `*** Move to:` headers out of it and skips
`*** Delete File:`.

These rules follow, and they are the point of the design:

- **No fallback.** If the payload cannot be parsed, the hook formats nothing and says so in one line. It does
  not fall back to "every file git reports as changed" — that would rewrite work in progress that this edit
  never touched.
- **No path outside the repository.** Every path is resolved and then checked to be under the repository
  root, so `../` and a symbolic link that points out of the tree are both refused. `bin/` and `obj/` are
  refused too; generated output is not ours to format.
- **They never fail an edit.** A `PostToolUse` failure cannot undo an edit that already happened, so both
  adapters exit 0 whatever went wrong and report it as text. Neither one stages a file, changes an API
  snapshot, installs a tool, or runs a style, ordering, build or test pass.

The child scripts run in a **child `pwsh` process**. They end with `exit`, which run in-process would end the
adapter before it could emit its protocol output — and a Codex hook that writes nothing is a hook that
failed. Concurrent edits serialize on a named mutex derived from the repository path, so two formatters
cannot run over one file.

### Codex needs the hooks trusted, once per clone

Codex does not run a repository's hooks until the project is trusted. Until then **nothing fires**. Run
`/hooks` in Codex to see the hook definitions this repository declares, review them, and trust the project.
Review them again whenever `.codex/hooks.json` or anything under `.codex/hooks/` changes in a pull request:
a hook is code that runs on your machine after every edit, and "it was already trusted" is not a review.

Never bypass project trust to make a hook fire.

### When no hook covers the edit

The hooks only see edits made through a tool that reports one. An edit made another way — a shell
redirection, an editor outside the agent, a `git apply`, a Codex session whose hooks are not trusted yet — is
not formatted by anything. So:

```bash
pwsh -File scripts/tidy-code.ps1              # format what git reports as changed
pwsh -File scripts/preflight.ps1              # and before committing, check the whole tree
```

`preflight.ps1` is the backstop for all of it, and the build is the backstop for `preflight`: formatting,
style and member ordering are build errors, so an unformatted file cannot reach a green pull request whether
a hook fired or not.

The tidy hook runs the **default scope only**, on the file the edit touched — CSharpier, under a second. Code
style and member ordering are not run on every edit: `dotnet format style` needs MSBuild and ReSharper loads
the whole solution, and neither belongs on the critical path of a single edit. All of them are build errors, and
`scripts/preflight.ps1` checks `-Scope all` before a commit (`-Fix` applies it), so nothing reaches a pull
request untidied.

When changing behavior, edit the canonical file. Keep only required names, descriptions, policies, tool/model
settings, and reference instructions in tool-specific files.
