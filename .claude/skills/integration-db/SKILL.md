---
name: integration-db
description: Run the correctly scoped integration test suite against MySQL, Oracle, PostgreSQL, SQL Server and SQLite, and troubleshoot the containers Testcontainers starts for it.
disable-model-invocation: true
---

# Integration test databases

Read [.agents/skills/integration-db/SKILL.md](../../../.agents/skills/integration-db/SKILL.md) in full and follow
it exactly. The suite starts and stops its own database containers, so the procedure is a test run and its
scoping rules - do not start anything by hand.
