# Authoring journal — bench-csharp-codehealth

## 2026-10-07 — answer key v1.0.0 drafted (key first)

- Wrote `benchmark/answer-key.json` and `benchmark/README.md` before any code: 48 `must-fire` (CH-001…048),
  27 `must-not-fire` (TRP-001…027), 1 `not-applicable` (NA-001, JS interop), 8 `score-band` (BND-001…008) and
  planned `clean` files. Lines are planned positions; they are fixed to the final code in step 2.
- Scope: every concept the coverage matrix assigns to `bench-csharp-codehealth` (D1, D2, D3, D4, D6, D17, D21, D24,
  D39, GD1, IC1, PF2, PF3, X1–X10, X12, X13, X16, X18–X23, X25–X30, X32). One realistic site per concept; two where a
  concept has two genuinely different shapes (duplicated code: a member clone and a duplicated predicate;
  unreachable code: a dead `#if` and a dead switch label; blocking on async: `.Result` in a handler and a sync
  wrapper; technical-debt marker: TODO and HACK).
- Score bands were chosen from intent before any scan (README, "Score bands").
- Decisions recorded in `benchmark/README.md` ("Contested truths"): a flat mapping switch is a trap for
  cyclomatic complexity; `GetAwaiter().GetResult()` in a console `Main` is a trap; nullable posture, `!` density
  and ConfigureAwait adoption are repository-level entries.
- Clean regions list this repository's code-health concepts, not `"*"`: the repository certifies the absence of
  code-health defects, not of every other kind.
- Validated with `python3 -m cai_bench validate` against `taxonomy.json`: OK (104 entries).

## 2026-10-07 — implemented; key lines fixed to the code

- Implemented the service: `Shipping.Rates.Core` (domain, tariff pricing, two carrier adapters, labels),
  `Shipping.Rates.Api` (minimal API, JWT scopes, security headers, HMAC webhooks), `Shipping.Rates.Tools`
  (CLI), a generated `CountryZones.g.cs` (from `data/country-zones.csv` by `tools/generate-zones.py`), and two test
  projects. `dotnet build -c Release`: 0 errors, 2 warnings (CS0618 and CA2200 — the warnings CH-049 demotes from
  errors; they are the visible symptom of that plant). `dotnet test`: 94 passed, 1 skipped (CH-020), 0 failed.
- Key changes against the draft, each because the code made the site more precise or the first site was wrong:
  - **CH-030 moved** from `AddressFormatter` to `Tools/Printing/PrintCommand.cs`. In a nullable-enabled project the
    compiler itself reports a dereference after `x?.` (CS8602, an error under warnings-as-errors), so the drafted
    site could not exist as a latent defect there. The CLI compiles with nullable disabled (CH-028), which is exactly
    where such a dereference survives review: `printerName?.Trim()` then `printerName.Length`, null whenever the
    optional argument is omitted.
  - **CH-049 added** (suppressed-diagnostic): `throw ex;` (CH-022) and the call of the obsolete `Quote` (CH-017) are
    compiler errors under warnings-as-errors. The realistic way such code survives is a project-level
    `<WarningsNotAsErrors>CS0618;CA2200</WarningsNotAsErrors>` with no reason — itself a defect a reviewer flags.
    No warning is suppressed: both still print in every build.
  - **TRP-011 moved** to `Api/Hosting/ApiServiceCollectionExtensions.cs`: the justified suppression is the opt-in to
    the library's own `[Experimental("SHIPRATES001")]` partner-adapter API, scoped to one statement.
  - **TRP-026/027** now name `SchedulePickupAsync` (Alder does not collect at all) instead of "Saturday pickup".
  - **CH-046** rationale: both current carriers insure, so the inverted guard is dormant today and live for any
    carrier that does not — a latent defect, not one the tests exercise.
  - CH-038, CH-029 rationales made exact to the code; CH-041's site is the re-spelled default in `IsStale`.
  - Clean list: every tracked file without a plant or trap (77 files).
- All `lines` were resolved from the final tree by anchor and printed with their text for a visual check (the
  keygen `--check` output, equivalent to `sed -n`); the key validates (162 entries: 49 must-fire, 27 must-not-fire,
  77 clean, 1 not-applicable, 8 score-band).
- Before the first scan: **TRP-028 added** (compiled-code-size, must-not-fire, `CountryZones.g.cs`). Measured with
  Mono.Cecil on the Release build: `ZoneFor` and `RequiresCustomsDeclaration` are 2,976 IL instructions each (the
  generated 249-arm switches); the largest authored method is `ZplLabelRenderer.Render` at 827 (CH-005); every
  other authored method is under 300, async state machines included. The generated type is a natural trap for an
  IL-size measure that does not honour `[GeneratedCode]`.

