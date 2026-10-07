# Authoring journal — bench-csharp-maturity-history

## 2026-10-07 — answer key v1.0.0 draft written (key first)

- Key written before any code, from `scanner-benchmark/coverage/matrix.json` (the rows whose coverage names this
  repository: D15, D16, D19, D20, D25, D34, D35, M1, M2, M4) and `taxonomy.json`, plus the release-hygiene plant the
  coordinator's brief asks for (P6's concept; the matrix assigns P6 to other repositories, so this is extra coverage)
  and a folder-structure band.
- 7 `must-fire` (hotspot, knowledge silo, stale knowledge, change coupling, ADR contradicted, README drift, changelog
  missing the last two releases), 9 `must-not-fire` traps, 12 score bands chosen from intent. `clean` entries for the
  repository's concepts over every tracked file will be generated from `git ls-files` once the tree exists.
- Plants are file-level: churn, ownership, staleness and coupling are properties of a file's history. Entries a
  scanner may report only by naming the file in a message (per author, per summary row) carry the path as `subject`.
- History plan: the scripted history is appended *after* this key-first commit, so the key precedes all code in the
  commit graph. Commit dates of the scripted history are fictional (January–September 2026) and earlier than this
  commit's real date; the order that matters - key before code - is the order of the graph.
- Validated with `python3 -m cai_bench validate`: OK, 28 entries.

## 2026-10-07 — implementation and scripted history (local, not yet pushed)

- The service was written first as its final tree (4 production projects, 2 test projects, 79 tests green, build with
  warnings as errors), then its history was composed backwards from it: every intermediate version of every file is
  derived from the final file by explicit, reviewed edits, so the history tells one consistent story (features land,
  bugs are fixed, a module is added and removed, a clock abstraction is replaced).
- 120 scripted commits, 2026-01-05 to 2026-09-30, five fictional authors with `@example.invalid` addresses, release
  tags v0.1.0 ... v0.6.0. **Every release tag was checked out and built in locked-restore mode, and its tests run:
  all green** (v0.1.0: 41 tests, v0.6.0: 79).
- **What was done about the key-first commit (the ORDER rule):** the key-first commit `a8b3b2a` (pushed) is kept
  unchanged, and so is the repository's initial commit; the scripted commits are appended on top of it, so the key
  precedes every line of code in the commit graph and no pushed commit was rewritten or force-pushed. Consequence: the
  scripted commits' (fictional) dates are earlier than the key-first commit's real date. Commits after the scripted
  history (this one onwards) are real maintenance by the benchmark author.
- `benchmark/history/build-history.sh` rebuilds the scripted part from `benchmark/history/patches/NNNN.patch` and
  verifies the final commit id `431370e` and all six tag targets; verified by running it into a fresh directory.
  Patch files are numbered rather than named after their subjects so that no file name in the tree repeats a term from
  the history (a README-drift check that searches file names would otherwise find "waitlist" in a patch name).
- Before scanning, the history was checked with an independent re-implementation of the published git-mining rules
  (time-decayed authorship with a six-month half-life and 1/sqrt(files) focus; co-change over commits of at most
  `max(5, 10 % of production files)` paths): one co-change pair above 10 revisions / 5 shared / 50 % (the plant, 82 %);
  two files with one author's >= 90 % share (the silo plant and the leaf-utility trap); two files with no living
  knowledge (the stale module); 11 of the last quarter's 40 production file-changes on the slot finder.
- Key changes (no label added or removed): `clean` entries generated from the file list (250 files, 12 concepts each,
  minus the concept of any plant or trap on that file, and minus the concept where a result would restate a plant:
  `RecurrenceRule.cs`, `PortalAppointmentFeed.cs`, `ReportsController.cs`, README.md for documentation quality,
  `docs/architecture.md` for documentation accuracy); STL-001, TRP-004, CPL-001 and ADR-001 rationales made precise to
  the history as built (TRP-004 had claimed the catalogue was the second most changed file; it is one of the four most
  changed). Validated: 278 entries.

## 2026-10-07 — scan iteration 1 (contained) and two model-judged passes (repo at 8cd110c, local)

- Scanner: the reference scanner, engine build of the pinned instrument (rubric-2026.10.1). Contained pass: 27 results;
  host passes with the model-judged evaluators: 29 results each. **The two model-judged passes agreed exactly**
  (same results, same scores: D19 90, D20 98, D21 90, D25 67, M4 100).
