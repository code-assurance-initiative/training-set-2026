# Benchmark: estate-quellbrook-notifier

**Theme.** Quellbrook Freight reference estate, notifier: a C# worker that consumes order and dispatch events and sends e-mail and SMS through abstracted providers; its scripted history carries the estate's SECURITY INCIDENT (a provider API key committed in sprint 2, revoked and removed in sprint 3, still readable in history).

This repository is part of the scanner benchmark of the Code Assurance Initiative (`code-assurance-initiative/scanner-benchmark`). Its answer key, `benchmark/answer-key.json`, labels the defects that are really there (`must-fire`), sites that look like defects and are not (`must-not-fire`), code certified clean, concepts that do not apply, and expected score ranges. The key is at integration level: it labels the repository as a whole service, at the final commit, plus the one finding that lives only in history. `benchmark/journal.md` records how the repository was made and every scan judged on the way.

## The estate

Quellbrook Freight is a fictional parcel and freight carrier (the name was checked against companies, products and
trademarks; see `scanner-benchmark/docs/ESTATE-NAME.md`). Its operations platform is five repositories, owned by four
teams, that together form one system:

| Repository | What it is | Team | Trajectory over the three sprints |
|---|---|---|---|
| `estate-quellbrook-gateway` | TypeScript API gateway / BFF (Node 22, Fastify): operator authentication, routing, shipment view | Edge | steady |
| `estate-quellbrook-web` | TypeScript React + Vite operator console served by nginx | Edge | steady |
| `estate-quellbrook-orders` | C# order service: Order aggregate, domain events, transactional outbox | Orders | improving |
| `estate-quellbrook-dispatch` | C# dispatch service: consignments, routes, drivers, vehicles | Fleet | falling in sprint 2, partly repaired in sprint 3 |
| `estate-quellbrook-notifier` | C# worker sending e-mail and SMS through abstracted providers | Customer Comms | security incident introduced (sprint 2) and fixed (sprint 3) |

Operators use the web console, which calls the gateway; the gateway calls the order and dispatch services over HTTP
with its own service identity. The order service publishes `orders.*` events to a RabbitMQ topic exchange; dispatch
consumes them and publishes `dispatch.*` events; the notifier consumes both and notifies consignees. Every repository
owns its contracts: producers publish JSON Schemas and an AsyncAPI document under `contracts/`, consumers keep a
pinned copy of the versions they consume under `contracts/consumed/`. Each repository has its own Dockerfile,
Kubernetes manifests (`deploy/k8s/`), GitHub Actions workflows pinned by commit SHA, `docs/architecture.md` with C4
diagrams, ADRs and a `catalog-info.yaml`.

The sprints are two weeks long and identical across the estate: **sprint 1** 2026-07-27 – 2026-08-07 (release
`v0.1.0`), **sprint 2** 2026-08-10 – 2026-08-21 (`v0.2.0`), **sprint 3** 2026-08-24 – 2026-09-04 (`v0.3.0`). The
history is scripted and reproducible (`benchmark/history/`); its authors are fictional (`@example.invalid`).

## The sprint story (for a film director) — THE SECURITY FIX

The Customer Comms team (Lucia Brennan, Sami Oyelaran) holds the estate's **security incident**.

- **Sprint 1 (v0.1.0).** A worker that consumes `orders.order-placed.v1` through an inbox, keeps the consignee's
  contact details for the order, and sends an order confirmation by e-mail through a provider abstraction.
- **Sprint 2 (v0.2.0) — the finding is introduced.** The worker starts consuming dispatch events ("out for delivery",
  "delivered") and sends SMS as well. To get the staging environment sending, the e-mail provider's API key is
  committed in `src/Quellbrook.Notifier/appsettings.json`.
- **Sprint 3 (v0.3.0) — the security fix.** The provider's leaked-key alert fires; the key is revoked and replaced,
  removed from the file and supplied from the secret store through an ExternalSecret; a secret scan over the full
  history is added to CI; ADR 0003 (secrets only from the secret store) and an incident note are written. The
  retention sweeper for contact data ships in the same sprint.

The key stays readable in the history (NTF-001, with its commit). The commits are listed in
`benchmark/history/README.md`.

## Planted defects (`must-fire`)

