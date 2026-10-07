# Authoring journal — bench-csharp-domain-events

## 2026-10-07 — answer key v1.0.0 drafted (key first)

- Wrote `benchmark/answer-key.json` before any code: 24 `must-fire` (DM1-001 … ES3-001), 23 `must-not-fire`
  (TRP-001 … TRP-023), 7 `score-band` (BND-001 … BND-007). Schema 1.2.
- Coverage follows the matrix rows that name this repository: DM1–DM7, DM9–DM12, ED1–ED4, ES1, ES2 as plants + traps
  + clean; C3, C4, C5, LA1, DM8, ED5, ES3 as score bands. DM8 (primitive obsession), ED5 (non-idempotent handler) and
  ES3 (personal data in the event store) additionally get a located plant (and ED5/ES3 a trap), because each is a real
  site a reviewer would point at — a band alone cannot say whether a scanner found the right site.
- `lines` are placeholders: the key is generated from a site list with line markers, resolved against the code once
  it exists. `clean` entries are added in step 2, one per tracked file, for the twenty theme concepts minus those with
  an entry in that file.
- Decisions recorded in `benchmark/README.md` ("Contested truths"). Concepts the brief mentions that have no
  taxonomy id (value-object equality, unhandled domain event, unversioned event-schema change) are not labelled.
- Validated with `python3 -m cai_bench validate` against `taxonomy.json`: OK.

## 2026-10-07 — implemented; key lines fixed to the code

- Implemented the solution: 10 `src/` projects (shared kernel, messaging, contracts, Lending domain/application/EF Core
  infrastructure, Billing domain/application/in-memory infrastructure, worker host) and 3 test projects (xUnit v3):
  `dotnet build Rentals.slnx -c Release` 0 warnings, `dotnet test` 50 Lending + 20 Billing + 2 end-to-end tests pass.
  No test touches a network or a broker (SQLite in memory, in-process bus, stubbed catalogue HTTP handler).
- Analyzer configuration (not suppressions at a site): CA1711 off in `src/.editorconfig` (the `IIntegrationEventHandler`
  contract name), CA1707 off in `tests/.editorconfig` (sentence-style test names). The event-sourced base's fold is
  `Apply(IDomainEvent)` (CA1716 rejects a virtual `When`).
- Key changes against the draft, each because the code made the site more precise (none weakens a plant):
  - DM1-001: the borrower is a nullable backing field plus a throwing getter (`private Member? _borrower` /
    `public Member Borrower`), the only way to keep the EF navigation without the null-forgiving operator; the entry
    spans both members.
  - ED5-001 / TRP-019: the account repository's write is `SaveAsync(account, receipt)` (the inbox receipt is appended
    atomically with the events); the entries end at that call.
  - TRP-004 moved from `EquipmentDamageReportedIntegrationEvent.ReplacementValue` to
    `LoanReturnedIntegrationEvent.DailyRate` (same idiom, `Money` from the shared kernel): the validator rejected the
    first site because it is within the line tolerance of DM3-001 on the same concept.
  - Rationales of DM4-001, DM7-001, TRP-008 and TRP-023 reworded to the code as written.
- `clean` entries generated from the tree: 194 files (every tracked file outside `benchmark/` and `LICENSE`), each for
  the twenty theme concepts minus those with a plant or trap in that file.
- All `lines` resolved from the final tree by unique markers and checked with `sed -n`; the key validates.

## 2026-10-07 — scan iteration 1 (contained), judged

- Scanner: the reference scanner at engine commit 6a05dfb6c (rubric-2026.10.1), contained mode, over repository
  commit 9d844ca (report.sarif sha256 cf5a625cdb92ed22…). 109 results; harness: **22 TP, 2 FN**, 2 traps caught, 10 clean-region
  FP, 1 redundant, 73 uncovered (57 of them one boundary rule). Score bands: C3 90 (BND-001 **out**), C5 40 (in),
  DM8 92 (in), ED5 59 (in), ES3 55 (in); C4 and LA1 unscored.
