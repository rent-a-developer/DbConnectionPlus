# CLAUDE.md

Read [AGENTS.md](AGENTS.md) first. It is the canonical repository guidance and everything in it applies to
Claude Code.

The files under `.claude/skills/` and `.claude/agents/` contain only Claude Code discovery metadata and point
to their shared procedures under `.agents/`. Keep reusable instructions in those shared files, not here or in
the wrappers.

`.claude/settings.json` wires the PostToolUse adapters under `.claude/hooks/` to the canonical implementations
in `scripts/`. Change hook behavior in `scripts/`; keep `.claude/hooks/` limited to Claude's hook protocol.
