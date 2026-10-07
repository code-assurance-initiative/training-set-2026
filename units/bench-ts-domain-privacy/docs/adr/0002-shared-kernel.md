# ADR 0002: A small shared kernel

- Status: accepted
- Date: 2026-10-07

## Context

Both contexts need the same building blocks: a base for aggregates and entities, strongly-typed identifiers, value
objects with value equality, a result type for expected failures, and money.

## Decision

`src/shared-kernel` holds `AggregateRoot`, `Entity`, `ValueObject`, `Identifier`, `DomainEvent`, `Result` and `Money`.
It depends on nothing else in the repository. It is owned by both contexts together: a change needs the agreement of
both, which is why it stays small. `Money` (an amount in minor units of one ISO 4217 currency) may appear in either
context's public surface; nothing else of one context may appear in the other's.

## Consequences

- Ids are classes (`MemberId`, `InvoiceId`, …) that compare by value and never equal an id of another type.
- New shared types need a reason that holds for both contexts; context-specific types stay in the context.
