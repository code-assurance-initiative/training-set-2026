# Authoring journal — bench-csharp-blazor-a11y

## 2026-10-07 — answer key v1.0.0 drafted (key first)

- Wrote `benchmark/answer-key.json` before any code: 25 `must-fire` (BA-001…025), 21 `must-not-fire`
  (TRP-001…021), 4 `score-band` (BND-001…004). Schema 1.2. Lines are planned positions; they are fixed to the final
  code in the implementation step, and the `clean` entries are added from the final tree.
- Purpose: the C# rows AC1–AC7, LA2, LA5, LA6 and X8 were measured only by clean regions in frozen keys (noise
  measured, recall and trap resistance not). This repository plants each of them in a Blazor Web App + Razor Pages
  codebase, with a look-alike trap beside each, and adds the precise frontend concepts of contract 1.4
  (`modal-focus-not-managed`, `autoplay-media-without-control`, `form-error-not-associated`, `focus-outline-removed`,
  `motion-without-reduced-motion`) and one markup-injection pair (`MarkupString`).
- Truth decisions recorded before the code:
  - A muted, control-less, looping hero video owes no captions (no audio: TRP-002) but does owe a pause mechanism
    (BA-013, WCAG 2.2.2).
  - A visually-hidden (clip) label is a label; a large-text pair between 3:1 and 4.5:1 passes AA; a disabled control is
    exempt from contrast; an outline removed only for `:focus:not(:focus-visible)` with a `:focus-visible` ring, or only
    from the heading the router focuses programmatically, is not a removed focus indicator.
  - A native `<dialog>` opened with `showModal()` manages focus; a `<div role="dialog">` with no focus code does not.
  - Focus-outline and motion plants are placed both in `<style>` blocks inside markup and in stylesheets, because
    both are where Blazor teams write CSS; one of each is in each place.
  - A Razor Page with a quoted route template is titled by its layout's `<title>`; it is not a Blazor routed component
    (TRP-015).
  - A string-named interop call to a browser API (`navigator.clipboard…`) is not a contract mismatch; neither is a call
    on an imported module that exports the name.
  - Prerender-time interop and an undisposed `DotNetObjectReference` have no taxonomy concept: the code is written
    free of them instead of carrying unlabelled defects.
- Validated with `python3 -m cai_bench validate` against `taxonomy.json`: OK (50 entries).

## 2026-10-07 — implemented; key lines fixed to the code

- Implemented the portal: .NET 10 Blazor Web App (interactive server, global interactivity with a static error page),
  Razor Pages staff area, OpenID Connect sign-in with member and staff policies, security headers (CSP, HSTS, frame
  and content-type policies), HtmlSanitizer for the site notice, typed interop wrappers and three scripts. Core library
  with in-memory stores. `dotnet build -c Release` (warnings as errors, latest-recommended analyzers) and
  `dotnet test -c Release` are green: 79 unit/bUnit tests, 9 in-memory integration tests. No vulnerable package,
  direct or transitive; locked-mode restore passes.
- Every plant and trap sits where the key says; lines resolved from the final tree with `grep -n` / `sed -n`. Plant
  and trap lines of one concept in one file are the elements' start lines, so neighbours (the two week buttons) do not
  overlap within the line tolerance.
- Decisions made while writing the code (no label changed meaning):
  - Room photos and videos come from the centre's media host (named in the CSP), not from files in the repository.
  - The building map's room areas are drawn by script; they are given `role="link"`, `tabindex="0"`, a name and an
    Enter handler, so the map carries no unlabelled keyboard defect.
  - The amenity chips do not cancel the default action of Space (Blazor decides `preventDefault` at render time, and
    cancelling every key would trap Tab); Space toggles the chip and may also scroll a scrollable page, which is not an
    accessibility failure.
  - A 120 ms transition on the settings switch knob was removed rather than leave an unlabelled motion site.
  - BA-007's panel shows reference, times, attendees and organiser (an earlier draft also had a copy-link button there).
  - The interop contract test covers the wrappers in `Interop/`, as ADR 0002 says; the two plants are direct calls from
    components, outside it, so the test is green with the plants in place.
- Clean list built from the final tree: 134 files (every tracked file except plant/trap files and `benchmark/`).
  The key validates: 184 entries (25 must-fire, 21 must-not-fire, 134 clean, 4 score-band).

## 2026-10-07 — scan iteration 1 (contained), judged

- Scanner: the reference scanner at engine commit 6a05dfb6c, rubric-2026.10.1, contained mode, over repository commit
  6c61ed5; 29 results. Untracked build output was removed (`git clean -xdf`) before the scan so the scanned tree is
  what a clone holds.
