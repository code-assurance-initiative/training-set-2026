# ADR 0003 — Third-party licence policy

- Status: accepted
- Date: 2025-09-01

## Context

The invoicing bundle is distributed to customers under our proprietary licence: they receive container images and a
command-line tool and run them on their own machines. Every third-party package compiled into, or shipped with, a
component of the bundle is redistributed by us and must allow that.

## Decision

For packages that are **shipped** (referenced by a project under `src/` or `tools/`, directly or transitively):

| Licence | Rule |
|---|---|
| MIT, Apache-2.0, BSD-2/3-Clause, MS-PL, ISC, Zlib, PostgreSQL | allowed |
| LGPL-2.0, LGPL-2.1, LGPL-3.0 | allowed only for an **unmodified** library consumed as a **separate assembly** (a NuGet package loaded at run time), so the user can replace it; never vendored or merged into our assemblies |
| GPL (any version, including `WITH` exceptions that cover only FOSS combinations), AGPL, SSPL, EUPL | **forbidden** unless a commercial licence for the bundle is recorded in this ADR |
| No licence, or "all rights reserved" | forbidden |
| Custom or source-available licences (e.g. revenue-capped community licences) | require a recorded review before use |

Packages referenced only by projects under `tests/` are not shipped and are not restricted by this policy; their
licence must still permit our use of them.

No commercial licence is currently recorded.

## Consequences

- Every new shipped dependency is checked against this table in review.
- A package whose licence changes between versions (for example to a commercial licence) is pinned to the last
  acceptable line until a decision is recorded; see ADR 0004.
