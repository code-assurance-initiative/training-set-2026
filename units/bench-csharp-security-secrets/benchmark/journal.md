# Authoring journal — bench-csharp-security-secrets

## 2026-10-06 — answer key v1.0.0 drafted (key first)

- Wrote `benchmark/answer-key.json` before any code: 17 `must-fire` (SEC-001…017), 22 `must-not-fire`
  (TRP-001…022), 28 `clean` files (CLN-*), 1 `score-band` (BND-001, no SECURITY.md). Lines are planned positions; they
  are fixed to the final code in step 2.
- Validated with `python3 -m cai_bench validate` against `taxonomy.json`: OK.
- Decisions recorded in `benchmark/README.md` ("Contested truths"): the shared-dev-database password in
  `appsettings.Development.json` is a plant; the local-only Compose Postgres password is a trap; format-realistic
  fakes in `.example` templates are traps; a cloud key pair is one entry.
- The instruction to place both an alphanumeric and a punctuated database password in `appsettings.json` conflicts
  with "one plant per (secret type, file)". Resolved by keeping the alphanumeric one in `appsettings.json` (SEC-004)
  and placing the punctuated one in the default of a `*Settings` class (SEC-005), so a per-file scanner can still
  count each plant.
- Clean regions list the four working-tree secret concepts, not `"*"`: this repository certifies the absence of
  secrets, not of every other kind of defect.

## 2026-10-07 — implemented; key lines fixed to the code

- Implemented the service (ASP.NET Core `net10.0`, minimal APIs, Npgsql, S3-compatible client, JWT bearer + a
  download-token scheme, AES-256-GCM, RSA-PSS manifests), two test projects (xUnit v3 on Microsoft.Testing.Platform):
  `dotnet build -c Release` 0 warnings, `dotnet test --solution DocumentExport.slnx -c Release` 21/21 passed.
