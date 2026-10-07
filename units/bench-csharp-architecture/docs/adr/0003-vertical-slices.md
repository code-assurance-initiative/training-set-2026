---
title: Vertical slices in the Application layer
status: accepted
enforcement: prose
---

# 0003. Vertical slices in the Application layer

## Context

A single `WorkOrderService` with twenty methods had become the place every change collided.

## Decision

The Application layer is organised by feature under `Features/<Slice>/` (Vehicles, WorkOrders, Inspections,
Maintenance). Each use case is one command or query with one handler, dispatched through the Mediator library.
A slice must not reference another slice's types: when one feature needs another to act, it sends that feature's
command through the mediator. Shared read models and ports live in `Abstractions/`.

Slice boundaries are checked in code review.

## Consequences

- A feature can be read, changed and tested on its own.
- Cross-slice calls are visible as mediator sends.
