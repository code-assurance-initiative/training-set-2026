# Authoring journal — bench-csharp-security-iac

## 2026-10-07 — answer key v1.0.0 drafted (key first)

- Wrote `benchmark/answer-key.json`: 19 `must-fire` (IAC-001…019), 20 `must-not-fire` (TRP-001…020), 87 `clean`
  files (CLN-*, whole file, the eleven theme concepts plus `download-without-integrity-check`), 7 `score-band`
  (BND-001…007). No `not-applicable` entries: every concept the repository names has something to measure.
- Coverage taken from `scanner-benchmark/coverage/matrix.json` rows naming this repository: D29 (CI workflow rules),
  D31 (Dockerfile, Compose, Kubernetes), D40, D41, D42, P4, P5, LA4. `build-provenance-and-signing` added as a band
  because the release workflow is part of the theme.
- The service, its tests and the deployment files were drafted locally while the key was written, so that line
  numbers in this draft are real; no scanner had been run on any of it. Lines are re-checked after implementation.
- Decisions recorded in `benchmark/README.md` ("Contested truths"): IAC-018's concept (CWE-214, not
  `ci-secret-exposure`); an internal Ingress without TLS is a defect; the log forwarder's read-only `/var/log`
  hostPath is left unlabelled; UID 1654 and a missing Dockerfile HEALTHCHECK on a Kubernetes-only image are traps.
- Absence defects are located on the block that lacks the property (Dockerfile stage, container entry, pod spec), and
  sites of one concept in one file are kept more than the line tolerance apart so that one result cannot satisfy two
  plants.
- Validated with `python3 -m cai_bench validate` against `taxonomy.json`: OK (133 entries).

## 2026-10-07 — implemented; key lines checked

- Implemented in four commits (scaffold; API + worker + tests; images, Compose and manifests; the workflows).
  `dotnet build -c Release`: 0 warnings (warnings are errors). `dotnet test`: 26/26 passed (16 unit, 10
  integration).
- Every located entry checked with `sed -n` against the committed files. Two traps were one line off in the draft and
  were corrected: TRP-010 is lines 50-54 and TRP-017 lines 37-41 (the secretKeyRef blocks). No plant moved.

## 2026-10-07 — scan iteration 1 (judged)

- Scanner: the reference scanner at engine commit 6a05dfb6c, rubric-2026.10.1, contained mode, over commit ee5be30;
  99 SARIF results (sha256 85423dcf5056925c…). Scored with the harness after its per-message discrimination update.
  Verdicts, one per unexpected result with the verbatim message: `cai-bench/_scans/bench-csharp-security-iac/iter1.verdicts.json`.
- Harness outcome: TP 12, FN 7, traps 20/20 left alone, 2 clean files hit, 51 noise, 22 redundant.
- **Found (12):** IAC-002, 003, 005, 007, 008, 009, 010, 011, 012, 014, 016, 019. IAC-010 (no probes) is credited to
  an unrelated registry-allow-list row on the container line; the real probe rows (CKV_K8S_8/9) sit at line 1.
- **False negatives (7), each plant re-verified as real:**
  - IAC-004 hostNetwork and IAC-006 containerd-socket hostPath: detected (KSV-0009, KSV-0023, CKV_K8S_19) but located
    on the DaemonSet's `spec` or at line 1, outside the tolerance. The plants sit on the exact lines; not moved.
  - IAC-015 write-all and IAC-018 token on kubectl's command line: detected by D36, but without a SARIF location
    (the message names `release.yml:7` and "2 CI commands"). IAC-018 is also reported by a semgrep rule the harness
    maps to `ci-secret-exposure`.
  - IAC-013 Ingress without TLS and IAC-017 pull_request_target building the PR head with secrets: not reported at all.
  - IAC-001 (no USER): DS-0002 reported at line 1; see the key change below (not counted as FN any more).
- **Key changes (location only, no label changed):** IAC-001, TRP-004 and TRP-005 are absences that hold for the whole
  Dockerfile (no USER / no HEALTHCHECK anywhere), so they became whole-file entries instead of the final-stage range.
  IAC-006 moved to lines 71-74 because the DaemonSet grew (below).
- **Valid findings, repository fixed:**
  1. The Fluent Bit DaemonSet had no probes: health server enabled in its config, readiness/liveness on
     `/api/v1/health`.
  2. Coverage gaps (D8): tests added for the reminder host (boots the worker over the in-memory store), the storage
     registration, and the PostgreSQL store with its exclusion constraint (Testcontainers; skipped when no container
     runtime is reachable). The PostgreSQL tests exposed a real bug - a non-UTC `DateTimeOffset` parameter made
     Npgsql throw - fixed by normalising every timestamp to UTC in the store. 33 tests now.
  3. SECURITY.md had no reporting URL: it links the private advisory form.