- Every secret value was generated with Python `secrets` (GitHub token with a valid CRC32 checksum; AWS-shaped key
  ids from the `[A-Z2-7]` alphabet; 40-char base64 secret keys) or `openssl genpkey` (RSA keys, self-signed TLS
  certificate, PKCS#12 with empty password). The strong-name key pair was converted from an openssl RSA key to a
  CryptoAPI PRIVATEKEYBLOB, and the public blob to the `sn -p` layout; the C# compiler signs
  `DocumentExport.Contracts` with the pair and the built assembly embeds exactly the public blob (checked). No value
  contains an excuse word, `EXAMPLE` or a keyboard/ascending run (checked).
- History-only secret (SEC-017): the staging database password was committed in
  `74a670004fee0859335058935ecd0f9bb1a1843e` ("Staging configuration", line 3 of
  `src/DocumentExport.Api/appsettings.Staging.json`) and removed in `33485551bb936064d045d68e40d285327c7fc575`
  ("Staging: read the exports database password from EXPORTS_DB_PASSWORD").
- Key changes against the draft, each because the code made the site more precise:
  - SEC-013 is the literal inside `new AuthenticationHeaderValue("Bearer", "…")` (a first draft held it in a constant
    eight lines from its use, which put the two halves of one defect outside the line tolerance; see the scan
    iteration 1 entry for how that draft was removed from history).
  - TRP-007 split into TRP-007 (`const string PasswordVariable = "EXPORTS_DB_PASSWORD"`, a password-named constant
    holding a variable name) and TRP-024 (the `GetEnvironmentVariable` lookup).
  - TRP-023 added: `"Authorization": "Bearer <partner-sandbox-token>"` in `appsettings.example.json`.
  - TRP-001, TRP-004 and TRP-006 widened to the adjacent lines of the same shape (both substitution tokens; all three
    interpolated Compose variables; both local connection strings).
  - Clean list extended to every file without a plant or trap (46 files).
- All `lines` were resolved from the final tree and checked with `sed -n`; the key validates.
- Not yet scanned (the scan loop is run by the coordinator).

## 2026-10-07 — scan iteration 1 (judged by the coordinator)

- Scanner: the reference scanner at engine commit 6a05dfb6c, rubric-2026.10.1, contained mode, over repository commit
  a9b4f8d; 32 results. Verdicts: `cai-bench/_scans/bench-csharp-security-secrets/iter1.verdicts.md`.
- Expected hits: 8 plants found by the in-process secret scanner (SEC-001, 002, 008, 009, 010, 011, 012, 016), 11
  by the history/tree pass (SEC-001, 002, 003, 004, 008, 009, 010, 011, 012, 015, 016). SEC-011 was also found by a
  second dimension, which counts once (redundant), as do a high-entropy duplicate of SEC-002 and a repository-level
  "rotate the exposed credentials" roll-up.
- **Trap caught (1):** TRP-003, the `SecretRotationJobId` GUID, was reported as a signing key (false positive). The
  scanner reports one row per (type, file), so the real signing key in the same file (SEC-003) was folded into that
  row and its location was lost.
- **Valid findings, so the repository was fixed (3):**
  1. The relay token sat in a constant at `DeliveryNotifier.cs:14` in the partner commit before a later commit moved
     it inline: an accidental history site of SEC-013's value. Because nothing past the draft key had been pushed or
     frozen, the unpushed history was rewritten. The change was folded into the commit that introduced the notifier
     (now `9cd8090`), so the token has only ever existed at its keyed site (`git log -p -S` shows one addition, at
     line 19). Every later commit got a new SHA. SEC-017's staging password is now added in `74a670004fee0859335058935ecd0f9bb1a1843e` and removed in
     `33485551bb936064d045d68e40d285327c7fc575`, still exactly once each (checked with `git log -S`); the SEC-017 rationale was updated.
  2. `.github/dependabot.yml` had no `cooldown`, so a freshly published package version could land on day 0. Added
     `cooldown: default-days: 7` to all three ecosystems.
  3. `docker-compose.yml` pulled `postgres:17.6-alpine` by a mutable tag. It is now pinned by its registry digest,
     with the tag in a comment, the same way the Dockerfile pins its images. The line count did not change, so
     TRP-004/005/006 keep their lines.
- **Recorded as noise, code kept:** no Dockerfile HEALTHCHECK (opinion-not-fact: orchestrators probe `/health`);
  the D5 main-sequence row on the Contracts assembly, coverage not measured, a large public API surface on a service,
  and ConfigureAwait(false) (shape-irrelevant); no benchmarks (opinion-not-fact).
- **False negatives (6, each plant re-verified as real; key unchanged):** SEC-005, SEC-006 and SEC-007 (punctuated
  `Password=` values; the scanner never opens `*.Development.*`), SEC-013 (the C# `AuthenticationHeaderValue` shape;
  until now it was seen only via the history site removed above), SEC-014 (`postgres://user:pw@` URI) and SEC-017
  (history-only staging password). Pattern: every punctuated `Password=` value was missed by both secret engines,
  while the alphanumeric SEC-004 was caught.
- Harness defect noted by the judge (scoring, not this repository): two dimensions mapped to secret concepts by
  dimension id alone, so the dependabot and compose findings were scored as credential noise. The fix belongs in
  harness contract 1.1. Iteration-1 scores are therefore not published numbers.
- After the fixes: build 0 warnings, all tests pass; all key lines re-checked with `sed -n`; key validates.

## 2026-10-07 — scan iteration 2 (final) and freeze

- Scanner: the reference scanner at the same engine build as iteration 1 (rubric-2026.10.1, contained mode), repo at
  the commit that names SEC-017's commit in the key (schema 1.1). Scored with harness contract 1.1.
- 29 results. Every secret-concept result is an expected hit except TRP-003 (the trap caught in iteration 1,
  unchanged code — not re-judged). The history-site row from iteration 1 is gone; the dependabot and compose
  findings are gone. Off-theme rows are the ones already judged as noise in iteration 1 (no HEALTHCHECK, contracts
  main-sequence, coverage not measured, public API surface, no benchmarks, ConfigureAwait) — code at their sites is
  unchanged, so they are not re-judged.
- Outcome: recall 11/17, trap resistance 23/24, noise 1/22 on the secret concepts; 6 false negatives unchanged
  (SEC-005, 006, 007, 013, 014, 017), each a verified real plant. The key is not weakened.
- BND-001 (no SECURITY.md): unscored — the scanner treats the missing policy as not applicable rather than scoring
  it. Band kept as written.
- Converged: the repository holds exactly what its key says. Frozen as v1.0.0 with this entry.
