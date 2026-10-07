# Authoring journal — bench-ts-baseline-clean

## 2026-10-07 — answer key v1.0.0 draft written (key first)

- Key written before any code, from `scanner-benchmark/coverage/matrix.json` (rows whose coverage includes this
  repository) and `taxonomy.json`; schemaVersion 1.2.
- `clean` (`"concepts": "*"`) over every planned file — production, tests, manifest and lock file, config, CI, docs,
  benchmark files — plus one repository-level `clean` entry for location-less properties.
- `not-applicable`: one entry per concept whose every covering dimension the matrix marks not-applicable here and which
  truly cannot occur in this repository (frontend/accessibility, privacy, DDD, events/messaging/event sourcing,
  Kubernetes, schema migrations, library versioning, BDD, .NET-only mechanisms).
- Deviation from the matrix: concepts the matrix lists only under C#-only dimensions but which can occur in TypeScript
  (e.g. `pointless-catch-rethrow`, `index-access-outside-bounds-guard`, `production-depends-on-test-code`,
  `unsynchronized-shared-state`, `async-void-method` as floating async callbacks, `architecture-rules-unenforced`)
  are certified `clean` at repository level, not not-applicable. Reason: the label must be true of the repository.
- Decisions recorded before the code: no domain layer (DM rows stay not-applicable); in-memory synchronous store behind
  interfaces (ADR 0002); JWT verification against a configured public key, so no secret and no outbound HTTP
  (ADR 0003); layering enforced by an ESLint import rule.
- `score-band` for every posture / metric / judged concept of a band-labelled matrix row (44) plus the band rows on
  finding concepts that are metrics here (D1, D2, R10, R5); bands from intent (see README). No `must-fire`, no
  `must-not-fire`: this is the control.
- Validated with `python3 -m cai_bench validate`: OK, 165 entries.

## 2026-10-07 — implementation complete; key file list updated

- Implemented as planned: Express 5 + zod 4 + pino 10 / pino-http 11 + helmet 8 + jose 6 on Node.js 22, TypeScript
  6.0 (`strict`, `noUncheckedIndexedAccess`, `exactOptionalPropertyTypes`), ESLint 10 with typescript-eslint
  `strictTypeChecked` + `stylisticTypeChecked`, Prettier, vitest 5 with `@vitest/coverage-v8`. 30 production files
  (1,094 non-blank lines), 21 test files (1,088 non-blank lines). 112 tests green (unit 61 + HTTP-level 33 test
  declarations, several parameterised); coverage 94.9 % lines / 91.0 % branches, gated in CI by thresholds
  (90/85/90/90). `npm run lint`, `typecheck`, `build`, `format:check` clean; `node dist/main.js` without
  configuration refuses to start with a structured fatal log.
- Supply chain at implementation time: `npm audit` 0 vulnerabilities (260 packages), `npm audit signatures` all
  verified, no deprecated package in the lock file. `npm outdated` lists exactly two development packages, both
  deliberately on the current release of the previous major: `typescript` 6.0.3 (typescript-eslint 8.71 supports
  `>=4.8.4 <6.1.0`, so 7.x would break linting) and `@types/node` 22.20.5 (the types follow the Node.js 22 runtime).
  Dependabot ignores major updates of these two with the reason in a comment. Lock file written by npm 10.9.9 (the npm
  line Node.js 22 ships). Three transitive dev-only packages are MPL-2.0 (`lightningcss` and its platform binary, via
  vite in the test runner): file-level weak copyleft in a tool that is never shipped — not a licence-policy defect.
- The ESLint layering rule was falsified: an `express` and an infrastructure import added to
  `src/application/clock.ts` produced 2 `no-restricted-imports` errors; the file was restored.
- Key change: the `clean` file list was regenerated from `git ls-files` (77 files, every tracked file; 178 entries).
  Differences from the draft: `src/composition.ts`, `src/main.ts`, `src/http/errors.ts`,
  `src/http/transport-security.ts` added; HTTP tests live under `tests/integration/` (the conventional name), and
  test files were split differently from the plan. Reason: the draft list was a plan; the key must name exactly what
  exists. No label kind, not-applicable entry or score band changed.

## 2026-10-07 — scan iteration 1 (judge: authoring agent)

- Scanner: the reference scanner, engine build of 2026-10-06 (rubric-2026.10.1), contained mode with its secret,
  SAST, dependency and IaC tools; repo at 302774c. 8 results (6 located, 2 location-less); 48 score bands, 27 scored.
  The scanner ran the vitest suite itself with v8 coverage (D8 100).
- **D8 `src/main.ts:1` "Low coverage: 0.0 % line coverage (0/15)" — valid.** True, and not only a number: the
  start-up failure path (fatal log, non-zero exit) and the signal-driven shutdown had no test anywhere. **Fixed
  (8860812):** start-up and signal handling moved to `src/lifecycle.ts` (`run(env, signals)`), tested with an event
  emitter as the signal source; `src/main.ts` is a two-line entry point, itself covered by a test that imports it
  with an invalid environment and checks the exit code.
- **R10 `src/http/routes/catalog-routes.ts:24` (8 lines × 5 locations across the route files),
  `catalog-routes.ts:15` (2 locations) and `stock-routes.ts:17` (2 locations) — valid.** Every handler repeated the
  same parse-then-respond block; five copies across three files is real duplication a reviewer would ask to extract.
  **Fixed (6e22299):** `src/http/handlers.ts` (`listPage`, `withParam`, `createFromBody`) and a local
  `stockMovement` builder; routes are now one registration each.
