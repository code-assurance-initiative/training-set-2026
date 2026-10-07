# 1. Lending and Billing are separate bounded contexts

Date: 2026-10-07 · Status: accepted

## Context

Lending decides who may borrow what and when; Billing decides what members owe. They change for different reasons
(lending rules versus fee policy) and were changed by different people.

## Decision

Model them as two bounded contexts with their own domain, application and infrastructure projects. They exchange
integration events defined in `Rentals.Contracts`. The only shared code is the shared kernel (`Money`, the
entity/aggregate base types) and the messaging abstractions.

## Consequences

Billing keeps its own view of members (an account per member id) and of loans (`LoanReference`). Consistency between
the contexts is eventual, through the outbox.