- **False negatives (2), plants re-verified, kept:**
  - DM6-001 — `[Index(nameof(Email), IsUnique = true)]` on `Member` is EF Core's attribute
    (`Microsoft.EntityFrameworkCore.IndexAttribute`): the domain project references the ORM for a mapping concern. The
    scanner reads member signatures and bodies, not attributes.
  - ES1-002 — `AccountBalanceProjection.When(PaymentReceived)` stores `DateTimeOffset.UtcNow`; the scanner checks folds
    on aggregates only, not projection folds.
- **Traps caught (2) — scanner false positives, code kept:**
  - TRP-002 (DM1 on `Equipment._units` and `Equipment.Units`, two rows): "references the aggregate root
    `EquipmentUnit`". Disproof: `EquipmentUnit` has no repository, is created only by `Equipment.AddUnit` and reached
    only through `Equipment`; the scanner's structural rule takes any entity with a private setter and a mutating
    method in a `Domain` namespace for a root.
  - TRP-022 (ES3 on `MemberRegisteredIntegrationEvent`): "Persisted event carries personal data". Disproof: an
    integration message, written to `outbox_messages` only until `OutboxDispatcher.DispatchPendingAsync` removes the
    row after publishing; nothing appends it to an event store.
- **Unexpected results on theme concepts, judged:**
  - ED5, 9 offline "Possible non-idempotent mutation (review)" notes on clean files — **false-positive** each: every
    one has a guard the heuristic does not recognise: `ChargeExtensionFeeHandler` (`HasChargeWithReason`),
    `MarkUnitDamagedHandler` (sets a fixed condition), `RecordMaintenanceHandler` / `RegisterEquipmentHandler`
    (caller-chosen id, `GetAsync` existence check), `CheckoutEquipmentHandler` (a unit on loan is refused),
    `ExtendLoanHandler` (extending to the current due date is a no-op), `RegisterMemberHandler` (lookup by e-mail +
    `HasSameContact`), `SuspendMemberHandler` (early return when already suspended),
    `OutboxDispatcher.ExecuteAsync` (republishes with the outbox row id as message id; consumers de-duplicate). The
    model-judged pass is what should clear them. Recorded as noise; files stay certified clean.
  - DM8 `MemberAccount.cs:35` "[currency, holderemail, holderfullname] across 3 signatures" — **valid** (uncertain →
    valid): holder name and e-mail did travel together through `Open` and the event. Two of the three signatures are
    the event's positional declaration and its compiler-generated `Deconstruct`, but the authored factory still
    took the pair loose. **Fixed** (0a53bb6): `MemberAccount.Open` takes an `AccountHolder` value object; the
    event stays flat (a serialised contract), so ES3-001 is unchanged.