- **Noise recorded (code kept):** registry allow-list (KSV-0125), secrets-as-files preference (CKV_K8S_35), UID >
  10000 preference on the API (TRP-007's property, reported at lines 1/38), node-agent token "not needed" (it is),
  D17 CA2007 (same as baseline), D5 zone of pain on the domain kernel. Many line-1 checkov rows restate found
  plants (redundant).
- Bands: D40 100 (in), D41 80 (out of 10-60: seccomp on one workload is credited repository-wide), D42 unscored
  (gated on governance resources), P4 100 (in), P5 0 (in), D36 25 (in), LA4 unscored offline.

## 2026-10-07 — scan iteration 2 (final) and freeze

- Scanner: same engine build (rubric-2026.10.1), contained mode, over commit a870e62; 92 SARIF results (sha256
  dea4a9a5…). One host pass with the model (`--host --with-llm`) alongside.
- Outcome: TP 13, FN 6, traps 18/20 left alone - TRP-004 and TRP-005 (no HEALTHCHECK on Kubernetes-only images) are
  caught now that they are located file-wide - and 1 clean file hit (the API Deployment: secrets-as-files, UID >
  10000 and registry allow-list preferences, already judged).
- The DaemonSet probe rows and the reminder-host / storage coverage rows are gone. The PostgreSQL store still shows
  0 % coverage: its tests need a container runtime, which the scanner's sandbox lacks, so they skip there (locally:
  100 %). Judged a false positive of the environment; code kept.
- Every other unexpected row recurs at an unchanged site and keeps its iteration-1 verdict.
- False negatives (6, each a verified real plant; key unchanged): IAC-004 and IAC-006 (detected, but on the
  DaemonSet's spec / line 1), IAC-015 and IAC-018 (detected by a supply-chain rule without a SARIF location),
  IAC-013 (Ingress without TLS: no rule) and IAC-017 (pull_request_target building the PR head: no rule fired).
- Bands: D40 100 in, D41 80 out (10-60; seccomp on one workload is credited repository-wide, AppArmor/SELinux absent),
  D42 unscored (gated on governance resources the repository does not ship), P4 100 in, P5 0 in, D36 25 in. LA4 was
  not invoked by the model pass (no LA4 call in the model-call log), so BND-006 is unscored. Bands kept as written.
- Converged: the repository holds exactly what its key says. Frozen as v1.0.0 with this entry.

## 2026-10-07 — v1.1.0: precise IaC concepts (harness contract 1.3)

- The taxonomy split the umbrellas `container-excessive-privilege` and `iac-misconfiguration` into precise
  concepts. Under v1.0.0 an unrelated registry-allow-list result on the same container line scored the
  missing-probes plant (IAC-010) as found; with precise concepts that cannot happen.
- **Key change:** 14 entries re-labelled to their precise concept (IAC-001 runs-as-root, IAC-004 host namespaces,
  IAC-005 privileged, IAC-006 host path, IAC-008 security context, IAC-010 health probes, IAC-011 automounted token,
  IAC-014 RBAC; traps TRP-003/007 runs-as-root, TRP-004/005 image HEALTHCHECK, TRP-008 probes, TRP-009 host path).
  Same ids, lines, labels, rationales and code. v1.0.0 stays as frozen.
- Re-scored scan iteration 2 (harness 1.3): recall 14/19, trap resistance 18/20; file-level recall 17/19 — the gap
  is the scanner reporting Kubernetes findings at line 1 of a multi-document file. IAC-015 is matched through the
  location the scanner names in its message (no SARIF location), which the harness records as `locationSource: message`.

## 2026-10-07 — v1.2.0: plant defect corrected (IAC-017)

- **Plant defect: invalid YAML hid the plant from every parser.** Line 34 of `.github/workflows/pr-preview.yml`
  (`run: gh pr comment … --body "Preview image: \`…\`"`) was an unquoted plain scalar containing `: `, which YAML
  rejects ("mapping values are not allowed here", line 34 column 83; actionlint: `could not parse as YAML`).
  GitHub would have refused to run the workflow, and the reference scanner could not parse the file, so the
  `pull_request_target` checkout plant (IAC-017) was invisible to any tool through no fault of the tool.
- **Fix:** line 34 now single-quotes the `run:` value. The shell command it yields is byte-for-byte the text the
  line always meant; no line moved. The defect is unchanged: `pull_request_target`, checkout of
  `github.event.pull_request.head.sha` (line 16), `docker build` / `docker push` of the PR head with
  `secrets.GHCR_PUSH_TOKEN` in the step (lines 21–28). Verified with PyYAML (every YAML file in the repository parses)
  and actionlint (the file is clean; before the fix it failed with the syntax error above).
- Key entries in that file re-checked against the code: IAC-017 16–28, TRP-018 24, TRP-019 33 — all hold.
  Only `keyVersion` changes (1.1.0 → 1.2.0); no entry changes.
- **The iteration-2 FN verdict on IAC-017 is withdrawn.** It was a judging error: the plant was broken, not the
  scanner. The re-scan of v1.2.0 is recorded below.
- Re-scan `v1.2-iter1` (same pinned engine, contained, over a2c4634): 93 SARIF results (sha256 350821524b3c…), exactly
  iteration 2's 92 plus one: `D29 pull-request-target-code-checkout` at `pr-preview.yml:16` — the expected IAC-017 hit.
  No result disappeared, none moved; no new verdicts were needed.
- Score (cai_bench at scanner-benchmark 46803cf, key v1.2.0): TP 15, FN 4 (IAC-004, IAC-006, IAC-010, IAC-013),
  traps 18/20, recall 15/19, file-level recall 18/19. Only outcome change against iteration 2 re-scored on the same
  harness and key: IAC-017 FN → TP. TRP-018 and TRP-019 (same file) stay TN.
- Frozen as v1.2.0 with this entry.
