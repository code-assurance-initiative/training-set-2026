# 4. An in-process bus behind messaging abstractions

Date: 2026-10-07 · Status: accepted

## Context

Both contexts run in one process today. A broker would add an operational dependency without a consumer outside the
process.

## Decision

Handlers depend on `IMessageBus`, `IIntegrationEventHandler<T>` and `ICommandHandler<T>` only. `InMemoryMessageBus`
delivers published events to every registered handler in a new scope; commands sent to another service's endpoint are
queued for it. Moving to a broker replaces the bus implementation, not the handlers.

## Consequences

Tests run the full flow without infrastructure. Delivery semantics (at least once, message ids) are already those of a
broker, so handlers are written for redelivery from the start.