- **Uncovered results (outside the theme), judged:**
  - D4 duplicated 10-line block in `LoanOpenedHandler` / `LoanReturnedHandler` — **valid**: the account lookup and
    its error were repeated in five consumers. **Fixed** (94f0c50): one `LoadRequiredAsync` extension.
  - D8 low coverage of `NotFoundException`, `DomainRuleViolationException`, `Entity`, `ConcurrencyException` —
    **valid**: the not-found path, entity equality and the event store's concurrency and inbox checks were untested.
    **Fixed** (94f0c50, e3313f3): tests for each behaviour.
  - D8 "No test project references Rentals.Worker" — **valid**: the composition root was untested. **Fixed**
    (908424c): wiring moved to `RentalsHost.AddRentals`, tested end to end (resolves both contexts, migrates).
  - P8 `Program.cs:26` "Schema created with EnsureCreated() (no migration path)" — **valid**. **Fixed** (908424c):
    EF Core migrations, applied by the worker at start-up; the tests migrate too.
  - P2 "Only 3/7 service-like projects use logging" — **valid** for the two application projects (handlers made
    decisions silently). **Fixed** (94f0c50, e3313f3): structured logging of redeliveries, charges, suspensions,
    checkouts and returns. Domain projects stay free of logging by design.
  - C1 "No data-protection/encryption" — **valid**, deliberate (personal data at rest is unencrypted; out of theme).
    **Key change:** score band BND-008 (`data-encryption-controls`, 0–40) records the posture, as in the injection
    repository.
  - C3 "Partial audit-trail evidence … missing: a dedicated audit read-model", score 90 (BND-001 out of band) —
    **opinion-not-fact**: the score credits Billing's event stream as an audit trail of sensitive data, but the
    sensitive data is the member's personal data in Lending, whose changes (contact details, suspension, erasure) are
    recorded nowhere, and no event records who made a change. Band kept (set before the scan).
  - C5 "erasure ✓ · export ✗ · consent ✗" — **valid**, deliberate: it is the posture BND-003 records (in band).
  - D23, 57 "Cross-context type …" rows — 56 **false-positive**: they treat each project as a context (`MemberId`
    from `Rentals.Lending.Domain` used by `Rentals.Lending.Application` is one context's domain used by its own
    application layer; the integration events in `Rentals.Contracts` and `MessageContext` in `Rentals.Messaging`
    are the published contract and the shared messaging kernel). 1 **valid**: `ExtendLoanCommand`
    (`Rentals.Lending.Application → Rentals.Billing.Application`) is the coupling behind the planted ED2-001; kept.
  - D17 `.editorconfig:37` (CA2007) and `tests/.editorconfig:3` (CA1707) "the only record that it was ever switched
    off is this line" — **false-positive**: the line above each states the reason (and `src/.editorconfig`
    re-enables CA2007 for production code). Same finding as in the baseline repository; out of theme, not labelled.
  - D18 thin `Rentals.Worker` project — **opinion-not-fact**: a host's composition root is thin by design.
  - D39 EF `Configure` methods over the IL budget — **opinion-not-fact**: model configuration runs once at start-up.
  - P6 changelog has one release — **opinion-not-fact**: a 1.0.0 project has one release.
- Repository commits after this scan: 0a53bb6, 94f0c50, e3313f3, 908424c. All tests pass (53 + 23 + 3).
- Key change: BND-008 added; lines re-resolved for the edited handlers; clean entries for the new files (migrations,
  `AccountHolder`, the lookup extension, `RentalsHost`, new tests).

## 2026-10-07 — scan iteration 2 (contained + model-judged host pass), judged

- Contained scan over d897c9a (report.sarif sha256 e484dfe5bdd4aaa6…): 113 results; **22 TP, 2 FN** (DM6-001, ES1-002, as
  before), 2 traps caught (TRP-002, TRP-022, as before), 1 redundant. Model-judged host pass (`--host --with-llm`,
  sha256 e3a468f7116eb3ef…): same 22 TP / 2 FN; ED5's nine offline notes on clean handlers are cleared by the model except one.
  Score bands (contained / model pass): C3 90/90 (BND-001 out), C5 40 (in), C1 0 (BND-008 in), DM8 96 (BND-005
  **out**, by one point), ED5 59 / 92 (BND-006 in / **out**), ES3 55 / 50 (in); C4 and LA1 unscored (LA1 builds no
  card unless a GDPR framework is configured).
