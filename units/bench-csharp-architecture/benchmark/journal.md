# Authoring journal — bench-csharp-architecture

## 2026-10-07 — answer key v1.0.0 drafted (key first)

- Wrote `benchmark/answer-key.json` before any code: 17 `must-fire`, 20 `must-not-fire`, 1 `not-applicable`,
  8 `score-band`, plus `clean` entries for the planned files (completed for every tracked file once implemented).
  Lines are planned positions; they are fixed to the final code in step 2. Validated against `taxonomy.json`: OK.
- Decisions (see `benchmark/README.md`):
  - The cycle plant is namespace-level: a project-reference cycle cannot exist in a .NET solution that builds.
  - The architecture tests pass; the plants are the rules the team wrote down and did not encode (a stale assembly
    list, a missing test file, a prose-only rule).
  - "Business logic in a controller" is labelled with the metric concept `business-logic-share`; taxonomy 1.0 has no
    finding-level concept for it (to report).
  - `boundary-type-leakage` is not applicable: one bounded context.
  - Project-level entries carry the project name as `subject` and the `.csproj` as `file`.
  - Clean regions list the architecture concepts, not `"*"`.

## 2026-10-07 — implemented; key lines fixed to the code

- Implemented FleetOps: ten production projects (Contracts, Domain, Application, Infrastructure,
  Infrastructure.Telematics, Api, Worker, ServiceDefaults, Diagnostics, empty Notifications) and three test projects.
  2943 non-blank production C# lines, 751 test lines. `dotnet build -c Release`: 0 warnings (warnings as errors,
  latest-recommended analyzers). `dotnet test`: 57/57 passed (38 application/domain/infrastructure, 13 API
  integration, 6 architecture). No vulnerable packages (direct or transitive).
- Mediator (source-generated, MIT) carries the CQRS handlers: a real vertical-slice choice that also makes the slices
  recognisable as such.
- FleetOps.Infrastructure has 126 public types in 14 namespaces (the god module).
- Martin metrics over production edges (tests excluded as Martin intends): Diagnostics Ce 5, Ca 1, I = 0.83;
  ServiceDefaults Ce 1, Ca 2, I = 0.33; Contracts I = 0. The architecture test project references every production
  project with code (TRP-004), so a tool that counts test projects as dependents sees Diagnostics at 5/7 = 0.71.
- Key changes against the draft, each because the code made the site more precise:
  - CAP-001: the singleton is registered through a generic factory (`AddSingleton<FuelPriceCache>(sp => …)`); a
    plain registration makes the host refuse to start in Development (scope validation), which no team would ship.
    Rationale updated.
  - TRP-015: the trap handler holds Domain-typed fields only (the clock moved out); rationale updated.
  - Line numbers fixed to the code; clean entries now cover all tracked files (226). ADR 0002 is left out of the
    `architecture-rules-unenforced` clean entry (deliberately unlabelled, see README).

## 2026-10-07 — scan iteration 1 (reference scanner, contained, rubric-2026.10.1)

SARIF sha256 `2666d0c2…7fc`. 65 results: 11 TP, 6 FN, 2 traps caught, 1 unmatched noise, 2 redundant, 49 uncovered.
Every non-hit was judged (five classes, METHOD.md §1.1); full verbatim list in the results file.

Found: LAY-001/002/003 and ARU-002 (ADR conformance and broken enforcement link), SHL-001, GOD-001, CAP-001,
SNG-001, FAT-001, SLC-001, CQS-001.

Missed (each plant re-verified as real and correctly placed — scanner false negatives, key unchanged):
- SDP-001: the project-level instability is computed with test projects counted as dependents; the architecture test
  project references Diagnostics, so 5/7 = 0.71 stays under the 0.8 gate. Over production edges it is 0.83.
- CYC-001: namespace cycle; only project-reference cycles are read.
- IND-001: no per-site pass-through detection; the indirection metric scored 92.
- BLC-001: no finding-level rule for business logic in a controller (the share metric scored 40, in band).
- DOM-001: the domain-infrastructure lens did not report the domain service calling the HTTP telematics client.
- ARU-001: an ADR declaring `enforcement: prose` for a checkable rule is not reported.

Traps caught: TRP-002 (Contracts "off the main sequence", opinion-not-fact — the row itself calls it healthy) and
TRP-011 (singleton holding a stateless transient, false-positive).

Valid findings — repository fixed:
- DM9 "approved" decided twice (ApprovedTotal != null in FleetReadModel and FleetReporting): removed the unused
  `SumApprovedSinceAsync` from the read model.
- X5 three `!` operators in production code: removed.
- IC1 placeholder `.invalid` e-mail defaults in options classes: no defaults, `[Required, EmailAddress]`, addresses in
  configuration (`mail.fleet.internal`).
- P8 `EnsureCreated`: EF Core migrations (`InitialSchema`), applied with `MigrateAsync`; migrations folder marked
  `generated_code` in `src/.editorconfig`.
- D8 30 files without coverage: 26 tests added (83 total). Writing them exposed a real bug — synchronous
  `SaveChanges` was not audited — fixed in the interceptor.

