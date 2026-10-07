# Benchmark: code-health defects and their look-alikes

This repository is one unit of the scanner benchmark in
[`code-assurance-initiative/scanner-benchmark`](https://github.com/code-assurance-initiative/scanner-benchmark).
Its labels are in [`answer-key.json`](answer-key.json) (format: `scanner-benchmark/docs/CONTRACT.md`, schema 1.2);
its authoring log is [`journal.md`](journal.md).

## Theme

A small .NET 10 **parcel shipping rates & label service**: an ASP.NET Core API that quotes rates across two
carriers (Alder Parcel, Corvid Courier — both fictional), creates shipping labels (ZPL), archives them and e-mails
them to the customer, plus a console tool that imports rate cards and prints labels. Like many real services it has
grown messy in places and stayed clean in others: the label pipeline and the carrier adapters carry the debt; the
domain model, the surcharge policy and the API surface are tidy.

The scaffolding matches `bench-csharp-baseline-clean` (CI pinned by commit SHA, CodeQL, Dependabot, README, ADR,
architecture doc, CHANGELOG, SECURITY.md, central package management with lock files, nullable, `src/` + `tests/`,
passing unit and integration tests), so that the only signal is the theme. The messy code builds and its tests pass:
every plant is a smell or a latent defect, never a compile error.

## Labels are about truth

A `must-fire` is something a careful senior reviewer would flag in a code review of *this* service. A
`must-not-fire` is a site a careless rule would flag and that reviewer would not. No label was chosen to match any
scanner. Lines in this table are indicative; the key holds the exact lines.

`CORE` = `src/Shipping.Rates.Core`, `API` = `src/Shipping.Rates.Api`, `TOOLS` = `src/Shipping.Rates.Tools`.

## Plants (`must-fire`)

| Id | Concept | Where | What and why it is a defect |
|---|---|---|---|
| CH-001 | high-cyclomatic-complexity | `CORE/Pricing/RateCalculator.cs` `ComputeSurcharges` | Every surcharge decided in one method: branches per carrier, level, zone, shape, value, with compound conditions. |
| CH-002 | high-cognitive-complexity | same method | Four levels of nesting, flag variables and early continues. |
| CH-003 | god-class | `CORE/Labels/LabelService.cs` | Parsing, validation, pricing, carrier choice, rendering, file persistence, SMTP e-mail and statistics in one 30+-method class. |
| CH-004 | long-method | `CORE/Labels/ZplLabelRenderer.cs` `Render` | The whole label in one 100+-line method. |
| CH-005 | compiled-code-size | same method | The largest authored method by emitted IL, far above a sensible budget. |
| CH-006 | duplicated-code | `CORE/Carriers/AlderParcelAdapter.cs` | Request building and response mapping copy-pasted from the Corvid adapter, already drifting. |
| CH-007 | duplicated-code | `CORE/Labels/LabelArchive.cs` | The same label-file predicate spelled out identically here and in the CLI print command. |
| CH-008 | low-class-cohesion | `CORE/Accounts/CarrierAccountManager.cs` | Four disconnected groups of fields and methods (credentials, rate limit, audit, webhook secret). |
| CH-009 | technical-debt-marker | `CORE/Carriers/CorvidCourierAdapter.cs` | `// TODO` recording known unfinished work. |
| CH-010 | technical-debt-marker | `CORE/Labels/LabelService.cs` | `// HACK` workaround in the shared path. |
| CH-011 | suppressed-diagnostic | `CORE/Labels/LabelRecord.cs` | File-wide `#pragma warning disable CS8618`, no reason, no restore — and the properties really can be null. |
| CH-012 | commented-out-code | `CORE/Labels/LabelService.cs` | The old label-number scheme left commented out. |
| CH-013 | empty-catch-block | `CORE/Labels/LabelService.cs` | Bare `catch { }` around the label e-mail: SMTP failures vanish. |
| CH-014 | unused-code | `CORE/Labels/LabelService.cs` | A private method nothing calls. |
| CH-015 | unreachable-code | `CORE/Pricing/RateCalculator.cs` | `#if` on a symbol nothing defines. |
| CH-016 | unreachable-code | `CORE/Carriers/CarrierRegistry.cs` | `case "alder":` under `switch (code.ToUpperInvariant())`. |
| CH-017 | obsolete-symbol-still-used | `CORE/Pricing/RateCalculator.cs` | `[Obsolete]` `Quote` still called by `LabelService`. |
| CH-018 | not-implemented-placeholder | `CORE/Carriers/CorvidCourierAdapter.cs` | `VoidLabelAsync` throws `NotImplementedException`, reachable from the API. |
| CH-019 | incomplete-implementation | `CORE/Pricing/RemoteAreaLookup.cs` | `IsRemoteArea` ignores its input and returns `false`: the surcharge never applies. |
| CH-020 | skipped-test-without-reason | `tests/…/ZplLabelRendererTests.cs` | `Skip = "fails on CI"` — no issue, no category. |
| CH-021 | pointless-catch-rethrow | `CORE/Carriers/CorvidCourierAdapter.cs` | `catch (HttpRequestException) { throw; }`. |
| CH-022 | rethrow-resets-stack-trace | `CORE/Labels/LabelService.cs` | `throw ex;`. |
| CH-023 | blocking-on-async-code | `API/Endpoints/LabelEndpoints.cs` | `.Result` on an async read inside a request handler. |
| CH-024 | blocking-on-async-code | `CORE/Labels/LabelArchive.cs` | Sync wrapper `.GetAwaiter().GetResult()` on the request path. |
| CH-025 | async-void-method | `CORE/Pricing/RateCardCache.cs` | `public async void WarmUp()` — not an event handler. |
| CH-026 | missing-cancellation-propagation | `CORE/Pricing/RateCardCache.cs` | HTTP download with no `CancellationToken`. |
| CH-027 | non-structured-log-message | `CORE/Carriers/CarrierRegistry.cs` | `Log…($"…{runtime value}…")`. |
| CH-028 | nullable-analysis-disabled | repository (`TOOLS` csproj) | `<Nullable>disable</Nullable>` in one project. |
| CH-029 | null-forgiving-suppression | repository (`LabelService`) | `!` on values that really can be null (carrier lookup, missing address, optional e-mail, index lookup). |
| CH-030 | null-dereference | `TOOLS/Printing/PrintCommand.cs` | `printerName?.Trim()`, then `printerName.Length` unguarded — the optional argument is null when omitted. |
| CH-031 | hand-rolled-structured-format-parsing | `CORE/Carriers/AlderParcelAdapter.cs` | Regex over a JSON error body in a project that uses System.Text.Json. |
| CH-032 | silent-error-fallback | `CORE/Carriers/CorvidCourierAdapter.cs` | Unparsable transit days silently become 3. |
| CH-033 | undrained-child-process-stream | `TOOLS/Printing/LabelPrinter.cs` | stdout and stderr redirected, only stdout read. |
| CH-034 | unbounded-truncation-loop | `CORE/Labels/ZplLabelRenderer.cs` `FitToWidth` | Drops characters while width (incl. fixed margins) is too large — no floor. |
| CH-035 | improper-resource-disposal | `CORE/Pricing/RateCardCache.cs` | Owned `Timer` never disposed by `Dispose`. |
| CH-036 | unrestored-process-global-state | `TOOLS/Import/RateCardImportCommand.cs` | Working directory restored only on the happy path. |
| CH-037 | argument-guard-tests-wrong-condition | `CORE/Pricing/MultiParcelQuoter.cs` | `ArgumentNullException` thrown for an empty list. |
| CH-038 | side-effect-in-conditional-guard | `CORE/Pricing/MultiParcelQuoter.cs` | `when … && ++count > max`. |
| CH-039 | lock-release-state-mismatch | `CORE/Labels/LabelPrintQueue.cs` | Returns "slot held" on a path whose `finally` releases it. |
| CH-040 | unguarded-expensive-debug-logging | `CORE/Pricing/QuoteService.cs` | `LogDebug` with `string.Join` over every quote, no level check. |
| CH-041 | inert-configuration-option | `CORE/Pricing/RateCardCache.cs` | Configured TTL stored and never read; the default re-spelled where it is used. |
| CH-042 | unsynchronized-callback-handoff | `TOOLS/Import/DropFolderWatcher.cs` | Watcher callback adds to a `List` the waiting loop reads, no lock. |
| CH-043 | collection-modified-during-enumeration | `CORE/Pricing/QuoteService.cs` | `foreach` over a `List` that the body removes from. |
| CH-044 | index-access-outside-bounds-guard | `CORE/Tracking/TrackingNumber.cs` | `value.Length > 0 && value[0] == … \|\| value[0] == …`. |
| CH-045 | loop-decision-on-fixed-element | `CORE/Pricing/MultiParcelQuoter.cs` | Per-parcel surcharge decided by `parcels[0]`. |
| CH-046 | contradictory-support-guard | `CORE/Pricing/QuoteService.cs` | `!(supportsInsurance \|\| insuredValue != 0)` — wrong polarity; dormant until a carrier without insurance is added. |
| CH-049 | suppressed-diagnostic | `CORE/Shipping.Rates.Core.csproj` | `<WarningsNotAsErrors>CS0618;CA2200</WarningsNotAsErrors>`: the obsolete-use and stack-trace-reset errors demoted project-wide, no reason, instead of fixing CH-017's caller and CH-022. |
| CH-050 | unused-code | `CORE/Pricing/RateCardCache.cs` | `_ttl` written in the constructor, read nowhere (the dead state behind CH-041). |
| CH-047 | type-lookup-by-simple-name | `CORE/Carriers/CarrierRegistry.cs` | `GetAssemblies().SelectMany(GetTypes).FirstOrDefault(t => t.Name == name)`. |
| CH-048 | redundant-condition-operand | `CORE/Tracking/TrackingNumber.cs` | `StartsWith("JJD") \|\| StartsWith("JJD00")`. |

## Traps (`must-not-fire`)

| Id | Concept | Where | Why it is not a defect |
|---|---|---|---|
| TRP-001 | high-cyclomatic-complexity | `CORE/Carriers/ServiceCodeMap.cs` | A flat (carrier, level) → product-code switch: many labels, no nesting, one-line arms. A lookup table, not complex logic. *Contested*, see below. |
| TRP-002…005 | cyclomatic / oversized file / god class / long method | `CORE/Pricing/Generated/CountryZones.g.cs` | Generated (`<auto-generated>` header, `[GeneratedCode]`), regenerated from the rate-card CSV; never edited by hand. |
| TRP-028 | compiled-code-size | `CORE/Pricing/Generated/CountryZones.g.cs` | Generated switch tables compile to ~3,000 IL instructions each; `[GeneratedCode]`, not authored. |
| TRP-029 | unused-code | `CORE/Pricing/RateCardCache.cs` | The never-read `_refreshTimer` field keeps the Timer alive (an unreferenced Timer is collected and stops firing). |
| TRP-030 | silent-error-fallback | `CORE/Carriers/AlderParcelAdapter.cs` | `"UNKNOWN"` error-code sentinel on an error path that is always logged and thrown. |
| TRP-006/007 | duplicated-code | `API/Contracts/AddressDto.cs`, `CORE/Carriers/Alder/AlderAddress.cs` | Intentional DTO pair across bounded contexts; data only. |
| TRP-008/009 | pointless rethrow / stack-trace reset | `CORE/Labels/LabelArchive.cs` | Logs with context, then `throw;`. |
| TRP-010 | async-void-method | `TOOLS/Import/RateCardReloader.cs` | Event handler; catches and logs everything. |
| TRP-011 | suppressed-diagnostic | `API/Hosting/ApiServiceCollectionExtensions.cs` | Opt-in to an `[Experimental]` API: scoped disable/restore around one statement, reason in the comment above. |
| TRP-012 | technical-debt-marker | `CORE/Carriers/CorvidCourierAdapter.cs` | `"TODO"` is a carrier status code in a string literal. |
| TRP-013 | blocking-on-async-code | `TOOLS/Program.cs` | `GetAwaiter().GetResult()` in a console `Main`. *Contested*, see below. |
| TRP-014 | low-class-cohesion | `CORE/Pricing/SurchargePolicy.cs` | Many small methods sharing one state: cohesive. |
| TRP-015 | empty-catch-block | `TOOLS/Import/RateCardReloader.cs` | Narrow `FileNotFoundException`, with the reason in a comment. |
| TRP-016 | non-structured-log-message | `CORE/Pricing/RateCardCache.cs` | Interpolation of a `const` only — a constant template. |
| TRP-017 | missing-configure-await | repository | Application libraries hosted only by ASP.NET Core and a console app; no synchronisation context, no package. |
| TRP-018 | unrestored-process-global-state | `TOOLS/Import/RateCardImportCommand.cs` | Restored in `finally`. |
| TRP-019 | collection-modified-during-enumeration | `CORE/Pricing/RateCardCache.cs` | `Dictionary.Remove` while enumerating `Keys` is supported since .NET Core 3.0. |
| TRP-020 | index-access-outside-bounds-guard | `CORE/Tracking/TrackingNumber.cs` | `value.Length == 0 \|\| value[0] == …` is covered. |
| TRP-021 | unbounded-truncation-loop | `CORE/Labels/ZplLabelRenderer.cs` | Loop condition has a floor. |
| TRP-022 | undrained-child-process-stream | `TOOLS/Printing/PrinterStatusProbe.cs` | Both streams drained. |
| TRP-023 | unreachable-code | `CORE/Carriers/CarrierRegistry.cs` | Upper-case labels under `ToUpperInvariant()`. |
| TRP-024 | hand-rolled-structured-format-parsing | `CORE/Tracking/TrackingNumber.cs` | Regex validating an identifier, not a structured format. |
| TRP-025 | silent-error-fallback | `CORE/Pricing/CutoffCalendar.cs` | Falls back to the documented default **and logs**. |
| TRP-026/027 | incomplete / placeholder | `CORE/Carriers/AlderParcelAdapter.cs` | `SchedulePickupAsync` throws `NotSupportedException` with a message: Alder does not collect (declared in its capabilities). |

## Contested truths, and how they were decided

- **TRP-001 — a long flat switch is not "too complex".** Cyclomatic complexity counts each case label, so a mapping
  table scores high. But the measure exists to estimate how hard code is to understand and test, and a table of
  one-line arms with no interaction is neither. A reviewer would accept it as written; replacing it with a
  dictionary moves the same table elsewhere. Had the arms contained logic, the answer would flip.
- **TRP-013 — `GetAwaiter().GetResult()` in a console `Main`.** `async Task Main` is nicer, but nothing is wrong:
  there is no synchronisation context to deadlock on and no thread pool being starved by a single blocked entry
  thread. The same call on a request path (CH-024) is a defect.
- **CH-028 / CH-029 / TRP-017 are repository-level.** Nullable posture, `!` density and ConfigureAwait adoption
  are properties of a project or codebase, and scanners report them without a line.
- **CH-016 vs TRP-023.** Two switches over an upper-cased code in one file: one has a lower-case label that can
  never match (defect), the other is correct. The trap guards against a rule that flags every case-normalised switch.

## What is clean

Every other tracked file (`CLN-*`) is certified free of every code-health concept this repository covers (the
concept list on each clean entry). Files that hold a plant or a trap carry no clean entry: they are labelled only at
their sites.

## Score bands (chosen from intent before the first scan)

| Id | Concept | Band | Why |
|---|---|---|---|
| BND-001 | high-cyclomatic-complexity | 70–97 | One over-complex method among ~200 small ones. |
| BND-002 | high-cognitive-complexity | 70–97 | The same. |
| BND-003 | duplicated-code | 75–99 | One clone pair in a ~2–3 kLoC codebase. |
| BND-004 | low-class-cohesion | 70–97 | One low-cohesion class among a few dozen. |
| BND-005 | compiled-code-size | 80–99 | One oversized method among a few hundred. |
| BND-006 | allocation-awareness | 60–80 | No allocation-aware APIs, none needed. |
| BND-007 | inconsistent-naming | 40–85 | Mostly idiomatic; the messy parts mix styles. Model-judged, wide. |
| BND-008 | low-value-comments | 40–85 | Restating and stale comments in the messy parts. Model-judged, wide. |

## What this repository does not cover

- `js-interop-contract-mismatch` is **not applicable** (`NA-001`): there is no Blazor and no JavaScript.
- `oversized-source-file` has a trap (the generated file) but no plant: a hand-written 500+-line file would
  duplicate the god-class plant without testing anything new.
- Security, dependencies, architecture, tests-as-a-dimension and history are covered by their own repositories.
  Findings of concepts this key does not cover are reported by the harness as *uncovered*, not as noise; an
  accidental real defect of another kind is fixed in the repository during authoring (see the journal).