- Iteration 1's fixes took: D4, P8, D8 (Worker, entity equality, event store) and the Billing DM8 clump are gone.
- **New results, judged:**
  - DM7, 4 rows "Repository for a non-root entity: EquipmentRepository → Equipment" (and `LoanRepository`,
    `MemberRepository`, `ReservationRepository`), in both passes — **false-positive**: `Equipment`, `Loan`,
    `Member` and `Reservation` all derive from `AggregateRoot<TId>` (e.g. `Member.cs:8`) and are the roots
    their repositories serve. The rows did not appear in iteration 1, whose only change in these files' project was
    the EF Core design-time package and migrations: the scanner's root set apparently stops recognising the roots as
    seen from the infrastructure project. Recorded as noise; files stay certified clean.
  - ED5 (model pass) `CheckoutEquipmentHandler.HandleAsync` "Non-idempotent mutation … the model confirms a re-run
    would double-apply it" — **valid** (uncertain → valid): a sequential retry is refused (the unit is on loan), but
    two concurrent deliveries could both pass the availability check before either commits — nothing in the
    database prevented two open loans for one unit. **Fixed** (610d7d9): a unique filtered index (one open loan per
    unit) in a new migration, with a test that falsifies without it.
  - ES3 (model pass) second row on `MemberAccountOpened` (`HolderFullName`, adjudicated as a real person) —
    **redundant** with ES3-001 (same event, same remedy). Second row on `MemberRegisteredIntegrationEvent`
    (`FullName`) — the TRP-022 false positive again.
  - D17 `DesignTimeLendingDbContextFactory.cs:7` "Dead code" — **false-positive**: `dotnet ef` discovers and
    instantiates `IDesignTimeDbContextFactory<T>` implementations by reflection (the message itself names reflection
    as a blind spot).
  - D8 `NotFoundException` 40 %, `ConcurrencyException` 33 % — **opinion-not-fact**: the uncovered lines are the
    standard exception constructors CA1032 requires; every path that throws them is tested.
  - P2 "Only 5/7 service-like projects use logging" — **opinion-not-fact**: the two without logging are the domain
    projects, which deliberately have no infrastructure dependencies.
  - D19 (model) "README lacks a section or guidance for contributors" — **false-positive**: `README.md` has a
    `## Contributing` section. D20 (model) "ADR 0003 consequences incomplete" — **opinion-not-fact**.
  - Recurring rows judged in iteration 1 (D23, D17 editorconfig, D18, D39, P6, C1, C3, C5, ED5 offline notes on
    clean handlers, TRP-002, TRP-022) are unchanged at unchanged sites and keep their verdicts.
- Score bands out: BND-001 (C3 90), BND-005 (DM8 96), BND-006 (ED5 92 in the model pass). All three bands were set
  before the first scan and are kept; see the results file for the reasoning.

## 2026-10-07 — scan iteration 3 (contained + model-judged host pass), converged; freeze v1.0.0

- Contained scan over eb05046 (sha256 c60aa0f38b0990e3…) and model-judged host pass (sha256 e6cd818267dcbcbd…): **22 TP, 2 FN**
  (DM6-001, ES1-002), 2 traps caught (TRP-002, TRP-022) — identical to iteration 2 on every theme concept. The
  deterministic results differ from iteration 2 only in the site the D39 roll-up names (the new index made
  `LoanConfiguration.Configure` the largest method).
- The model pass still confirms `CheckoutEquipmentHandler` as non-idempotent — now a **false-positive**: the unique
  filtered index `ux_loans_open_unit` (610d7d9) makes a second open loan for a unit impossible to commit, and a
  sequential retry is refused by the availability check. The model-judged pass is not reproducible run to run: D19
  became "the Contributing section lacks guidance" (**opinion-not-fact**; the section exists and says how to
  contribute) and a new M4 row "README mentions a separate HTTP front end deployable not part of this repository"
  appeared (**false-positive**: the README states exactly that, and the code has no HTTP entry point — there is no
  drift).
- Every unexpected result is now recorded noise or a deliberate posture; the key matches the code. Score bands: in —
  BND-003 (C5 40), BND-007 (ES3 55/50), BND-008 (C1 0), BND-006 contained (ED5 59); out — BND-001 (C3 90), BND-005
  (DM8 96), BND-006 model pass (ED5 92); unscored — BND-002 (C4), BND-004 (LA1). Bands were set before the first
  scan and are not moved.
- Frozen: code and key tagged `v1.0.0`.

## 2026-10-07 — v1.1.0: three new concepts (key change after freeze = new version)

