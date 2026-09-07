# Security Policy

## Supported versions

| Version | Supported |
|---|---|
| 4.x | ✅ |
| < 4.0 | ❌ |

Fixes are released from `main` as a new patch version of every package, which are versioned and released
together.

## Reporting a vulnerability

**Please do not open a public issue for a security problem.**

Report it privately through GitHub's
[security advisory form](https://github.com/rent-a-developer/DbConnectionPlus/security/advisories/new), or by
email to the address on the [maintainer's profile](https://github.com/rent-a-developer).

Please include the affected package and version, what an attacker can do with it, and a minimal reproduction
if you have one. You can expect an acknowledgement within a few days, and an assessment of whether the report
is accepted along with a rough timeline once it has been reproduced. Please give us a chance to ship a fix
before disclosing publicly.

## Scope

The library builds SQL statements and sends them to a database on the caller's behalf, so the reports most
worth sending concern:

- **SQL injection** — anything that lets caller-supplied *data* be interpreted as SQL. Interpolated statements
  are parameterized by design, and a path where that fails is a vulnerability.
- **Identifier handling** — table, column and temporary-table names that are not correctly quoted or escaped
  for the target database.
- **Credential leakage** — a connection string or credential ending up in an exception message, log or
  command interception callback.

Out of scope: vulnerabilities in the underlying ADO.NET drivers or database servers, which belong to their
vendors. Transitive dependencies are watched by Dependabot and a dependency review on every pull request; if
you spot one we have missed, an ordinary issue is fine.
