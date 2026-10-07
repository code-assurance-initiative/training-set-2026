# Authoring journal — bench-ts-frontend-a11y

## 2026-10-07 — answer key v1.0.0 drafted (key first)

- Wrote `benchmark/answer-key.json` before any code: 20 `must-fire` (A11Y-001…020), 17 `must-not-fire`
  (TRP-001…017), 5 `score-band` (BND-001…005). Schema 1.2. Lines are planned positions; they are fixed to the final
  code in the implementation step, and the `clean` entries are added from the final tree.
- Coverage from `coverage/matrix.json` rows that name this repository: AC1–AC6 (must-fire, must-not-fire, clean),
  AC7 (score-band; a repository-level plant as well, because the absence of any accessibility enforcement is itself a
  defect a reviewer would raise), LA2/LA5/LA6 (score-band; one located plant each for alt text and link text so the
  band has a cause), R1 (score-band), R3 (must-fire, must-not-fire, clean). Beyond the matrix: `cross-site-scripting`
  through React's raw-HTML escape hatch (one plant, one sanitised trap), because a React frontend is where that sink
  lives.
- Truth decisions recorded before the code:
  - A muted, control-less, looping hero video owes no captions (no audio) but does owe a pause mechanism (moving,
    auto-starting, > 5 s): one site, a trap for text alternatives and a plant for motion safety.
  - A hand-rolled modal with no `aria-modal`, focus move, containment or Escape is labelled
    `non-keyboard-accessible-interaction` — its defect is what a keyboard user can and cannot reach. A native
    `<dialog>` opened with `showModal()` is the trap.
  - A visually-hidden (clip) label is a label; a large-text colour pair between 3:1 and 4.5:1 passes AA; a disabled
    control is exempt from contrast; an outline removed only for `:focus:not(:focus-visible)` with a `:focus-visible`
    ring is not a removed focus indicator.
  - No concept exists for React-specific correctness (index keys, effect dependencies, state mutation) or for error
    association; the code is written free of those instead of carrying unlabelled defects.
- Validated with `python3 -m cai_bench validate` against `taxonomy.json`: OK.

## 2026-10-07 — implemented; key lines fixed to the code

- Implemented the app: Vite 8, React 19.3, TypeScript 6.0 (`strict`, `noUncheckedIndexedAccess`,
  `exactOptionalPropertyTypes`), DOMPurify 3, vitest 5 + Testing Library + jsdom; ESLint with typescript-eslint
  `strictTypeChecked` and the React hooks rules, Prettier. `npm ci`, `npm run lint`, `npm run typecheck`,
  `npm run format:check`, `npm run build` and `npm run test:coverage` (14 files, 65 tests, ~97 % lines) are green;
  `npm audit` reports 0 vulnerabilities.
- Every plant and trap sits where the key says; lines resolved from the final tree with `grep -n` / `sed -n`.
- Decisions made while writing the code (no label changed meaning):
  - Routing is path-based (History API), not hash-based: a hash router would have turned the skip link's
    `#main-content` into a route change, making TRP-007 a real defect.
  - The hero title trap uses #ffffff on #2f8f9d (3.8:1) at 36px bold — large text, inside the 3:1–4.5:1 window.
  - The focus-outline plant removes the outline with no other focus style at all (an earlier draft also changed the
    border colour, which could count as a replacement indicator).
  - The plant stylesheets hold only one failing colour pair each, and the trap pairs live in their own files, so a
    scanner that reports one pair per stylesheet is not prevented from seeing a trap by a plant.
  - The large-component plant is a module of several components and helpers (no single function over 100 lines),
    so the defect is the file, not a long function.
  - Helper functions of the loans module are not exported (exporting them only for tests would be a defect of its
    own); the tests drive the page.
- Clean list built from the final tree: 65 files (every tracked file except plant/trap files, the lock file, `LICENSE`
  and `benchmark/`). The key validates: 107 entries (20 must-fire, 17 must-not-fire, 65 clean, 5 score-band).

## 2026-10-07 — scan iteration 1 (judged)

- Scanner: the reference scanner at engine commit 6a05dfb6c, rubric-2026.10.1, contained mode, over repository commit
  98736a3; 17 results. Untracked build output (`node_modules/`, `dist/`, `coverage/`) was removed before the scan so the
  scanned tree is what a clone holds.
- Expected hits: 12 of 20 plants (A11Y-001…009, 011, 012, 018). Every located HTML/JSX plant of AC1–AC5 was found on
  its line; AC6 found the stylesheet contrast plant.