- The harness (contract 1.4) added `value-object-mutability`, `domain-event-never-handled` and
  `event-schema-change-without-upcaster`, which v1.0.0 could not label. v1.0.0 stays where it is; this is v1.1.0
  (`keyVersion` "1.1.0", schemaVersion stays "1.2", the highest the validator accepts).
- Code, as realistic commits in the existing contexts (tests green: 31 Billing, 56 Lending, 3 end-to-end):
  - 9fe669e — Billing's in-memory event store keeps events serialised (type name, schema version, JSON) through a
    new `AccountEventSerializer`, as a durable store does; ADR 0003 updated; round-trip tests per event type.
  - 0afd2db — `LateFeeCharged` stores the daily fee (schema v2) with a v1→v2 upcaster and a test on a literal v1
    event: the **trap** TRP-026.
  - 96f0557 — `DepositHeld.Loan` renamed to `LoanId` with no version bump and no upcaster: the **plant** ESC-001
    (stored v1 JSON has `loan`; replay gives an empty loan id).
  - 0e62ec7 — equipment storage locations: `StorageLocation` (class, public setters, reference equality used by
    `Equipment.Relocate`) is the **plant** VOM-001; `EquipmentRelocated` (raised, never handled) is the **plant**
    DEH-001; EF owned-type mapping, migration `EquipmentStorageLocation`, `RelocateEquipmentCommand`/handler, tests.
  - 1d1a8ec — CHANGELOG 1.1.0, version 1.1.0.
- Key changes: + VOM-001, DEH-001, DEH-002, ESC-001 (must-fire); + TRP-024 (`LoanReference`, readonly record struct
  as a dictionary key), TRP-025 (`ChargePosted`, handled by the fold and the projection), TRP-026 (must-not-fire).
  **DEH-002 labels a v1.0.0 site**: `MemberPersonalDataErased` was already raised and never handled; with the concept
  in the taxonomy, a truthful key has to say so (the v1.0.0 README noted only the `ExtendLoanEvent` case). Clean
  entries gain the three concepts, except the raise/consequence sites listed in the README ("not certified either
  way"); clean entries CLN-207 … CLN-215 for the new files. Lines of TRP-002 and TRP-008 moved (Equipment gained
  `Location`); every other entry's site is unchanged (re-mapped by diff against v1.0.0 and checked with `sed -n`).
  Validated: OK, 277 entries.
- The reference mapping lists all three concepts as `unmapped` (no rule): the four plants are expected FNs and the
  three traps TNs for it; the rescan checks whether any existing rule (DM5, ES2, ED*, D*) reacts to the new code.

## 2026-10-07 — v1.1.0 (cont.): boundary-type-leakage (coordinator request, before the tag)

