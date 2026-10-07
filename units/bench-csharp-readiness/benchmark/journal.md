# Authoring journal — bench-csharp-readiness

## 2026-10-07 — answer key v1.0.0 draft written (key first)

- Matrix rows assigned to this repository (`scanner-benchmark/coverage/matrix.json`): D37, LA3, P1, P2, P3, P6, P7,
  P8, P10, PF1 — all `score-band`. Each has a band (BND-001..010) set from intent.
- The theme also asks for concrete readiness defects at sites. Twelve plants (RDY-001..012) use existing taxonomy
  concepts, the most precise one true of each site; no new concept was needed. Posture concepts carry located plants
  where the defect has one site a reviewer would point at (README, "Labels are about truth"). RDY-009 (coverage
  collected, not gated) is repository-level: CI-gate posture is a pipeline property.
- Sixteen traps (TRP-001..016). Extra bands for concepts the plants dent although the matrix assigns them elsewhere
  (deployment, DR, CI-gate honesty, authorization, headers, HTTPS, validation, lock files: BND-011..018), so the
  posture reading of each plant is recorded.
- Decision: Dockerfiles + Kubernetes manifests + release/deploy workflows are PRESENT (README, "Deployment shape").
- Decision: a packable client library (`ParcelTracking.Client`) exists so library versioning (P10) has something to
  measure, and it doubles as the resilience-left-to-the-host trap (TRP-004).
- `clean` over every planned file; plant files are clean for every concept except their planted ones (the validator
  forbids a region that is both `*`-clean and planted). Lines are planned values, to be fixed to the final code.
- Validated with `python3 -m cai_bench validate` (harness 1.3): OK, 169 entries.

## 2026-10-07 — implementation complete; key fixed to the code

- Implemented as planned: six production projects (Core, Infrastructure, Api, Worker, Client, Cli) plus a
  BenchmarkDotNet project; 54 production C# files. Build green with warnings as errors; 78 tests green (59 unit,
  19 integration; WebApplicationFactory over in-memory SQLite, in-process token issuer, fake carrier; no network).
  `dotnet list package --vulnerable --include-transitive`, `--deprecated` and `--outdated`: nothing reported.
- Key change: every `lines` entry set to the final code (checked with a line dump). The migration files carry the
  timestamp `dotnet ef` generated (`20261007085056_InitialCreate`), not the planned one.
- Key change: `clean` list regenerated from `git ls-files` (every tracked file). Differences from the plan:
  `.config/dotnet-tools.json` (pins dotnet-ef 10.0.12 for the release workflow), `Hosting/SecurityTxt.cs`,
  `Security/ScopeClaims.cs`, `Security/JwtAuthenticationOptions.cs`, `Security/ConfigureJwtBearerOptions.cs`,
  `Tracking/DeliveryPerformance.cs`, `Tracking/RedirectOutcome.cs`, `Worker/HeartbeatPublisher.cs`, the test files;
  no `src/.editorconfig`, no `Worker/Heartbeat.cs`.
