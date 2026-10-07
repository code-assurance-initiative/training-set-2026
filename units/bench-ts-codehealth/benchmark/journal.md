# Authoring journal — bench-ts-codehealth

## 2026-10-07 — answer key v1.0.0 drafted (key first)

- Wrote `benchmark/answer-key.json` and `benchmark/README.md` before any code: 49 `must-fire` (CH-001…049),
  32 `must-not-fire` (TRP-001…032), 9 `score-band` (BND-001…009) and planned `clean` files (60). Lines are planned
  positions; they are fixed to the final code in step 2. Validated with `python3 -m cai_bench validate`: OK
  (150 entries).
- Scope: every concept the coverage matrix assigns to `bench-ts-codehealth` (D1, D2, D3, D6, D10, D17, D21, D24,
  AX6, DM4, DM5, PF3, R1–R4, R6, R7, R9–R11, X2, X4, X5, X6, X13, X16, X18, X19, X23, X25, X29), plus the
  TypeScript defects the C# counterpart covers where they occur in TypeScript too (silent fallback, pointless
  rethrow, not-implemented placeholder, incomplete stub, unreachable switch arm, deprecated symbol still used).
  One realistic site per concept; two where a concept has two genuinely different shapes (floating promise: a bare
  unawaited call on a request path and an async `forEach` callback; unused code: a private method, an unused export
  and a write-only field; technical-debt marker: TODO and HACK; suppressed diagnostic: `@ts-ignore`, a bare
  file-wide `eslint-disable` and a rule switch-off block in the ESLint config).
- Harness change (scanner-benchmark b63206f): two concepts added because the taxonomy had none for these TypeScript
  defects — `floating-promise` (a dropped asynchronous result; X1's dropped-promise rows, previously off-concept, now
  map to it) and `unchecked-any-external-data` (an `any` cast on data from outside the program; no scanner rule
  evidences it, it is attached to the type-safety dimension R1 as unevidenced). Matrix check and mapping tests green.
- Score bands chosen from intent before any scan (README, "Score bands").
- Decisions recorded in `benchmark/README.md` ("Contested truths"): flat switch, type-only import cycle, voided
  fire-and-forget promise and synchronous reads in a CLI are traps; strict-null posture and `!` density are
  repository-level entries; loose equality and parameter mutation are not covered (no concept, nothing planted).
- Clean regions list this repository's code-health concepts, not `"*"`: the repository certifies the absence of
  code-health defects, not of every other kind.

## 2026-10-07 — implemented; key lines fixed to the code

- Implemented the service: `src/domain` (Shipment and LabelBatch aggregates, value objects, events, ports),
  `src/application` (pricing, labels, carrier accounts), `src/infrastructure` (two carrier adapters and registry,
  rate-card cache, label archive, mail pickup directory, CUPS printing, in-memory repository, tariff zones generated
  from `data/country-zones.csv` by `tools/generate-zones.mjs`), `src/api` (Express 5 routes, JWT scopes, signed
  webhooks) and the `parcel-rates` CLI under `tools/` (own tsconfig with strict null checks off: CH-024).
  Express 5, zod 4, pino 10, helmet 8, jose 6, TypeScript 6.0, ESLint 10 + typescript-eslint strictTypeChecked,
  vitest 5. `npm run lint`, `typecheck` (service and tools), `build`, `format:check` clean; 98 tests passed, 1 skipped
  (CH-046); coverage 94.7 % lines / 85.2 % branches over hand-written source (the generated zones file is excluded
  from coverage), gated in CI.