- **Traps caught (3), all `false-positive`, recorded as scanner noise:**
  - TRP-005 — "Click handler on a non-interactive <article>" on the result card. The card's own "View details"
    button performs the same action (`onOpen(item.id)`); the card handler is a pointer convenience that ignores
    clicks on the button. The scanner's duplicate-control escape did not recognise the shared action because the
    two handlers have different names.
  - TRP-008 — "Click handler on a non-interactive <dialog>" on the native borrow `<dialog>`. It is opened with
    `showModal()`: Escape and the Cancel button close it, and the handler only acts when the click lands on the
    backdrop (`event.target === event.currentTarget`). The scanner's dismiss-backdrop escape looks for a
    `role="dialog"`/`aria-modal` descendant and does not know the native element.
  - TRP-010 — "Low contrast colour pair in CSS (3.8:1)" on `.hero__title`. The same rule sets `font-size: 2.25rem`
    and `font-weight: 700` (36px bold): large text, for which AA requires 3:1. The scanner applies the normal-text
    4.5:1 bar regardless of the size declared in the rule.
- **Valid (1), repository fixed:** D3 "FunctionTooLong: BorrowDialog … 108 significant lines" — accurate against
  the stated 100-line bar, and the component did hold two jobs (the modal dialog and the whole reservation form).
  Fixed by extracting `BorrowForm` (state, validation, submission, fields); `BorrowDialog` keeps the native dialog,
  its backdrop handler and the heading. TRP-008 moved to lines 35–41 (same element, same code); `BorrowForm.tsx`
  joined the clean list. No label changed.
- **Uncovered, judged:** P6 "Changelog looks stale/thin: … 2 versioned entries, against a bar of 3" —
  `opinion-not-fact`: the count is right, but a project at its first release has one release to log; "stale" is
  a conclusion the count does not support. Kept as is.
