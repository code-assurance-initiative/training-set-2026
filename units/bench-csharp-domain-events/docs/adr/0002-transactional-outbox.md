# 2. Integration events go through a transactional outbox

Date: 2026-10-07 · Status: accepted

## Context

Saving a change and publishing its event are two writes to two systems. Publishing after commit loses the event when
the process dies in between; publishing before commit announces changes that may roll back.

## Decision

`LendingDbContext.SaveChangesAsync` maps the domain events raised by the saved aggregates to integration events and
adds them as `outbox_messages` rows in the same transaction. `OutboxDispatcher` publishes committed rows, oldest first,
and deletes each after the bus accepts it. Delivery is at least once; the outbox row id is the message id.

Consumers that are not naturally idempotent record processed message ids in an inbox, atomically with their own state
change.

The schema evolves through EF Core migrations (`Persistence/Migrations`); the worker applies pending migrations at
start-up, before the dispatcher runs.

## Consequences

A crash between publish and delete republishes a message with the same id. Handlers must tolerate redelivery.
