# AI-agent integration

Reusable AI-agent instructions have one canonical location:

```text
AGENTS.md                              Repository guidance
.agents/skills/*/SKILL.md              Workflow procedures
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
all substantive behavior to the same scripts.

When changing behavior, edit the canonical file. Keep only required names, descriptions, policies, tool/model
settings, and reference instructions in tool-specific files.
