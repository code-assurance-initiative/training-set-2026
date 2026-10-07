# Benchmark: production readiness of a TypeScript monorepo, with realistic gaps

This repository is one unit of the scanner benchmark in
[`code-assurance-initiative/scanner-benchmark`](https://github.com/code-assurance-initiative/scanner-benchmark).
Its labels are in [`answer-key.json`](answer-key.json) (format: `scanner-benchmark/docs/CONTRACT.md`, schema 1.2);
its authoring log is [`journal.md`](journal.md).

## Theme

**parcel-tracking**, a Node.js 22 / strict TypeScript npm-workspaces monorepo that is *almost* ready for production:

| Workspace | Role |
|---|---|
| `apps/tracking-service` | one image, two processes: an Express 5 JSON API (JWT bearer tokens with named scopes, zod validation, helmet, pino) and a worker that polls carriers for active parcels and delivers signed webhooks to merchants; PostgreSQL through knex with versioned migrations; cucumber specifications |
| `packages/tracking-client` | the published merchant client for the API |
| `packages/webhooks` | the published package merchants use to verify webhook signatures and parse event payloads |

Deployment: one digest-pinned image, two Deployments and a migration Job on Kubernetes (`deploy/k8s`), with
NetworkPolicies, Pod Security Admission labels and a namespaced admission policy; tag-triggered release workflows
publish the image (by digest, with a provenance attestation) and the packages (with npm provenance).

The repository exists to close the TypeScript coverage gaps of the benchmark: **test reliability** (flaky tests),
**Kubernetes egress, syscall confinement and admission**, **schema migrations**, **package publishing and versioning**,
**executable specifications** and **subsumed condition operands** — plus the TypeScript forms of the readiness rows the
C# readiness repository measures (outbound resilience, cancellation, authorization, validation, logging, health).

## Labels are about truth

A `must-fire` is something a careful senior reviewer would flag before this goes to production; a `must-not-fire` is a
site a careless rule would flag and that reviewer would not. Neither was chosen to match a scanner. Several plants carry
a *posture* concept (`network-egress-policy`, `versioned-schema-migrations`, `library-api-versioning`,
`executable-specifications`, …): a posture failure that has one site a reviewer would point at is labelled at that site.
A scanner that only scores the posture repository-wide cannot hit such a plant; that is a limit of the scanner, not a
reason to drop the site. Where the most precise concept true of a site is a per-artefact one (the migration Job's missing
seccomp profile is `container-confinement-profile-unset`), the precise concept is named and the posture is banded.

## Plants (`must-fire`)

`S` = `apps/tracking-service`.

| Id | Concept | Where | What and why |
|---|---|---|---|
| FLK-001 | flaky-test | `S/tests/unit/share-links.test.ts` | Expected expiry computed from a second read of the wall clock: fails when a second boundary falls between the two reads. Rare, real, proven (journal). |
| FLK-002 | flaky-test | `S/tests/integration/poller.test.ts` | Real-timer poller, fixed sleep, then "at least N polls": passes on an idle machine, fails under load. |
| MIG-001 | versioned-schema-migrations | `S/src/worker/delivery-log.ts` | The worker creates its delivery-log table at start-up, outside the versioned migrations. |
| LIB-001 | library-api-versioning | `packages/tracking-client/CHANGELOG.md` | A release that removes a public method and renames a returned field is published as a minor version. |
| LIB-002 | library-api-versioning | `packages/webhooks/package.json` | No `exports` map, no `files` allow-list: everything is published and every internal module is importable. |
| BDD-001 | executable-specifications | `S/features/returns/return-to-sender.feature` | A feature no cucumber profile loads: never executed, steps undefined. |
| SUB-001 | redundant-condition-operand | `S/src/parcels/status-mapping.ts` | `startsWith("DL") \|\| startsWith("DLV")`: the second operand is dead; delivery exceptions are mapped to delivered. |
| EGR-001 | network-egress-policy | `deploy/k8s/networkpolicy.yaml` | The migration Job may reach `0.0.0.0/0` on every port. |
| CNF-001 | container-confinement-profile-unset | `deploy/k8s/migrate-job.yaml` | The migration Job sets no seccomp profile (both Deployments do). |
| ADM-001 | runtime-threat-detection-and-admission | `deploy/k8s/namespace.yaml` | Pod Security Admission labels for `warn` and `audit` only: nothing is enforced. |
| RDY-001 | outbound-http-resilience | `S/src/worker/webhook-dispatcher.ts` | Webhook POSTs with a bare fetch: no timeout, no signal, no retry. |
| RDY-002 | missing-cancellation-propagation | `S/src/worker/poller.ts` | `pollOnce` takes the AbortSignal and does not pass it to the carrier call. |
| RDY-003 | missing-authorization | `S/src/http/routes/merchants.ts` | Merchant delivery statistics served anonymously for any merchant id (the router is mounted before authentication in `S/src/http/app.ts`). |
| RDY-004 | unchecked-any-external-data | `S/src/http/routes/parcels.ts` | The redirect body is force-asserted to its type instead of parsed (the TypeScript form of missing input validation). |
| RDY-005 | observability | `S/src/worker/webhook-dispatcher.ts` | Delivery outcomes on console.log/console.error instead of the structured logger. |
| RDY-006 | empty-catch-block | `S/src/health/carrier-health.ts` | Every error swallowed; the carrier is reported reachable. |
| RDY-007 | non-structured-log-message | `S/src/worker/poller.ts` | A warning built with a template literal. |

## Traps (`must-not-fire`)

| Id | Concept | Where | Why it is not a defect |
|---|---|---|---|
| TRP-001 | flaky-test | `S/tests/unit/share-link-verification.test.ts` | Expiry tested on fake timers with a fixed system time. |
| TRP-002 | flaky-test | `S/tests/unit/retry.test.ts` | Backoff delays run in virtual time advanced by the test. |
| TRP-003 | flaky-test | `S/tests/integration/webhook-dispatch.test.ts` | Real timers, but the test waits for the outcome with a condition polled to a timeout, not a fixed sleep. |
| TRP-004 | versioned-schema-migrations | `S/src/db/migrations/20261001_adopt_carriers.ts` | An idempotent (existence-checked) create-table inside a versioned migration. |
| TRP-005 | versioned-schema-migrations | `S/tests/support/database.ts` | Tests apply the real migrations to an in-memory database. |
| TRP-006 | versioned-schema-migrations | `deploy/k8s/migrate-job.yaml` | The Job that runs the versioned migrations before a rollout. |
| TRP-007 | library-api-versioning | `packages/tracking-client/package.json` | A proper `exports` map with types, a `files` allow-list, provenance. |
| TRP-008 | library-api-versioning | `S/package.json` | A private workspace package with a placeholder version: never published. |
| TRP-009 | executable-specifications | `S/features/webhooks/signed-delivery.feature` | Outside the default profile, but run by the `webhooks` profile in CI. |
| TRP-010 | redundant-condition-operand | `S/src/parcels/status-mapping.ts` | `startsWith("EX") && !startsWith("EXR")`: a negated narrowing, not an implied operand. |
| TRP-011 | network-egress-policy | `deploy/k8s/networkpolicy.yaml` | The worker's `0.0.0.0/0` on 443 only, private and metadata ranges excepted: the narrowest rule for arbitrary merchant HTTPS endpoints. |
| TRP-012 | network-egress-policy | `deploy/k8s/networkpolicy.yaml` | An ingress-only policy; egress is covered by the namespace default-deny. |
| TRP-021 | network-egress-policy | `deploy/k8s/networkpolicy.yaml` | The database allowance: one CIDR, one port; with DNS the API's whole egress. |
| TRP-022 | open-redirect | `S/src/http/routes/parcels.ts` | `service.redirect()` redirects a parcel to a pickup point; no HTTP redirect exists. |
| TRP-023..025 | iac-misconfiguration | `deploy/k8s/{api,worker,migrate-job}.yaml` | Secrets as `secretKeyRef` environment variables: a hardening preference, not a misconfiguration. |
| TRP-026..028 | image-not-from-allowed-registry | `deploy/k8s/{api,worker,migrate-job}.yaml` | The team's own registry, by digest, admitted only by digest. |
| TRP-029, TRP-030 | layer-dependency-violation | `S/features/support/world.ts`, `S/features/steps/webhook-steps.ts` | Specifications importing the shared test database helper: test code depending on test code. |
| TRP-031..034 | unused-code | `S/cucumber.mjs`, `S/features/**` | cucumber's configuration and its step/support files, loaded by the runner through globs, never imported. |
| TRP-035 | authorization-enforcement | repository | Scopes are named once and declared per route at registration: named policies. |
| TRP-013 | workload-syscall-confinement | repository | No explicit AppArmor/SELinux profile: the runtime default applies; not a defect. |
| TRP-014 | runtime-threat-detection-and-admission | repository | No Falco/Tetragon: a cluster-operator control, not an application repository's. |
| TRP-015 | outbound-http-resilience | `S/src/worker/carrier-client.ts` | Per-attempt timeout combined with the caller's signal, bounded retries. |
| TRP-016 | outbound-http-resilience | `packages/tracking-client/src/client.ts` | A library takes the caller's signal and fetch and leaves policy to the host. |
| TRP-017, TRP-018 | missing-authorization | `S/src/http/routes/health.ts` | Probe endpoints and `/.well-known/security.txt` are public by design. |
| TRP-019 | non-structured-log-message | `S/src/worker/worker.ts` | A constant message split over two literals, values in the object argument. |
| TRP-020 | missing-image-healthcheck | `Dockerfile` | The image runs only on Kubernetes, which ignores HEALTHCHECK; probes are in the Deployments. |

## Contested truths, and how they were decided

- **Rare flaky tests are flaky tests.** FLK-001 fails on perhaps one run in tens of thousands and FLK-002 essentially
  only on a loaded machine. A scanner that re-runs the suite a few times will usually not see either; that does not
  make them deterministic. Each is proven in the journal (a deterministic reproduction and a measured rate).
- **The worker's HTTPS egress (TRP-011) vs the Job's (EGR-001).** Both say `0.0.0.0/0`. The worker must reach
  merchant-chosen public HTTPS endpoints, and its rule is port 443 with every private and link-local range excepted;
  the Job needs one database port and is given the whole address space on every port.
- **No AppArmor profile (TRP-013), no Falco (TRP-014).** Recommendations that are true of many clusters are not
  defects of this repository: the runtime's default AppArmor profile applies without a manifest opting in, and runtime
  threat detection is installed by whoever operates the cluster. The repository's own admission intent (the namespace's
  Pod Security labels) is a different matter, and its gap is planted (ADM-001).
