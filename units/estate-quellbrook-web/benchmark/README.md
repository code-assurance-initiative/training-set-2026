# Benchmark: estate-quellbrook-web

**Theme.** Quellbrook Freight reference estate, operator web front end: a React + Vite single-page application (orders, order detail with shipment status, dispatch board) served by nginx behind the gateway; owned by the Edge team. Its plants are accessibility defects a team without automated accessibility checks ships.

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

## The sprint story (for a film director)

The Edge team (Mara Ellingsworth, Teodor Vasko, Ines Halvard) owns the web console and the gateway; it is the
estate's **steady** team.

- **Sprint 1 (v0.1.0).** Vite + React skeleton, layout with a skip link and navigation, the orders list and the order
  detail page against the gateway, nginx image with security headers, manifests and CI.
- **Sprint 2 (v0.2.0).** The dispatch board (routes of a day with their vehicle and stops, assigning a consignment),
  cancelling an order from its detail page, the shipment status on the order page.
- **Sprint 3 (v0.3.0).** Order search and status filter, board density preference, more component tests.

No accessibility checks run in CI, and three accessibility defects ship along the way (WEB-001..003). The commits are
listed in `benchmark/history/README.md`.

## Planted defects (`must-fire`)

| Id | Concept | Site | Why |
|---|---|---|---|
| WEB-001 | `non-keyboard-accessible-interaction` | `src/features/orders/OrdersTable.tsx:23-28` | Each row of the orders table opens the order on click (onClick on the <tr>), but the row is not focusable and has no key handler and no link inside it: keyboard and switch users cannot open an order from the list at all (WCAG 2.1.1). |
| WEB-002 | `form-control-without-label` | `src/features/orders/OrdersPage.tsx:18-25` | The orders search box (sprint 3) has a placeholder but no <label>, aria-label or aria-labelledby; the placeholder disappears on input and is not a reliable accessible name (WCAG 1.3.1, 4.1.2). The status select beside it is labelled. |
| WEB-003 | `missing-text-alternative` | `src/features/dispatch/RouteCard.tsx:23` | The dispatch board shows each route's vehicle as an icon image (van or rigid truck) with no alt attribute; the vehicle kind is information the operator needs (heavy express parcels need a rigid vehicle) and is not given anywhere else on the card (WCAG 1.1.1). |

## Traps (`must-not-fire`)

| Id | Concept | Site | Why |
|---|---|---|---|
| TRP-001 | `missing-text-alternative` | `src/components/Layout.tsx:31` | The header's divider image is decorative: alt="" and aria-hidden mark it so on purpose, and the brand name is in the text beside it. |
| TRP-002 | `form-control-without-label` | `src/features/dispatch/DispatchBoard.tsx:76-83` | The dispatch board's stop filter has no <label> element but an aria-label that names it ('Show only stops with this postal code'), under a visible heading that says what it filters. It has an accessible name. |
| TRP-003 | `sensitive-data-in-browser-storage` | `src/preferences/boardDensity.ts:6-20` | The board's compact/comfortable density preference is kept in localStorage. It is a display preference, not personal data or a token. |
| TRP-005 | `missing-image-healthcheck` | `Dockerfile` | The image runs only on Kubernetes, which ignores a Dockerfile HEALTHCHECK; the probes (nginx /healthz) are in deploy/k8s/deployment.yaml. |
| TRP-006 | `high-cyclomatic-complexity` | `src/features/orders/OrderDetailPage.tsx:12` | OrderDetailPage is a React component with cyclomatic complexity 11 (the loading, failure and not-found branches, optional address and delivery parts, the cancellability rule). That is below the threshold of 15 used for the services and reads as a flat sequence of conditional renders; not high complexity. |
| TRP-007 | `adr-quality` | `docs/adr/0001-record-architecture-decisions.md` | ADR 0001 records the decision to record decisions (Nygard's first ADR); a process decision is a legitimate and conventional first record with status, context, decision and two-sided consequences. Promoted from a model-judged result (the same site is a trap in the gateway, dispatch and notifier repositories). |
| TRP-004 | `non-keyboard-accessible-interaction` | `src/features/orders/CancelOrderDialog.tsx:31-42` | The cancel-order dialog is a native <dialog> opened with showModal(); its click handler closes it when the backdrop is clicked. Keyboard users close it with Escape (native) or its Close button; the click handler is a pointer convenience, not the only way. |

## Certified clean

86 `clean` entries, one per tracked file: files without a label are certified clean for every concept (`"*"`); a file that carries a plant or a trap is certified clean for every finding concept except the labelled ones and the concepts a result of those labels would restate.

## Not applicable

- `sql-injection` — A browser application: no database access.
- `versioned-schema-migrations` — A browser application: no database.
- `dual-write-without-outbox` — A browser application: it publishes no messages.
- `missing-cancellation-propagation` — Every fetch takes an AbortSignal from the component's effect.

## Score bands

Bands were set from the intent of the code, before any scan, and are wide where a model judges.

| Id | Concept | Band | Why |
|---|---|---|---|
| BND-001 | `high-cyclomatic-complexity` | 80–100 | Small handlers and aggregates; no method above the usual threshold. |
| BND-002 | `high-cognitive-complexity` | 80–100 | Small handlers and aggregates; flat control flow. |
| BND-003 | `duplicated-code` | 80–100 | No copied blocks of note. |
| BND-004 | `test-coverage` | 60–100 | Component tests for every page and the API client. |
| BND-005 | `test-pyramid-distribution` | 30–100 | Component tests dominate; unit tests for helpers. |
| BND-006 | `dependencies-not-locked` | 80–100 | package-lock.json committed; CI installs with npm ci. |
| BND-007 | `security-tooling-in-ci` | 60–100 | CodeQL on every pull request, npm audit and signature verification in CI. |
| BND-008 | `vulnerability-disclosure-policy` | 80–100 | SECURITY.md with a private reporting channel and response times. |
| BND-009 | `readme-quality` | 70–100 | README with purpose, build/run, configuration, testing, architecture and ownership sections. |
| BND-010 | `architecture-documentation` | 70–100 | docs/architecture.md with C4 diagrams in Mermaid, plus ADRs. |
| BND-011 | `folder-structure` | 70–100 | Features under src/features, shared components, api client, tests/. |
| BND-012 | `ci-build-and-test-pipeline` | 80–100 | CI builds and tests every push and pull request. |
| BND-013 | `observability` | 0–100 | A static single-page application: browser code with no server-side logging to assess. |
| BND-014 | `deployment-rollback-safety` | 50–100 | Rolling updates by image digest with a rollback step in the deploy workflow. |
| BND-015 | `release-hygiene` | 40–100 | CHANGELOG with every release, package.json version 0.3.0 and tags v0.1.0..v0.3.0. |
| BND-016 | `build-provenance-and-signing` | 40–100 | Release images built by CI with provenance and SBOM attestations. |
| BND-017 | `network-egress-policy` | 50–100 | Default-deny NetworkPolicy; nginx needs no egress. |
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
| BND-031 | `accessibility-checks-in-ci` | 0–40 | No accessibility lint rules and no automated accessibility assertions in tests or CI; the plants above are what that lets through. |
| BND-032 | `untyped-javascript-share` | 90–100 | TypeScript throughout, strict mode. |
| BND-033 | `frontend-tooling-scripts` | 70–100 | dev, build, test, lint, typecheck and format scripts. |
| BND-034 | `security-response-headers` | 60–100 | nginx sends CSP, frame, referrer and content-type headers. |
