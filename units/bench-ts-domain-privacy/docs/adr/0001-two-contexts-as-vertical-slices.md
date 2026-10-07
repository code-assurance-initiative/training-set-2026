# ADR 0001: Two bounded contexts, each organised as vertical slices

- Status: accepted
- Date: 2026-10-07

## Context

The club runs two kinds of work that change for different reasons and are owned by different people: the front desk
and the class schedule (members, consents, classes, bookings, reminders) and the bookkeeping (accounts, invoices, fees).
A change to how classes are booked must not ripple into invoicing, and the bookkeeping rules must not be bent to fit the
booking model. Inside each area most changes are one feature at a time: a new endpoint, a new job, a new rule.

## Decision

- **Membership** (`src/membership`) and **Billing** (`src/billing`) are bounded contexts. Each has its own domain model
  (`domain/`: aggregates, value objects, repository ports), its own persistence (`db/`) and its own tables.
- Inside a context, code is organised by feature: one folder per slice under `features/`, holding the handler, its
  route or job entry point and any query only it needs. A slice may use its context's domain model and ports; it does
  not use another slice's code. What two slices need lives in the context's `domain/`.
- The contexts talk only through integration messages (ADR 0005). Membership's contract is
  `src/membership/contracts/integration-events.ts`: plain values, ids as strings. Billing keeps its own model of the
  people it invoices and never compiles against Membership's domain types; what both contexts share is the shared
  kernel (ADR 0002).

## Consequences

- A feature is added or removed by adding or removing one folder and one line of wiring.
- Billing can be split into its own service by replacing the in-process bus with a broker.
- Some code repeats across slices (row mapping, small queries); that is accepted to keep slices independent.