- **RDY-004 names `unchecked-any-external-data`.** The C# readiness repository labels its missing request validation
  `inbound-input-validation`; here the site is a force-assertion of the untyped request body, which the more precise
  concept describes. The file is left uncertified for `inbound-input-validation` as well.
- **LIB-001 is located at the changelog.** The defect is a version number that does not say what the release did; the
  release entry that records the breaking change under a minor version is where a reviewer points.

## Subjects

The flaky-test entries carry their test file as `subject`, and the two library entries their npm package name: a
check that re-runs the suite reports a flaky test by name without a location, and a package-level check names the
package. Traps of a concept never share a subject with a plant of it.

## Certified clean

Every tracked file is labelled `clean` for every concept (files that contain a plant are clean for every concept
except the planted ones and any concept also true of the planted site; `S/src/http/app.ts`, `S/src/worker/worker.ts`,
`packages/tracking-client/package.json` and `docs/operations/deployment.md` leave out the concepts of the plant whose
other end they hold — the router mount, the call of the start-up table creation, the version number, and the documented
Job egress). A repository-level `clean` entry certifies
dependencies (current, supported, locked, non-vulnerable, used), CI action and image pinning, strict TypeScript, no
module cycles and no floating promises.

## Not applicable

Accessibility and frontend concepts (no markup), domain-driven design and event-sourcing concepts (a transaction-script
service with no aggregates), personal-data concepts (merchant ids, tracking numbers, carrier codes and destination
countries only) and .NET-only mechanisms.

## Score bands

Posture concepts are banded from intent before any scan (`BND-*`): egress, syscall confinement and admission (each
dented by one plant), migrations and library versioning (middling: half done), executable specifications (good, one
dead feature), CI, CI gate honesty, headers, lock files, security tooling and disclosure (high), observability,
authorization and validation (each dented by one plant), outbound resilience (middling), release hygiene, deployment,
provenance and coverage.

## Not covered here

Code-health metrics, architecture, domain modelling, injection, secrets and dependency vulnerabilities are measured by
their own benchmark repositories; this one keeps them clean.