- **Hits:** HOT-001 (D15: "changed 11 times in last 90 days ... cyclomatic complexity 25 in SlotFinder.FindOpenSlots
  ... 10 of those changes were fix/bug commits"), SIL-001 (D16 off-boarding row naming CancellationPolicy.cs),
  STL-001 (D34 summary row naming both Recurrence files), ADR-001 (D25 on ADR 0004, citing ReportsController). All
  nine traps of the draft were left alone.
- **Misses, each re-verified:**
  - CPL-001 (D35 reported no pair, score 100). The plant is real: 9 shared commits of at most five files, 12 and 11
    revisions, 82 %; neither file names a type of the other. The scanner drops a pair whose two projects have a declared
    `ProjectReference` (Api references Infrastructure), treating the assembly edge as the explanation although neither
    file uses the other. Scanner false negative; key unchanged.
  - REL-001 (P6 100). The changelog really stops at 0.4.0 while v0.5.0 and v0.6.0 are tagged and the version is
    0.6.0. The scanner checks the presence of a changelog and of version tags, not that they agree. False negative.
  - DOC-001 (M4 100, "0 drift item(s) (2 unverifiable claim(s) dropped)"). The model did report two drift items, but
    they were dropped because the scanner's term check found the module's name in a `.json` file - which can only be
    `benchmark/answer-key.json`, whose DOC-001 rationale named the module. That is contamination by the benchmark's
    own key, not evidence about the code. **Key change:** DOC-001's rationale no longer spells out the module's name
    (no key label changed). Re-test in iteration 2.
- **Unexpected results, verdicts** (none needs a code change):
  - D16 "Further sole-owners (lower concentration) ... anonymized user #2 (1 file(s))" - **false-positive**: the one
    file is IcsCalendarWriter.cs, the leaf-utility trap TRP-007 (single author, RFC-specified, fully tested). The row
    names no file, so the harness counts it as noise rather than as a caught trap.
  - D17 `.editorconfig:37` CA2007 off - **false-positive**: the reason is on line 36 and `src/.editorconfig` turns the
    rule back on for production code. **Key change:** promoted to trap TRP-010 (`suppressed-diagnostic`), as in the
    other benchmark repositories that share this convention.
  - D1/D2/D39 on SlotFinder.FindOpenSlots (cyclomatic 25, cognitive 50, 363 IL instructions) - **valid**, and the
    complexity half of HOT-001: the hotspot is planted, so it is kept. Not a separate defect to fix.
  - D4 duplicated blocks AppointmentResponse.cs:44-60 / PortalAppointmentFeed.cs:53-69 (the status and reason code
    tables) - **valid**, and the material of CPL-001 (the duplicated wire vocabulary is why the pair co-changes).
    Kept; reported to the coordinator as a candidate `duplicated-code` label for a later key version.
  - D5 "ClinicScheduling.Domain: zone of pain" - **opinion-not-fact**: a concrete domain layer depended on by the
    application layer is the intended shape (ADR 0002).
  - DM2 primitive Guid ids on Appointment (4) - **opinion-not-fact**: strongly typed ids are a style choice; this
    small service's ADRs choose a layered, not a DDD, design.
  - DM3 (2) "integration event couples to a producer-owned domain type" on BookSeriesRequest / CancelAppointmentRequest
    - **false-positive**: these are HTTP request models with a parsing helper, not integration events; the service
    publishes no events.
  - ED3 (10) "event not named in past tense" on request/response records - **false-positive**: none of them is an event.
  - D21 (model) "NoShow vs NoShowFee named inconsistently" - **opinion-not-fact**: `NoShow` is a decision code (like
    `LateCancellation`), `NoShowFee` an amount; they are different concepts, consistently named.
- Score bands: in - D16 83, D34 75, M1 80, M2 70, M3 100, D19 90, D20 98, D25 67. Out - BND-001 hotspots (D15 94 vs
  [40, 85]: the scanner normalises hotspot risk by all churn in the window, so one dominant hotspot costs little;
  band set before the scan, kept), BND-004 change coupling (D35 100, the CPL-001 miss), BND-010 documentation accuracy
  (M4 100, the DOC-001 miss), BND-011 release hygiene (P6 100, the REL-001 miss).

## 2026-10-07 — scan iteration 2 (final) and freeze

- Pushed: main fast-forwarded from the key-first commit to 88d9d05 (no force push was needed: the scripted history
  extends the pushed key-first commit), plus release tags v0.1.0 ... v0.6.0.
- Contained pass at 88d9d05: the same results as iteration 1 (D35/P6 still silent; the model-judged rows need the host
  pass). Model-judged host pass at 88d9d05: **DOC-001 is now found** - M4 "README lists a project
  'ClinicScheduling.Waitlist' in the Architecture table, but the evidence shows only 6 projects" (TP), and a second M4
  row about the same section's endpoints (**redundant**, same finding). M4 scored 60, inside BND-010.
- Final outcome: 5 of 7 plants found (HOT-001, SIL-001, STL-001, ADR-001, DOC-001); false negatives CPL-001 (a
  declared project reference is taken as the explanation of a file pair that never references each other) and REL-001
  (changelog not compared with tags/version). 9 of 10 traps left alone; TRP-010 (`.editorconfig` CA2007) caught.
  Noise: the D16 fold row (the leaf-utility trap in substance). Bands: 9 in, 3 out (hotspots 94 vs [40, 85]; change
  coupling and release hygiene follow the two misses).
- Converged: every unexpected result is judged (journal, iteration 1), none needed a code change, and the key matches
  the code and the history. Frozen as v1.0.0 with this entry.