- The coverage matrix showed `boundary-type-leakage` (the reference scanner's D23) covered by no entry here. Read the
  detector first (`BoundaryCouplingDetector` / `BoundaryCouplingAnalyzer`, read only): with no declared contexts it
  attributes types to contexts per .NET project (assembly), which is why v1.0.0's scans already carried 57 D23 rows
  (`Rentals.Lending.Domain → Rentals.Lending.Application` etc.). No scanner configuration added.
- Code: da7f315 — ADR 0005 (what may cross the boundary: shared kernel and contracts yes, a context's own types no);
  e813432 — Billing's `AccountBalanceQuery` takes Lending's `MemberId` instead of a `Guid` (the **plant** BTL-001).
- Key: + BTL-001; + BTL-002 on `ChargeExtensionFeeHandler` (Lending's `ExtendLoanCommand` in Billing's public
  surface — a v1.0.0 site judged valid in iteration 1, now labelled because the key covers the concept); + TRP-027
  (`Money` in Billing's public surface, the shared kernel). Clean entries gain the concept except the four files listed
  in the README; CLN-216 for ADR 0005. Validated: OK, 281 entries.
- Known before the scan: D23 rows carry no SARIF location (the site is only in prose: type, contexts, member), so they
  can match only repository-level entries; located plants cannot be TPs for this scanner under contract 1.4.

## 2026-10-07 — v1.1.0 scans (contained + model-judged host pass), judged; freeze v1.1.0

- Scanner: the reference scanner at engine commit 6a05dfb6c (rubric-2026.10.1), as for v1.0.0.
- v1.1 iteration 1, contained, over 1caabf6 (sha256 c5970b7d47a73cea…; key at 1caabf6): 22 TP, 6 FN, traps 24/26
  TN. v1.1 iteration 2, contained, over 0adc685 (sha256 bfe038c8e5dd63fc…): **22 TP, 8 FN**, traps 25/27 TN (TRP-002,
  TRP-022 caught, as in v1.0.0); model-judged host pass over 0adc685 (sha256 c03e4cf2e13d47bf…): same 22 TP / 8 FN /
  25 TN. Score bands: same outcomes as v1.0.0 on both passes.
- New entries: VOM-001, DEH-001, DEH-002, ESC-001 are FNs (their concepts are `unmapped` for this scanner — no rule);
  BTL-001 and BTL-002 are FNs although D23 **reports both defects in prose** (`MemberId (Rentals.Lending.Domain →
  Rentals.Billing.Application) … via AccountBalanceQuery.MemberId`; `ExtendLoanCommand … via
  ChargeExtensionFeeHandler.HandleAsync`): D23 rows carry no SARIF location, so they cannot match a located entry.
  TRP-024 … TRP-027 are TNs. No existing rule (DM5, ES2, ED5, DM*, D*) fired on `StorageLocation`, `EquipmentRelocated`,
  `DepositHeld`, `LateFeeCharged`, the serializer or the relocation handler.
- New results vs v1.0.0's final scans, judged (no `valid` accidental defect, so no code change):
  - D23 ×3 `MemberId … via AccountBalanceQuery..ctor / .MemberId / .Deconstruct` (both passes) — **valid**: the
    BTL-001 plant (one logical finding, three members; unmatched for lack of a location).
  - D23 ×3 `EquipmentId (Rentals.Lending.Domain → Rentals.Lending.Application) … via RelocateEquipmentCommand…` —
    **false-positive**: one context's domain id in the same context's application command; D23 takes each project for
    a context (the same verdict as the 56 such rows of v1.0.0).
  - Model pass: D19 "README lacks a section or guidance for contributors" — **false-positive** (`## Contributing`
    exists); D20 ADR 0003 "Consequences incomplete" (reworded after the ADR's Decision gained a sentence) —
    **opinion-not-fact**; M4 "README claims the HTTP front end is a separate deployable … but a `benchmark/` directory
    exists" — **false-positive**: `benchmark/` holds this repository's answer key, not a front end.
  - Gone: P6 "changelog thin" (the changelog now has two releases).
- With `boundary-type-leakage` covered, v1.0.0's location-less D23 rows (previously `uncovered`) now count as noise;
  their verdicts are unchanged.
- Frozen: code and key tagged `v1.1.0`.

## 2026-10-07 — v1.1.1: the boundary-leak plants name their type (contract 1.2 subject)

- The reference scanner reports boundary type leakage per exposed member and without a SARIF location; its message
  names the leaking type and the member it is exposed through. Under contract 1.4 a location-less result can only
  match a located entry through `subject`, so v1.1.0 scored BTL-001 and BTL-002 as misses although the scanner named
  both. **Key change:** BTL-001 `subject: AccountBalanceQuery`, BTL-002 `subject: ChargeExtensionFeeHandler.HandleAsync`
  (the whole-token forms the scanner prints). Code, ids, labels and lines unchanged.
- Two further rows restate BTL-001 through other members (`AccountBalanceQuery.MemberId`, `.Deconstruct`); the
  whole-token rule cannot tie them to the subject, so the harness counts them as noise. The judge's verdict is
  `redundant` (same defect); the results file records that.
