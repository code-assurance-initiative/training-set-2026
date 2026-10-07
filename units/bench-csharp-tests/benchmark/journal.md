# Authoring journal — bench-csharp-tests

## 2026-10-07 — answer key v1.0.0 draft written (key first)

- Key written before any code, from `scanner-benchmark/coverage/matrix.json` (rows covering this repository: D8, D9,
  D10, D11, AX8, P9, P11, P12) and `taxonomy.json`.
- Harness change first (scanner-benchmark 8779a5c): new concept **`test-failure-swallowed`** — a test whose act and
  assertions run inside a catch-all that discards the failure. No existing concept denoted it (the reference mapping
  listed the corresponding title as off-concept), and `test-without-assertion` would be untrue of a test that does
  assert. Added to `concepts.py`, D10 in `dims.py`, the D10 discriminator in `discrim.py`; regenerated; matrix check
  and unit tests green.
- 11 plants (8 test-quality, one whole-file coverage gap, one production → test reference, one ungated coverage
  collection at repository level), 13 traps, 5 score bands from intent, `clean` over the remaining planned files for
  the concepts the repository measures. Lines are planned positions, to be fixed to the implementation.
- `excessive-mocking` is planted although the reference mapping has no rule for it: labels are truth, not a
  prediction of one scanner.
- Validated with `python3 -m cai_bench validate`: OK, 62 entries.

## 2026-10-07 — implementation complete; key fixed to the code

- Implemented `Fx.Conversion.Core` / `.Rates` / `.Api` (net10.0, 1,172 non-blank production lines) and four test
  projects (unit, integration, Reqnroll specs, test support; 1,175 non-blank lines). Build green with warnings as
  errors; **112 tests pass, 2 skipped** (TQ-003 and TRP-003) — 95 unit, 13 integration, 4 BDD scenarios. Measured
  line coverage 88 % merged; `CashRounding.cs` 0 % (COV-001); every other production file ≥ 50 %.
- Real defect found in my own production code while testing on a da-DK machine: `[Range(typeof(decimal), "0.01", …)]`
  parses its limits in the current culture, so every quote request was rejected outside en-* cultures. Fixed
  (`ParseLimitsInInvariantCulture`); not a plant.
- TQ-006 verified real: `TZ=Pacific/Pago_Pago` at 09:23 UTC fails it (expected age 0, actual 1); UTC and da-DK at the
  same moment pass it.
- Key changes (reason: the draft named planned positions; the key must name what exists):
  - every `lines` entry fixed to the test method header (attribute line through signature line). Whole-method
    spans were tried first and rejected by the validator: TQ-002/TRP-009 and TQ-004/TRP-010 overlapped within the
    line tolerance.
  - **TRP-003 moved** from `Rounding/MoneyRoundingTests.cs` to `Allocation/AllocatorTests.cs`. The planned deferral
    ("Ceiling of a tiny negative amount prints -0.00") turned out to be false — the test passes unskipped — and a skip
    reason that names a non-existent bug would be a lie. Replaced with a real, reproduced bug (weights summing past
    `int.MaxValue` throw `OverflowException`), tracked as issue #1 in this repository and linked from the reason.
  - TRP-005 rationale: the in-memory client uses the reserved `.test` domain (HSTS excludes `localhost`).
  - `Money/` folder became `Monetary/` (namespace clash with the `Money` type); method names are PascalCase.
  - `clean` list regenerated from `git ls-files`.
- Validated: OK, 110 entries (11 must-fire, 13 must-not-fire, 82 clean, 5 score bands).

## 2026-10-07 — scan iteration 1 (contained, repository at 8413dc0)

- Scanner: the reference scanner, engine kennel main 6a05dfb6c (rubric-2026.10.1), contained mode. 13 results
  (sarif sha256 e0f3fadb…). Harness: 7/11 found, traps 12/13, 1 trap caught.
