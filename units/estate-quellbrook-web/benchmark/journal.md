# Authoring journal — estate-quellbrook-web

## 2026-10-07 — answer key v1.0.0 draft written (key first)

- Part of Phase 4, the reference estate of the fictional carrier Quellbrook Freight (five repositories: gateway, web,
  orders, dispatch, notifier). The estate is designed as a whole first — services, HTTP and message contracts, teams,
  the three-sprint calendar and the story each repository's history tells — and this key is written from that design,
  before any code.
- The key is at integration level: the defects planted at their final sites, the traps a realistic service of this
  kind naturally contains, principled not-applicable entries and score bands set from intent. `clean` entries for
  every tracked file will be generated from `git ls-files` once the tree exists (files without a label: `"*"`).
- Planned line numbers are approximate and will be set to the final code (journalled) before the first scan.
- History plan (as in bench-csharp-maturity-history): the scripted sprint history is appended on top of this
  key-first commit, so the key precedes every line of code in the commit graph; the scripted commits carry fictional
  authors and dates in 2026-07..2026-09, earlier than this commit's real date. Nothing is force-pushed.
- Validated with `python3 -m cai_bench validate`: OK.

## 2026-10-07 — implementation and scripted history (local, not pushed)

- Written forward, sprint by sprint; each release tag's tree was formatted, linted, type-checked, built and tested
  (with coverage thresholds) before its commits were made. Tags `v0.1.0` (14 tests), `v0.2.0` (21), `v0.3.0` (24).
- The plants as built: WEB-001 (`<tr onClick>` in the orders table, sprint 1 — its test clicks the row with a pointer),
  WEB-003 (the vehicle icon `<img>` without `alt` on each route card, sprint 2), WEB-002 (the placeholder-only search
  box, sprint 3). The repository has no accessibility lint rules and no automated accessibility assertions (the
  `accessibility-checks-in-ci` band expects a low score).
- `useLoad` was first written with an ESLint suppression of `react-hooks/exhaustive-deps`; rewritten with
  `useEffectEvent` before committing, so the repository carries no suppression.
- Key: lines set to the final code; new trap TRP-005 (no HEALTHCHECK on a Kubernetes-only image, a known shape in
  the other estate repositories); `clean` entries from `git ls-files`. Validated: OK.

## 2026-10-07 — scan iteration 1 (contained, local, before any push) and history packaging

- Contained pass at `6faee89` (tree cleaned of `node_modules/`, `dist/`, `coverage/`): 18 results. **All three
  plants found on their lines:** WEB-001 (AC4 "Click handler on a non-interactive <tr>", OrdersTable.tsx:23), WEB-002
  (AC2 "<input> without a programmatic label: only a placeholder", OrdersPage.tsx:18), WEB-003 (AC1 "<img> without a
  text alternative", RouteCard.tsx:23).
- **Valid → repository fixed (scripted history, before the first push):** D29 `header-redefinition` × 8 in
  `nginx/default.conf` — the locations re-declared `add_header`, which in nginx drops every server-level header: the
  page lost Permissions-Policy and the assets frame and referrer policies. A sprint-3 fix commit (2026-09-03) sets the
  headers once at server level and caches with `expires`.
- **Noise, code kept:** AC4 on the cancel dialog's backdrop click (**false-positive**: a native modal `<dialog>`;
  Escape and the Close button close it — TRP-004 caught, as in bench-ts-frontend-a11y); DS-0026 (shape-irrelevant —
  TRP-005 caught); KSV-0125 (opinion); D41 (opinion); P2 "No structured logging" in browser code (shape-irrelevant: a
  static single-page application has no server log); R2 "Complex function OrderDetailPage (cyclomatic 11)"
  (opinion-not-fact: R2's bar of 10 is below the scanner's own D1 threshold of 15; a flat sequence of conditional
  renders → trap TRP-006). AC7 "no accessibility enforcement" is true and expected (band
  `accessibility-checks-in-ci` [0, 40]).
- Release tags checked out and run through `npm ci`, format, lint, type-check and tests with coverage: `v0.1.0` 14,
  `v0.2.0` 21, `v0.3.0` 24 tests, all green.
- `benchmark/history/`: patches and `build-history.sh` (verified into a fresh directory: HEAD and three tags match).
- Key changes: trap TRP-006.

## 2026-10-07 — scan iteration 2 (contained and model-judged, pushed `c633a99`) and freeze

- Contained pass: 10 results. The eight D29 header rows are gone (fixed in sprint 3). **All three plants found on
  their lines** (WEB-001 AC4, WEB-002 AC2, WEB-003 AC1). Traps: TRP-004, TRP-005, TRP-006 fire as in iteration 1;
  TRP-001..003 respected. KSV-0125 on the organisation's own registry and D41 recorded as opinion; P2 as
  shape-irrelevant. Every score band in.
- Model-judged host pass: 11 results, the same three plants. New rows: D20 on ADR 0001 ("a process, not a decision")
  — **opinion-not-fact**, promoted to trap **TRP-007** as in the gateway, dispatch and notifier; D19 "no contributor
  guidance" on README.md — **false-positive** (the Contributing section is there; the model says it could not see
  it); M4 "README claims signed build provenance with no workflow for it" — **false-positive** (`deploy.yml` verifies
  the attestation `release.yml` creates with `actions/attest-build-provenance`). Every score band in.
- No further valid finding; no code change. Key changes: trap TRP-007.
- Frozen as `v1.0.0`: code and answer key together.
