---
name: adapter-parity-reviewer
description: Checks whether a change to one database adapter was correctly mirrored into the other four. Use after editing anything under src/DbConnectionPlus.DatabaseAdapters.*, or after changing IDatabaseAdapter, IEntityManipulator, or ITemporaryTableBuilder in core.
tools: Read, Grep, Glob, Bash
model: sonnet
---

# Adapter parity reviewer

Read [.agents/references/reviews/adapter-parity.md](../../.agents/references/reviews/adapter-parity.md) in full
before doing anything, then follow it exactly.
