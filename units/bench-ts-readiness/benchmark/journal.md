# Authoring journal — bench-ts-readiness

## 2026-10-07 — answer key v1.0.0 draft written (key first)

- Purpose: the TypeScript coverage gaps of `scanner-benchmark/coverage/MATRIX.md` ("Coverage gaps (frozen keys)"):
  D11, D40, D41, D42, P8, P10, P11, X9 had no frozen TypeScript measuring label. Each row's concept is labelled here
  with plants, traps and/or a score band: `flaky-test` (FLK-001/002, TRP-001..003), `network-egress-policy` (EGR-001,
  TRP-011/012, BND-001), `workload-syscall-confinement` (TRP-013, BND-002; the per-workload plant is the precise
  `container-confinement-profile-unset`, CNF-001), `runtime-threat-detection-and-admission` (ADM-001, TRP-014,
  BND-003), `versioned-schema-migrations` (MIG-001, TRP-004..006, BND-004), `library-api-versioning` (LIB-001/002,
  TRP-007/008, BND-005), `executable-specifications` (BDD-001, TRP-009, BND-006), `redundant-condition-operand`
  (SUB-001, TRP-010).
- Before writing labels, each row's reference detector was read at the pinned instrument to make sure the repository
  is in scope for it (knowing the scanner decides where to probe, never a label): the Kubernetes rows need workloads
  (and, for admission, a committed policy resource — the namespaced image-digest policy is a real control the team
  ships); the migration row reads knex migrations; the library row needs a published (non-private) npm package with an
  entry point; the BDD row needs `.feature` files; the subsumed-operand row reads double-quoted `startsWith`/`endsWith`
  chains in TypeScript (the code uses Prettier's default double quotes); the reliability row re-runs vitest suites.
- TypeScript forms of the C# readiness rows added where they fit the service naturally: RDY-001..007 and traps
  TRP-015..020 (outbound resilience, cancellation, authorization, validation — named by the precise concept
  `unchecked-any-external-data`, see README — logging, empty catch, structured log messages, image health check).
- Flaky plants carry the test file as `subject` (contract 1.2): a re-running test-reliability check reports a flaky
  test by name without a location. Traps of `flaky-test` live in other files, so no plant and trap share a subject.
- `clean` over every planned file (generated); plant files are clean for every concept except the planted ones and any
  concept also true of the planted site. Harness note: the key validator still compares paths by suffix (contract 1.4
  scoring compares them exactly), so the root `CHANGELOG.md` and `package.json` leave `library-api-versioning` out of
  their certificates because same-named package files hold plants of it.
- Lines are planned values, to be fixed to the final code. Validated with `python3 -m cai_bench validate`: OK,
  218 entries.

## 2026-10-07 — implementation complete; key fixed to the code

- Implemented as planned: npm workspaces on Node.js 22, TypeScript 6.0 (`strict`, `noUncheckedIndexedAccess`,
  `exactOptionalPropertyTypes`), ESLint 10 with typescript-eslint `strictTypeChecked`, Prettier (default double
  quotes), vitest 5. Service: Express 5, zod 4, helmet, pino 10, jose 6, knex 3 over `pg`; tests and specifications on
  pg-mem 3 (no native modules, no network beyond the loopback interface); cucumber 13 with TypeScript steps via tsx.
  Production TypeScript 1,691 non-blank lines in 43 files; 93 tests in 19 files (1,419 non-blank lines of tests and
  step definitions) plus 11 scenarios in two cucumber profiles,
  all green; coverage 96 % lines, gated at 85/80/85/85. `npm audit`: 0 vulnerabilities. The Docker image builds and
  its entry points refuse to start without configuration.
- Key changes (planned names are not what exists): the signed links are recipient share links (`links/share-links.ts`;
  FLK-001 in `tests/unit/share-links.test.ts`, TRP-001 in `tests/unit/share-link-verification.test.ts`); the worker's
  wiring moved from `worker/main.ts` to a tested `worker/worker.ts` (TRP-019 moved with it); entry points are
  one-line calls into `api/server.ts`, `worker/worker.ts` and `db/migrator.ts`; added `routes/public-tracking.ts`,
  `worker/webhook-outbox.ts`, `worker/health-server.ts`, `packages/webhooks/src/internal/signature-header.ts`,
  `.github/workflows/deploy.yml`, test support and tests. The `clean` list is regenerated from `git ls-files`.
- Key change: new trap TRP-021 (the database egress allowance — "DNS plus one service", the user-visible example of a
  correct egress policy). Written down while implementing it.
- Key change: LIB-002 is whole-file (the defect is what the manifest does not declare), and LIB-001/LIB-002 carry the
  package name as `subject`; TRP-001..003 are whole test files. TRP-007 is narrowed to the surface lines of the
  client manifest, so a result on its version line (LIB-001's other end) is not caught by the trap; that manifest, the
  app's router mount (RDY-003), the worker's call of the start-up table creation (MIG-001) and the deployment runbook's
  description of the Job's egress (EGR-001) are left uncertified for the concept of the plant whose other end they hold.
- The carrier status checks were reordered (delivered, out for delivery, returned, exception) so SUB-001 and TRP-010
  are more than the line tolerance apart; behaviour unchanged (the mapping tests pass unmodified).
- **Proof that the flaky plants are flaky** (scripts kept outside the repository; numbers as measured):
  - FLK-001, deterministic: with the two clock reads at 1 759 999 999.999 s and 1 760 000 000.000 s the link expires
    at 1 760 000 899 and the test expects 1 760 000 900 — it fails. Empirically: the test body run 2,000,000 times in
    a tight loop failed 9 times, once per second boundary crossed in the 8.7 s run; in a normal run the window is the
    few microseconds between the reads, so a single run fails on the order of once in 10^4–10^5.
  - FLK-002: the full vitest suite run 30 times on this (shared, load average 50–65 on 32 cores) machine failed 2
    times, each time on this test ("expected 4 to be greater than or equal to 5"); with 48 extra busy processes it
    failed 10 times in 15. Run alone on a quiet machine it passes every time (0 failures in 200 iterations of the
    test body). The fixed sleep was first 150 ms, which failed 22 of 30 full-suite runs on this machine: not a
    believable state for a repository whose CI is green, so it was raised to 300 ms before commit — rare, still real.
  - BDD-001: `cucumber-js --dry-run features/returns/return-to-sender.feature` reports 2 undefined scenarios and 5
    undefined steps; neither profile's `paths` includes `features/returns`.
- Validated: OK, 233 entries.

## 2026-10-07 — scan iteration 1 (contained)

- Scanner: the reference scanner at the pinned instrument (kennel main 6a05dfb6c, rubric-2026.10.1), contained mode;
  repository at 148ebe7. 26 results. Harness: recall 3/17 — CNF-001 (D31 KSV-0104 at `migrate-job.yaml:27`, within
  tolerance of the pod security context), RDY-007 (X4), SUB-001 (X9) — trap resistance 18/21 (TRP-013 D41 "No
  AppArmor/SELinux confinement", TRP-014 D42 "No runtime threat detection", TRP-020 DS-0026 HEALTHCHECK caught).
  The test-reliability check ran the vitest suite three times (93 tests, 0 flaky): both flaky plants passed all three
  runs, as a rare flake does.
- **Missed plants, all re-verified as real and correctly placed:**
  - FLK-001, FLK-002: three re-runs cannot be expected to catch a flake of 10^-4 (FLK-001) or one that needs a loaded
    machine (FLK-002); no static rule for TypeScript tests (the fixed-sleep rule reads C# only).
  - MIG-001: the schema card scores 100 once any versioned migration history exists, and has no knex auto-create rule.
  - LIB-001, LIB-002: the library card on npm reads only "is a version declared"; it does not compare changelog
    content with the version bump, and does not read `exports`/`files`.
  - BDD-001: the BDD card counts `.feature` files (100 with any); it does not ask whether a runner loads them.
  - EGR-001: the egress card is a repository-wide switch (any NetworkPolicy plus the word "egress" scores 100).
  - ADM-001: the admission card credits any admission token (the namespaced policy) and does not read Pod Security
    `warn`/`audit` vs `enforce`.
  - RDY-001, RDY-002: outbound resilience and cancellation report "no outbound HTTP usage": the clients call an
    injected `fetchImpl`, which the TypeScript arms do not recognise as fetch.
  - RDY-003: authorization is scored repository-wide (one guarded route credits all); no per-route itemisation.
  - RDY-004: `unchecked-any-external-data` is unmapped for this scanner (no rule); S1 does not assess server controls
    for TypeScript.
  - RDY-005: observability neither credits nor flags console output in a module that also logs.
  - RDY-006: the swallowed-exception check has no TypeScript arm.
- **Valid → repository fixed:**
  - R10 `poller.ts:23` / `webhook-dispatcher.ts:23` "duplicated block with local edits" — the two run loops were one
    loop copied; extracted `repeatUntilAborted` (050f405).
  - R10 `api/server.ts:13` / `worker/worker.ts:20` — the database seam, stop signal and listen sequence were copied;
    extracted `src/process.ts` (050f405).
  - R11 `features/steps/webhook-steps.ts:4` and `tests/integration/webhook-dispatch.test.ts:7` — relative imports into
    another workspace package's sources; now imported by package name (mapped to its sources for vitest and tsx, and
    declared as a devDependency of the service) (d1efb26).
  - D36 `release-packages.yml:53` "release publish has no approval gate" — true: any tag pusher could publish to npm;
    the publish job now runs in the `npm` environment with required reviewers (d1efb26).
- **Noise, code kept; promoted to traps (key change):**
  - D29 `unknown-value-in-redirect` at `parcels.ts:56` — false-positive: the call redirects a parcel (a database
    update); no HTTP redirect exists → TRP-022.
  - D31 CKV_K8S_35 "secrets as environment variables" (api, worker, Job; reported at the resource start) —
    opinion-not-fact → TRP-023..025 at the secretKeyRef blocks (the reported line stays outside them: a clean-region
    false positive with imprecise location, as in the C# readiness repository).
  - D31 KSV-0125 "untrusted registry" (three containers) — opinion-not-fact: the team's own registry, by digest →
    TRP-026..028.
  - R11 `features/support/world.ts:5`, `features/steps/webhook-steps.ts:8` "production code depends on a test helper"
    — false-positive: specifications are test code → TRP-029, TRP-030.
  - R7 dead files `cucumber.mjs`, both step files, `world.ts` — false-positive: loaded by cucumber through its
    configuration and import globs → TRP-031..034.
  - C2 "No named authorization policies" — false-positive (scopes named in `Scopes`, declared per route) →
    repository-level TRP-035 (as TRP-003 in the TypeScript baseline).
- **Recorded:** D31 CKV_K8S_31 at `migrate-job.yaml:2` — redundant: the same missing seccomp profile as CNF-001
  (found by KSV-0104), reported at the resource start. D41/D42/DS-0026 rows land on their traps
  (opinion-not-fact / shape-irrelevant / shape-irrelevant).
- Score bands: D40 100 (out of 50–85), D42 80 (out of 30–70), P8 100, P10 100, P11 100 (out) — posture switched on
  by one good site, kept as set before the scan. Unscored: CI gate honesty, outbound resilience, validation, headers,
  lock files.
- Key changes: TRP-022..035 added; RDY-001/002/005/007 and TRP-019 lines moved with the refactor. Validated: OK,
  248 entries.

## 2026-10-07 — scan iteration 2 (final) and freeze

- Contained pass at 0606450: 21 results. Recall 3/17 (CNF-001, RDY-007, SUB-001), trap resistance 21/35: the
  iteration-1 noise now lands on the traps it was promoted to (TRP-013, -014, -020, -022, -026..035). The R10, R11
  cross-package and D36 rows are gone after the iteration-1 fixes; nothing new. The test-reliability check again ran
  the suite three times with 0 flaky (both flaky plants passed).
- Remaining unmatched results, all judged in iteration 1: CKV_K8S_35 at the three resource starts (opinion-not-fact,
  outside TRP-023..025 by location) and CKV_K8S_31 at `migrate-job.yaml:2` (redundant with CNF-001).
- No model-judged score band is in this key, so no host `--with-llm` pass was needed.
- Score bands out, kept as set before the first scan: D40 100, D42 80, P8 100, P10 100, P11 100 (repository-wide
  switches credited by one good site).
- Converged: the repository holds its seventeen plants, 35 traps and recorded scanner noise only. Frozen as v1.0.0
  with this entry.
