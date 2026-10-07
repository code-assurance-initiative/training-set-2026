# bench-ts-baseline-clean — the TypeScript control repository

Part of the [code-assurance-initiative scanner benchmark](https://github.com/code-assurance-initiative/scanner-benchmark).
Labels are in [`answer-key.json`](answer-key.json) (format: `scanner-benchmark/docs/CONTRACT.md`); the authoring log is
[`journal.md`](journal.md).

## Theme

A small, realistic Node.js 22 + TypeScript web API for a warehouse: **SKUs, bin locations, stock levels and
reservations with expiry** — the same domain as the C# control, `bench-csharp-baseline-clean`, so the two can be
compared. It is written to be what a careful team would ship with a current, mainstream stack (Express 5, zod, pino,
helmet, jose, vitest, ESLint with typescript-eslint, Prettier), and nothing is planted in it. It is the benchmark's
TypeScript **control**: every result a scanner reports here is either noise or a real defect the authors missed (the
authoring loop fixes the latter in the repository, never in the key).

There are **no `must-fire` and no `must-not-fire` labels** in the first draft. A control repository with planted
traps would stop being a control; traps live in the themed repositories. A trap is added only when a scanner reports
a site in this repository that a careful reviewer would not flag (the result is then recorded as noise and the site
promoted), exactly as in the C# control.

## What is certified clean, and why

Every tracked file carries a `clean` label for all concepts (`"concepts": "*"`): production code, tests, manifest and
lock file, configuration, CI, documentation. One repository-level `clean` entry covers properties that have no single
location (dependency currency and vulnerabilities, licences, end-of-life platforms, secrets in history, strict null
checks, pinned CI actions, import cycles and layering, authorisation of every route, …).

The code is clean by construction, not by suppression:

| Property | How it is met |
|---|---|
| Complexity, size, duplication | Short single-purpose functions; one module per concern; request schemas and problem responses defined once |
| Type safety | Every file TypeScript under `strict` (plus `noUncheckedIndexedAccess`, `exactOptionalPropertyTypes`); no `any`, no `!` non-null assertions, no `@ts-` directives; typescript-eslint `strictTypeChecked` |
| Error handling | Expected failures are values (`Result<T>`), mapped to RFC 9457 problem responses; no empty catch, no catch-and-rethrow; unexpected errors are logged and answered with a generic 500 without stack traces |
| Async hygiene | The store is synchronous and in process (ADR 0002), so no I/O promise is left unawaited or needs cancelling; no floating promises (lint-enforced); the expiry timer callback is synchronous |
| Logging | pino structured logging with pino-http request logs; messages are constant strings with data in fields; nothing sensitive is logged (there is nothing sensitive) and the `authorization` header is redacted |
| Security | JWT bearer authentication with jose: ES256 signature, issuer, audience and expiry verified against a **public** key from the environment — no secret anywhere, nothing fetched over the network; a scope check on every `/api` route; only `/health` is anonymous; helmet security headers (CSP, HSTS, `X-Content-Type-Options`, frame and referrer policy); plain HTTP refused outside development; JSON bodies size-limited and every input parsed with zod; no SQL, shell, file-path, template, deserialisation-of-types or outbound HTTP sinks |
| Supply chain | `package-lock.json` committed and installed with `npm ci`; `engines` and `.nvmrc` pin Node.js 22 LTS; current, non-deprecated packages with no `npm audit` finding; actions pinned by commit SHA; Dependabot with a cooldown; CodeQL on `pull_request`; `npm audit` on a weekly schedule |
| Tests | vitest unit tests for services and stores, supertest HTTP-level tests for every route; v8 coverage with thresholds that fail the run; time is a fake clock; test JWT keys are generated in memory per run |
| Debt | No TODO/FIXME/HACK, no commented-out code, no `eslint-disable`, no unused code |

## What is not applicable, and why

`not-applicable` entries (one per concept) are concepts that cannot occur in this repository — a result for any of them
is noise:

- **Frontend and accessibility** (markup, ARIA, alt texts, link and heading texts, a11y checks in CI): a JSON-only API.
- **Privacy / compliance** (data encryption controls, audit trail, retention, data-subject rights, personal-data
  inventory): the service handles SKUs, bins and quantities only — no data about people.
- **Domain-driven design** (aggregates, entities, value objects, domain rules): a plain http / application /
  infrastructure split with a transaction-script application layer and readonly types, and no DDD vocabulary.
  *Decision recorded before writing the code: no domain layer, so these stay not-applicable.*
- **Events, messaging, event sourcing, CQRS query handlers**: none.
- **Kubernetes runtime posture**: no manifests, nothing deployed.
- **Schema migrations**: no database (ADR 0002). **Library versioning**: not a library. **BDD**: none.
- **.NET-only mechanisms**: IL size, solution structure, DI-container lifetimes, reflection type lookup, Blazor
  interop, `throw ex` stack resets (JavaScript fixes a stack when the error is created), thread hand-offs and lock
  release (single-threaded event loop, synchronous store).

Deviation from `coverage/matrix.json`: the matrix marks several dimensions as C#-only and therefore not-applicable for a
TypeScript repository (X1, X3, X20, X21, X27, X28, AX2, AX8, X12, X14, D4, D7, X7). Where the *concept* behind such a
dimension can occur in TypeScript (catch-and-rethrow, mistyped guards, unprotected index access, a collection mutated
while iterated, production importing test code, shared mutable state, unreachable code, SSRF, duplication, silent
fallbacks, unenforced architecture rules, floating async callbacks), it is certified **clean** at repository level
instead of not-applicable: the code was written to be free of it, which is a stronger and truer claim. Either label
makes a scanner's result on it noise.

## Score bands — chosen from intent, before any scan

`score-band` entries are 0–100 expectations for posture, metric and model-judged properties:

- **Clean posture and structure** (README, architecture docs, folder structure, CI, authorisation, security headers,
  HTTPS, input validation, lock file, disclosure policy, tooling scripts, complexity, duplication, size): **[80, 100]**
  or **[85, 100]**; type safety **[90, 100]** (all TypeScript, strict).
- **Metrics whose definition varies between scanners** (instability, main-sequence distance, cohesion, call
  indirection, coverage ratios, test pyramid, dependency freshness): **[60, 100]** or **[70, 100]**.
- **Reward-leaning properties** (benchmarks, allocation awareness) and the business-logic share: **[40, 100]**.
- **Properties with nothing to measure** (deployment rollback, disaster recovery, build provenance for a repository
  that releases nothing): wide bands **[30–50, 100]**.
- **Model-judged properties**: **[60, 100]**, wide for judge variance.
- **Knowledge concentration**: **[0, 100]** — no claim (one author by construction).

## Reproduce

```bash
git clone https://github.com/code-assurance-initiative/bench-ts-baseline-clean
git clone https://github.com/code-assurance-initiative/scanner-benchmark
cd bench-ts-baseline-clean && npm ci && npm run lint && npm run typecheck && npm test
# run any scanner over the clone, producing SARIF, then:
cd ../scanner-benchmark
python3 -m cai_bench validate --key ../bench-ts-baseline-clean/benchmark/answer-key.json --taxonomy taxonomy.json
python3 -m cai_bench score --help
```
