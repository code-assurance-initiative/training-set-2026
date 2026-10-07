# ADR 0005: Integration messages through a transactional outbox

- Status: accepted
- Date: 2026-10-07

## Context

Billing must learn when a member registers (to open an account) and when a member is erased (to anonymise its copy).
Publishing after the commit loses the message if the process dies in between; publishing before it announces changes
that may roll back.

## Decision

A context writes its integration messages to `outbox_messages` in the transaction that makes the change. A dispatcher
job publishes committed messages in order on an in-process bus and deletes each one after its subscribers ran, so the
personal data some messages carry does not outlive their delivery. Delivery is at least once: subscribers are
idempotent (Billing looks the account up by member before opening one; anonymising twice changes nothing).

## Consequences

- A message is delayed by up to one job interval.
- Moving Billing out of the process replaces the bus, not the outbox.
