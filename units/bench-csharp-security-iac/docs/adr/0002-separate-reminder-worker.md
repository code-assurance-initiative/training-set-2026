# 2. Run reminders as a separate workload

Date: 2026-09-15

## Status

Accepted

## Context

Reminders must be sent once per booking, on a schedule, regardless of API traffic. The API runs three replicas.

## Decision

A separate `Depot.Slots.Reminders` host runs the reminder pass in a `BackgroundService`, as a single-replica
Deployment. A booking is marked as announced only after the chat service accepts the message, so a failed send is
retried on the next pass.

## Consequences

With one replica, a reminder can be late by up to one poll interval plus the pod's restart time. Running two replicas
would need a lease to avoid double announcements.
