# Authoring journal — bench-csharp-baseline-clean

## 2026-10-06 — answer key v1.0.0 draft written (key first)

- Key written before any code, from `scanner-benchmark/coverage/matrix.json` (rows whose coverage includes this
  repository) and `taxonomy.json`.
- `clean` (`"concepts": "*"`) over every planned file — production, tests, manifests and lock files, config, CI, docs,
  benchmark files — plus one repository-level `clean` entry for location-less properties.
- `not-applicable`: one entry per concept whose every covering dimension the matrix marks not-applicable here (privacy,
  DDD, events/event sourcing, frontend/accessibility, Kubernetes, schema migrations, library versioning, BDD,
  architecture rules). Concepts that the not-applicable dimensions share with measured ones (module cycles, layer
  violations, complexity, duplication, oversized files, unused code, outdated dependencies, test coverage) are *not*
  marked not-applicable: they are measured here and covered by the `clean` labels and score bands.
- Decision: no domain layer (plain Api / Application / Infrastructure), so the DM rows stay not-applicable.
- Deviation from the matrix: `unused-dependency`, `undeclared-dependency`, `misplaced-dev-dependency` (R8, frontend) are
  certified `clean` at repository level, not not-applicable, because as concepts they apply to NuGet manifests too.
- `score-band` for every posture / metric / judged concept of a band-labelled matrix row; bands from intent (see
  README). No `must-fire`, no `must-not-fire`: this is the control.
- Validated with `python3 -m cai_bench validate` (taxonomy 1.0): OK, 192 entries.

## 2026-10-07 — implementation complete; key file list updated

- Implemented as planned: `Warehouse.Stock.Api` / `.Application` / `.Infrastructure` (net10.0), 51 production C#
  files (1,140 non-blank lines), 15 test files (786 non-blank lines). Build green with warnings as errors and
  `AnalysisLevel=latest-recommended`; 63 tests green (41 unit, 22 integration).
- Key change: the `clean` file list was regenerated from `git ls-files` (99 files, every tracked file). Differences
  from the draft: `src/.editorconfig` added (enforces `ConfigureAwait(false)` in production code via CA2007); otherwise
  the planned paths held. Reason: the draft list was a plan; the key must name exactly what exists.
- Key change: the repository-level `clean` rationale and the manifest rationale now say *directly referenced*
  packages are current. Reason: `dotnet list package --outdated --include-transitive` shows transitive packages
  (e.g. Microsoft.IdentityModel.* 8.19.2, resolved by Microsoft.AspNetCore.Authentication.JwtBearer 10.0.12) behind
  their newest releases; pinning transitives by hand is not what a competent team does for a framework package, and
  the key must not claim more than is true. No package, direct or transitive, is vulnerable or deprecated.
- Test runner decision: xUnit v3 runs through VSTest (`xunit.v3.mtp-off` + `Microsoft.NET.Test.Sdk` +
  `xunit.runner.visualstudio`) with `coverlet.collector`, so `dotnet test --collect:"XPlat Code Coverage"` measures
  coverage (measured locally: 85 % line coverage in the unit-test run). The Microsoft.Testing.Platform variant of
  xUnit v3 cannot take the VSTest `--collect` switch. CI does not collect coverage, because it does not gate on it.
- Dependabot: `cooldown.default-days: 7` on both ecosystems. No container images exist, so nothing to pin by digest.
- Score bands unchanged (chosen before any scan).

## 2026-10-07 — scan iteration 1 (judge: coordinator)

- Scanner: the reference scanner, engine build of 2026-10-06 (rubric-2026.10.1), contained mode with its secret,
  SAST, dependency and IaC tools; repo at 018e7b6. 4 results; 48 score bands, 30 scored.
- **D17 `.editorconfig:37` "CA2007 switched off with no recorded reason" — false-positive.** The reason is on line 36
  (tests must not use ConfigureAwait(false), xUnit1030) and `src/.editorconfig` turns the rule back on for every
  production file. Code kept; **key change:** promoted to trap TRP-001 (`suppressed-diagnostic`, must-not-fire).
- **D36 "dependency advisory scan runs only on code events" — valid.** Without a scheduled run, an advisory
  published against an unchanged lock file goes unnoticed until the next push. **Fixed:** weekly `schedule:` on CI.
- **D5 "Application: zone of pain" — opinion-not-fact.** The main-sequence distance is a heuristic; a concrete
  application layer depended on by the API and infrastructure projects is the intended shape of a layered service
  of this size. Recorded as noise; code kept.
- **P6 "changelog thin: 2 versioned entries, bar 3" — opinion-not-fact.** The service has had two releases;
  inventing a third to clear a bar would falsify the history. Recorded as noise.
- **Score band out:** BND-023 architecture documentation [80,100], scored 70 — the scanner wants 8 ADRs for its top
  tier; this repository records its 3 real decisions. Band kept (set before the scan; moving it now would select on
  the outcome); recorded as out-of-band, opinion-not-fact.
- Unscored bands: the model-judged rows need the scanner's LLM pass (run separately); knowledge concentration /
  freshness abstain on a single-author history; outbound resilience, rollback, DR, lock-file and CI-gate rows emit
  no score on this repository.

## 2026-10-07 — scan iteration 2 (contained + model-judged pass)

- Contained pass at 32629ea: 3 results — D5 and P6 (unchanged code, judged in iteration 1: noise) and D17 on
  `.editorconfig:37`, which now lands on trap TRP-001 as intended (the scanner's false positive, recorded).
  The D36 finding is gone after the scheduled CI run.
- Model-judged pass (host mode, the scanner's LLM evaluators): D19 90, D20 100, D21 100, D25 100, M4 100 — all in
  band. One finding: **D19 "no contributor guidance" — valid** (a public MIT repository says nothing about how to
  contribute). **Fixed:** a short Contributing section in README.md. D22/D24 and several posture rows emit no
  score here: recorded as unscored, bands kept.
- BND-023 (architecture documentation, scored 70 against [80,100]) stays out of band — see iteration 1.

## 2026-10-07 — scan iteration 3 (final) and freeze

- Contained pass at f55612f: the same 3 results as iteration 2 (D5, P6 noise; D17 on trap TRP-001). Nothing new.
- Model-judged pass: D19 90, D20 100, D21 100, D25 100, M4 90 — in band. The D19 contributor finding is gone.
  New: **M4 "README/code drift: lists only 3 main projects, evidence shows 5" — false-positive.** The Architecture
  section describes the three production projects; the two test projects are described under Testing. The scanner
  itself marks the row as an unverified model reading. It did not appear in the identical pass of iteration 2, so it
  is also evidence that the model-judged rows are not deterministic between runs. Recorded as noise; code kept.
- Converged: certified clean apart from recorded scanner noise; one score band out (BND-023, opinion-not-fact).
  Frozen as v1.0.0 with this entry.
