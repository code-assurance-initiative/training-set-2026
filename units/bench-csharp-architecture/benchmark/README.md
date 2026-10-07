# Benchmark: architecture and structure defects, and their look-alikes

This repository is one unit of the scanner benchmark in
[`code-assurance-initiative/scanner-benchmark`](https://github.com/code-assurance-initiative/scanner-benchmark).
Its labels are in [`answer-key.json`](answer-key.json) (format: `scanner-benchmark/docs/CONTRACT.md`, schema 1.2);
its authoring log is [`journal.md`](journal.md).

## Theme

**FleetOps**, a small fleet-maintenance system in .NET (`net10.0`): vehicles, work orders, inspections and
maintenance plans. It is a multi-project solution with the usual clean-architecture shape:

| Project | Role |
|---|---|
| `FleetOps.Contracts` | wire DTOs shared by the API and the worker; depends on nothing |
| `FleetOps.Domain` | entities, value objects, repository ports, maintenance rules |
| `FleetOps.Application` | vertical feature slices (`Features/<Slice>`) with CQRS handlers on the Mediator library |
| `FleetOps.Infrastructure` | EF Core (SQLite) persistence and every external integration |
| `FleetOps.Infrastructure.Telematics` | HTTP adapter for the vehicle telematics vendor |
| `FleetOps.Api` | ASP.NET Core host and controllers (the composition root) |
| `FleetOps.Worker` | background host that raises due-maintenance reminders |
| `FleetOps.ServiceDefaults` | shared host defaults (health checks, problem details) |
| `FleetOps.Diagnostics` | health checks probing each subsystem |
| `FleetOps.Notifications` | empty |

Architecture rules are declared in `docs/adr/` in a form any tool can read: each rule is one sentence naming two
repository paths in backticks ("Code under `src/A/` must not reference `src/B/`."), and each ADR states how it is
enforced in YAML front matter (`enforcement: test | prose | not-applicable`, `enforcement_link: <test file>`).
`tests/FleetOps.ArchitectureTests` (ArchUnitNET) checks the rules the code keeps, and passes.

### Why the architecture tests pass although the rules are broken

The planted violations are exactly the rules a team wrote down and **did not encode**, which is how such violations
survive in real codebases:

- ADR 0002 (layering) is checked by `LayeringTests`, which tests the Domain assembly against the list of assemblies
  that existed when it was written. The telematics adapter was later added as its own assembly
  (`FleetOps.Infrastructure.Telematics`) and never added to that list, so the Domain -> Telematics reference
  (LAY-001/LAY-002) passes the test.
- ADR 0004 (thin controllers) links a test file that was never written, so nothing checks it (ARU-002), and a
  controller talks to the DbContext (LAY-003).
- ADR 0003 (vertical slices) is enforced by review only (`enforcement: prose`) (ARU-001), and one slice calls
  another slice's handler (SLC-001).

A test that failed on the plants would not be committed by a team that runs its tests; a test that covers the rules
the code keeps is what such a team has.

## Labels are about truth

A `must-fire` is something a careful senior reviewer would flag; a `must-not-fire` is a site a careless rule would
flag and that reviewer would not. Neither was chosen to match a scanner. Project-level entries (findings a scanner
reports per project, without a line) carry the project name as `subject` (contract 1.2), and their `file` is the
project's `.csproj`.

## Plants (`must-fire`)

| Id | Concept | Where | What and why |
|---|---|---|---|
| LAY-001 | layer-dependency-violation | `src/FleetOps.Domain/FleetOps.Domain.csproj` | Domain references the `FleetOps.Infrastructure.Telematics` project, against ADR 0002. |
| LAY-002 | layer-dependency-violation | `src/FleetOps.Domain/Maintenance/MaintenanceDueEvaluator.cs` | The code half: a Domain file imports the telematics adapter namespace. |
| DOM-001 | domain-depends-on-infrastructure | same file | The domain service calls the HTTP telematics client itself. |
| LAY-003 | layer-dependency-violation | `src/FleetOps.Api/Controllers/FleetDashboardController.cs` | A controller injects `FleetOpsDbContext` and queries it directly, bypassing Application (ADR 0004). |
| SDP-001 | unstable-dependency | `src/FleetOps.ServiceDefaults/FleetOps.ServiceDefaults.csproj` | The stable shared host-defaults library references `FleetOps.Diagnostics`, which depends on five projects and is referenced by nothing else in production (I = 0.83). |
| CYC-001 | module-dependency-cycle | `src/FleetOps.Infrastructure/Email/ReminderMailer.cs` | Namespace cycle `Infrastructure.Scheduling` <-> `Infrastructure.Email`. **Namespace-level, because a project-reference cycle cannot exist in a solution that builds** (NuGet rejects it with NU1108). |
| SHL-001 | solution-structure | `src/FleetOps.Notifications/FleetOps.Notifications.csproj` | Empty shell project in the solution, referenced by nothing. |
| GOD-001 | oversized-module | `src/FleetOps.Infrastructure/FleetOps.Infrastructure.csproj` | The god module: every integration in one project, > 120 public types in > 10 namespaces. |
| IND-001 | call-indirection | `src/FleetOps.Application/Facades/WorkOrderFacade.cs` | A pass-through facade whose every method forwards to the mediator unchanged. |
| BLC-001 | business-logic-in-controller | `src/FleetOps.Api/Controllers/WorkOrdersController.cs` | Pricing, status-transition and approval-limit rules computed in a controller action. |
| CAP-001 | captive-dependency | `src/FleetOps.Infrastructure/FuelCards/FuelPriceCache.cs` | Singleton keeps a scoped store (registered through a factory, which is how such a mismatch gets past the container's scope validation). |
| SNG-001 | unsynchronized-shared-state | `src/FleetOps.Infrastructure/Geocoding/GeocodingCache.cs` | Singleton mutates a `Dictionary` without a lock. |
| FAT-001 | fat-interface | `src/FleetOps.Infrastructure/Reporting/IFleetReporting.cs` | 17 unrelated report operations in one interface. |
| SLC-001 | cross-slice-coupling | `src/FleetOps.Application/Features/Inspections/RecordInspectionHandler.cs` | The Inspections slice holds the WorkOrders slice's concrete handler. |
| CQS-001 | query-with-side-effects | `src/FleetOps.Application/Features/WorkOrders/GetWorkOrderHandler.cs` | A query handler writes "last viewed" and saves. |
| ARU-001 | architecture-rules-unenforced | `docs/adr/0003-vertical-slices.md` | A checkable rule enforced by `prose` only. |
| ARU-002 | architecture-rules-unenforced | `docs/adr/0004-thin-controllers.md` | `enforcement: test` linking a test file that does not exist. |

**BLC-001 and the taxonomy.** Taxonomy 1.0 had no finding-level concept for "domain logic in the presentation
layer", so key v1.0.0 labelled the plant with the nearest metric, `business-logic-share` (domain/application lines
versus glue), which logic in a controller lowers. Key v1.1.0 relabels it to the finding concept
`business-logic-in-controller` that the harness has since added; the site is unchanged and no code changed. The share
itself is still banded (BND-006). Every `clean` entry that certified `business-logic-share` now also certifies
`business-logic-in-controller`, except the two controllers that are not certified for it: `WorkOrdersController.cs`
(the plant) and `FleetDashboardController.cs` (its status counts are read-model composition, not a business rule,
but the file is the LAY-003 plant and is left unlabelled for this concept rather than certified).

## Traps (`must-not-fire`)

| Id | Concept | Where | Why it is not a defect |
|---|---|---|---|
| TRP-001 | unstable-dependency | `FleetOps.Contracts` | Everyone depends on it and it depends on nothing: stable by design. |
| TRP-002 | module-off-main-sequence | `FleetOps.Contracts` | Concrete and stable (A = 0, I = 0) because DTOs cannot be abstract; a contracts package is not in the "zone of pain". |
| TRP-003 | layer-dependency-violation | `tests/FleetOps.Application.Tests` csproj | A test project referencing Infrastructure; tests are not a layer. |
| TRP-004 | layer-dependency-violation | `tests/FleetOps.ArchitectureTests` csproj | The architecture tests reference every production project with code, to check them. |
| TRP-005 | layer-dependency-violation | `FleetOps.Infrastructure` csproj -> Domain | The allowed direction. |
| TRP-006 | layer-dependency-violation | `src/FleetOps.Api/Program.cs` | The composition root references every layer. |
| TRP-007 | layer-dependency-violation | `FleetOps.Api` csproj | The host project composes all layers. |
| TRP-008 | layer-dependency-violation | `src/FleetOps.Infrastructure/Persistence/VehicleRepository.cs` | Dependency inversion: a Domain port implemented in Infrastructure. |
| TRP-009 | solution-structure | `FleetOps.Worker` csproj | A thin but purposeful host (one BackgroundService). |
| TRP-010 | captive-dependency | `.../Scheduling/ReminderScheduler.cs` | Singleton with `IServiceScopeFactory`, a scope per run. |
| TRP-011 | captive-dependency | `.../Scheduling/MaintenanceReminderPlanner.cs` | Singleton holding a stateless transient. *Contested*, see below. |
| TRP-012 | unsynchronized-shared-state | `.../Tyres/TyrePriceCache.cs` | `ConcurrentDictionary`. |
| TRP-013 | unsynchronized-shared-state | `.../Parts/PartsCatalogueSnapshot.cs` | Mutations inside `lock`. |
| TRP-014 | fat-interface | `src/FleetOps.Application/Abstractions/IFleetReadModel.cs` | A composite of three small role interfaces; declares nothing itself. |
| TRP-015 | cross-slice-coupling | `.../Features/Maintenance/GetDueMaintenanceHandler.cs` | A slice handler with fields of Domain types, not another slice's types. |
| TRP-016 | query-with-side-effects | `.../Features/Vehicles/GetVehicleHandler.cs` | Memory cache, logging and a metric are not persistent state. |
| TRP-017 | call-indirection | `.../FuelCards/RetryingFuelCardClient.cs` | A decorator that adds retries, not a pass-through. |
| TRP-018 | architecture-rules-unenforced | `docs/adr/0001-record-architecture-decisions.md` | A process decision; `enforcement: not-applicable`. |
| TRP-019 | architecture-rules-unenforced | `docs/adr/0005-dependency-free-contracts.md` | Enforced by a real, complete architecture test. |
| TRP-020 | module-dependency-cycle | repository | The project graph is acyclic; its diamond is not a cycle. |
| TRP-021 | solution-structure | `FleetOps.ServiceDefaults` csproj | A thin but purposeful shared host-defaults library; small by design (added after scan 1). |
| TRP-022 | suppressed-diagnostic | `.editorconfig` | CA2007 off at the root with its reason on the line above, back on for `src/` (added after scan 1). |

## Contested truths, and how they were decided

- **TRP-011, a singleton holding a transient.** The captive-dependency problem is a longer-lived service keeping a
  shorter-lived one whose lifetime *matters* (scoped state, a DbContext, a disposable). `ReminderWindowCalculator` is
  stateless and not disposable; one instance kept forever behaves exactly like a new one per call. The framework's
  own scope validation rejects scoped-in-singleton and accepts this. Had the transient held state or been
  disposable, the answer would flip.
- **TRP-002, Contracts "off the main sequence".** Martin's metric places any concrete, stable package in the zone of
  pain. The zone matters for packages that are *volatile* as well as concrete; a DTO package changes rarely and only
  additively (ADR 0005), so a reviewer would not act on it.
- **CYC-001 is a namespace cycle.** See the plant table. A scanner that only reads project references cannot see it;
  that is a real limit, not a reason to weaken the label.
- **ADR 0002 is not labelled for `architecture-rules-unenforced`.** It has an automated test, but the test's
  assembly list is stale (see above). Whether "enforced by an incomplete test" counts as unenforced is a judgement
  call, so the file carries no label of that concept either way.

## Score bands (set from intent before the first scan)

| Id | Concept | Band | Why |
|---|---|---|---|
| BND-001 | unstable-dependency | 40-90 | No project cycle; one SDP violation and one outward reference among ten projects. |
| BND-002 | solution-structure | 50-90 | Conventional layout that builds, minus one shell project. |
| BND-003 | oversized-module | 50-90 | One god module among ten projects. |
| BND-004 | call-indirection | 40-90 | One pass-through layer on the work-order path. |
| BND-005 | architecture-style-fit | 50-100 | Recognisable clean architecture with slices and CQRS; heavy for its size. |
| BND-006 | business-logic-share | 0-50 | The glue dominates by design. |
| BND-007 | folder-structure | 80-100 | `src/` + `tests/`, one root namespace. |
| BND-008 | internal-api-inconsistency | 40-100 | Model-judged; controllers mix three conventions. Wide. |

## What is clean

Every other tracked file is certified free of the architecture concepts this repository covers (each `clean` entry
lists them). Files with a plant or trap are certified for the remaining concepts only.

## Not covered

- `event-not-named-in-past-tense` is `not-applicable` (NA-002): nothing publishes events; the Contracts records are
  DTOs and summaries (added after scan 1).
- `boundary-type-leakage` is `not-applicable` (NA-001): one bounded context. Cross-context leakage is better
  measured in a repository with real bounded contexts (the domain-events unit).
- Code-health, security, testing and readiness concepts are covered by their own units; results of those concepts
  here are reported as uncovered, not as noise.