- Key changes against the draft, each because the code made a site more precise or the draft was wrong:
  - **No `noUnusedLocals` / `noUnusedParameters`** in the service tsconfig: with them the compiler itself rejects the
    unused private method (CH-015) and the write-only field (CH-049), so those plants could not exist; a messy
    service that never turned them on is the realistic way they survive.
  - **CH-050 added** (unused-code): `#refreshTimer` is written in the constructor and read nowhere, not even by
    `dispose()` — the field-level face of CH-034. In JavaScript nothing needs the field to keep the interval alive
    (unlike .NET's Timer, see bench-csharp-codehealth TRP-029), so it is truly dead state. Declared six lines from
    `#ttlMs` (CH-049) so the two plants do not share a window.
  - **TRP-033 added** (non-structured-log-message): `log.info(`…`)` in the import command is a console sink for the
    operator, not the structured logger, although pino is a dependency of the package.
  - **TRP-030 moved** to `RateCalculator`: the rate-card source (the disposable cache in production) is handed to the
    calculator, not to QuoteService, and the calculator correctly does not dispose it.
  - **TRP-026** spans `importArchive` from its function line to its `finally`, so a result on its `chdir` lands on
    the trap. **TRP-031** spans both conditional skips of the CUPS test.
  - **TRP-011** anchors the URL line carrying `state=TODO` (the JSDoc above names the same carrier state).
  - Accidental defects removed while writing (not plants): a `toISOString()` date of a local-midnight value (the
    ship date was a day early east of UTC; the cut-off calendar now works in UTC), an unvalidated
    `as RateCard[]` cast of the downloaded cards (now a zod schema — the only `any`-style cast left is CH-023),
    `mkdirSync` inside an async method, a discarded event object, and a `readdir`/`stat` catch that turned every
    error into "missing".
  - Clean list: every tracked file without a plant or trap (84 files).
- Every `lines` entry resolved from the final tree by anchor and printed with its text for a visual check; the key
  validates (176 entries: 50 must-fire, 33 must-not-fire, 84 clean, 9 score-band).

## 2026-10-07 — scan iteration 1 (repository commit a8eb4cb)

- Scanner: the reference scanner (engine commit 6a05dfb6c, rubric-2026.10.1), contained mode; 48 results; SARIF
  sha256 `667dc4e990bb8acbcbaa4cd35e079f0f2a810cac285cec570bf0680fcb2a179a`. Harness: 26/50 plants found, 30/33 traps
  held, 1 clean-region hit, 8 uncovered. Bands: D1 92 / R2 99 (BND-001 out: R2 above 97), D2 84, R10 96, D6 93, R1 99,
  D8/R4 100, R6 100 in band; D21/D24 unscored (the model pass is next).
- **Valid, repository fixed:** `parseAddress` cyclomatic 16 (D1 + R2, one finding): postcode formats moved to a table;
  `archive`/`reprint` duplicated the label-file load (R10): one `labelFile` helper.
- **Valid, plant corrected (the scanner pointed at the real site, the key had the wrong line):** CH-029 now spans
  `async load` from its declaration to the `readFileSync` call (PF3 reports the function line); CH-040 spans the
  registry's import block (R9 reports a cycle for the file, without a line).
- **Valid, key changed:** CH-051 added (duplicated-code): `importFolder` and `importArchive` repeat the
  change-directory sequence — the planted CH-038 and its trap TRP-026; one helper with a `finally` fixes both.
- **False positives / opinions promoted to traps:** TRP-034 (R2 counts the optional `?` members of the hooks interface
  as 18 branches — the file has no control flow); TRP-035 (R2 cyclomatic 11 on the three-command CLI dispatcher).
- **Traps caught:** TRP-001 (R2 on the flat service-code switch), TRP-021 (X4 on a template interpolating a module
  constant), TRP-033 (X4 on the CLI's console sink named `log`).
- **Noise recorded:** D10 "No assertions" on CH-047's site (false: `expect.fail` is an assertion; the real defect —
  the swallowing catch — is not what the row says); X2 location-less roll-up (valid: it is CH-030, which the harness
  cannot credit); uncovered C2 (false positive, as in the baseline), P6 (opinion), P7 on `download` (valid: CH-030
  through another lens), R4/R6 "imports a generated file that names no file" ×5 (false: the file exists and loads).
- **False negatives (24), every plant re-verified as real:** `@ts-ignore` (CH-010) and the ESLint rule switch-off
  (CH-012), commented-out code, empty catch, unused private method / export / write-only fields (CH-015/016/049/050),
  deprecated method still called, not-implemented throw, constant-returning stub, catch-only-rethrow, `as any` on the
  response body, `!` density (repository-level), optional dereference in the tools, the bare unawaited call on the
  request path (CH-027), fetch without a signal (only the roll-up), inert TTL option, stderr never drained (X13),
  test without assertion (CH-045), swallowed test failure, mock-only route test. The TypeScript arm of the debt
  dimension reads task comments only, and several card rules read TypeScript shapes narrower than the defect.
- Not the instrument, recorded: GitHub CodeQL raised `js/http-to-file-access` (label archive, mail pickup — writing
  carrier labels and mail files is the design), `js/user-controlled-bypass` (same false positive as in the baseline)
  and `js/missing-rate-limiting` ×2 (the gateway's job per SECURITY.md). CI green.

## 2026-10-07 — scan iteration 2 (repository commit 7a28e0e) and the model-judged pass

- Contained pass: 46 results; SARIF sha256 `facfc6c7cabd6e61c26a6eb8473d77b555cd8812de850ed36a18fe4bb9aa0da8`.
  Every iteration-1 fix took effect: the D1 `parseAddress` row and both label-service/importer R10 rows on fixed code
  are gone, CH-029 (PF3), CH-040 (R9) and CH-051 (R10) are hits. 29/51 plants found; traps caught TRP-001, TRP-021,
  TRP-033 and the two new ones (TRP-034, TRP-035 — the same false positives as iteration 1, now in the key). Bands as
  in iteration 1 (BND-001 out: R2 99).
- New result, judged: R2 `parseAddress` (cyclomatic 12, below D1's bar after the fix, above R2's bar of 10) —
  opinion-not-fact: flat field validation. **Key change:** promoted to trap TRP-036.
- Host pass with the model-judged dimensions (`--host --with-llm`): SARIF sha256
  `2e1c1935f92e56dfc41685366480ca7260e47071623ff043d05bfa4095fb1f9f`. D21 100 — **BND-008 out of 40–85** — and D24
  unscored: both dimensions sample symbols/comments from the C# workspace only, so on TypeScript the naming score rests
  on an empty sample (false-positive: a score without evidence) and the comment score is not produced. Band kept.
  M4 × 8 "README/code drift" (configuration variables, two endpoints, the test suites): false positives — each claim
  holds in `src/config.ts`, the route files and `tests/`; the rule matches names of files only. D19 "no contributor
  guidance": opinion-not-fact. Run once only: the model-judged rows here do not touch any labelled concept.

## 2026-10-07 — scan iteration 3 (repository commit 024770a, final) and freeze

- Contained pass: SARIF sha256 `facfc6c7cabd6e61c26a6eb8473d77b555cd8812de850ed36a18fe4bb9aa0da8` — byte-identical to
  iteration 2 (only the key changed). Outcome: recall 29/51, trap resistance 30/36 (caught: TRP-001, TRP-021,
  TRP-033, TRP-034, TRP-035, TRP-036 — all judged scanner noise, code kept), no clean-region hit; the remaining
  unmatched rows are the recorded D10 row on CH-047's site and the X2 roll-up of CH-030; uncovered rows as recorded.
  Bands: BND-001 out (R2 99, D1 93), BND-008 out in the model pass (D21 100 on an empty sample), BND-009 unscored,
  the rest in band. Bands kept as set before the first scan.
- False negatives (22), each re-verified real and correctly placed: CH-010, CH-012, CH-013, CH-014, CH-015, CH-016,
  CH-018, CH-019, CH-020, CH-022, CH-023, CH-025, CH-026, CH-027, CH-030, CH-033, CH-036, CH-045, CH-047, CH-048,
  CH-049, CH-050. The key is not weakened for any of them.
- Converged: every unexpected result is judged and either fixed or recorded; the key matches the code. Frozen as
  v1.0.0 with this entry.
