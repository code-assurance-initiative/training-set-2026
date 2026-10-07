# Benchmark: domain-driven design, messaging and event sourcing

This repository is one unit of the scanner benchmark in
[`code-assurance-initiative/scanner-benchmark`](https://github.com/code-assurance-initiative/scanner-benchmark).
Its labels are in [`answer-key.json`](answer-key.json) (format: `scanner-benchmark/docs/CONTRACT.md`); its
authoring log is [`journal.md`](journal.md).

## Theme

The core of a small **equipment-rental (tool library)** service in .NET (`net10.0`), written as two bounded contexts:

- **Lending** — members, the equipment catalogue (equipment types with their physical units and maintenance
  records), reservations and loans. Aggregates with strongly-typed ids and value objects, persisted with EF Core
  (SQLite), integration events written to a transactional **outbox** in the same `SaveChanges`, and an outbox
  dispatcher that publishes after commit.
- **Billing** — one **event-sourced** aggregate (the member account: charges, deposits, payments), an event-store
  abstraction with an in-memory implementation, a balance **projection**, and consumers of Lending's integration
  events with an **inbox** for de-duplication.

Messaging is an in-process bus behind `IMessageBus` / `IIntegrationEventHandler<T>` / `ICommandHandler<T>`
abstractions, so nothing needs a running broker. Every defect sits where real teams put it; every trap is the safe
idiom a careless rule mistakes for one. The rest of the repository carries the shared scaffolding (CI pinned by commit
SHA, CodeQL, Dependabot, README, ADRs, architecture doc, CHANGELOG, `SECURITY.md`, central package management with lock
files, nullable, `src/` + `tests/`) so that the only signal is the theme.

## Labels are about truth

A `must-fire` is a defect a careful reviewer of a DDD / event-driven codebase would raise; a `must-not-fire` is a site
that a careless rule would flag and a careful reviewer would not. Where a scanner reports a type-level finding for a
defect that lives on one member (public setters, an EF attribute), the entry's line range runs from the type
declaration to the member, so a report at either matches.

## Plants, traps and score bands

### Plants (`must-fire`)

| Id | Concept | Site | Why |
|---|---|---|---|
| DM1-001 | `cross-aggregate-object-reference` | `Rentals.Lending.Domain/Loans/Loan.cs` | The Loan aggregate holds the Member aggregate itself (an EF navigation) instead of its MemberId. Loading or saving a loan now drags the member into the loan's consistency boundary: a change saved through the loan can write member state, and the two can no longer be stored, locked or scaled apart. |
| DM2-001 | `primitive-entity-identifier` | `Rentals.Lending.Domain/Loans/Loan.cs` | The loaned equipment unit is identified by a raw Guid although the domain has an EquipmentUnitId type: a LoanId, MemberId or EquipmentId Guid can be passed where a unit id is meant and the compiler cannot tell. |
| DM3-001 | `integration-event-leaks-domain-type` | `Rentals.Contracts/IntegrationEvents/EquipmentDamageReportedIntegrationEvent.cs` | The published integration event carries the Lending domain's own UnitCondition enum, so the contracts assembly references Rentals.Lending.Domain and every consumer (Billing) compiles against the producer's domain model: renaming or adding a condition in Lending is a breaking change for other contexts. |
| DM4-001 | `anemic-domain-model` | `Rentals.Lending.Domain/Reservations/Reservation.cs` | Reservation is an aggregate root with state and no behaviour: confirming, cancelling and the window and member-standing rules all live in ReservationService, which sets its properties directly. Nothing on the aggregate protects its invariants (any caller can set a cancelled reservation back to Confirmed). |
| DM5-001 | `publicly-mutable-entity-state` | `Rentals.Lending.Domain/Members/Member.cs` | Member.Tier has a public setter next to ChangeTier, which also adjusts the loan limit. Any caller can assign the tier and leave LoanLimit inconsistent with it, bypassing the aggregate's own method. |
| DM5-002 | `publicly-mutable-entity-state` | `Rentals.Lending.Domain/Reservations/Reservation.cs` | Every property of the Reservation aggregate (member, equipment, window, status) has a public setter; the status can be moved from Cancelled back to Confirmed by assignment. |
| DM6-001 | `domain-depends-on-infrastructure` | `Rentals.Lending.Domain/Members/Member.cs` | The Member aggregate carries EF Core's [Index] attribute (Microsoft.EntityFrameworkCore), so the domain project references the ORM to express a database index. Persistence mapping belongs in the infrastructure configuration, where the other aggregates' mappings already are. |
| DM6-002 | `domain-depends-on-infrastructure` | `Rentals.Lending.Domain/Catalogue/UnitAvailabilityService.cs` | A domain service takes an EF Core DbContext and queries the Loans set itself: the domain layer reaches the database directly instead of through a repository port it owns. |
| DM7-001 | `repository-for-non-aggregate` | `Rentals.Lending.Domain/Maintenance/IMaintenanceRecordRepository.cs` | MaintenanceRecord is a child of the equipment whose unit it describes, yet it has its own repository: RecordMaintenanceHandler adds records without loading Equipment, so nothing checks that the unit exists or belongs to any equipment, and Equipment cannot keep any rule that spans its units' maintenance. |
| DM7-002 | `repository-for-non-aggregate` | `Rentals.Lending.Domain/Members/IMemberRepository.cs` | The member repository contract returns a live IQueryable<Member>: callers compose arbitrary queries against the persistence provider, so the port leaks the ORM and its query translation limits into the application. |
| DM8-001 | `primitive-obsession` | `Rentals.Lending.Domain/Members/Member.cs` | fullName, email and phone travel together as three strings through Register, ChangeContactDetails and HasSameContact: the missing value object (contact details) leaves validation and equality of the three to every caller and lets them be passed in the wrong order. |
| DM9-001 | `scattered-domain-rule` | `(repository)` | The rule 'a member in good standing may borrow' (Status is Active AND the membership has not expired) is decided twice, in CheckoutEquipmentHandler and in ReservationService, over Member's Status and MembershipExpiresAt, and nowhere on Member. A change to the rule has to be found and made in both places. |
| DM10-001 | `multi-aggregate-transaction` | `Rentals.Lending.Application/Loans/CheckoutEquipmentHandler.cs` | One command writes two aggregates in one unit of work: it adds the Loan and updates the Member (its lifetime loan counter). The member's consistency now depends on the loan transaction; the counter should follow from the LoanOpened event. |
| DM11-001 | `constructible-invalid-entity` | `Rentals.Lending.Domain/Maintenance/MaintenanceRecord.cs` | The public constructor stores a raw description and cost without checking them and the type offers no factory: a maintenance record with an empty description or a negative cost can be created and persisted. |
| DM12-001 | `ambient-nondeterminism-in-domain` | `Rentals.Lending.Domain/Members/Member.cs` | RenewMembership reads DateTimeOffset.UtcNow inside the aggregate: the new expiry cannot be tested at a chosen instant and differs from the time the command was accepted; every other domain method receives its instant. |
| ED1-001 | `synchronous-remote-call-in-event-handler` | `Rentals.Billing.Application/Handlers/LoanOpenedHandler.cs` | Handling LoanOpened, Billing calls the catalogue's HTTP API to fetch the replacement value for the deposit. The consumer is coupled in time to the producer's availability (a catalogue outage stalls or fails deposit holds); the value belongs in the event, as the damage event already does. |
| ED2-001 | `command-with-multiple-handlers` | `Rentals.Lending.Application/Loans/ExtendLoanCommand.cs` | ExtendLoanCommand has two handlers: Lending's ExtendLoanHandler and Billing's ChargeExtensionFeeHandler. A command expresses one intent with one owner; Billing reacting to an extension should subscribe to an event. |
| ED3-001 | `event-not-named-in-past-tense` | `Rentals.Lending.Domain/Loans/ExtendLoanEvent.cs` | A domain event named as an instruction (ExtendLoan) instead of a fact (LoanExtended): it reads as a command and invites consumers to treat it as a request that may be refused. |
| ED4-001 | `dual-write-without-outbox` | `Rentals.Lending.Application/Members/SuspendMemberHandler.cs` | The handler commits the suspension and then publishes MemberSuspendedIntegrationEvent straight to the bus. A crash or bus failure between the two loses the event for good (Billing never learns of the suspension); the other handlers write their integration events through the outbox in the same transaction. |
| ED5-001 | `non-idempotent-message-handler` | `Rentals.Billing.Application/Handlers/EquipmentDamageReportedHandler.cs` | Each delivery of EquipmentDamageReported posts a new damage charge to the member's account. The bus delivers at least once and the handler keeps no record of processed message ids, so a redelivery charges the member twice. |
| ES1-001 | `nondeterministic-event-fold` | `Rentals.Billing.Domain/Accounts/MemberAccount.cs` | The fold for LateFeeCharged stamps LastActivityAt with the wall clock instead of the event's ChargedAt, so rehydrating the same stream at a different time produces a different aggregate state. |
| ES1-002 | `nondeterministic-event-fold` | `Rentals.Billing.Application/Projections/AccountBalanceProjection.cs` | The balance projection's fold for PaymentReceived stores the wall-clock time as the last payment time: rebuilding the read model from the same events gives different values. A projection fold is a fold. |
| ES2-001 | `mutable-persisted-event` | `Rentals.Billing.Domain/Accounts/Events/PaymentReceived.cs` | A persisted event with public setters: code holding the event (or the in-memory store's reference to it) can rewrite the amount or reference of a payment that has already been recorded. |
| VOM-001 | `value-object-mutability` | `Rentals.Lending.Domain/Catalogue/StorageLocation.cs` | (v1.1.0) A value (branch and shelf) modelled as a class with public setters and reference equality: a holder can rewrite an equipment's shelf in place, bypassing `Equipment.Relocate` and its event, and `Relocate`'s `Location == location` compares references, so a "move" to the same place raises the event again. |
| DEH-001 | `domain-event-never-handled` | `Rentals.Lending.Domain/Catalogue/EquipmentRelocated.cs` | (v1.1.0) Raised so that members with a reservation are told where to collect the equipment; nothing maps or handles it, and the DbContext clears it after saving. |
| DEH-002 | `domain-event-never-handled` | `Rentals.Lending.Domain/Members/MemberEvents.cs` | (v1.1.0, present since v1.0.0) `MemberPersonalDataErased` goes nowhere: Billing, which holds the member's name and e-mail, is never told of an erasure. |
| ESC-001 | `event-schema-change-without-upcaster` | `Rentals.Billing.Domain/Accounts/Events/DepositHeld.cs` | (v1.1.0) A persisted event's property renamed (`Loan` → `LoanId`, commit 96f0557) after events were stored as JSON (9fe669e), with no schema-version bump and no upcaster: old events replay with an empty loan id, so deposits are never released. |
| BTL-001 | `boundary-type-leakage` | `Rentals.Billing.Application/Projections/AccountBalanceQuery.cs` | (v1.1.0) Billing's public balance query takes Lending's domain `MemberId` (commit e813432): Billing's callers compile against Lending's model, against ADR 0005. |
| BTL-002 | `boundary-type-leakage` | `Rentals.Billing.Application/Handlers/ChargeExtensionFeeHandler.cs` | (v1.1.0, present since v1.0.0) Billing's public handler is typed by Lending's `ExtendLoanCommand` (with Lending's ids) instead of an integration contract. |
| ES3-001 | `personal-data-in-event-store` | `Rentals.Billing.Domain/Accounts/Events/MemberAccountOpened.cs` | The first event of every account stream records the holder's full name and e-mail address in the append-only event store, with no crypto-shredding: erasing a member (EraseMemberPersonalData) cannot remove them from Billing's history. |

### Traps (`must-not-fire`)

| Id | Concept | Site | Why |
|---|---|---|---|
| TRP-001 | `cross-aggregate-object-reference` | `Rentals.Lending.Domain/Reservations/Reservation.cs` | Reservation refers to the member and the equipment by their strongly-typed ids, which is the aggregate-boundary rule followed, not broken (whatever else is wrong with Reservation). |
| TRP-002 | `cross-aggregate-object-reference` | `Rentals.Lending.Domain/Catalogue/Equipment.cs` | Equipment holds its own child entities (the physical units). EquipmentUnit has identity and behaviour but is reached only through Equipment and has no repository: it is inside the boundary, not another aggregate. |
| TRP-003 | `primitive-entity-identifier` | `Rentals.Contracts/IntegrationEvents/LoanOpenedIntegrationEvent.cs` | Integration events cross a context boundary and carry ids as primitives on purpose, so consumers do not depend on the producer's id types. |
| TRP-004 | `integration-event-leaks-domain-type` | `Rentals.Contracts/IntegrationEvents/LoanReturnedIntegrationEvent.cs` | Money is an immutable record in the shared kernel both contexts already depend on; carrying it couples no consumer to the producer's domain model. |
| TRP-005 | `anemic-domain-model` | `Rentals.SharedKernel/Money.cs` | A value object: data with value equality and a guarded constructor. It has no identity and no lifecycle, so 'no state-changing behaviour' is what it should have. |
| TRP-006 | `anemic-domain-model` | `Rentals.Billing.Application/Projections/AccountBalanceView.cs` | A read model written only by its projection and served to queries: a DTO, not an entity; it has no invariants of its own to protect. |
| TRP-007 | `publicly-mutable-entity-state` | `Rentals.Billing.Application/Projections/AccountBalanceView.cs` | Public setters on a read model that only the projection writes are the normal shape of a denormalised view. |
| TRP-008 | `publicly-mutable-entity-state` | `Rentals.Lending.Domain/Catalogue/Equipment.cs` | Equipment's setters are private (EF Core materialises through them) and its units are exposed as a read-only view over a private list; all changes go through AddUnit, Rename, ChangeRates, ReportDamage and RetireUnit. |
| TRP-009 | `domain-depends-on-infrastructure` | `Rentals.Lending.Infrastructure/Persistence/Configurations/EquipmentConfiguration.cs` | EF Core mapping of a domain type, in the infrastructure project: infrastructure depending on the domain is the allowed direction. |
| TRP-010 | `repository-for-non-aggregate` | `Rentals.Lending.Domain/Loans/ILoanRepository.cs` | A repository for the Loan aggregate root that returns materialised aggregates and lists, never a query handle. |
| TRP-011 | `multi-aggregate-transaction` | `Rentals.Lending.Application/Loans/ReturnEquipmentHandler.cs` | The handler reads Equipment (for the replacement value it puts on the damage event) but writes only the Loan; the unit's condition follows from the published event in a separate transaction. |
| TRP-012 | `constructible-invalid-entity` | `Rentals.Lending.Domain/Catalogue/Equipment.cs` | The public constructor validates its raw name (ArgumentException.ThrowIfNullOrWhiteSpace) and receives the rates as Money, which validates itself. |
| TRP-013 | `constructible-invalid-entity` | `Rentals.Lending.Domain/Catalogue/EquipmentUnit.cs` | The constructor takes only value objects (EquipmentUnitId, SerialNumber), each of which validated itself. |
| TRP-014 | `ambient-nondeterminism-in-domain` | `Rentals.Lending.Domain/Loans/LoanId.cs` | Generating a new identity with Guid.NewGuid is not a rule input: no domain decision depends on its value. |
| TRP-015 | `synchronous-remote-call-in-event-handler` | `Rentals.Billing.Application/Handlers/MemberSuspendedHandler.cs` | SendAsync here puts a command on the message bus for the statements service; it is asynchronous messaging, not a request/response call to another service. |
| TRP-016 | `command-with-multiple-handlers` | `Rentals.Contracts/IntegrationEvents/EquipmentDamageReportedIntegrationEvent.cs` | An event with two subscribers (Billing charges the damage, Lending marks the unit damaged): fan-out is what events are for. |
| TRP-017 | `event-not-named-in-past-tense` | `Rentals.Billing.Domain/Accounts/Events/DepositHeld.cs` | 'Held' is the irregular past participle of 'hold': the name states a fact. |
| TRP-018 | `dual-write-without-outbox` | `Rentals.Lending.Infrastructure/Messaging/OutboxDispatcher.cs` | The outbox dispatcher publishes messages that were committed with the business change and then marks them dispatched; a crash in between republishes (at least once), it never loses one. This is the outbox. |
| TRP-019 | `non-idempotent-message-handler` | `Rentals.Billing.Application/Handlers/LoanReturnedHandler.cs` | The handler records each message id in the inbox and skips a message it has already processed, so a redelivered LoanReturned does not release the deposit or charge the late fee twice. |
| TRP-020 | `nondeterministic-event-fold` | `Rentals.Billing.Domain/Accounts/MemberAccount.cs` | The new charge id is generated in the decision method and recorded in the event; the fold only copies it, so replay is deterministic. |
| TRP-021 | `mutable-persisted-event` | `Rentals.Billing.Domain/Accounts/Events/DepositReleased.cs` | init-only properties: they can be set by an object initialiser when the event is created and never after. |
| TRP-022 | `personal-data-in-event-store` | `Rentals.Contracts/IntegrationEvents/MemberRegisteredIntegrationEvent.cs` | An integration message, not a stored event: it sits in the outbox only until it is dispatched (the row is then deleted) and is never appended to an event store. |
| TRP-023 | `scattered-domain-rule` | `Rentals.Lending.Domain/Loans/Loan.cs` | The overdue rule lives on Loan, the type that owns the data; the overdue-loans query asks the loan rather than re-deciding it. |
| TRP-024 | `value-object-mutability` | `Rentals.Billing.Domain/Accounts/LoanReference.cs` | (v1.1.0) A `readonly record struct`: init-only and value-equal; used as a dictionary key, which is exactly what it supports. |
| TRP-025 | `domain-event-never-handled` | `Rentals.Billing.Domain/Accounts/Events/ChargePosted.cs` | (v1.1.0) No message handler, but folded by the aggregate and applied by `AccountBalanceProjection`: a projection is a subscriber. |
| TRP-027 | `boundary-type-leakage` | `Rentals.Billing.Domain/Accounts/MemberAccount.cs` | (v1.1.0) `Money` in Billing's public surface is the shared kernel (ADR 0001, ADR 0005), owned by neither context. |
| TRP-026 | `event-schema-change-without-upcaster` | `Rentals.Billing.Domain/Accounts/Events/LateFeeCharged.cs` | (v1.1.0) The stored shape changed (commit 0afd2db) the right way: schema version 2 and a v1→v2 upcaster, with a test that reads a literal v1 event. |

### Score bands

| Id | Concept | Band | Why |
|---|---|---|---|
| BND-001 | `audit-trail` | 0–60 | Member data in Lending changes with no audit record; only Billing's event-sourced account keeps a full history of its (financial) changes. Partial at best. |
| BND-002 | `data-retention-policy` | 0–40 | Personal data (members, account events) is kept indefinitely: no retention period, TTL or purge job exists. Dispatched outbox rows are deleted, which is not a retention policy for personal data. |
| BND-003 | `data-subject-rights` | 10–50 | Erasure exists for Lending (EraseMemberPersonalData anonymises the member) but cannot reach Billing's event store (ES3-001); there is no export and no consent handling. |
| BND-004 | `personal-data-inventory` | 20–80 | Genuine personal data is held in a handful of fields (member name, e-mail, phone; the account opening event's holder name and e-mail; the registration integration event). |
| BND-005 | `primitive-obsession` | 40–95 | The model uses value objects and typed ids throughout, with one real data clump (fullName/email/phone on Member, DM8-001). Mostly good, not perfect. |
| BND-006 | `non-idempotent-message-handler` | 20–85 | Of roughly a dozen mutating handlers, one consumer double-applies on redelivery (ED5-001); the others are guarded by an inbox, a lookup or a natural key, or set fixed values. Judged dimension: wide band. |
| BND-008 | `data-encryption-controls` | 0–40 | Personal data (member contact details, account holder name and e-mail) is stored without encryption at rest and no data-protection or key-management API is used; encryption is left to the platform. Absent in code. |
| BND-007 | `personal-data-in-event-store` | 0–75 | One stored event type (MemberAccountOpened) carries a person's name and e-mail with no crypto-shredding; the other stored events carry ids and amounts only. |

## What is certified clean

Every other tracked source, project, configuration and documentation file carries a `clean` label for the
twenty-four theme concepts (all DM / ED / ES concepts of the taxonomy that this repository covers, plus
`boundary-type-leakage`; four added in v1.1.0). A file that holds a plant or a trap is certified clean for the theme concepts that have no entry in it.
Deliberately **not** certified either way (v1.1.0):

- `domain-event-never-handled` in `Member.cs`, `Loan.cs`, `ExtendLoanEvent.cs` and `Equipment.cs`: they raise events
  that no subscriber handles. Two have a consequence that does happen by another route (`MemberSuspended`: the
  integration event is published directly, ED4-001; `ExtendLoanEvent`: the fee is charged through the command's
  second handler, ED2-001), two record transitions nothing in this design reacts to (`MemberReinstated`,
  `MembershipRenewed`), and `Equipment.cs` is the raise site of DEH-001. Whether an event with no intended consumer is
  this defect is a judgement this key does not make; the declarations of the labelled ones carry the entries.
- `event-schema-change-without-upcaster` in `AccountEventSerializer.cs` (where ESC-001's missing version bump sits) and
  `MemberAccount.cs` (where its replay goes wrong); `value-object-mutability` in `Equipment.cs` (where VOM-001's
  reference comparison is used).
- `boundary-type-leakage` in `EquipmentDamageReportedIntegrationEvent.cs` and `EquipmentDamageReportedHandler.cs` (the
  Lending enum carried by the contract, DM3-001, reaches Billing there), `ExtendLoanCommand.cs` (the leaked type of
  BTL-002) and `Rentals.Billing.Application.csproj` (the project reference that makes BTL-002 possible).

## Not covered here, and why

- Until v1.0.0 three concepts had no taxonomy id: value-object mutability, a domain event never handled, and a
  persisted-event schema change without an upcaster. v1.1.0 adds plants and traps for all three (above).
- **Personal-data inventory** (`personal-data-inventory`) is a score band only: a field reported as personal data
  is an inventory row, not a defect, so no site is labelled.
- Security, readiness and code-health concepts are out of theme; results for them are `uncovered`.

## Contested truths (decided before the code)

- **Integration events with primitive ids** (TRP-003) are correct, not primitive obsession: the contract must not
  depend on a context's id types.
- **A child entity with behaviour is not another aggregate** (TRP-002): `EquipmentUnit` has identity and methods
  but no repository and is only reached through `Equipment`.
- **A projection's fold is an event fold** (ES1-002): rebuilding a read model must give the same rows, so a wall-clock
  read there is the same defect as in an aggregate's `Apply`.
- **A transient integration message is not the event store** (TRP-022): outbox rows are deleted after dispatch.
- **Idempotency is judged on redelivery**: a handler is idempotent if a second delivery of the same message (or a
  retry of the same command) does not apply its effect twice — through an inbox, a lookup, a natural key or a
  fixed-value write.

- **A projection is a handler** (TRP-025): an event folded by its aggregate and applied by a projection has
  subscribers, even with no message handler.
- **A schema change is judged against what is already stored** (ESC-001, TRP-026): the Billing store keeps events as
  JSON with a schema version, so a rename of a stored property is a breaking change to existing data unless the version
  is bumped and an upcaster lifts the old form.

## Reproduce

```bash
git clone https://github.com/code-assurance-initiative/bench-csharp-domain-events
git clone https://github.com/code-assurance-initiative/scanner-benchmark
cd bench-csharp-domain-events && dotnet build -c Release && dotnet test -c Release
# run any scanner over the clone, producing SARIF, then:
cd ../scanner-benchmark
python3 -m cai_bench validate --key ../bench-csharp-domain-events/benchmark/answer-key.json --taxonomy taxonomy.json
python3 -m cai_bench score --help
```
