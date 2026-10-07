# bench-ts-frontend-a11y — frontend quality and accessibility

Part of the [code-assurance-initiative scanner benchmark](https://github.com/code-assurance-initiative/scanner-benchmark).
Labels are in [`answer-key.json`](answer-key.json) (format: `scanner-benchmark/docs/CONTRACT.md`); the authoring log is
[`journal.md`](journal.md).

## Theme

A small, realistic **React 19 + TypeScript single-page app** built with Vite: the public catalogue of a community
library. Visitors search the catalogue, filter results with chips, open an item in a detail dialog, borrow it through a
form dialog, browse a photo gallery and the recording of a storytime session, check their loans in a data table, keep a
reading list, and change preferences on a settings page; toasts confirm what happened. Strict TypeScript, vitest + Testing Library
tests, a committed `package-lock.json`, CI with SHA-pinned actions, CodeQL and Dependabot.

The team that wrote it is competent at React and TypeScript but has **no accessibility discipline**: nothing lints or
tests accessibility, and the defects such a check would catch are in the code. Beside each defect sits a look-alike
that is correct, so a scanner that pattern-matches without understanding is caught.

## Plants (`must-fire`)

| Id | Where | Concept | Why it is a defect |
|---|---|---|---|
| A11Y-001 | `ItemDetailDialog.tsx` cover `<img>` | missing-text-alternative | content image with no `alt`/name |
| A11Y-002 | `StorytimeRecording.tsx` `<video controls>` | missing-text-alternative | speech recording without a captions track (WCAG 1.2.2) |
| A11Y-003 | `ToastRegion.tsx` dismiss button | form-control-without-label | icon-only `<button>` with no accessible name |
| A11Y-004 | `LoansPage.tsx` filter `<input>` | form-control-without-label | named only by a placeholder |
| A11Y-005 | `index.html` `<html>` | page-structure-violation | no `lang` |
| A11Y-006 | `SettingsPage.tsx` section headings | page-structure-violation | `h1` → `h3` within one render |
| A11Y-007 | `Gallery.tsx` thumbnail `<div onClick>` | non-keyboard-accessible-interaction | no role, tabindex, key handler or alternative control |
| A11Y-008 | `SearchBar.tsx` `tabIndex={1}` | non-keyboard-accessible-interaction | positive tabindex |
| A11Y-009 | `SearchBar.tsx` `<a href="#" onClick>` | non-keyboard-accessible-interaction | link used as a button |
| A11Y-010 | `ItemDetailDialog.tsx` `<div role="dialog">` | modal-focus-not-managed | modal without `aria-modal`, focus move, focus containment, focus return or Escape |
| A11Y-011 | `SettingsPage.tsx` `role="switch"` | invalid-aria-usage | required `aria-checked` missing |
| A11Y-012 | `catalogue.css` result metadata | visual-and-motion-safety | body text at about 2.7:1 |
| A11Y-013 | `HomePage.tsx` hero `<video autoPlay loop muted>` | autoplay-media-without-control | autoplaying looping video with no way to pause or stop it (WCAG 2.2.2) |
| A11Y-014 | `loading.css` shimmer | motion-without-reduced-motion | infinite animation, no `prefers-reduced-motion` guard |
| A11Y-015 | `search.css` `:focus { outline: none }` | focus-outline-removed | focus indicator removed, nothing replaces it |
| A11Y-016 | `ItemDetailDialog.tsx` summary | cross-site-scripting | API-supplied HTML into `dangerouslySetInnerHTML`, unsanitised |
| A11Y-017 | `LoansPage.tsx` (whole file) | oversized-source-file | 500+ line kitchen-sink module of six responsibilities |
| A11Y-018 | repository | accessibility-checks-in-ci | no a11y lint rule set, no a11y assertion in tests or CI |
| A11Y-019 | `HomePage.tsx` reading-room photo | alt-text-quality | `alt` is the camera file name |
| A11Y-020 | `HomePage.tsx` opening-hours link | link-and-button-text-quality | "Click here" |
| A11Y-021 | `ReadingListPage.tsx` rows `key={index}` | react-index-as-key | reorderable, filterable list whose rows hold a note draft: the draft follows the position, not the book |
| A11Y-022 | `Gallery.tsx` arrow-key effect | react-hook-missing-dependency | handler reads `activeIndex`, deps list only `viewerOpen`: stale closure, the viewer sticks after one step |
| A11Y-023 | `useReadingList.ts` `markFinished` | react-state-mutation | assigns `entry.finished` on the object held in state, then copies the array |
| A11Y-024 | `ReadingListPage.tsx` add-a-book title error | form-error-not-associated | error shown under the field, but no `aria-describedby`, no `aria-invalid`, no live region |

## Traps (`must-not-fire`)

| Id | Where | Concept | Why it is NOT a defect |
|---|---|---|---|
| TRP-001 | `Gallery.tsx` ornament | missing-text-alternative | decorative image, `alt=""` + `role="presentation"` |
| TRP-002 | `HomePage.tsx` hero video | missing-text-alternative | muted, control-less footage has no audio to caption |
| TRP-003 | `ItemDetailDialog.tsx` close button | form-control-without-label | icon-only, but `aria-label="Close"` |
| TRP-004 | `SearchBar.tsx` label | form-control-without-label | visually-hidden (clip) label is still the programmatic label |
| TRP-005 | `ResultCard.tsx` card `onClick` | non-keyboard-accessible-interaction | pointer convenience; a real button in the card does the same |
| TRP-006 | `FilterChip.tsx` | non-keyboard-accessible-interaction | `role="checkbox"` + `aria-checked` + `tabIndex={0}` + key handler |
| TRP-007 | `SiteHeader.tsx` skip link | non-keyboard-accessible-interaction | `href="#main-content"` is a real fragment target |
| TRP-008 | `BorrowDialog.tsx` native `<dialog>` | non-keyboard-accessible-interaction | `showModal()` gives focus move, inert background and Escape; backdrop click is a convenience |
| TRP-009 | `ResultList.tsx` "Load more" | non-keyboard-accessible-interaction | native `<button>` styled as a link |
| TRP-010 | `hero.css` title | visual-and-motion-safety | below 4.5:1 but large text (36px bold): AA requires 3:1 |
| TRP-011 | `buttons.css` `:disabled` | visual-and-motion-safety | inactive controls are exempt from contrast (WCAG 1.4.3) |
| TRP-012 | `toast.css` | motion-without-reduced-motion | animation only inside `prefers-reduced-motion: no-preference` |
| TRP-013 | `buttons.css` focus ring | focus-outline-removed | outline dropped only for `:focus:not(:focus-visible)`; a `:focus-visible` ring replaces it |
| TRP-014 | `ResultCard.tsx` `<h3>` | page-structure-violation | renders under the list's `<h2>`; no skip in the page |
| TRP-015 | `NoticeBanner.tsx` | cross-site-scripting | `dangerouslySetInnerHTML` fed by `DOMPurify.sanitize` with an allow-list |
| TRP-016 | `subjectHeadings.ts` | oversized-source-file | long data table, no logic |
| TRP-017 | `IconButton.tsx` icon | invalid-aria-usage | `aria-hidden` on a non-focusable decorative svg inside a named button |
| TRP-018 | `ReadingListPage.tsx` reading tips | react-index-as-key | index key on a constant, never-reordered, stateless list |
| TRP-019 | `ReadingListPage.tsx` `shown` memo | react-hook-missing-dependency | reads `entries` and `hideFinished`, lists both |
| TRP-020 | `useReadingList.ts` `move` | react-state-mutation | `splice` only on a fresh copy, returned from a functional update |
| TRP-021 | `BorrowForm.tsx` card-number error | form-error-not-associated | `aria-invalid` + `aria-describedby` naming the error |
| TRP-022 | `BorrowDialog.tsx` native `<dialog>` | modal-focus-not-managed | `showModal()`: the platform moves, contains and returns focus |

## What is certified clean

Every other tracked source, stylesheet and markup file carries a `clean` label for the themed concepts (text
alternatives, labels, page structure, keyboard semantics, ARIA, contrast/focus/motion, markup injection and file size;
since v1.1.0 also modal focus, autoplaying media, index keys, hook dependencies, state mutation and form-error
association).
Files that hold a plant or a trap are not certified as a whole; the plant and trap entries speak for their sites. (One exception since v1.1.0: `BorrowForm.tsx` stays certified
as a whole beside its TRP-021 look-alike, because the whole file is correct for every listed concept.)

## Score bands

- **Accessibility enforcement** [0, 20]: nothing enforces it (the A11Y-018 plant is the same fact as a finding).
- **Type safety** [90, 100]: all TypeScript under `strict`.
- **Model-judged text quality** (alt text [50, 90], link/button text [70, 100], heading/label text [80, 100]): set
  from the code's intent before any scan — one weak alt among a handful, one vague link among a few dozen, no
  placeholder headings or labels.

## Deliberately not covered

- **Secrets, injection beyond markup, dependencies, IaC, architecture, tests**: other benchmark repositories.
- `index.html` is the SPA shell and has no `<main>` of its own; `App.tsx` renders the `<main>` landmark into it. This
  is correct, but it is not a labelled trap because any page-structure result on that file would sit on the A11Y-005
  site; such results are judged from the raw scanner output in the journal.

## Versions

- **v1.0.0** — the original key and code.
- **v1.1.0** — the harness gained concepts the author lacked (contract 1.4). A11Y-010, -013, -014 and -015 (and the
  traps TRP-012, -013) were relabelled to the precise concepts on the same sites; the React-correctness and
  form-error plants the author had omitted were added (A11Y-021…024, a reading-list page and a gallery keyboard
  shortcut) with their look-alikes (TRP-018…022). `react-hooks/exhaustive-deps` is a warning in the plugin's preset,
  so `npm run lint` passes with one warning, on the A11Y-022 site; no rule was changed and nothing is suppressed.

## Reproduce

```bash
git clone https://github.com/code-assurance-initiative/bench-ts-frontend-a11y
git clone https://github.com/code-assurance-initiative/scanner-benchmark
cd bench-ts-frontend-a11y && npm ci && npm run lint && npm run typecheck && npm test && npm run build
# run any scanner over the clone, producing SARIF, then:
cd ../scanner-benchmark
python3 -m cai_bench validate --key ../bench-ts-frontend-a11y/benchmark/answer-key.json --taxonomy taxonomy.json
python3 -m cai_bench score --help
```