Noise (recorded, code kept): D18 thin-surface advice (opinion-not-fact; ServiceDefaults promoted to trap TRP-021),
D17 documented CA2007 suppression (false-positive; promoted to trap TRP-022), D39 IL size of the DI method
(opinion-not-fact), C1 at-rest encryption (shape-irrelevant), ED3 x7 DTOs read as events (false-positive; NA-002
added), ED5 idempotency of an HTTP command handler (shape-irrelevant), P2 logging in libraries (opinion-not-fact), P6
changelog of a first release (opinion-not-fact), PF1 benchmarks (shape-irrelevant).

Score bands: in — D18 82, D26 89, AX5 100, AX10 40, M3 100; out — D5 100 (band 40–90; the SDP miss above),
D27 92 (band 40–90); D22 unscored (offline pass).

Key changes: TRP-021, TRP-022, NA-002 added (reasons above); clean entries regenerated for the new files.

## 2026-10-07 — scan iteration 2

SARIF sha256 `d74afc95…4165`. 36 results: 11 TP, 6 FN (the same six, unchanged), 4 traps caught (TRP-002, TRP-011,
and the two promoted after scan 1, TRP-021 and TRP-022), NA-002 caught 7 times (the ED3 DTO rows), 2 redundant,
12 uncovered. The four repository fixes of iteration 1 removed their findings (DM9, IC1, P8, X5; D8 down from 30 to 5
files). Bands: D27 now 90 (in); D5 still 100 (out).

New findings:
- D11 flaky test (`ReadSideTests.FleetAndInspectionReports`, 2 passed / 1 failed): **valid** — the most-common-defects
  report ordered by count only, so ties came back in database order. Fixed in the report (tie broken by description).
- D8 five files at 33–40 % line coverage: **opinion-not-fact** — the uncovered lines are the standard exception
  constructors (CA1032); no behaviour, no risk. Recorded as noise.

Recurring rows on unchanged code were not judged again. No key change.

## 2026-10-07 — scan iteration 3 (converged) and model-judged pass; freeze v1.0.0

Iteration 3 (contained, commit df6ea32), SARIF sha256 `5a9dc115…0a5f`: identical outcome to iteration 2 except the
flaky-test row is gone — 11 TP, 6 FN, 4 traps caught, NA-002 x7, 2 redundant, 11 uncovered rows, every one of them
already judged as recorded noise. The key matches the code.

Model-judged pass (host, `--with-llm`, one run, SARIF `5e0383a2…`): D22 100 (BND-008 in band), D19 90, D20 96, D21 100,
D24 20, D25 100, M4 80. An extra D26 "Split FleetOps.Infrastructure" row is a second hit on GOD-001. New rows judged:
- D19 "no licence statement": false-positive (README ends with a License section).
- D24 x7 "redundant comment" on the section headings of the DI registration method: opinion-not-fact.
- ED5 non-idempotent (model-confirmed): shape-irrelevant, as in iteration 1.
- M4 "README omits several projects": opinion-not-fact (it points to docs/architecture.md, which lists them).
- M4 "README advertises Docker containerisation": false-positive (the README mentions C4 "containers"; no Docker).
One run only, so run-to-run variance of the model-judged rows is not measured here.

Final: recall 11/17, trap resistance 18/22, bands 7/8 in (D5 100 out, the consequence of the SDP-001 miss).
Frozen: code and key together as v1.0.0.

## 2026-10-07 — v1.1.0: BLC-001 relabelled to a finding concept (key only)

v1.0.0 stays where it is; a change after the freeze is a new version. Harness contract 1.4 added the finding concept
`business-logic-in-controller` (unmapped for the reference scanner: no rule of it locates rules in a controller).
Key v1.1.0 (`keyVersion` 1.1.0; `schemaVersion` stays 1.2, the highest the validator accepts — the answer-key format
did not change in 1.3/1.4):
- BLC-001: `business-logic-share` -> `business-logic-in-controller`, same file and lines (47–74, re-verified with
  `sed -n`: `Approve` through its closing brace). `business-logic-share` stays covered by BND-006 and the clean entries.
- 233 `clean` entries that certified `business-logic-share` now also certify `business-logic-in-controller`. Not
  CLN-028 (`WorkOrdersController.cs`, which holds the plant and never listed the metric) and not CLN-024
  (`FleetDashboardController.cs`, the LAY-003 site: its status counts are read composition, not a business rule, but
  it is left unlabelled for the new concept rather than certified).
No code change.

Rescan at c6a9e9e (key v1.1.0):
- Contained, SARIF sha256 `5a9dc115…0a5f` — byte-identical to v1.0.0 iteration 3 (no code changed). 11 TP, 6 FN, 4
  traps caught, NA-002 x7, 2 redundant, 11 uncovered. BLC-001 is still an FN, now reported under the scanner's
  `unmappedConcepts` (no rule of it maps `business-logic-in-controller`): the same miss, now attributed to the right
  concept. Bands unchanged (7/8 in; D5 100 out).
- Host pass with `--with-llm`, SARIF sha256 `e2f932fa…3dea`: same 45 rows as the v1.0.0 model-judged pass except one
  re-worded D19 "no licence statement" row at README.md:1 — the same claim, still false (README "## License" section,
  MIT, links LICENSE): false-positive, recorded as noise. BND-008 D22 100, in band. Same outcomes as v1.0.0.
No new or changed result needed a repository fix; nothing to promote. Freeze v1.1.0.