## 2026-10-07 — scan iteration 1 (repository commit 923ad51)

- Scanner: the reference scanner (rubric-2026.10.1), contained mode; 74 results; SARIF sha256
  `35c1f5cfd90921ffede62f0d5976f243dcff85cd4cae69ec3ced05221e379bb0`. Every verdict with its reason:
  `cai-bench/_scans/bench-csharp-codehealth/iter1.verdicts.md` (and the results file in scanner-benchmark).
- Outcome against the key: 44/49 plants found, 27/28 traps held (TRP-013 caught: `GetAwaiter().GetResult()` in a
  console `Main`), one clean region hit (`LabelOptions.cs`, judged valid — see below). Bands D1 94, D2 87, D4 98,
  D6 92, D39 99 all in band; PF2, D21, D24 unscored (no model pass yet).
- **Valid, repository fixed:** Render's three inline handling-instruction branches moved to a helper (D1 16 / D2 17
  on the long method; Render stays 100+ significant lines and 250+ IL, so CH-004/CH-005 are unchanged); the
  placeholder default sender address removed (e-mail only when `Labels:FromAddress` is configured); retention period
  with an hourly purge (C4); standard resilience handler on every outbound client, retries off for unsafe methods
  (P7); the webhook handler and the CLI dispatcher take and forward a CancellationToken (two of the five methods in
  the X2 roll-up); tests for the CLI paths, the rate-card cache and every service code (D8 coverage and CRAP rows).
- **Valid, key changed:** CH-050 added — `_ttl` is a write-only field, i.e. unused code, the dead state behind
  CH-041. The key had missed it; the code is kept because it is the planted inert option. To keep the new plant and
  the trap below from sharing a ±3 window, the two field declarations were reordered (8 lines apart).