- **Score band out:** BND-001 (accessibility enforcement, band 0–20) scored 40: the scanner reports rung 0 ("No
  accessibility enforcement found") but its scale has a floor of 4/10 for rung 0. Band kept (set from intent).
- **Missed plants (8), each re-verified in the code:** A11Y-010 (hand-rolled modal: no rule for focus management or
  `aria-modal`), A11Y-013 (autoplaying looping video: no motion rule for media), A11Y-014 and A11Y-015 (animation and
  outline removal in a `.css` file: those two checks read only `<style>` blocks and inline styles), A11Y-016
  (`dangerouslySetInnerHTML` fed by API data held in state: the SAST rules taint only component props and
  parameters), A11Y-017 (the large-file dimension scored 78 for two files over 400 lines but raises a finding only
  below 70, and the separate long-file check, with a 500-line bar, did not fire on it), A11Y-019 / A11Y-020
  (model-judged; this contained pass is offline by design — judged in the host pass).

## 2026-10-07 — scan iteration 2 (contained + two model passes), judged

- Contained scan over a04a767 (the BorrowForm extraction): 16 results. The D3 row is gone; every other result is
  the same as in iteration 1 — 12 expected hits, the three caught traps (TRP-005, TRP-008, TRP-010, same verdicts),
  the AC7 posture plant and the P6 changelog row (same verdict). Nothing new to judge: converged for the
  deterministic scanner.
- Host pass with the model on (`--host --with-llm`, default configuration): the same results plus one more —
  M4 "README/code drift: ESLint uses typescript-eslint strict + React hooks rules". **`false-positive`:** the README
  sentence is true — `eslint.config.js` applies `tseslint.configs.strictTypeChecked` and the React hooks plugin, and
  `package.json` declares both packages; the check matched the claim only against file names and manifest contents,
  read the claim's words as package names and missed the config file. Kept as is.
- **The model-judged text-quality dimensions are not composed in the default configuration.** LA2, LA5 and LA6 run
  only when the scan is given the WCAG / EN 301 549 compliance framework (`CODEHEALTH_COMPLIANCE_FRAMEWORKS`); the
  default host pass therefore produced no LA2/LA5/LA6 card, so A11Y-019 and A11Y-020 count as missed and BND-003…005
  as unscored. The benchmark measures the default configuration (the coordinator's decision for LA1, applied
  here). Recorded, key unchanged.
- A second host pass with `CODEHEALTH_COMPLIANCE_FRAMEWORKS=wcag-2.2` (informative, not the default
  configuration): LA2 "Weak alt text" at `HomePage.tsx:35` (A11Y-019) and LA5 "Vague link/button text" at
  `HomePage.tsx:56` (A11Y-020) — both plants found on their lines and nothing else; scores LA2 75 (3 of 4 static alts
  meaningful, exactly the intended ratio), LA5 92, LA6 100 — all three inside their bands.
- **Found while reviewing the clean files, fixed (not reported by the scanner):** the `:focus-visible` ring colour
  #f59e0b on the white page is about 2.1:1, below the 3:1 non-text contrast a focus indicator needs (WCAG 1.4.11),
  in `buttons.css`, `search.css` and `settings.css`. Changed to #b45309 (5.0:1). Those certified-clean and trap
  sites now are what the key says (TRP-013 calls the ring high-contrast). No line moved.

## 2026-10-07 — scan iteration 3 (final) and freeze v1.0.0

- Over 6d5e9d3 (the code frozen here; the tag commit adds only this journal entry): contained scan 15 results,
  default-configuration host model pass 16 results, WCAG-framework host model pass 17 results. The model passes
  agreed with themselves and with iteration 2 on every model-judged score (D19 90, D20 100, D21 100, M4 96; with
  the framework LA2 75, LA5 92, LA6 100). No new result; every unexpected result is recorded noise with a verdict.
- Final outcome in the default configuration: 12 of 20 plants found (60 %), 14 of 17 traps left alone (82 %),
  noise 20 % (the three caught traps); with the WCAG framework on, 14 of 20 (70 %). Missed: A11Y-010, 013, 014, 015,
  016, 017 (and A11Y-019, 020 only because their dimensions are off by default). Score bands: type safety in;
  accessibility enforcement out (40 against 0–20: the scale's floor for rung 0); the three model-judged bands
  unscored by default, all in with the framework.
- Key unchanged in meaning since the draft; lines and clean list as journalled above. Code and key frozen together
  as v1.0.0.

## 2026-10-07 — v1.1.0 key draft (key first): relabels and the omitted plants

The harness (contract 1.4) gained the concepts this repository's author lacked. A change after the freeze is a new
version: v1.0.0 stays where it is; this is v1.1.0 (`keyVersion` "1.1.0"; `schemaVersion` stays "1.2", the highest the
validator accepts — the 1.3/1.4 contract changes are outcome rules, not key format).

- **Relabelled, same sites:** A11Y-010 → `modal-focus-not-managed`; A11Y-013 → `autoplay-media-without-control`;
  A11Y-014 → `motion-without-reduced-motion`; A11Y-015 → `focus-outline-removed`. Their look-alike traps TRP-012
  (reduced-motion-guarded toast animation) → `motion-without-reduced-motion` and TRP-013 (`:focus-visible` ring) →
  `focus-outline-removed`, so plant and trap name the same precise concept; A11Y-012 (contrast) and TRP-010/011
  (contrast exemptions) stay on the umbrella `visual-and-motion-safety`, whose residue is contrast.
  A11Y-013's hero video is muted: the taxonomy text of `autoplay-media-without-control` speaks of "audio, or video with
  sound", while its title and WCAG 2.2.2 cover any auto-starting moving media with no way to pause it. The label
  follows the defect (2.2.2); the rationale says 1.4.2 does not apply.
- **New plants** (the ones the v1.0.0 README listed as deliberately not covered): A11Y-021 `react-index-as-key`
  (reading list keyed by index), A11Y-022 `react-hook-missing-dependency` (gallery arrow-key effect),
  A11Y-023 `react-state-mutation` (reading list `markFinished`), A11Y-024 `form-error-not-associated` (add-a-book
  title error).
- **New traps:** TRP-018 (index key on a static tips list), TRP-019 (complete memo dependencies), TRP-020 (splice on a
  copy in a functional update), TRP-021 (the borrow form's associated card-number error — existing code), TRP-022
  (native `<dialog>` + `showModal()` manages focus — existing code, the look-alike of A11Y-010).
- **Clean lists:** the six new non-umbrella concepts are added to every `clean` entry (each certified file checked:
  no index key, no incomplete dependency array — `eslint` reports only the A11Y-022 site —, no in-place state change,
  no form error, no hand-rolled modal, no media). New clean files: `readingList.ts`, `reading-list.css`, the reading
  list test.
- Lines are those of the code written for this version (committed next); fixed again after the code lands.
- **Lint:** `react-hooks/exhaustive-deps` is `warn` in the plugin's `recommended-latest` preset, so the planted missing
  dependency does not fail `npm run lint` (one warning). No rule was changed, nothing suppressed at the site. The
  in-place mutation (A11Y-023) is not reported by the plugin's compiler-based `immutability` rule (it sits in a custom
  hook's event callback), so no rule needed demoting either.

## 2026-10-07 — v1.1.0 scan iteration 1 (contained), judged

- Contained scan over abc2311 (`_scans/bench-ts-frontend-a11y/v1.1-iter1`): 20 results, against 16 at v1.0.0 iteration
  3. Compared result by result with the v1.0.0 final scan: the A11Y-007 row (AC4, click-only thumbnail) moved from
  line 29 to 50 with the arrow-key effect above it (same plant, still a TP); the AC7 row now counts 24 markup files
  instead of 23 (the A11Y-018 plant, still a TP). Everything else is unchanged, including the three caught traps and
  the P6 changelog row. Four rows are new, all R10 duplication:
  - "Duplicated block (12 lines × 3 locations)" at `BorrowForm.tsx:92`, `ReadingListPage.tsx:174`,
    `SettingsPage.tsx:58` — **valid**: the same checkbox-plus-label markup (wrapper, `useId`, `checked`, an
    `onChange` that unwraps `event.target.checked`, the label) written out in three forms; the third copy, in the new
    reading list, took it over the bar. Fixed by one `CheckboxField` component used by all three.
  - "Duplicated block (10 lines × 2 locations)" at `ReadingListPage.tsx:145`/`:156` and "Duplicated block (9 lines ×
    2 locations)" at `BorrowForm.tsx:75`/`ReadingListPage.tsx:155` — **valid**: the label-plus-text-input markup
    repeated in the new page (title, author, and the per-row note). Fixed by one `TextField` component used for all
    three. The form-error plant A11Y-024 stays what it was, and is now more natural: the shared field has no error slot,
    so the page renders the message after it, tied to nothing.
  - "Duplication concentrated across 6 sibling directories (5 clone groups)" at `BorrowForm.tsx:92` — **valid**, the
    roll-up of the rows above; cleared by the same two components.
- Key: A11Y-021, A11Y-024, TRP-018, TRP-019 moved to the refactored lines; A11Y-024's rationale names the field
  component; new clean entries for `CheckboxField.tsx` and `TextField.tsx`. No label changed.
- The new plants are all FNs, as expected: their concepts are `unmapped` for this scanner (no rule reads JSX keys,
  dependency arrays, state updates, dialog focus, autoplay or error association). The relabelled A11Y-014/015 are
  mapped children of the umbrella; their outcome is unchanged (FN at v1.0.0 too: the AC6 checks read no `.css` motion
  or outline rule). A11Y-010 and A11Y-013 were FNs before and are FNs on unmapped concepts now.

## 2026-10-07 — v1.1.0 scan iteration 2 (contained + two model passes), converged, freeze v1.1.0

- Over 503b290 (the code frozen here; the tag commit adds only this journal entry): contained scan 15 results — the four
  R10 rows are gone; against the v1.0.0 final scan the only differences are the A11Y-007 row on its new line (50,
  TP) and the AC7 row's file count (TP). Nothing new to judge.
- Host pass with the model, default configuration: 15 results. The M4 "README/code drift" row judged
  `false-positive` at v1.0.0 did not recur (M4 scored 90 with no finding, against 96 with the row at v1.0.0; the README
  and the ESLint configuration are unchanged, so this is the model pass varying, not a fix).
- Second host pass with `CODEHEALTH_COMPLIANCE_FRAMEWORKS=wcag-2.2` (informative, not the default configuration):
  17 results — LA2 at A11Y-019 and LA5 at A11Y-020 found on their lines as at v1.0.0; LA2 75 and LA5 95 in band. LA6
  was **not measured** this time: the scanner reports that its judge answered one batch short and the re-ask did not
  complete, so BND-005 is unscored in this pass (100, in band, at v1.0.0). Instrument variance, recorded; key unchanged.
- Outcome, default configuration: 12 of 24 plants found (50 %), 19 of 22 traps left alone (86 %), noise 20 % (the
  same three caught traps as v1.0.0). Every new plant is an FN: A11Y-021…024 are on concepts the scanner's mapping
  lists as `unmapped` (no rule), as are the relabelled A11Y-010 and A11Y-013; A11Y-014/015 are mapped children of the
  umbrella and stay FNs as at v1.0.0. The five new traps were left alone (TN). No result changed outcome because of a
  relabel: no scanner result had matched any relabelled entry at v1.0.0.
- Code and key frozen together as v1.1.0.
