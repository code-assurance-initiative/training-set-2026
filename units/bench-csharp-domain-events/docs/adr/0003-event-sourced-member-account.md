# 3. The member account is event-sourced

Date: 2026-10-07 · Status: accepted

## Context

Members dispute fees. Support needs to see every charge, deposit and payment in order, and finance needs balances as
they were at a date.

## Decision

`MemberAccount` is event-sourced: commands decide and emit events, `Apply` folds them into state, and the account is
loaded by replaying its stream. Events are immutable records. Balances for screens come from a projection
(`AccountBalanceView`), rebuilt from the log when its shape changes.

The event store is an `IEventStore` port. The development implementation is in memory and records inbox receipts
under the same lock as appends, which is the guarantee a database-backed store gives with one transaction. It keeps
events in their stored form (a type name, a schema version and JSON, `AccountEventSerializer`), as a durable store
does, so that the serialisation is exercised by every test that touches an account.

## Consequences

Folds must be deterministic: they read only the event. Events cannot be edited after the fact; corrections are new
events.
