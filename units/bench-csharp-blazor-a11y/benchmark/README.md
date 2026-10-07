# bench-csharp-blazor-a11y — Blazor accessibility and JS-interop correctness

Part of the [code-assurance-initiative scanner benchmark](https://github.com/code-assurance-initiative/scanner-benchmark).
Labels are in [`answer-key.json`](answer-key.json) (format: `scanner-benchmark/docs/CONTRACT.md`); the authoring log is
[`journal.md`](journal.md). It is the C# counterpart of `bench-ts-frontend-a11y`.

## Theme

A small, realistic **Blazor Web App (net10.0, interactive server rendering)** with a **Razor Pages staff area**: the
room-booking portal of a community centre. Visitors browse rooms (photo gallery, narrated tour video, a map of the
building), filter by amenities, see a week calendar of bookings, book a room through a form, review the booking in a
summary dialog, cancel bookings and change their preferences. Staff edit rooms and the site notice in Razor Pages under
`/Admin`. JavaScript interop covers the map, a native `<dialog>`, the clipboard and a calendar-file download.
xUnit v3 + bUnit component tests, Central Package Management with lock files, CI with SHA-pinned actions, CodeQL and
Dependabot.

The team that wrote it is competent at Blazor but has **no accessibility discipline**: nothing tests accessibility, and
the defects such a check would catch are in the markup and the stylesheets. Its JS interop is mostly routed through
typed wrappers that a contract test guards; two components bypass them and call functions by string name that the
scripts no longer define. Beside each defect sits a look-alike that is correct.

## Plants (`must-fire`)

| Id | Where | Concept | Why it is a defect |
|---|---|---|---|
| BA-001 | `RoomDetail.razor` selected photo `<img>` | missing-text-alternative | content image with no alt or name |
| BA-002 | `RoomTourVideo.razor` `<video controls>` | missing-text-alternative | narrated tour without a captions track (WCAG 1.2.2) |
| BA-003 | `Calendar.razor` next-week button | form-control-without-label | icon-only `<button>` with no accessible name |
| BA-004 | `MyBookings.razor` filter `<input>` | form-control-without-label | named only by a placeholder |
| BA-005 | `_AdminLayout.cshtml` `<html>` | page-structure-violation | the staff area's documents declare no `lang` |
| BA-006 | `Settings.razor` section headings | page-structure-violation | `h1` → `h3` within one render |
| BA-007 | `Calendar.razor` booked slot `<div @onclick>` | non-keyboard-accessible-interaction | no role, tabindex, key handler or alternative control |
| BA-008 | `Book.razor` `tabindex="1"` | non-keyboard-accessible-interaction | positive tabindex |
| BA-009 | `MyBookings.razor` `<a href="#" @onclick>` | non-keyboard-accessible-interaction | link used as a button |
| BA-010 | `ConfirmDialog.razor` `<div role="dialog">` | modal-focus-not-managed | no `aria-modal`, focus move, containment, Escape or focus return |
| BA-011 | `Settings.razor` `role="switch"` | invalid-aria-usage | required `aria-checked` missing |
| BA-012 | `Calendar.razor.css` free-slot label | visual-and-motion-safety | normal text at about 2.5:1 |
| BA-013 | `Home.razor` hero `<video autoplay loop muted>` | autoplay-media-without-control | moving media that starts by itself with no way to pause it (WCAG 2.2.2) |
| BA-014 | `LoadingSkeleton.razor` `<style>` shimmer | motion-without-reduced-motion | infinite animation, no `prefers-reduced-motion` guard |
| BA-015 | `ToastHost.razor.css` slide-in | motion-without-reduced-motion | unconditional animation in a component stylesheet |
| BA-016 | `_AdminLayout.cshtml` `<style>` toolbar | focus-outline-removed | `outline: none`, no focus style anywhere |
| BA-017 | `NavMenu.razor.css` `:focus` | focus-outline-removed | focus ring removed from the navigation links, nothing replaces it |
| BA-018 | `Book.razor` attendees error | form-error-not-associated | error shown after the field, tied to nothing |
| BA-019 | `RoomDetail.razor` description | cross-site-scripting | stored, unsanitised HTML rendered through `MarkupString` |
| BA-020 | `RoomMap.razor` interop call | js-interop-contract-mismatch | string-named function the map script no longer defines |
| BA-021 | `MyBookings.razor.cs` interop call | js-interop-contract-mismatch | string-named function no script defines |
| BA-022 | repository | accessibility-checks-in-ci | no accessibility assertion in tests or CI |
| BA-023 | `Home.razor` foyer illustration | alt-text-quality | `alt` is the file name |
| BA-024 | `Home.razor` hire-terms link | link-and-button-text-quality | "click here" |
| BA-025 | `EditRoom.cshtml` hourly-rate label | heading-and-label-text-quality | the label reads "Value" |

## Traps (`must-not-fire`)

| Id | Where | Concept | Why it is NOT a defect |
|---|---|---|---|
| TRP-001 | `MainLayout.razor` footer divider | missing-text-alternative | decorative image, `alt=""` |
| TRP-002 | `Home.razor` hero video | missing-text-alternative | muted footage has no audio to caption |
| TRP-003 | `Calendar.razor` previous-week button | form-control-without-label | icon-only, but `aria-label` |
| TRP-004 | `Rooms.razor` search label | form-control-without-label | a visually-hidden (clip) label is still the programmatic label |
| TRP-005 | `AmenityChips.razor` chips | non-keyboard-accessible-interaction | `role="checkbox"` + `aria-checked` + `tabindex="0"` + key handler |
| TRP-006 | `MainLayout.razor` skip link | non-keyboard-accessible-interaction | `href="#main-content"` is a real fragment target |
| TRP-007 | `Rooms.razor` "Show more rooms" | non-keyboard-accessible-interaction | native `<button>` styled as a link |
| TRP-008 | `BookingSummaryDialog.razor` | modal-focus-not-managed | native `<dialog>` opened with `showModal()`: the platform manages focus |
| TRP-009 | `Home.razor.css` hero title | visual-and-motion-safety | below 4.5:1 but large text (36px bold): AA requires 3:1 |
| TRP-010 | `app.css` `:disabled` | visual-and-motion-safety | inactive controls are exempt from contrast |
| TRP-011 | `SavedIndicator.razor` `<style>` | motion-without-reduced-motion | animation only inside `prefers-reduced-motion: no-preference` |
| TRP-012 | `app.css` `h1:focus` | focus-outline-removed | the router's programmatic focus target, not a keyboard control |
| TRP-013 | `AmenityChips.razor.css` | focus-outline-removed | outline dropped only for `:focus:not(:focus-visible)`; a `:focus-visible` ring replaces it |
| TRP-014 | `RoomCard.razor` `<h3>` | page-structure-violation | rendered under the list's `<h2>`; no skip in the page |
| TRP-015 | `EditRoom.cshtml` (whole file) | page-structure-violation | a Razor Page (`@page "{id:guid}"`) titled by its layout, not a Blazor routed component |
| TRP-016 | `NoticeBanner.razor` | cross-site-scripting | `MarkupString` of allow-list-sanitised HTML |
| TRP-017 | `CopyLinkButton.razor` icon | invalid-aria-usage | `aria-hidden` on a non-focusable decorative svg inside a named button |
| TRP-018 | `AmenityChips.razor` chips | invalid-aria-usage | `role="checkbox"` with its required `aria-checked` |
| TRP-019 | `Book.razor` e-mail error | form-error-not-associated | `aria-invalid` + `aria-describedby` naming the error |
| TRP-020 | `ClipboardInterop.cs` | js-interop-contract-mismatch | a browser API, defined by the platform |
| TRP-021 | `DialogInterop.cs` | js-interop-contract-mismatch | exported by the imported dialog module under that exact name |
| TRP-022 | `ConfirmDialog.razor` heading | heading-and-label-text-quality | renders its `Title` parameter; every caller passes a descriptive question |

## What is certified clean

Every other tracked source, markup, stylesheet and script file carries a `clean` label for the themed concepts (text
alternatives, labels, page structure, keyboard semantics, ARIA, contrast/focus/motion, modal focus, autoplaying media,
form-error association, markup injection, JS-interop contracts and the three model-judged text-quality concepts). Files
that hold a plant or a trap are not certified as a whole; the plant and trap entries speak for their sites.

The JS interop outside the two plants is correct by construction: interop runs in `OnAfterRenderAsync` (never during
prerendering), `DotNetObjectReference` and imported modules are disposed, and the contract test checks every function
name the typed wrappers in `Interop/` invoke against the exports of `wwwroot/js`.

## Score bands

- **Accessibility enforcement** [0, 20]: nothing enforces it (BA-022 is the same fact as a finding).
- **Model-judged text quality** (alt text [50, 90], link/button text [70, 100], heading/label text [70, 100]): set from
  the code's intent before any scan — one weak alt among four static ones, one vague link and one placeholder label
  among a few dozen.

## Deliberately not covered

- **Secrets, injection beyond markup, dependencies, IaC, architecture, tests**: other benchmark repositories.
- **Interop called during prerendering, and an undisposed `DotNetObjectReference`**: real Blazor defects, but the
  taxonomy has no concept for them; the code is written free of both rather than carry unlabelled defects.

## Configuration

The model-judged text-quality concepts (BA-023…025, BND-002…004) are measured by some scanners only when an
accessibility compliance framework is switched on. The headline numbers are the scanner's default configuration; a
framework-on run is recorded as a secondary configuration.

## Reproduce

```bash
git clone https://github.com/code-assurance-initiative/bench-csharp-blazor-a11y
git clone https://github.com/code-assurance-initiative/scanner-benchmark
cd bench-csharp-blazor-a11y && dotnet build -c Release && dotnet test -c Release
# run any scanner over the clone, producing SARIF, then:
cd ../scanner-benchmark
python3 -m cai_bench validate --key ../bench-csharp-blazor-a11y/benchmark/answer-key.json --taxonomy taxonomy.json
python3 -m cai_bench score --help
```
