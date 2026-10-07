# Benchmark: engineering maturity read from git history and documentation

This repository is one unit of the scanner benchmark in
[`code-assurance-initiative/scanner-benchmark`](https://github.com/code-assurance-initiative/scanner-benchmark).
Its labels are in [`answer-key.json`](answer-key.json) (format: `scanner-benchmark/docs/CONTRACT.md`, schema 1.2);
its authoring log is [`journal.md`](journal.md).

## Theme

**ClinicScheduling**, a small appointment-scheduling API for a chain of physiotherapy clinics, in .NET (`net10.0`):
practitioners publish opening hours, patients book, cancel and reschedule appointments, treatment series are expanded
into sessions, reminders go out by SMS/e-mail, and a feed keeps the patient portal in sync.

What this repository measures is not the code alone but **the way the code was made**: its git history and its
documentation. The history is the input, so it is scripted: `benchmark/history/build-history.sh` rebuilds every commit
after the key-first commit deterministically (fixed author and committer dates, fictional authors with
`@example.invalid` addresses, about nine months of work by a team of five), so the same history can be reproduced
byte for byte and its SHAs checked.

**Always scan a full clone.** History-derived dimensions read `git log`; a shallow or exported copy has no history and
cannot be scored against this key.

### The scripted history

| | |
|---|---|
| Span | 2026-01-05 to 2026-09-30, 120 commits, release tags `v0.1.0` (Feb 27) ... `v0.6.0` (Sep 29); every tag builds in locked-restore mode and passes its tests |
| Team | Lena Marsh (lead, all year), Tomas Wrenfield (January to 8 May, then left), Priya Castellan (from February), Oskar Vale (from March), Noor Haddley (from May 18) - all fictional, `@example.invalid` |
| Order | The scripted commits sit on top of the key-first commit (`a8b3b2a`), so the key precedes every line of code in the commit graph; their dates are earlier than that commit's real date because they are fictional |
| Rebuild | `benchmark/history/build-history.sh <new-dir>` re-applies `benchmark/history/patches/NNNN.patch` with fixed authors, committers and dates, re-creates the tags, and checks the final commit id and every tag target |

What the history contains, by plant:

- **HOT-001** - `SlotFinder.FindOpenSlots` grows from a simple loop (January) to one method with breaks, holidays,
  buffers, skills, video visits, notice periods, caps and cut-offs; 19 commits by four people, 11 of them between
  July and September, nearly all `fix(availability)`.
- **SIL-001** - all nine commits to `CancellationPolicy.cs` are by Priya Castellan.
- **STL-001** - `Recurrence/` is created on 12 January by Tomas Wrenfield and never touched again; he leaves in May.
- **CPL-001** - `AppointmentResponse.cs` (12 revisions) and `PortalAppointmentFeed.cs` (11) change together in 9
  commits of at most five files each (82 %), from the feed's creation in April to September.
- **ADR-001** - `Controllers/ReportsController.cs` arrives on 21 July; ADR 0004 (February) is never revisited.
- **DOC-001** - the waitlist module lives from 23 March to 25 June; the README section about it survives, although
  the README itself is edited again in August.
- **REL-001** - the changelog is updated for 0.1.0 ... 0.4.0; the 0.5.0 and 0.6.0 release commits bump the version
  only.
- Supporting detail for the ADR trap: an `IClock` abstraction exists from January to May and is replaced by
  `TimeProvider` (ADR 0006) in one commit, after which ADR 0003 is marked superseded.

## Labels are about truth

A `must-fire` is something a careful senior reviewer would flag; a `must-not-fire` is a site a careless rule would
flag and that reviewer would not. Neither was chosen to match a scanner. Several concepts here are properties of a
file's history or of a document rather than of a line, so most entries are whole-file entries; where a scanner may
report a file only by naming it in a message (per author, per summary row), the entry carries the file path as its
`subject` (contract 1.2).

## Plants (`must-fire`)

| Id | Concept | Where | What and why |
|---|---|---|---|
| HOT-001 | churn-complexity-hotspot | `src/ClinicScheduling.Domain/Availability/SlotFinder.cs` | The most complex method in the service, edited by four people and over and over in the last quarter, mostly bug fixes. |
| SIL-001 | knowledge-concentration | `src/ClinicScheduling.Domain/Policies/CancellationPolicy.cs` | Core cancellation/no-show policy; every commit to it, ever, is by one (active) author. |
| STL-001 | knowledge-freshness | `src/ClinicScheduling.Domain/Recurrence/RecurrenceExpander.cs` | The recurrence module, written once nine months ago by an author who has left, untouched since while its dependents changed. |
| CPL-001 | change-coupling | `src/ClinicScheduling.Api/Contracts/AppointmentResponse.cs` ↔ `src/ClinicScheduling.Infrastructure/PatientPortal/PortalAppointmentFeed.cs` | Two modules encode the same public appointment view with no code dependency; every field change lands in both. Keyed on the lexicographically first path. |
| ADR-001 | adr-conformance | `docs/adr/0004-minimal-api-endpoints.md` | The ADR says Minimal APIs, no MVC controllers; `Controllers/ReportsController.cs` is an MVC controller. |
| DOC-001 | documentation-accuracy | repository (README.md) | The README describes a waitlist module removed in 0.4.0. |
| REL-001 | release-hygiene | `CHANGELOG.md` | The changelog stops at 0.4.0; releases 0.5.0 and 0.6.0 are tagged but undocumented. |

## Traps (`must-not-fire`)

| Id | Concept | Where | Why it is not a defect |
|---|---|---|---|
| TRP-001 | churn-complexity-hotspot | `src/ClinicScheduling.Infrastructure/Holidays/PublicHolidays.g.cs` | Large generated table, committed once, never changed: no churn, no complexity. |
| TRP-002 | knowledge-freshness | same file | Old and written by a departed author, but generated: the knowledge lives in the generator and its CSV. |
| TRP-003 | knowledge-concentration | same file | Single-author because generated in one go. |
| TRP-004 | churn-complexity-hotspot | `src/ClinicScheduling.Infrastructure/Notifications/ReminderTexts.cs` | Very frequently changed, but a string catalogue with no control flow. |
| TRP-005 | change-coupling | same file | Co-changes with the composer that reads it: explicit, expected coupling. |
| TRP-006 | change-coupling | `tests/ClinicScheduling.UnitTests/Availability/SlotFinderTests.cs` | A test file changing with its subject. |
| TRP-007 | knowledge-concentration | `src/ClinicScheduling.Infrastructure/Calendar/IcsCalendarWriter.cs` | Single-author leaf utility with thorough tests, specified by RFC 5545. |
| TRP-008 | adr-conformance | `docs/adr/0003-clock-abstraction.md` | The code contradicts it, but it is correctly marked superseded by ADR 0006, which the code follows. |
| TRP-009 | churn-complexity-hotspot | `src/ClinicScheduling.Domain/Policies/CancellationPolicy.cs` | Changed often, but simple. |

## Score bands

Chosen from intent before any scan (see `answer-key.json` for each rationale): hotspots [40, 85], knowledge
concentration [50, 90], knowledge freshness [50, 90], change coupling [40, 90], README [70, 100], architecture
documentation [70, 100], folder structure [80, 100], release hygiene [30, 80]; model-judged: documentation quality
[50, 95], ADR quality [60, 100], ADR conformance [30, 85], documentation accuracy [20, 80].

## What is clean

Every tracked file is certified clean for this repository's concepts (the history, documentation and release
concepts above) except at the plant and trap sites, for the concept of that site. A few files carry no label for one
concept because a result there would be the same finding as a plant: `RecurrenceRule.cs` (the stale module, STL-001),
`PortalAppointmentFeed.cs` (the other half of CPL-001), `ReportsController.cs` (the code that breaks ADR-001), and
README.md for documentation accuracy and quality (DOC-001). `docs/architecture.md` is not certified for documentation
accuracy: it calls the API project "Minimal API endpoints", which the reports controller makes not quite true. The repository otherwise follows the shared C#
conventions (CI pinned by SHA, Central Package Management with lock files, nullable, `SECURITY.md`, `src/` + `tests/`,
real passing tests); findings of other concepts are judged in the journal but are not this repository's subject.

## What this repository does not cover

Security, dependency, code-health and architecture *rules* are covered by their own benchmark repositories. Nothing
here is not-applicable by design: every concept listed above has something to measure.

## Notes for scanner users

- **Wall clock.** Some history measures decay knowledge against the time of the scan, not the time of the last
  commit. The labels describe the history as it stands at the tag; a scanner that ages contributions against the
  wall clock will drift on later re-scans, which is that scanner's property, not a change of truth.
- **Two kinds of tag.** `v0.1.0` … `v0.6.0` are the service's own releases inside the scripted history (REL-001 is
  about them). `v1.0.0` is the benchmark freeze tag (code and key frozen together); it is not a release of the
  service.