- Key change: new trap TRP-017 (the readiness probe's carrier client: 2 s timeout, no retry by design). Reason: it
  is a second outbound client without a resilience handler and is correct; written down while implementing it.
- Rationale sharpened for RDY-004 (the unused `reports:read` policy), RDY-005 (with nullable on, ASP.NET Core adds
  an implicit Required to a non-nullable string, so only absence is rejected) and RDY-007 (the worker's heartbeat
  follows the same lie).
- `.editorconfig` marks `**/Migrations/*.cs` as `generated_code = true` (style rules otherwise fail the build on the
  EF Core output); it suppresses no analyzer.

## 2026-10-07 — scan iteration 1 (contained)

- Scanner: the reference scanner at the pinned instrument (kennel main 6a05dfb6c, rubric-2026.10.1), contained mode;
  repo at 1d91a2a. 29 results. Harness: recall 3/12 (RDY-007 X3 empty catch, RDY-009 P12 coverage not gated —
  location-less, matched by the repository-level plant — RDY-012 X4 interpolated log), trap resistance 16/17
  (TRP-014 caught: DS-0026 HEALTHCHECK on the Kubernetes-only API image).
- **Missed plants, all re-verified as real and correctly placed:**
  - RDY-001 (webhook client without resilience) and RDY-008 (EnsureCreated in the worker): P7 and P8 are
    repository-wide switches — any `AddStandardResilienceHandler` credits every client, and EnsureCreated is
    ignored once a `Migrations/` directory exists. P7 and P8 both scored 100.
  - RDY-002 (token accepted, not forwarded): X2 checks only whether a token *parameter* exists.
  - RDY-003 (no graceful shutdown): X2 found `PollOnceAsync` (scorecard row at TrackingPoller.cs:26, Info level),
    but that row is not in the SARIF; only the location-less summary "Only 26/27 async methods accept a
    CancellationToken" is, and it cannot be matched to a site. Recorded as a detection without a location.
  - RDY-004 (anonymous controller): C2 counts `[Authorize]` attributes repo-wide; it never itemises endpoints.
  - RDY-005, RDY-010, RDY-011 (validation, headers branch, HSTS never applied): S1 is word-matching
    (`[ApiController]`, a header-name literal, `AddHsts`) and scored 100.
  - RDY-006 (Console in the worker): P2 neither credits nor flags Console output, and the worker logs elsewhere.
- **Valid → repository fixed:** D8 low coverage on DesignTimeDbContextFactory, NotificationDispatcher and
  TrackingPoller (tests added); CKV_K8S_40 / KSV-0020 UID 1654 (UID 10001 in manifests and Dockerfiles); D36 no
  signing and no checksums for release files (SHA256SUMS + build-provenance attestation, verified by the deploy
  workflow); C4 partial data retention (RetentionSweeper: 30 days for delivered notifications, 180 days for
  completed parcels; docs/operations/data-retention.md).
- **Noise, code kept:** D5 Core "zone of pain" (opinion); CKV_K8S_35 secrets as env vars (opinion → traps
  TRP-023/024, labelled at the secretKeyRef lines although the scanner reports the resource start); KSV-0125
  untrusted registry (opinion → traps TRP-021/022); D41 no AppArmor (opinion); D42 no runtime threat detection
  (shape-irrelevant); C1 no encryption API (opinion; the row itself says to ignore delegated encryption at rest);
  ED5 non-idempotent dispatcher (false positive: DeliveredAt guard + EventId → trap TRP-018); P10 large public
  surface 55/64 (false positive: counted over unpublished service projects → repository-level trap TRP-019);
  P2 logging not universal (opinion); PF2 no allocation-aware APIs (opinion); PF3 ConfigureAwait (shape-irrelevant:
  application code without a SynchronizationContext; the library uses it everywhere → repository-level trap
  TRP-020); D17 empty catch (redundant with the X3 hit on RDY-007).
- Key changes: traps TRP-018..024 (above); RDY-008 lines moved to 43-47 (the worker's Program.cs gained the
  retention registration above it). Bands unchanged. Out of band: P6 100 (thin changelog not penalised), P7 100,
  P8 100, C2 100, S1 100 (posture switched on by one good site), P5 40 (runbook with RTO/RPO, restore and drills
  not credited as DR documentation).

## 2026-10-07 — scan iteration 2 (contained + model-judged host pass)

- Contained pass at fc5b2db: 20 results. Recall unchanged (3/12: RDY-007, RDY-009, RDY-012); the D8, D36 and UID
  rows are gone after the iteration-1 fixes. Traps now catch the scanner's iteration-1 opinions as intended:
  TRP-014, TRP-018, TRP-019, TRP-020, TRP-021, TRP-022 (trap resistance 18/24). CKV_K8S_35 still lands on the
  resource start (api.yaml:8, worker.yaml:9), outside TRP-023/024 at the secretKeyRef lines: recorded as a
  clean-region false positive with imprecise location.
- **C4 "missing a scheduled purge / cleanup job" — false-positive.** RetentionSweeper is exactly that (a
  PeriodicTimer BackgroundService calling PurgeAsync); the row wants the job by name. Code kept; repository-level
  trap TRP-025.
- Model-judged host pass (same commit): D19 90, D20 93, D21 100, D22 100, D24 100, D25 100, M4 80; LA3 (disclosure
  policy quality) emits no score in this pass — BND-010 stays unscored.
  - D19 "no contributor guidance" — false-positive (README has a Contributing section).
  - D20 ADR 0003 consequences one-sided — valid; consequences expanded (latency, upstream load, tuning).
  - M4 "README lists src/ParcelTracking.Benchmarks" — false-positive (it lists benchmarks/…, where it is).
  - M4 "README omits the test projects" — valid (minor); the Testing section now names them.
- All other rows unchanged from iteration 1 and judged there.

## 2026-10-07 — scan iteration 3 (final) and freeze

- Contained pass at a8c3f3f: byte-identical SARIF to iteration 2 (only docs changed). 20 results; with TRP-025 the
  C4 row now lands on its trap. Recall 3/12, trap resistance 18/25, every other result recorded noise judged in
  iterations 1–2.
- Model-judged host pass: the D20 and both M4 rows are gone after the iteration-2 fixes; D19 "no contributor
  guidance" repeats (false-positive, judged in iteration 2). LA3 again emits no score: BND-010 unscored.
- Converged: the repository holds its twelve plants, its 25 traps and recorded scanner noise only. Score bands out:
  P6, P7, P8, C2, S1 (×3) at 100 and P5 at 40 — each kept as set before the first scan.
- Frozen as v1.0.0 with this entry.
