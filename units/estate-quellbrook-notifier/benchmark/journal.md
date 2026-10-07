# Authoring journal — estate-quellbrook-notifier

## 2026-10-07 — answer key v1.0.0 draft written (key first)

- Part of Phase 4, the reference estate of the fictional carrier Quellbrook Freight (five repositories: gateway, web,
  orders, dispatch, notifier). The estate is designed as a whole first — services, HTTP and message contracts, teams,
  the three-sprint calendar and the story each repository's history tells — and this key is written from that design,
  before any code.
- The key is at integration level: the defects planted at their final sites, the traps a realistic service of this
  kind naturally contains, principled not-applicable entries and score bands set from intent. `clean` entries for
  every tracked file will be generated from `git ls-files` once the tree exists (files without a label: `"*"`).
- Planned line numbers are approximate and will be set to the final code (journalled) before the first scan.
- History plan (as in bench-csharp-maturity-history): the scripted sprint history is appended on top of this
  key-first commit, so the key precedes every line of code in the commit graph; the scripted commits carry fictional
  authors and dates in 2026-07..2026-09, earlier than this commit's real date. Nothing is force-pushed.
- Validated with `python3 -m cai_bench validate`: OK.

## 2026-10-07 — implementation and scripted history (local, not pushed)

- Written forward, sprint by sprint; each release tag's tree built (warnings as errors) and tested before its commits
  were made. 25 scripted commits, tags `v0.1.0` (23 tests), `v0.2.0` (30), `v0.3.0` (33).
- The security incident, as built: on 2026-08-18 (sprint 2) `chore(email): send from the staging environment` writes a
  newly generated key in the e-mail provider's real format (`SG.` + 22 + `.` + 43 characters, CSPRNG, authenticates
  nothing) into `src/Quellbrook.Notifier/appsettings.json` line 15, commit `a033c7c`. On 2026-08-25 (sprint 3) it is
  removed, both provider keys come from an ExternalSecret, CI scans the commits of every push and pull request with
  gitleaks, ADR 0003 and an incident note are written. The key stays readable in history (NTF-001, with `commit`).
- The PII log line (NTF-002) is added on 2026-08-19 to chase staging bounces and stays; the SMS sender masks its
  number (TRP-002).
- Found while writing the tests and fixed in the history: a consumer test stopped the `BackgroundService` right after
  starting it, and since .NET 10 runs `ExecuteAsync` on the thread pool it could cancel before the queue was declared
  (failed once locally); recorded in the story as a sprint-2 fix commit. The same test exists in the dispatch
  repository, already pushed: fixed forward there.
- `docs/privacy.md` makes no claim about log contents (an earlier draft said logs carry no contact details, which the
  NTF-002 line contradicts; removed so the document is accurate).
- Key: lines set to the final code (NTF-001 to line 15 of the file in that commit), `commit` set, `https-enforcement`
  not applicable (no HTTP served), audit band widened; `clean` entries from `git ls-files`. Validated: OK.

## 2026-10-07 — scan iteration 1 (contained, local, before any push) and history packaging

- Contained pass at `66fce9c`: 10 results. **NTF-001 found**: D28 "Secret: sendgrid-api-token: gitleaks matched rule
  'sendgrid-api-token' here, in git history" at `appsettings.json:15` in commit `a033c7c` (with the location-less
  "rotate the exposed credentials" roll-up, a summary row). **NTF-002 missed**: the recipient's address is passed to
  a source-generated `[LoggerMessage]` method, which the personal-data-in-log rule does not read (the same blind spot
  as in bench-csharp-security-injection). Plant re-verified; false negative.
- **Valid → repository fixed (scripted history, before the first push):** D8 "NotifierServices.cs 0 % coverage" —
  the registration was untested; a sprint-3 test commit now builds the host, resolves every handler, channel and
  background service, and checks that a missing provider key stops the worker (2026-09-03).
- **Noise, code kept:** D17 CA2007 (TRP-005 caught), CKV_K8S_35 and KSV-0125 (opinion, as in the other services),
  D41/D42 (opinion / shape-irrelevant), C1 (opinion; encryption delegated, the row says so), C4 "partial retention —
  missing an expiry limit and a scheduled purge" (**false-positive**: `RetentionOptions` and the hourly
  `RetentionSweeper` are exactly that → repository-level trap TRP-007).
- Release tags built in locked mode with tests: `v0.1.0` 23, `v0.2.0` 30, `v0.3.0` 33.
- `benchmark/history/`: 25 patches and `build-history.sh`. The two patches that add and remove the provider key carry
  the marker `@@REDACTED_SECRET@@` instead of the key, so the working tree never contains it; the script reads the
  value from the published commit `a033c7c` (present in every full clone) and substitutes it before applying. Verified
  by rebuilding into a fresh directory: HEAD and the three tags match. No file under `benchmark/` contains the key.
- Key changes: trap TRP-007.

## 2026-10-07 — scan iteration 2 (final) and freeze

- Pushed: main fast-forwarded from the key-first commit to `614f388` (no force push), plus tags `v0.1.0` … `v0.3.0`.
  GitHub accepted the push of the history that contains the planted key (push protection is off for this
  organisation's benchmark repositories, as the coordinator arranged).
- Contained pass at `614f388`: 9 results. NTF-001 found again (D28, `appsettings.json:15` @ `a033c7c`); NTF-002
  missed; the D8 row of iteration 1 is gone; TRP-005 (D17) and TRP-007 (C4 "missing expiry limit / scheduled purge")
  caught; the rest is the recorded noise of iteration 1.
- Model-judged host pass at `614f388`: D19 90, D20 73, D21 100, M4 100. The host has no gitleaks, so the history
  secret is not seen there (NTF-001 is a contained-mode finding). New row: D20 "ADR 0001 documents a meta-process" —
  **opinion-not-fact** (the conventional first ADR) → trap TRP-008, added with this entry (key only; the code is
  unchanged since the scans).
- Converged: two plants (one found), eight traps, recorded noise only. Frozen as v1.0.0 with this entry.