- **False positives promoted to traps:** TRP-029 (the never-read `_refreshTimer` field is what keeps the Timer
  alive); TRP-030 (`ExtractErrorCode`'s `"UNKNOWN"` sentinel is always logged and thrown, not silent).
- **Noise recorded:** D5 main sequence (opinion-not-fact), C1 encryption at rest (opinion-not-fact), C3 EF audit
  interceptor (shape-irrelevant).
- **False negatives (5, each plant re-verified as real; key unchanged):** CH-021 (pointless catch-rethrow: no rule),
  CH-023 (`.Result`: not detected), CH-026 (cancellation: only a location-less roll-up reaches SARIF), CH-034
  (truncation loop measured through a helper), CH-049 (`WarningsNotAsErrors`: only `NoWarn` is read).
- After the fixes: build 0 errors (the two CH-049 warnings), 120 passed / 1 skipped (CH-020); key regenerated and
  validated (170 entries).

## 2026-10-07 — scan iteration 2 (repository commit 9f9e86d)

- Same scanner and mode; 71 results; SARIF sha256 `cc42018e8eef374ea63f840dabafd84aea87cc577c6837be39782d1a63a6a0c8`.
- Every iteration-1 fix took effect: the Render complexity rows, the placeholder sender, C4, P7 and the coverage
  rows for the cache, the CLI dispatcher, the watcher, the reloader and the status probe are gone. 45/50 plants
  found (CH-050 hit), the same 5 false negatives. Traps: TRP-013, TRP-029, TRP-030 caught (the three judged
  false positives, unchanged code). Bands unchanged and in band (D2 now 88).
- New results, judged:
  - D3 TooManyMethods now counts 34 methods (PurgeExpired joined LabelService) — still CH-003, no change.
  - X2 roll-up 24/27: the remaining three are DownloadAsync (CH-026), WarmUp (CH-025, async void) and the
    FileSystemWatcher event handler (whose signature cannot take a token). Valid; it is the location-less
    expression of CH-026, which the harness cannot credit to the located plant. No change.
  - D10 fixed-sleep synchronisation in `ImportTests` (valid: the test raced the watcher with `Task.Delay(200)`).
    Fixed at the root: `watch` now also counts rate cards that were already in the folder when it started (a real
    race in the tool: a drop landing just before the command ran was missed), so the test writes first and needs
    no sleep.
  - D8 `Program.cs` 0 % (valid): a test now runs `Main` without arguments.
  - D8 `LabelPrinter.cs` 40 % and `PrintCommand.cs` 0 % (+ CRAP 30): valid, residual. The success paths need a
    real CUPS printer (the offline test rule), and every offline path through PrintCommand runs into the planted
    release-guard defect (CH-039: the slot is released twice and `SemaphoreSlim` throws), so a test there would
    either print for real or assert a planted bug.
- Build 0 errors, 121 passed / 1 skipped; key regenerated and validated.

## 2026-10-07 — scan iteration 3 (repository commit d837ab1) and the first model-judged pass

- Contained pass: 71 results, SARIF sha256 `827e2def12e6dcf3f94ed5095444899113917350e1c4ecd90d5c647942ec1c50`;
  identical outcome to iteration 2 (45/50, the same 5 FNs, TRP-013/029/030 caught, the X2 roll-up), minus the two
  coverage rows fixed in iteration 2. Remaining off-concept rows are the recorded D5/C1/C3 noise and the documented
  D8 residual (LabelPrinter, PrintCommand) and CH-020's D10 row.
- Host pass with the model-judged dimensions (`--host --with-llm`): SARIF sha256
  `6765f32271098e2f83c38054525c3b2939d18377741b3936aec9054cfa599b22`. Bands: D24 55 (in 40–85); **D21 100, out of
  40–85**; PF2 not applicable (unscored, as expected: no packaged library, fewer than 8 allocation-aware uses).
- Judged model rows:
  - D24 "redundant comment" × 22. The seven on the restating comments of the messy parts (CarrierAccountManager,
    LabelService, RateCalculator) are valid and are the posture BND-008 measures. The section signposts inside the
    overlong `Render` are valid as low-value comments too (they label what the next ZPL line already says; they are
    a symptom of CH-004), and so is "handling instructions" above the call that now names it. Kept as part of the
    measured posture. One is a false positive: "receipt stub, torn off by the sender" explains the physical purpose
    of a printed box, which the `^GB` command cannot say.
  - M4 "README/code drift" × 9: false positives. Every claim is implemented and findable by name: CSV import
    (`RateCardCsv`, `RateCardImportCommand`), drop waiting (`DropFolderWatcher`), CUPS (`LabelPrinter` runs `lp`),
    e-mail (`SmtpClient` in `LabelService`), archive and index (`LabelArchive`, `index.json`), the hourly job
    (`LabelRetentionService`), HMAC (`SignWebhook`/`VerifyWebhook`), the resilience pipeline
    (`AddStandardResilienceHandler`), the scopes (`AuthorizationPolicies`).
- **D21 out of band — the plant was under-implemented, not the scanner wrong.** BND-007's rationale (written before
  any scan) says the messy parts mix naming styles, "snake_case locals, Hungarian-style prefixes, abbreviations";
  the code had only abbreviations (`cc`, `isRmt`, `tmp`). The repository was brought in line with the key, not the
  key with the scanner: `surcharge_total` (snake_case) in `ComputeSurcharges`, `mTotal`/`lstParcels`/`get_str` in
  `LabelService`, an `m_windowStart` field in `CarrierAccountManager`. Renames only, no line moved; band unchanged.

## 2026-10-07 — scan iteration 4 (repository commit 29f24c9, final) and freeze

- Contained pass: SARIF sha256 `827e2def12e6dcf3f94ed5095444899113917350e1c4ecd90d5c647942ec1c50` — byte-identical
  to iteration 3 (the renames moved no line and changed no result).
- Two host passes with the model-judged dimensions: both SARIF sha256
  `7cae066b11c8a7a5bb6eafdf942c775553829a10bdf927565e97aed31fca8f68` (identical; the model calls were answered
  the same way twice). D24 70 (in 40–85). **D21 90, out of 40–85** (iteration 3: 100).
- New model rows judged: D21 "Private fields in `CarrierAccountManager` use a mix of `m_` prefix … and `_`" —
  valid (the planted inconsistency). Thirteen further D21 rows each conclude "No inconsistency … No change
  needed" (or, for `BuildRateRequest`, call two same-named copy-pasted methods a naming question): false positives —
  a row whose own text says nothing is wrong publishes a non-finding as a finding. The D24 rows are the same
  comments as iteration 3 with reworded advice (same verdicts).
- **BND-007 stays out of band, band kept.** The model samples member symbols; the planted snake_case/Hungarian names
  are mostly locals, so one of five planted names reached its sample. A 90 for a codebase whose naming is mostly
  consistent is defensible, but the band was set before the first scan and a band is never moved to meet a score.
  Recorded as an opinion-level disagreement in the results.
- Converged: every unexpected result is judged and either fixed or recorded; the key matches the code. Final
  outcome on the contained pass: recall 45/50, trap resistance 27/30, noise 4 results; false negatives CH-021,
  CH-023, CH-026, CH-034, CH-049 (each re-verified real). Frozen as v1.0.0 with this entry.