- Expected hits: 16 of 25 plants — every located Razor/CSHTML plant of AC1–AC5 on its line (photo without alt, tour
  video without captions, icon-only next-week button, placeholder-only filter, admin layout without `lang`, h1→h3,
  click-only booked slot, `tabindex="1"`, `href="#"` cancel link, switch without `aria-checked`), the contrast plant in
  a scoped stylesheet, the motion and focus-outline plants that sit in `<style>` blocks, the repository-level AC7
  plant, and both JS-interop plants on the string literal's line.
- **Traps caught (2), recorded as scanner noise:**
  - TRP-009 — "Low contrast colour pair in CSS (3.8:1)" on `.hero-title`: **false-positive**. The same rule sets
    `font-size: 2.25rem` and `font-weight: 700` (36px bold, large text), for which AA requires 3:1.
  - "Routed page without a `<PageTitle>`" on `Pages/Admin/EditRoom.cshtml:7`: **false-positive**. The file is a Razor
    Page (`@page "{id:guid}"`), not a Blazor routed component; `<PageTitle>` does nothing there. Its title is the admin
    layout's `<title>@ViewData["Title"] · Staff area · Harbour Lane</title>`, and the page sets `ViewData["Title"]` on
    line 4. This is the TRP-015 site, but the scanner reports the page at its first element (line 7), not at the
    `@page` directive the draft key named (line 1), so the result fell off the trap. **Key change:** TRP-015 now
    covers the whole file. Its claim was always about the page as a document (its title and outline), and every line
    of the file is page-structure-correct; the label and concept are unchanged, and widening it can only charge a
    scanner, never credit one.
- **Valid (2), repository fixed:**
  - D8 "Low coverage: 0.0 %" on the three admin page models (`EditRoom`, `Index`, `Notice`): accurate — no test
    exercised them. Fixed with page-model unit tests (get, post, invalid post, unknown room, notice round trip).
  - X2 "Not all async methods take a CancellationToken: 14/15" (detail: `LoadAsync`): accurate — the bookings page
    passed `CancellationToken.None` everywhere, so leaving the page did not stop its work. Fixed: the component owns a
    `CancellationTokenSource` cancelled on dispose and passes its token to every call. The BA-021 interop call moved
    from line 52 to 54 (same call, the token added as its first argument).
- **Found while reviewing the results, fixed:** the root `.editorconfig` comment above the CA2007 entry (copied from
  the shared scaffolding) pointed at a `src/.editorconfig` this repository does not have. Rewritten to say where the
  rule is off (tests, the web project) and where it is on (the Core library).
- **Noise, code kept:**
  - D5 "HarbourLane.Bookings.Core: zone of pain": **opinion-not-fact** — a concrete core library depended on by the
    host is the intended shape of a two-project application (ADR 0001); the main-sequence distance is a heuristic.
  - D17 `.editorconfig` CA2007 severity none: **false-positive** — the reason is the comment directly above the entry
    (and the Core library switches the rule back on). D17 `tests/.editorconfig` CA1707 severity none:
    **false-positive** — the comment directly above says why (test names are sentences).
  - D17 "Dead code: Money": **false-positive** — `Money.PerHour` is called from `RoomCard.razor:9` and
    `RoomDetail.razor:16`; the reference walk did not include the Razor-generated code.
  - P2 "Logging is not universal: 1/2": **opinion-not-fact** — the row itself says the other project is a library
    whose host decides logging; the host logs.
  - P6 "Changelog looks stale/thin: 2 versioned entries, bar 3": **opinion-not-fact** — a project at its first
    release has one release to log.
  - X7 "Silent fallback default on parse failure: `RoomName` falls back to "Room"": **false-positive** — nothing is
    parsed: the room id comes from the booking store as a `Guid` and is looked up in a dictionary of the catalogue's
    rooms, which are never removed; the fallback is a display default for a list, not a swallowed parse error.