- Hits: COV-001 (Low coverage, CashRounding.cs 0 %), TQ-001 (No assertions), TQ-003 (Skipped test "wip"), TQ-004
  (Fixed-sleep synchronisation), TQ-005 (live external host), TQ-008 (Test cannot fail), GATE-001 (coverage collected
  but not gated).
- Unexpected results, each judged:
  - **D10 "No assertions: PartsAlwaysSumToTheTotal" — false-positive**, caught by trap TRP-009. The CsCheck
    `Sample(predicate)` throws `CsCheckException` with the shrunk counter-example when the predicate is false; the
    property-assert API is the assertion. Code kept.
  - **D4 duplicated block, ConversionRequest.cs:8-17 / QuoteRequest.cs:7-16 — valid.** The two request contracts
    repeated the same From/To validation. Fixed in 743d844 (abstract `CurrencyPairRequest`). Key: new file added to
    `clean`.
  - **D5 "Fx.Conversion.Core: zone of pain" — opinion-not-fact.** A concrete library that the host and the adapter
    depend on is the intended shape (ADR 0001); the main-sequence distance is a heuristic. Recorded.
  - **D17 `.editorconfig:37` CA2007 severity none — false-positive.** The reason is the comment on line 36 directly
    above it, and `src/.editorconfig` switches the rule back on for production code. Same site and verdict as the
    baseline repository's trap; not promoted here (off theme). Recorded.
  - **IC1 "Disabled test … (\"wip\")" — redundant** with the D10 hit on TQ-003 (same test, same issue). Counted once.
  - **IC1 "Disabled test … (\"BUG: … issues/1\") … re-enable or delete it" — opinion-not-fact.** The skip is real,
    but the framing ("coverage that looks present but never runs", "re-enable or delete") ignores a categorised
    reason naming a reproduced bug and its tracking issue. Off-concept in the mapping (skip with a reason), so it does
    not reach TRP-003. Recorded.
- Missed plants (each re-verified as real and correctly placed; no key change):
  - TQ-002 `Assert.True(true)` — the scanner counts any `Assert*` call as an assertion; a tautology is not recognised.
  - TQ-006 local-vs-UTC date — no static check for ambient clock reads in tests; the suite re-run (D11) cannot see a
    time-zone-dependent failure on a UTC build box. Reproduced in `TZ=Pacific/Pago_Pago`.
  - TQ-007 six substitutes verifying calls only — the scanner reports mock *frameworks* as Info, never per-test mock
    dominance.
  - AX8-001 API → tests/Fx.Conversion.TestSupport — AX8 scored 100 with no finding. The referenced project lives
    under `tests/` and references `xunit.v3.assert`; it is not classified as a test project (it holds no test
    methods), so the reference is not seen.
- Score bands: D9 95 in [70,100]; P11 100 in [80,100]; **D8 100 out of [60,95]** — the scanner's coverage curve
  saturates (88 % line coverage scores 10/10) and does not deduct for a wholly untested core module whose row it does
  report; band kept (set before the scan), recorded as opinion-not-fact. P9 and P12 publish no score (unscored).

## 2026-10-07 — scan iteration 2 (contained, repository at eabe7b3) — converged; freeze v1.0.0

- 12 results (sarif sha256 0d5b1de8…). The D4 duplication is gone after 743d844. Every other result is unchanged
  code already judged in iteration 1: D5 (opinion-not-fact), D17 (false-positive), IC1 ×2 (redundant;
  opinion-not-fact), D10 on TRP-009 (false-positive, trap caught). Same 7/11 hits and the same four missed plants.
- No model-judged score bands in this repository, so no `--with-llm` pass was run.
- Final: recall 7/11 (63.6 %), trap resistance 12/13, noise 1/8 covered results; score bands D9 and P11 in, D8 out
  (100 vs [60,95]), P9 and P12 unscored.
- Frozen: code and key together at tag v1.0.0.