| Id | Concept | Site | Why |
|---|---|---|---|
| NTF-001 | `secret-in-version-history` | `src/Quellbrook.Notifier/appsettings.json:15 @ `a033c7cf647a`` | In sprint 2 the email provider's API key (the provider's real key format) was committed in the worker's appsettings.json to get the staging environment sending. It was found by the provider's leaked-key alert in sprint 3, revoked and replaced, and removed from the file the same day (docs/incidents/2026-08-25-email-provider-key.md). The working tree is clean; the key is still readable in the commit that added it. A scanner cannot know that it was revoked: reporting a credential in history is correct, and the team must confirm the revocation. Lines refer to the file as it was in that commit. |
| NTF-002 | `sensitive-data-in-logs` | `src/Quellbrook.Notifier/Channels/EmailSender.cs:34-38` | After the provider accepts a message the e-mail sender logs the recipient's full e-mail address at Information level (added in sprint 2 to chase staging bounces and never removed). Information logs are shipped to the shared log platform and kept there for 30 days, longer than the notifier keeps the address itself; the address adds nothing a support engineer needs beyond the order id and message id that are logged with it. The SMS sender (TRP-002) shows the intended pattern. |

## Traps (`must-not-fire`)

| Id | Concept | Site | Why |
|---|---|---|---|
| TRP-001 | `hardcoded-credential` | `src/Quellbrook.Notifier/appsettings.json:15` | The provider key's place in appsettings.json is an empty string: the value is supplied at run time from a Kubernetes Secret (EmailProvider__ApiKey), and the options validation refuses to start without it. |
| TRP-002 | `sensitive-data-in-logs` | `src/Quellbrook.Notifier/Channels/SmsSender.cs:26-31` | The SMS sender logs the destination number only through ContactMask.Phone (country code and last two digits); no personal data reaches the log. |
| TRP-003 | `hardcoded-credential` | `deploy/k8s/deployment.yaml:69-78` | Provider keys, database and broker credentials are read from Kubernetes Secrets (secretKeyRef) that an ExternalSecret materialises from the secret store. |
| TRP-004 | `non-idempotent-message-handler` | `src/Quellbrook.Notifier/Messaging/InboxProcessor.cs:25-44` | Every message is processed inside one transaction that first records its message id in the inbox and skips a message id already seen; sending is recorded in the notification log before the provider call is repeated, so a redelivered message sends nothing twice. |
| TRP-005 | `suppressed-diagnostic` | `.editorconfig:36-37` | CA2007 is switched off at the root for tests with its reason on the line above, and src/.editorconfig switches it back on for production code. |
| TRP-007 | `data-retention-policy` | `(repository)` | Retention is declared and enforced: RetentionOptions gives each kind of stored data its maximum age (30 days after delivery for contact details, 60 days without delivery, 90 days for the notification log, 30 days for message ids) and RetentionSweeper deletes expired rows every hour (docs/privacy.md). A report of a missing expiry limit or scheduled purge is wrong. Repository-level: such a report has no site. |
| TRP-008 | `adr-quality` | `docs/adr/0001-record-architecture-decisions.md` | ADR 0001 records the decision to record decisions (Nygard's first ADR); a process decision is a legitimate and conventional first record with status, context, decision and two-sided consequences. Promoted from a model-judged result in scan iteration 2 (the same site is a trap in the dispatch repository). |
| TRP-006 | `hardcoded-credential` | `tests/Quellbrook.Notifier.UnitTests/Channels/EmailSenderTests.cs:11` | The unit tests configure the provider clients with a fixed, obviously fake key against an in-process stub handler; it reaches no network and authenticates nothing. |

## Certified clean

109 `clean` entries, one per tracked file: files without a label are certified clean for every concept (`"*"`); a file that carries a plant or a trap is certified clean for every finding concept except the labelled ones and the concepts a result of those labels would restate.

## Not applicable

- `missing-text-alternative` — The notifier is a background worker; it has no user interface: there are no images.
- `form-control-without-label` — The notifier is a background worker; it has no user interface: there are no form controls.
- `page-structure-violation` — The notifier is a background worker; it has no user interface: there are no pages.
- `non-keyboard-accessible-interaction` — The notifier is a background worker; it has no user interface: there are no interactive markup.
- `invalid-aria-usage` — The notifier is a background worker; it has no user interface: there are no ARIA attributes.
- `visual-and-motion-safety` — The notifier is a background worker; it has no user interface: there are no styles or animation.
- `accessibility-checks-in-ci` — The notifier is a background worker; it has no user interface: there are no user interface to check.
- `alt-text-quality` — The notifier is a background worker; it has no user interface: there are no images.
- `link-and-button-text-quality` — The notifier is a background worker; it has no user interface: there are no links or buttons.
- `heading-and-label-text-quality` — The notifier is a background worker; it has no user interface: there are no headings or labels.
- `cross-site-scripting` — E-mails are plain text; no HTML is rendered from message data.
- `missing-authorization` — The worker exposes no endpoint; it consumes messages from the broker.
- `security-response-headers` — The worker serves no HTTP.
- `https-enforcement` — The worker serves no HTTP; its outbound calls to the providers are HTTPS.
- `nondeterministic-event-fold` — No event-sourced aggregate.

## Score bands

Bands were set from the intent of the code, before any scan, and are wide where a model judges.

| Id | Concept | Band | Why |
|---|---|---|---|
| BND-001 | `high-cyclomatic-complexity` | 80–100 | Small handlers and aggregates; no method above the usual threshold. |
| BND-002 | `high-cognitive-complexity` | 80–100 | Small handlers and aggregates; flat control flow. |
| BND-003 | `duplicated-code` | 80–100 | No copied blocks of note. |
| BND-004 | `test-coverage` | 70–100 | Unit tests for the domain and application layers, integration tests over the HTTP API and persistence. |
| BND-005 | `test-pyramid-distribution` | 50–100 | More unit than integration tests. |
| BND-006 | `dependencies-not-locked` | 80–100 | Central package management with committed lock files, restored in locked mode in CI. |
| BND-007 | `security-tooling-in-ci` | 70–100 | CodeQL, a vulnerable-package gate and (since the sprint 3 incident) a secret scan over the full history in CI. |
| BND-008 | `vulnerability-disclosure-policy` | 80–100 | SECURITY.md with a private reporting channel and response times. |
| BND-009 | `readme-quality` | 70–100 | README with purpose, build/run, configuration, testing, architecture and ownership sections. |
| BND-010 | `architecture-documentation` | 70–100 | docs/architecture.md with C4 diagrams in Mermaid, plus ADRs. |
| BND-011 | `folder-structure` | 80–100 | src/ and tests/ separation, one root namespace prefix. |
| BND-012 | `ci-build-and-test-pipeline` | 80–100 | CI builds and tests every push and pull request. |
| BND-013 | `observability` | 60–100 | Structured logging through ILogger, OpenTelemetry traces and metrics, health checks. |
| BND-014 | `deployment-rollback-safety` | 50–100 | Rolling updates by image digest with a rollback step in the deploy workflow. |
| BND-015 | `release-hygiene` | 40–100 | CHANGELOG with every release, <Version> 0.3.0 and tags v0.1.0..v0.3.0. |
| BND-016 | `build-provenance-and-signing` | 40–100 | Release images built by CI with provenance and SBOM attestations. |
| BND-017 | `network-egress-policy` | 60–100 | Default-deny NetworkPolicies with one allowance per real flow. |
| BND-018 | `workload-syscall-confinement` | 10–80 | seccomp RuntimeDefault, all capabilities dropped; no AppArmor or custom profiles. |
| BND-019 | `runtime-threat-detection-and-admission` | 0–40 | Pod Security Admission 'restricted' on the namespace; no runtime threat detection or signed-image admission. |
| BND-020 | `churn-complexity-hotspot` | 70–100 | Six weeks of history; churn lands on small files. |
| BND-021 | `knowledge-concentration` | 30–100 | A team of two or three authors over six weeks; some files are naturally single-author. |
| BND-022 | `knowledge-freshness` | 70–100 | The whole history is recent and the authors are active. |
| BND-023 | `change-coupling` | 50–100 | Co-changes are explicit dependencies or tests changing with their subject. |
| BND-024 | `documentation-quality` | 60–100 | Model-judged. README, architecture overview and ADRs are clear and current. Wide band. |
| BND-025 | `adr-quality` | 60–100 | Model-judged. ADRs carry status, context, decision and two-sided consequences. Wide band. |
| BND-026 | `adr-conformance` | 60–100 | Model-judged. The code follows its ADRs. Wide band. |
| BND-027 | `documentation-accuracy` | 50–100 | Model-judged. README and architecture describe the code as it is. Wide band. |
| BND-028 | `inconsistent-naming` | 60–100 | Model-judged. Consistent domain vocabulary. Wide band. |
| BND-029 | `low-value-comments` | 60–100 | Model-judged. Comments explain why, not what. Wide band. |
| BND-030 | `internal-api-inconsistency` | 60–100 | Model-judged. Endpoints and handlers follow one shape. Wide band. |
| BND-031 | `outbound-http-resilience` | 70–100 | Both provider clients use the standard resilience handler (timeouts, retries, circuit breaker). |
| BND-032 | `versioned-schema-migrations` | 80–100 | EF Core migrations, one per schema change. |
| BND-033 | `data-retention-policy` | 50–100 | Recipient contact data is deleted 30 days after delivery or cancellation, notification log rows after 90 days, by a scheduled sweeper; docs/privacy.md. |
| BND-034 | `audit-trail` | 0–80 | A notification log records what was sent, when and through which channel, with masked recipients; no audit of data changes. |
| BND-035 | `data-subject-rights` | 0–70 | Erasure happens by retention; no on-request erasure operation in the worker. |
| BND-036 | `data-encryption-controls` | 0–70 | TLS to the providers, database and broker; encryption at rest by the platform. |