- **R10 `src/http/problem.ts:24` "Duplicated block with local edits" — valid.** `sendResult` and `sendCreated` were
  one function copied and edited. **Fixed (6e22299):** one `sendResult` with an optional `locationOf`.
- **R10 `src/application/catalog/catalog-service.ts:18` (7 lines × 2, lines 18 and 35) — opinion-not-fact.** The
  similarity is measured correctly, but the two spans are the SKU and bin halves of parallel accessor methods
  (register / get / list): one to three statements each over different store methods, messages and log fields.
  Extracting them would take three lambdas to say less; "the copies drift apart" is the intended behaviour of two
  different entity kinds. Code kept; **key change:** promoted to traps TRP-001 (lines 13–28) and TRP-002 (lines
  30–45), `duplicated-code`, must-not-fire.
- **C2 (repo-level) "No named authorization policies … who may do what is encoded inline where it is checked, so the
  rules can't be listed" — false-positive.** Disproof: `src/http/scopes.ts` names the three scopes, and every `/api`
  route declares its scope with `requireScope(Scopes.x)` at registration (`src/http/routes/*.ts`), the equivalent of a
  named-policy attribute; the README lists the scope per endpoint. **Key change:** promoted to trap TRP-003
  (`authorization-enforcement`, repository-level must-not-fire).
- **P6 (repo-level) "Changelog looks stale/thin: 2 versioned entries, bar 3" — opinion-not-fact.** The service has
  had one release; inventing more to clear a bar would falsify the history. Recorded as noise (as in the C# control).
- Score bands out: BND-003 duplicated-code (R10 81 vs [85,100]) — partly valid, fixed above, re-measured next
  iteration; BND-017 authorization-enforcement (C2 70) — the false positive above; BND-026 architecture
  documentation (M2 70) — the scanner's top tier wants dozens of ADRs, the repository records its three real
  decisions (opinion-not-fact, as in the C# control); BND-032 release hygiene (P6 30) — the scanner did not count the
  `version` in package.json as a version stamp (1.0.0 is on its list of scaffold defaults), although it matches the
  one CHANGELOG release; false-positive, version not changed to suit it. Bands kept (set before the first scan).
- Unscored: D5 (instability, main sequence), D16/D34 (single-author history), D27, S1 (server controls are not
  assessed for TypeScript and a clean S1 abstains), SC1 (never scored), D36 (publishes nothing), P4/P5/P7/P12,
  PF1/PF2, and the model-judged rows (need the LLM pass).
- Not the instrument, recorded for completeness: GitHub CodeQL (security-and-quality) on the same commit raised
  `js/user-controlled-bypass` at `src/http/authentication.ts:61` (`if (!accessToken)` — the value comes from a
  cryptographic verification, so the user cannot control it beyond presenting a valid token; false positive) and
  `js/missing-rate-limiting` at `src/app.ts:53` (rate limiting is the gateway's job per SECURITY.md, and the guarded
  operation is a signature check, not a password check; opinion). Code kept.

## 2026-10-07 — scan iteration 2 (contained + two model-judged passes), converged, freeze

- Contained pass at 017cb1c: 3 results. C2 "No named authorization policies" lands on trap TRP-003 and R10
  `catalog-service.ts:18` on trap TRP-001 (both the scanner's recorded noise, now caught by the key); P6 "changelog
  thin" (opinion-not-fact, iteration 1). The D8 `src/main.ts` row and the four route/problem R10 rows are gone; R10
  rose from 81 to 96 (BND-003 now in band). Nothing new.
- Model-judged passes (host mode, run twice): D19 90, D20 100, D21 100, M4 60 — all in band; D22, D24, D25, LA3, LA4
  emitted no score (bands unscored, kept). The two passes produced byte-identical SARIF (sha256 7312900…): no
  disagreement this time, and identical output suggests model responses were reused rather than re-drawn, so this is
  not evidence of determinism.
- **M4 × 39 "README/code drift: README claims …" — false-positive (each).** The model listed nearly every sentence of
  the README as drift (every configuration default, the layering, the test layout, the licence, the contribution
  rules), each marked by the scanner itself as an unverified model reading. Each claim was checked against the tree
  and holds: the defaults are those in `src/config.ts`; `JWT_PUBLIC_KEY` is required; the three layers, `app.ts`,
  `composition.ts`, `lifecycle.ts`, `main.ts` and the ESLint layering rule exist as described; the tests and CI do
  what the README says; LICENSE is MIT. ("calls no other services" appears twice in the 39.) Recorded as noise; code
  and README kept. M4 still scores 60 (in band, at its floor).
- Score bands out, unchanged from iteration 1 and kept: BND-017 (C2 70, the false positive caught by TRP-003),
  BND-026 (M2 70, opinion-not-fact), BND-032 (P6 30, false-positive: package.json `version` 1.0.0 not counted).
- Host passes do not produce D1/D2/D3/D6/D26/AX*/R9/R11/D28–D30 scores; the contained pass is the measurement of
  record for those.
- Converged: certified clean apart from recorded scanner noise; three traps, all three caught by this scanner in
  this iteration's terms (TRP-001, TRP-003 fired; TRP-002 held because the scanner names only the first copy).
  Frozen as v1.0.0 with this entry.