- **Score band out:** BND-001 (accessibility enforcement, band 0–20) scored 40: the scanner reports rung 0 ("No
  accessibility enforcement found", the BA-022 hit) on a scale whose floor is 4/10. Band kept (set from intent).
  BND-002…004 unscored: the model-judged dimensions do not run in a contained pass.
- **Missed plants (9), each re-verified in the code:** BA-010 (hand-rolled modal), BA-013 (autoplaying hero video)
  and BA-018 (unassociated attendees error) — concepts no rule of this scanner maps; BA-015 and BA-017 (animation and
  outline removal in `.razor.css` files — those two checks read only `<style>` blocks and inline styles; the `<style>`
  plants BA-014 and BA-016 were found); BA-019 (staff-written room description cast to `MarkupString` — no markup
  injection rule fired); BA-023…025 (model-judged; not run in a contained pass — judged in the host passes).
- Clean list regenerated from the tree (the two new test files joined it; clean ids renumbered, pre-freeze).

## 2026-10-07 — scan iteration 2 (contained + model pass), judged

- Contained scan over 2e8a00e: 25 results. The three D8 rows and the X2 row are gone (fixed in iteration 1); the
  EditRoom page-title row now lands on TRP-015 (trap caught, false-positive as judged). Everything else is the
  iteration-1 set with the same verdicts (D5, D17 ×3, P2, P6, X7 noise; TRP-009 caught). 16 of 25 plants found.
- Host pass with the model on (`--host --with-llm`, default configuration): the same results (the D17 dead-code row
  for `Money` does not appear in host mode) plus one model-judged row: D20 on ADR 0002, "does not explicitly state the
  trade-off regarding developer experience or code verbosity". **Valid:** the ADR listed only what the wrappers buy.
  Fixed in 5e74f77 by a consequence bullet that states the cost (a wrapper class per script, one more hop per call)
  and when the decision would be revisited. Model scores: D19 80, D20 90, D21 100, D24 100, D25 100, M4 100.
- LA2, LA5 and LA6 produced no card: they run only with an accessibility compliance framework configured.

## 2026-10-07 — scan iteration 3 (contained + model pass + framework-on model pass), converged

- Over 5e74f77: contained scan 25 results, host model pass (default configuration) 24 results — identical to
  iteration 2 except that the D20 row is gone (D20 100). No new result in the default configuration.
- **Secondary configuration** — host model pass with `CODEHEALTH_COMPLIANCE_FRAMEWORKS=wcag-2.2` (not the default
  configuration; recorded separately, never in headline numbers): 27 results — the default-configuration set plus
  LA2 "Weak alt text" at `Home.razor:27` (BA-023) and LA5 "Vague link/button text" at `Home.razor:36` (BA-024), both on
  their lines, and one LA6 row:
  - LA6 "Weak heading/label text" at `ConfirmDialog.razor:5`, reading `"@Title"`: **false-positive.** The heading
    renders the component's `Title` parameter; the only caller passes "Cancel this booking?". The judge was shown the
    Razor expression, not the text a user hears. **Key change:** promoted to trap TRP-022
    (`heading-and-label-text-quality`, must-not-fire) — a parameter-bound heading is a good probe for any judged
    text-quality check. No code changed.
  - BA-025 (the hourly-rate label reading "Value") was not reported: re-verified — the label names nothing about the
    field it labels; a scanner false negative.
  - Scores with the framework on: LA2 75 (3 of 4 static alts meaningful — exactly the intended ratio), LA5 97, LA6 98;
    all three bands in.
- Outcome, default configuration (headline): 16 of 25 plants found (64 %), 19 of 21 traps left alone at iteration 3
  (TRP-009 and TRP-015 caught), noise 2 of 18 covered results. Framework on: 18 of 25 (72 %).

## 2026-10-07 — scan iteration 4 (confirmation) and freeze v1.0.0

- Contained scan over 12ec46a (the code frozen here; the tag commit adds only this journal entry): 18 covered results,
  result for result identical to iteration 3. The iteration-3 model passes (same code) were re-scored against the final
  key: no new result.
- Final outcome, default configuration: **16 of 25 plants found (64 %), 20 of 22 traps left alone (91 %), noise 2 of
  18 covered results** (the two caught traps, TRP-009 and TRP-015). Framework on (secondary): 18 of 25 (72 %), 19 of 22
  traps (TRP-022 caught as well).
- Missed (default): BA-010, BA-013, BA-018 (no rule maps their concepts), BA-015, BA-017 (motion and outline removal
  in `.razor.css` files are not read; the `<style>`-block twins BA-014/016 were found), BA-019 (`MarkupString` of
  stored HTML), BA-023…025 (model-judged dimensions off by default; with the framework on, BA-023 and BA-024 were
  found and BA-025 was not).
- Score bands: BND-001 out (40 against 0–20: the enforcement scale's floor for rung 0); BND-002…004 unscored by
  default, in with the framework on (LA2 75, LA5 97, LA6 98).
- Key changes during the loop: TRP-015 widened from the `@page` line to the whole file (the claim is about the page);
  TRP-022 added (promoted from scanner noise); BA-021 line 52 → 54 after the cancellation fix. No plant was removed or
  weakened. Code and key frozen together as v1.0.0.
