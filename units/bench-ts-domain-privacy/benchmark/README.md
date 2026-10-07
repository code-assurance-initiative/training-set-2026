# Benchmark: domain modelling, vertical slices and personal data (TypeScript)

This repository is one unit of the scanner benchmark in
[`code-assurance-initiative/scanner-benchmark`](https://github.com/code-assurance-initiative/scanner-benchmark).
Its labels are in [`answer-key.json`](answer-key.json) (format: `scanner-benchmark/docs/CONTRACT.md`); its
authoring log is [`journal.md`](journal.md).

## Theme

A small **fitness-club** service in Node.js 22 and strict TypeScript: members, scheduled classes, class bookings with
e-mail and SMS reminders, and monthly invoicing. It is written as two bounded contexts, each organised as **vertical
feature slices** (`src/<context>/features/<slice>/`) around a shared domain model per context:

- **Membership** — the Member aggregate (name, e-mail, phone, date of birth, consents per purpose), ClassSession and
  ClassBooking aggregates; slices to register, change contact details, record consent, schedule and book classes,
  cancel a membership, send class reminders, erase a member, export a member's data and purge lapsed members.
- **Billing** — the BillingAccount and Invoice aggregates (invoice lines are child entities); slices to open an
  account when a member registers, anonymise it on erasure, issue invoices, charge late fees and list overdue invoices.

Aggregates, entities and value objects derive from a small shared kernel (`src/shared-kernel`, ADR 0002); ids are
value-object classes. Persistence is PostgreSQL through knex with versioned migrations; tests run the same migrations
against an in-memory PostgreSQL emulation (pg-mem), so nothing touches a network. The contexts talk only through
integration events written to a transactional outbox (ADR 0005). Personal data is described in
`docs/privacy/data-inventory.md` (data classes, purpose, lawful basis, protection, retention): phone and date of birth
are encrypted at field level, personal-data changes are audited, lapsed members are erased after a retention period,
and members can export their data and be erased.

The rest of the repository carries the shared TypeScript scaffolding (CI pinned by commit SHA, CodeQL, Dependabot with
a cooldown, `SECURITY.md`, README, ADRs, architecture document, CHANGELOG, ESLint + Prettier, vitest with coverage
thresholds) so that the only signal is the theme.

## Labels are about truth

A `must-fire` is a defect a careful reviewer of a DDD, vertical-slice or privacy-sensitive codebase would raise; a
`must-not-fire` is a site a careless rule would flag and a careful reviewer would not. Where a scanner may report a
type-level finding for a defect that lives on one member, the entry's line range runs from the type declaration to the
member, so a report at either matches.

## Plants, traps and score bands

### Plants (`must-fire`)

| Id | Concept | Site | Why |
|---|---|---|---|
| AGG-001 | `cross-aggregate-object-reference` | `src/membership/domain/classes/class-booking.ts` | The ClassBooking aggregate holds the Member aggregate itself instead of its MemberId: the booking store loads the whole member (name, contact details, consents) with every booking, and code holding a booking can change the member outside the member's own consistency boundary. A booking needs the member's identity, not the member. |
| STI-001 | `primitive-entity-identifier` | `src/billing/domain/invoices/invoice.ts` | Invoice identifies the billing account it belongs to by a raw string although the context has a BillingAccountId type, which every other reference uses: an invoice id, a member id or any string can be passed where an account id is meant and the compiler cannot tell. |
| DIB-001 | `domain-depends-on-infrastructure` | `src/membership/domain/members/contact-uniqueness.ts` | A domain service in the membership domain imports the query builder and queries the members table itself to decide whether an e-mail address is taken: the domain layer depends on the database driver and the table layout instead of on a port it owns (the member repository). |
| REP-001 | `repository-for-non-aggregate` | `src/billing/domain/invoices/invoice-line-repository.ts` | InvoiceLine is a child entity of the Invoice aggregate, yet it has its own repository: the late-fee slice adds a line straight to an issued invoice without loading the Invoice, so the invoice's total and its rule that an issued or paid invoice takes no new lines are bypassed. |
| SDR-001 | `scattered-domain-rule` | (repository) | The rule 'a member in good standing may attend classes' (status active AND the paid membership period has not ended) is decided twice, in the book-class slice and in the class-reminder slice, over Member's status and membershipEndsOn, and nowhere on Member. A change to the rule (a grace period, a suspension state) has to be found and made in both places. |
| MAT-001 | `multi-aggregate-transaction` | `src/membership/features/cancel-membership/cancel-membership.ts` | Cancelling a membership saves the Member and every one of the member's future ClassBooking aggregates in one transaction: the bookings' consistency now depends on the member's, a concurrent booking change fails the whole cancellation, and the two aggregates can no longer be stored apart. The bookings should be released by a handler of the MembershipCancelled event. |
| CIE-001 | `constructible-invalid-entity` | `src/membership/domain/classes/class-session.ts` | ClassSession's public constructor stores a raw title, capacity and duration without checking them and the type offers no factory: a session with an empty title, zero or negative capacity or a negative duration can be created and persisted (the HTTP schema is the only guard, and the domain does not rely on it elsewhere). |
| CSC-001 | `cross-slice-coupling` | `src/membership/features/send-class-reminders/send-class-reminders.ts` | The class-reminder slice depends on the book-class slice's internal UpcomingBookingsQuery (the query behind the 'my bookings' endpoint): a change to what the booking screen lists silently changes who gets reminded, and the two slices can no longer change independently. The reminder slice should own its query. |
| BTL-001 | `boundary-type-leakage` | `src/billing/domain/accounts/billing-account.ts` | Billing's BillingAccount aggregate exposes Membership's domain type MemberId (imported from the membership domain) in its public surface: Billing compiles against Membership's model, against ADR 0001, which makes the integration events with plain string ids the only contract between the contexts. |
| LOG-001 | `sensitive-data-in-logs` | `src/membership/features/send-class-reminders/sms-gateway.ts` | A failed SMS reminder is logged with the member's phone number in the message text: the number lands in the log store, which has none of the protection, retention or erasure that the members table has (the privacy notice promises phone numbers are kept only encrypted). The e-mail gateway logs a pseudonym instead. |
| CON-001 | `consent-not-checked` | `src/membership/features/send-class-reminders/send-class-reminders.ts` | SMS reminders are optional and, per the privacy notice, sent only to members who consented to them; this path sends one to every member with a phone number and never asks Member.hasConsented('sms-reminders'), so members who refused or withdrew that consent are texted anyway. |

### Traps (`must-not-fire`)

| Id | Concept | Site | Why |
|---|---|---|---|
| TRP-001 | `cross-aggregate-object-reference` | `src/membership/domain/classes/class-booking.ts` | The booking refers to the ClassSession aggregate by its strongly-typed id, which is the aggregate-boundary rule followed, not broken. |
| TRP-002 | `cross-aggregate-object-reference` | `src/billing/domain/invoices/invoice.ts` | Invoice holds its own child entities (its lines). InvoiceLine has identity but is created and changed only through Invoice: it is inside the boundary, not another aggregate. |
| TRP-003 | `primitive-entity-identifier` | `src/membership/contracts/integration-events.ts` | Integration events cross the context boundary and carry ids as plain strings on purpose, so the consuming context does not depend on the producer's id types (ADR 0001). |
| TRP-004 | `domain-depends-on-infrastructure` | `src/membership/db/member-store.ts` | The knex implementation of the member repository port, outside the domain folder: infrastructure depending on the domain is the allowed direction. |
| TRP-005 | `repository-for-non-aggregate` | `src/billing/domain/invoices/invoice-repository.ts` | A repository for the Invoice aggregate root, returning whole invoices with their lines. |
| TRP-006 | `scattered-domain-rule` | `src/billing/domain/invoices/invoice.ts` | The overdue rule lives on Invoice, the type that owns the due date and the status; the late-fee and the overdue-listing slices both ask the invoice rather than re-deciding it. |
| TRP-007 | `multi-aggregate-transaction` | `src/membership/features/book-class/book-class.ts` | The handler reads the Member (standing) and the ClassSession (capacity, start) for context but writes only the new ClassBooking. |
| TRP-008 | `constructible-invalid-entity` | `src/membership/domain/members/member.ts` | Member's constructor is private; the only ways to create one are the validating register factory, which takes value objects that validated themselves, and rehydration from the store. |
| TRP-009 | `constructible-invalid-entity` | `src/billing/domain/accounts/billing-account.ts` | The public constructor guards the raw holder name and e-mail it receives (it throws on an empty name, a malformed address, or no holder on an account that is not anonymised); ids and the birth date arrive as value objects. |
| TRP-010 | `cross-slice-coupling` | `src/membership/features/erase-member/erase-member.ts` | The erasure slice depends on the membership domain's unit-of-work port (the repositories of one transaction), which every slice of the context shares; it references no other slice. |
| TRP-011 | `boundary-type-leakage` | `src/billing/domain/invoices/invoice.ts` | Money in Billing's public surface is the shared kernel (ADR 0002), owned by neither context. |
| TRP-012 | `sensitive-data-in-logs` | `src/membership/features/send-class-reminders/email-gateway.ts` | The failed e-mail reminder is logged with a keyed pseudonym of the member id and the e-mail domain only; the address itself never reaches the log. |
| TRP-013 | `consent-not-checked` | `src/membership/features/send-class-reminders/send-class-reminders.ts` | The e-mail reminder of a booked class is a service message the member asked for by booking; its lawful basis is the membership contract (privacy notice), not consent, so no consent check belongs here. |
| TRP-014 | `anemic-domain-model` | `src/billing/domain/invoices/invoice-line.ts` | (Promoted from scan iteration 1.) An invoice line is an immutable child entity of Invoice: it is created and totalled only through the aggregate root, whose addLine/issue rules protect it. A line with no state-changing behaviour of its own is the design, not business logic living in a service. |

### Score bands

| Id | Concept | Band | Why |
|---|---|---|---|
| BND-001 | `data-encryption-controls` | 35–80 | Phone numbers and dates of birth in the members table are encrypted at field level (AES-256-GCM, key from configuration) and HTTPS is enforced behind the proxy, but Billing copies the date of birth into billing_accounts in plaintext, against the data inventory, and keys are not held in a key-management service. Partial. |
| BND-002 | `audit-trail` | 30–75 | Every change to a member's personal data (registration, contact details, consent, erasure) is written to an append-only audit log in the same transaction by the member store; Billing's copy of name, e-mail and date of birth changes with no audit record. Partial. |
| BND-003 | `data-retention-policy` | 30–80 | A retention job erases members whose membership lapsed more than the configured retention period ago (24 months by default); the reminder delivery log, which keeps the phone number or e-mail address each reminder went to, is never purged although the data inventory gives it 90 days. Partial. |
| BND-004 | `data-subject-rights` | 40–90 | Erasure (anonymisation, propagated to Billing by an integration event), export of a member's data and recorded, withdrawable consent per purpose all exist; consent is not enforced on the SMS reminder path (CON-001). Mostly supported. |
| BND-005 | `personal-data-inventory` | 10–70 | Genuine personal data is held in a handful of fields: the member's first and last name, e-mail, phone and date of birth; Billing's holder name, e-mail and date of birth; the reminder recipient. Measured only in the secondary configuration (compliance framework switched on). |

## What is certified clean

Every other tracked file (source, tests, configuration, documentation) carries a `clean` label for the eleven theme
concepts (`cross-aggregate-object-reference`, `primitive-entity-identifier`, `domain-depends-on-infrastructure`,
`repository-for-non-aggregate`, `scattered-domain-rule`, `multi-aggregate-transaction`, `constructible-invalid-entity`,
`cross-slice-coupling`, `boundary-type-leakage`, `sensitive-data-in-logs`, `consent-not-checked`). A file that holds a
plant or a trap is certified clean for the theme concepts that have no entry in it. Deliberately not certified
either way: `scattered-domain-rule` in `book-class.ts` and `send-class-reminders.ts`, which hold the two copies of the
rule that the repository-level plant SDR-001 names (a scanner may point at either copy); and `boundary-type-leakage` in
`billing-account-repository.ts` and `billing-account-store.ts`, where BTL-001's `MemberId` spreads into the account
lookup by member (the same leak, labelled once at the aggregate).

The compliance postures (`data-encryption-controls`, `audit-trail`, `data-retention-policy`, `data-subject-rights`,
`personal-data-inventory`) are score bands, not sites: they are properties of the whole service.

## Not covered here, and why

- **Other domain-modelling and event-driven concepts** (public setters, event naming, outbox, idempotency, value-object
  mutability…) are not labelled: they are measured by `bench-csharp-domain-events` and `bench-ts-codehealth`. Results
  for them are `uncovered`. The anemic-model concept has one trap (TRP-014), promoted from a scan; no file is certified
  clean of it.
- **Security, readiness and code-health concepts** are out of theme; results for them are `uncovered`.
- **`personal-data-inventory`** is a score band only: a field reported as personal data is an inventory row, not a
  defect. It is judged by a model and, in the reference scanner, only composed when a compliance framework is
  configured, so it is measured in a secondary configuration (see the journal).

## Contested truths (decided before the code)

- **Integration events with string ids** (TRP-003) are correct, not primitive identifiers: the contract must not make
  Billing depend on Membership's id types. The same id typed as Membership's `MemberId` inside Billing's aggregate
  (BTL-001) is the leak.
- **A child entity is not another aggregate** (TRP-002): invoice lines have identity but are created only through
  `Invoice`. A separate repository for them (REP-001) is the defect.
- **A slice may depend on its context's domain model and ports** (TRP-010); it may not depend on another slice's
  internals (CSC-001). Shared code of a context lives in its `domain/` folder, not in a slice.
- **Reading other aggregates is not writing them** (TRP-007): one transaction may load several aggregates for a
  decision and save one.
- **Consent is a lawful basis, not a precondition of all processing** (TRP-013): a reminder of a class the member booked
  is part of the service (contract basis, privacy notice); an optional SMS channel is consent-based and must check it
  (CON-001).
- **A pseudonym is not personal data in the log's hands** (TRP-012): the log holds a keyed HMAC of the member id that
  only the service can link back, and the e-mail domain, which identifies nobody.
- **Field encryption is judged against the data inventory**: the inventory says which data classes must be encrypted
  at rest; plaintext e-mail (needed for lookup and log-in, protected by the database's storage encryption per the
  inventory) is a documented decision, the plaintext copy of the date of birth in Billing is not (BND-001).

## Reproduce

```bash
git clone https://github.com/code-assurance-initiative/bench-ts-domain-privacy
git clone https://github.com/code-assurance-initiative/scanner-benchmark
cd bench-ts-domain-privacy && npm ci && npm run typecheck && npm test
# run any scanner over the clone, producing SARIF, then:
cd ../scanner-benchmark
python3 -m cai_bench validate --key ../bench-ts-domain-privacy/benchmark/answer-key.json --taxonomy taxonomy.json
python3 -m cai_bench score --help
```
