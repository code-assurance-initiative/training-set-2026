# bench-csharp-baseline-clean — the control repository

Part of the [code-assurance-initiative scanner benchmark](https://github.com/code-assurance-initiative/scanner-benchmark).
Labels are in [`answer-key.json`](answer-key.json) (format: `scanner-benchmark/docs/CONTRACT.md`); the authoring log is
[`journal.md`](journal.md).

## Theme

A small, realistic ASP.NET Core (net10.0) web API for a warehouse: **SKUs, bin locations, stock levels and
reservations with expiry**. It is written to be what a careful team would ship — and nothing is planted in it. It is
the benchmark's **control**: every result a scanner reports here is either noise or a real defect the authors missed
(the authoring loop fixes the latter in the repository, never in the key).

There are **no `must-fire` and no `must-not-fire` labels**. A control repository with traps would stop being a control;
traps live in the themed repositories.

## What is certified clean, and why

Every tracked file carries a `clean` label for all concepts (`"concepts": "*"`): production code, tests, project
and lock files, configuration, CI, and documentation. One repository-level `clean` entry covers properties that have no
single location (dependency currency, licences, end-of-life platforms, secrets in history, nullable analysis,
pinned CI actions).

The code is clean by construction, not by suppression:

| Property | How it is met |
|---|---|
| Complexity, size, duplication, cohesion | Short single-purpose methods; one type per file; shared validation patterns in one class |
| Async hygiene | Every async method takes a `CancellationToken` last and forwards it; library projects use `ConfigureAwait(false)`; no blocking waits, no `async void` |
| Null safety | `<Nullable>enable</Nullable>` everywhere, warnings as errors, no `!` operator |
| Error handling | Expected failures are values (`OperationResult<T>`), mapped to RFC 7807 problem responses; no empty or rethrowing catch blocks; unhandled exceptions go to the framework's problem-details handler without stack traces |
| Logging | Source-generated `[LoggerMessage]` structured logging in every project; nothing sensitive is logged (there is nothing sensitive) |
| Concurrency | One store-owned lock (`System.Threading.Lock`) around all inventory reads and writes; no static mutable state |
| Security | JWT bearer authentication configured with authority and audience only — **no secret anywhere**, signing keys come from the issuer's metadata; three named scope policies; `[Authorize]` on every controller and action plus an authenticated-user fallback policy; only `/health` is anonymous; HTTPS redirection, HSTS, CSP and companion security headers; validated request models; no SQL, shell, file-path, XML, deserialisation-of-types or outbound HTTP sinks |
| Supply chain | Central Package Management, committed `packages.lock.json`, current non-deprecated direct packages (no vulnerable package, direct or transitive), actions pinned by commit SHA, Dependabot, CodeQL on `pull_request` |
| Tests | xUnit v3 (VSTest runner, coverlet collector so coverage is measurable) unit tests for services and stores, `WebApplicationFactory` integration tests for every endpoint; time is a `FakeTimeProvider`, test JWT signing keys are generated in memory per run |
| Debt | No TODO/FIXME/HACK, no commented-out code, no `#pragma warning disable`, no unused code |

## What is not applicable, and why

`not-applicable` entries (one per concept) are concepts that cannot occur in this repository — a result for any of them
is noise:

- **Privacy / compliance** (data encryption controls, audit trail, retention, data-subject rights, personal-data
  inventory): the service handles SKUs, bins and quantities only — no data about people.
- **Domain-driven design** (aggregates, entities, value objects, domain rules): the repository deliberately has a
  plain Api / Application / Infrastructure split with a transaction-script application layer and immutable records,
  and no DDD vocabulary. *Decision recorded before writing the code: no domain layer, so these stay not-applicable.*
- **Events, messaging, event sourcing**: none.
- **Frontend and accessibility** (markup, ARIA, JS/TS tooling, Blazor interop): a JSON-only API.
- **Kubernetes runtime posture**: no manifests, nothing deployed.
- **Schema migrations**: no database (ADR 0002). **Library versioning**: not a library. **BDD**: none.
- **Checkable architecture rules**: the only architecture rule is the layering, and project references make the
  compiler enforce it.

Deviation from `coverage/matrix.json`: the matrix lists `unused-dependency`, `undeclared-dependency` and
`misplaced-dev-dependency` under frontend dimension R8, which is not applicable here. As *concepts* they do apply
to NuGet manifests, so they are certified **clean** at repository level instead of not-applicable.

## Score bands — chosen from intent, before any scan

`score-band` entries are 0–100 expectations for posture, metric and model-judged properties:

- **Clean posture and structure** (README, architecture docs, folder structure, CI, authorization, security headers,
  HTTPS, input validation, lock files, disclosure policy, complexity, duplication, size): **[80, 100]** or
  **[85, 100]** — the repository is built to meet them.
- **Metrics whose definition varies between scanners** (instability, cohesion, call indirection, coverage ratios,
  business-logic share, test pyramid): **[60, 100]** or **[70, 100]**.
- **Reward-leaning properties** (benchmarks, allocation-aware APIs): **[40, 100]** — their absence in a small CRUD API is
  the neutral posture, not a defect.
- **Properties with nothing to measure** (deployment rollback, disaster recovery, build provenance for a repository
  that releases nothing): wide bands **[30–50, 100]** — a scanner should not deduct for machinery that has no reason to
  exist.
- **Model-judged properties**: **[60, 100]**, wide for judge variance.
- **Knowledge concentration**: **[0, 100]** — no claim. The repository has one author by construction; history-based
  measurement belongs to `bench-csharp-maturity-history`.

## Reproduce

```bash
git clone https://github.com/code-assurance-initiative/bench-csharp-baseline-clean
git clone https://github.com/code-assurance-initiative/scanner-benchmark
cd bench-csharp-baseline-clean && dotnet build -c Release && dotnet test -c Release
# run any scanner over the clone, producing SARIF, then:
cd ../scanner-benchmark
python3 -m cai_bench validate --key ../bench-csharp-baseline-clean/benchmark/answer-key.json --taxonomy taxonomy.json
python3 -m cai_bench score --help
```
