# Benchmark: code-health defects and their look-alikes (TypeScript)

This repository is one unit of the scanner benchmark in
[`code-assurance-initiative/scanner-benchmark`](https://github.com/code-assurance-initiative/scanner-benchmark).
Its labels are in [`answer-key.json`](answer-key.json) (format: `scanner-benchmark/docs/CONTRACT.md`, schema 1.2);
its authoring log is [`journal.md`](journal.md). It is the TypeScript counterpart of `bench-csharp-codehealth` and
follows the same method.

## Theme

A small Node.js 22 + TypeScript **parcel shipping rates & label service**: an Express API that quotes rates across
two carriers (Alder Parcel, Corvid Courier — both fictional), creates shipping labels (ZPL), archives them and
e-mails them to the customer, plus a command-line tool that imports rate cards and prints labels. Like many real
services it has grown messy in places and stayed clean in others: the label pipeline, the carrier adapters, the
rate-card cache and the tools carry the debt; the value objects, the surcharge policy, the API surface and the
rich label-batch aggregate are tidy.

The scaffolding matches `bench-ts-baseline-clean` (CI pinned by commit SHA, CodeQL, Dependabot with a cooldown,
README, ADRs, architecture doc, CHANGELOG, SECURITY.md, committed `package-lock.json`, strict TypeScript, ESLint with
typescript-eslint, vitest, `src/` + `tests/`), so that the only signal is the theme. The messy code type-checks,
lints and its tests pass: every plant is a smell or a latent defect, never a build error. Where the lint gate would
have stopped a plant, the realistic way it got through is itself planted (an unexplained rule switch-off for the
legacy files, a bare file-wide disable, a `@ts-ignore`).

## Labels are about truth

A `must-fire` is something a careful senior reviewer would flag in a code review of *this* service. A
`must-not-fire` is a site a careless rule would flag and that reviewer would not. No label was chosen to match any
scanner. Lines are not given here; the key holds them.

`APP` = `src/application`, `DOM` = `src/domain`, `INF` = `src/infrastructure`, `API` = `src/api`.

## Plants (`must-fire`)

| Id | Concept | Where | What and why it is a defect |
|---|---|---|---|
| CH-001 | high-cyclomatic-complexity | `APP/pricing/rate-calculator.ts` | computeSurcharges decides every surcharge in one method: nested branches per carrier, service level, zone, parcel shape and declared value, with compound conditions. Well over any reasonable decision-point budget, and each branch is a distinct path that needs its own test. |
| CH-002 | high-cognitive-complexity | `APP/pricing/rate-calculator.ts` | The same method nests its branches four levels deep (carrier, service level, zone, parcel shape) with flag variables and early continues: a reader has to hold the whole decision tree to change one surcharge. |
| CH-003 | god-class | `APP/labels/label-service.ts` | LabelService parses label requests, validates addresses, prices the shipment, picks a carrier, renders the label, writes files and an index, sends the customer e-mail, keeps statistics and purges old labels: more than thirty methods and several hundred lines spanning at least five unrelated responsibilities. The whole class is the site. |
| CH-004 | oversized-source-file | `APP/labels/label-service.ts` | The file holding LabelService runs to well over 500 significant lines of hand-written code, far beyond what one module of this service should hold; the size is the symptom of the god class, and a reviewer would ask for it to be split along its responsibilities. |
| CH-005 | long-method | `APP/labels/zpl-label-renderer.ts` | render writes the whole ZPL label in one function of well over a hundred lines: sender, recipient, routing, barcode, service box, references and the receipt stub, each a section a reader has to scroll through. |
| CH-006 | duplicated-code | `INF/carriers/alder-parcel-adapter.ts` | The rate-request building and the parcel mapping were copied from the Corvid adapter and are already drifting (one rounds weights up, the other does not): a fix to one copy will be missed in the other. |
| CH-007 | low-class-cohesion | `APP/accounts/carrier-account-manager.ts` | CarrierAccountManager holds four groups of fields and methods that never touch each other: API credentials, a request rate limit, an audit trail and the webhook signing secret. Four classes in one. |
| CH-008 | technical-debt-marker | `INF/carriers/corvid-courier-adapter.ts` | A TODO comment recording known unfinished work in the production adapter (multi-piece shipments are sent as single parcels). |
| CH-009 | technical-debt-marker | `APP/labels/label-service.ts` | A HACK comment marking a workaround in the shared label path (the Corvid label is re-rotated after rendering). |
| CH-010 | suppressed-diagnostic | `APP/labels/label-service.ts` | @ts-ignore with no reason hides a real type error: the optional contact e-mail is assigned to a field typed as always present, so the e-mail step later reads undefined. |
| CH-011 | suppressed-diagnostic | `INF/carriers/alder-parcel-adapter.ts` | A bare file-wide eslint-disable with no rule list and no reason switches every lint rule off for the adapter, which is how its unchecked any casts got past review. |
| CH-012 | suppressed-diagnostic | `eslint.config.js` | A config block turns the type-safety and promise rules (no-floating-promises, no-explicit-any, no-non-null-assertion, no-empty, ban-ts-comment, ...) off for the label pipeline with no reason given: the project-level suppression that lets the plants in that module through the lint gate. |
| CH-013 | commented-out-code | `APP/labels/label-service.ts` | The old label-number scheme left behind as commented-out code instead of being deleted (version control keeps it). |
| CH-014 | empty-catch-block | `APP/labels/label-service.ts` | An empty catch around the customer e-mail: any failure to send vanishes without a log line or a metric, and the customer silently gets no label. |
| CH-015 | unused-code | `APP/labels/label-service.ts` | A private method that nothing calls: dead code kept in the god class. |
| CH-016 | unused-code | `DOM/value-objects/tracking-number.ts` | An exported function that no module, tool or test imports: dead code at module level. |
| CH-017 | unreachable-code | `INF/carriers/carrier-registry.ts` | A lower-case case label under switch (code.toUpperCase()): it can never match, so that arm is unreachable (the intended 'ALDER' alias was never served). |
| CH-018 | obsolete-symbol-still-used | `APP/labels/label-service.ts` | RateCalculator.quote is marked @deprecated in favour of quoteAll, yet the label service still calls it. |
| CH-019 | not-implemented-placeholder | `INF/carriers/corvid-courier-adapter.ts` | voidLabel throws a 'Not implemented' error although the API exposes label voiding for every carrier: a placeholder reachable in production. |
| CH-020 | incomplete-implementation | `APP/pricing/remote-area-lookup.ts` | isRemoteArea ignores its input and returns false, so the remote-area surcharge the calculator depends on never applies: a stub left in production. |
| CH-021 | silent-error-fallback | `INF/carriers/corvid-courier-adapter.ts` | An unparsable transit-days value from the carrier silently becomes 3 days, with no log line and no signal to the caller: delivery promises are made up. |
| CH-022 | pointless-catch-rethrow | `INF/carriers/corvid-courier-adapter.ts` | A catch whose only statement rethrows the caught error unchanged: it adds nothing and suggests handling that does not exist. |
| CH-023 | unchecked-any-external-data | `INF/carriers/alder-parcel-adapter.ts` | The carrier's JSON response body is cast to any and its fields read directly: a changed or failed response turns into undefined amounts deep in pricing instead of a validation error at the boundary. |
| CH-024 | nullable-analysis-disabled | repository | tools/tsconfig.json compiles the rate-card CLI with strictNullChecks switched off while the service has it on, so null and undefined flow unchecked through the tools. Repository-level: strict-null posture is a property of the build configuration, reported without a line. |
| CH-025 | null-forgiving-suppression | repository | LabelService silences null checking with non-null assertions (!) on values that really can be missing: a carrier lookup by code, an optional sender address, a map lookup by label id and a find over rate quotes. Repository-level: assertion density is reported without a line. |
| CH-026 | null-dereference | `tools/print-label.ts` | printerName is optional and treated as possibly undefined one line earlier (printerName?.trim()), then dereferenced unguarded; it is undefined whenever the option is omitted. The tools compile without strict null checks (CH-024), so the compiler does not catch it. |
| CH-027 | floating-promise | `API/routes/label-routes.ts` | The label route starts archiving the created label and neither awaits nor handles the promise: an archive failure is an unhandled rejection (which terminates Node by default) and the response claims an archived label that may not exist. |
| CH-028 | floating-promise | `tools/import-rate-cards.ts` | files.forEach(async ...) drops each import's promise: the command reports success and exits before the imports have finished, and a failed import is an unhandled rejection. |
| CH-029 | blocking-on-async-code | `INF/labels/label-archive.ts` | The async request-path method load reads the label file with readFileSync, blocking the event loop (and every other request) for the duration of the disk read. |
| CH-030 | missing-cancellation-propagation | `INF/rates/rate-card-cache.ts` | download fetches the rate card with no AbortSignal and no timeout: a hung rate-card host holds the refresh (and shutdown) forever; the carrier adapters show the codebase's own convention of passing a signal. |
| CH-031 | non-structured-log-message | `INF/carriers/carrier-registry.ts` | A pino log call whose message is a template literal interpolating runtime values: the values are baked into the message text instead of logged as fields, so the line cannot be queried or aggregated. |
| CH-032 | unguarded-expensive-debug-logging | `APP/pricing/quote-service.ts` | A debug log call serialises every quote (JSON.stringify over a mapped array) on every request even when debug logging is off. |
| CH-033 | inert-configuration-option | `INF/rates/rate-card-cache.ts` | The configured cache lifetime is stored and never read; isStale re-spells the default instead, so configuring the TTL changes nothing. |
| CH-034 | improper-resource-disposal | `INF/rates/rate-card-cache.ts` | The cache creates and owns a RefreshTimer (a disposable class) but its dispose() never disposes it: the interval keeps running after the cache is disposed. |
| CH-035 | hand-rolled-structured-format-parsing | `INF/carriers/alder-parcel-adapter.ts` | A regular expression pulls the error code out of the JSON error body, in a module that already parses JSON: it breaks on whitespace, key order or escaping that JSON.parse handles. |
| CH-036 | undrained-child-process-stream | `INF/printing/label-printer.ts` | lp is spawned with stdout and stderr both piped; only stdout is read while the code waits for exit, so a printer that writes enough to stderr fills the pipe and the print hangs. |
| CH-037 | unbounded-truncation-loop | `APP/labels/zpl-label-renderer.ts` | fitToWidth drops the last character while the measured width (which includes fixed margins) exceeds the field: when the margins alone exceed it, the loop runs past the empty string and never ends. |
| CH-038 | unrestored-process-global-state | `tools/import-rate-cards.ts` | importFolder changes the working directory and restores it only on the straight-line path: an import error leaves the process in the drop folder. |
| CH-039 | loop-decision-on-fixed-element | `APP/pricing/multi-parcel-quoter.ts` | Inside the per-parcel loop the oversize surcharge is decided by parcels[0] instead of parcels[i]: every parcel is charged (or not) according to the first one. |
| CH-049 | unused-code | `INF/rates/rate-card-cache.ts` | The #ttlMs field is written in the constructor and read nowhere: dead state (the mechanism behind CH-033). |
| CH-050 | unused-code | `INF/rates/rate-card-cache.ts` | #refreshTimer is assigned in the constructor and read nowhere, not even by dispose(): dead state, the field-level face of CH-034 (one fix, disposing it, clears both). |
| CH-051 | duplicated-code | `tools/import-rate-cards.ts` | importFolder and importArchive repeat the same change-directory, list, parse and change-back sequence with local edits; one withWorkingDirectory helper (with its restore in a finally) would replace both and fix CH-038 on the way. |
| CH-040 | module-dependency-cycle | `INF/carriers/carrier-registry.ts` | carrier-registry imports the Corvid adapter, and the adapter imports a runtime constant back from carrier-registry: a value-level import cycle whose evaluation order decides whether the constant is initialised. The site is the registry's import block. |
| CH-041 | layer-dependency-violation | `DOM/value-objects/tracking-number.ts` | A domain value object imports from the infrastructure layer (the carrier prefix table), pointing the dependency outward against the layering the repository documents. |
| CH-042 | fat-interface | `DOM/ports/shipment-repository.ts` | The ShipmentRepository port declares well over fifteen required operations; every consumer uses two or three of them and every implementation (and test double) has to provide all. |
| CH-043 | anemic-domain-model | `DOM/entities/shipment.ts` | Shipment is an aggregate with state and no behaviour: every status change, label assignment and void is done by the application services writing its fields, so its invariants (a voided shipment has no active label) are enforced nowhere. |
| CH-044 | publicly-mutable-entity-state | `DOM/entities/shipment.ts` | Shipment exposes its status, label and parcels as public writable fields (and the parcels array itself), so any caller can put it into an invalid state. |
| CH-045 | test-without-assertion | `tests/unit/label-service.test.ts` | The test exercises label creation and asserts nothing: it passes whatever the service returns. |
| CH-046 | skipped-test-without-reason | `tests/unit/zpl-label-renderer.test.ts` | A skipped test with no reason, issue or condition recorded: nobody can tell whether it is obsolete or hiding a bug. |
| CH-047 | test-failure-swallowed | `tests/unit/carrier-adapters.test.ts` | The act and the assertion run inside a try whose catch discards everything, including the assertion error: the test cannot fail. |
| CH-048 | excessive-mocking | `tests/unit/quote-routes.test.ts` | Every collaborator of the route (calculator, registry, cache, logger, clock) is replaced by a mock and the test asserts the mocks were called: it tests the wiring it set up, not the behaviour. |

## Traps (`must-not-fire`)

| Id | Concept | Where | Why it is not a defect |
|---|---|---|---|
| TRP-001 | high-cyclomatic-complexity | `APP/pricing/service-code-map.ts` | A flat (carrier, service level) to product-code switch: many case labels, no nesting, one-line arms. A lookup table, not complex logic. |
| TRP-002 | high-cognitive-complexity | `APP/pricing/service-code-map.ts` | The same flat switch: nothing nests, nothing interacts; a reader takes in one arm at a time. |
| TRP-003 | oversized-source-file | `INF/zones/country-zones.generated.ts` | Generated from the rate-card CSV by tools/generate-zones.mjs, with a header saying so; never edited by hand. Its size is data, not authored code. |
| TRP-004 | high-cyclomatic-complexity | `INF/zones/country-zones.generated.ts` | Generated lookup switch (see TRP-003); not authored logic. |
| TRP-005 | long-method | `INF/zones/country-zones.generated.ts` | Generated lookup function (see TRP-003); its length is the size of the table. |
| TRP-006 | duplicated-code | `API/contracts/address-dto.ts` | The API's address contract and the Alder adapter's wire address are an intentional pair of small data shapes in different boundaries: they change for different reasons (our API versioning vs the carrier's schema). Data only, no logic. |
| TRP-007 | duplicated-code | `INF/carriers/alder/alder-address.ts` | The other half of the DTO pair (TRP-006). |
| TRP-008 | pointless-catch-rethrow | `INF/labels/label-archive.ts` | The catch logs the failure with the label id and path and then rethrows: the log adds context the caller does not have. Not a pointless rethrow, not a swallowed error. |
| TRP-009 | empty-catch-block | `INF/labels/label-archive.ts` | The same catch: it logs and rethrows, nothing is swallowed. |
| TRP-010 | technical-debt-marker | `INF/carriers/corvid-courier-adapter.ts` | 'TODO' here is a carrier status code in a string literal (Corvid's 'to do' state for a parcel not yet collected), not a comment marking unfinished work. |
| TRP-011 | technical-debt-marker | `INF/carriers/corvid-tracking.ts` | A documented URL path segment of the carrier's tracking API inside a string literal, not a debt marker. |
| TRP-012 | suppressed-diagnostic | `APP/labels/zpl-label-renderer.ts` | One line, one named rule, with the reason after the double dash: the regular expression strips control characters from field data on purpose, which is exactly what no-control-regex warns about. |
| TRP-013 | suppressed-diagnostic | `tests/unit/money.test.ts` | @ts-expect-error with a reason in a test that deliberately passes an ill-typed value to prove the runtime guard rejects it; unlike @ts-ignore it fails the build if the error ever disappears. |
| TRP-014 | floating-promise | `INF/rates/rate-card-cache.ts` | Deliberate fire-and-forget with the reason in a comment: refresh() catches and logs every failure itself, so the voided promise can never reject. |
| TRP-015 | low-class-cohesion | `APP/pricing/surcharge-policy.ts` | Many small methods that all read the same rate table and thresholds: cohesive, however many methods it has. |
| TRP-016 | module-dependency-cycle | `APP/pricing/quote.ts` | quote.ts and surcharge-policy.ts refer to each other's types only, with import type: the imports are erased at compile time, so there is no runtime cycle. |
| TRP-017 | fat-interface | `DOM/ports/label-lifecycle-hooks.ts` | A hooks interface whose members are all optional callbacks: an implementer provides only the hooks it needs, so the member count burdens nobody. |
| TRP-018 | fat-interface | `API/contracts/quote-contracts.ts` | A data-shape interface (a response contract) with many fields and no operations: there is nothing to segregate. |
| TRP-019 | publicly-mutable-entity-state | `DOM/entities/label-batch.ts` | LabelBatch exposes its labels through a getter that returns a read-only copy, and its id as a readonly field; all changes go through its methods. |
| TRP-020 | blocking-on-async-code | `tools/import-rate-cards.ts` | A run-to-completion CLI reads its manifest synchronously inside its async main: there is no event loop serving anyone else, so blocking costs nothing. |
| TRP-021 | non-structured-log-message | `INF/rates/rate-card-cache.ts` | The template interpolates a module constant only: the message is the same text on every call, so nothing that belongs in a field is baked into it. |
| TRP-022 | unguarded-expensive-debug-logging | `APP/pricing/quote-service.ts` | The expensive debug serialisation runs only inside an isLevelEnabled('debug') guard. |
| TRP-023 | hand-rolled-structured-format-parsing | `DOM/value-objects/tracking-number.ts` | A regular expression that validates the shape of a tracking number, an identifier, not a structured format. |
| TRP-024 | undrained-child-process-stream | `INF/printing/printer-status-probe.ts` | Both stdout and stderr of lpstat are read to the end. |
| TRP-025 | unbounded-truncation-loop | `APP/labels/zpl-label-renderer.ts` | The truncation loop's condition has a floor (text.length > 0): it stops at the empty string. |
| TRP-026 | unrestored-process-global-state | `tools/import-rate-cards.ts` | importArchive changes the working directory and restores it in a finally block, on every path. |
| TRP-027 | unreachable-code | `INF/carriers/carrier-registry.ts` | Upper-case labels under switch (code.toUpperCase()): every arm is reachable. |
| TRP-028 | silent-error-fallback | `APP/pricing/cutoff-calendar.ts` | An unparsable cut-off time falls back to the documented default and logs a warning that names the bad value: neither silent nor invented. |
| TRP-029 | unused-code | `APP/pricing/rate-card-csv.ts` | Used by the rate-card CLI under tools/ (and its tests), not by the service: an export with a consumer outside src is not dead. |
| TRP-030 | improper-resource-disposal | `APP/pricing/rate-calculator.ts` | RateCalculator is handed the rate-card source (the disposable cache in production) by the composition root and does not dispose it: it does not own it; the composition root disposes it on shutdown. |
| TRP-033 | non-structured-log-message | `tools/import-rate-cards.ts` | `log` here is the import command's console sink (ImportLog writes a line for the operator to stdout), not a structured logger: a formatted human message is what it is for. |
| TRP-034 | high-cyclomatic-complexity | `DOM/ports/label-lifecycle-hooks.ts` | An interface of optional method signatures: there is no control flow in the file at all. The `?` of an optional member is not a branch. |
| TRP-035 | high-cyclomatic-complexity | `tools/rates-cli.ts` | A three-command CLI dispatcher (a switch with one usage check per command, cyclomatic 11): flat, short and read one arm at a time; well under any reasonable function-complexity budget. |
| TRP-036 | high-cyclomatic-complexity | `APP/labels/label-service.ts` | parseAddress is flat field validation (one single-line check per required field plus a table-driven postcode check), cyclomatic 12: each condition stands alone and reads at a glance; below any reasonable function budget. |
| TRP-031 | skipped-test-without-reason | `tests/integration/label-printer.test.ts` | A conditional skip whose condition is the reason (no lp binary on the machine) and whose comment says so. |
| TRP-032 | test-without-assertion | `tests/integration/quote-api.test.ts` | The test asserts through a local helper (expectProblem) that calls expect: it has assertions, just not inline. |

## Score bands (chosen from intent before the first scan)

| Id | Concept | Band | Why |
|---|---|---|---|
| BND-001 | high-cyclomatic-complexity | 70–97 | One genuinely over-complex method (CH-001) among roughly two hundred small functions: a real but bounded deduction. The generated lookup and the flat switch add nothing. |
| BND-002 | high-cognitive-complexity | 70–97 | Same reasoning as BND-001 for the nested decision tree of CH-002. |
| BND-003 | duplicated-code | 75–99 | One copy-pasted pair of carrier-adapter members (CH-006) in a ~3 kLoC codebase: a small density of clones. |
| BND-004 | low-class-cohesion | 70–97 | One low-cohesion class (CH-007) among a few dozen cohesive ones. |
| BND-005 | untyped-javascript-share | 85–100 | Every source file is TypeScript under strict compiler options; only the tools build relaxes strict null checks. |
| BND-006 | test-coverage | 70–100 | Unit and HTTP-level tests import nearly every production module; a few infrastructure modules (the printer, the CLI entry) are reached only partly. |
| BND-007 | frontend-tooling-scripts | 85–100 | package.json wires test, lint, typecheck and format scripts. |
| BND-008 | inconsistent-naming | 40–85 | Mostly idiomatic TypeScript naming, but the messy legacy parts mix styles (snake_case locals, Hungarian-style prefixes, abbreviations). Judged by a model on a sample: wide band. |
| BND-009 | low-value-comments | 40–85 | The clean parts carry few, purposeful comments; the messy parts have comments that restate the code and one that is stale. Judged by a model on a sample: wide band. |

## Contested truths, and how they were decided

- **TRP-001/002 — a long flat switch is not "too complex".** Cyclomatic complexity counts every case label, so a
  mapping table scores high; but the measure exists to estimate how hard code is to understand and test, and a table
  of one-line arms with no interaction is neither. Had the arms contained logic, the answer would flip.
- **TRP-016 — a type-only import cycle.** `import type` is erased at compile time (`verbatimModuleSyntax` guarantees
  it), so the two modules do not load each other. Two modules naming each other's types is ordinary; the runtime
  cycle in the carrier registry (CH-040) is the defect.
- **TRP-014 — `void` on a promise.** Dropping a promise is a defect when its failure goes unobserved (CH-027,
  CH-028). Here the called method catches and logs everything itself and the comment says the call is deliberately
  not awaited; nothing can reject.
- **TRP-020 — synchronous file reads in a CLI.** The same call on a request path blocks every other request
  (CH-029); in a run-to-completion command there is nobody else to block.
- **CH-024 / CH-025 are repository-level.** Strict-null posture and non-null-assertion density are properties of
  the build configuration and of the codebase, reported without a line. CH-026 is the located consequence.
- **CH-012 is a configuration site.** The ESLint block that switches the type-safety and promise rules off for the
  legacy files is the TypeScript form of a project-wide warning demotion: no reason is given and it is how the other
  plants in those files pass the lint gate.
- **CH-017 vs TRP-027.** Two arms of one switch over an upper-cased code: the lower-case label can never match
  (defect); the upper-case ones are correct.
- **TRP-034/035/036 — a low cyclomatic number is not complexity.** A three-command dispatcher (11), flat field
  validation (12) and an interface of optional members (no control flow at all) were reported by a function scanner
  whose bar is 10. None of them is hard to read or test; the planted method (CH-001) scores 27.
- **Concepts without a reference rule.** `unchecked-any-external-data` (CH-023), empty catch blocks, commented-out
  code, `@ts-ignore` and deprecated-symbol use are labelled because they are true defects, whether or not a given
  scanner reads TypeScript for them.

## What is clean

Every other tracked file (`CLN-*`) is certified free of every code-health concept this repository covers (the
concept list on each clean entry). Files that hold a plant or a trap carry no clean entry: they are labelled only at
their sites.

## What this repository does not cover

- `oversized-source-file`'s scanner thresholds that depend on the *share* of large files are not probed separately:
  one hand-written oversized file (CH-004) and one generated one (TRP-003) are what a real service of this size has.
- Loose equality (`==`) and mutation of parameters have no concept in the benchmark taxonomy and no dimension that
  denotes them; the code uses `===` throughout and does not mutate its parameters, so nothing is planted for either.
- C#-only mechanisms (`async void`, `ConfigureAwait`, IL size, stack-trace reset by `throw ex`, lock release, thread
  hand-offs) cannot occur in a single-threaded TypeScript service and are not labelled.
- Security, dependencies, architecture-as-a-dimension, frontend accessibility and history are covered by their own
  repositories. Findings of concepts this key does not cover are reported by the harness as *uncovered*, not as
  noise; an accidental real defect of another kind is fixed in the repository during authoring (see the journal).
