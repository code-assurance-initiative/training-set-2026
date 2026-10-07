# ADR 0002 — Third-party licence policy

- Status: accepted
- Date: 2025-06-10

## Context

The service and `manifest-export` are installed on operators' servers and PCs under our proprietary licence (ADR
0001). Every package installed by `npm ci --omit=dev` for either of them is redistributed by us and must allow that.

## Decision

For packages that are **shipped** (a production dependency of the service or of `manifest-export`, directly or
transitively):

| Licence                                                                                                | Rule                                                                                                                             |
| ------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------- |
| MIT, MIT-0, ISC, Apache-2.0, BSD-2-Clause, BSD-3-Clause, 0BSD, Zlib, Unlicense, CC0-1.0, BlueOak-1.0.0 | allowed                                                                                                                          |
| MPL-2.0, LGPL-2.1, LGPL-3.0                                                                            | allowed only for an **unmodified** package installed as its own module in `node_modules`, never copied or bundled into our files |
| GPL (any version), AGPL (any version), SSPL, EUPL                                                      | **forbidden** unless a commercial licence is recorded in this ADR                                                                |
| No licence, `UNLICENSED`, "all rights reserved"                                                        | forbidden                                                                                                                        |
| Source-available or custom licences                                                                    | require a recorded review before use                                                                                             |

A licence **expression** is read as SPDX defines it: for `A OR B` we may choose either, and we take the allowed
option (for example `(MIT OR GPL-3.0-or-later)` is used under MIT); for `A AND B` both apply.

devDependencies are not shipped and are not restricted by this policy; their licence must still permit our use of
them in development and CI.

No commercial licence is currently recorded.

## Consequences

- Every new production dependency is checked against this table in review, including what it pulls in.
- A package whose licence changes between versions is held at the last acceptable version until a decision is
  recorded here.
