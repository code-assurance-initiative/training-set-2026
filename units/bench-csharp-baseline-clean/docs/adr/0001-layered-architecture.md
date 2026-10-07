# ADR 0001: A plain layered architecture

- Status: accepted
- Date: 2026-10-06

## Context

The service has a handful of business rules (bin capacity, available stock, reservation lifetimes) and a small HTTP
surface. It needs a structure that keeps those rules testable without a web host, without ceremony the problem does
not call for.

## Decision

Three projects: `Api` (HTTP concerns), `Application` (services with the business rules, and the store interfaces they
need) and `Infrastructure` (store implementations, background work). `Application` references no other project;
`Infrastructure` references `Application`; `Api` references both and is the composition root.

Application services are transaction scripts over immutable records. We deliberately do not use a domain-driven
design model (aggregates, value objects, domain events): the rules are few and live naturally in three services.

Expected failures are values (`OperationResult<T>`), not exceptions, so every caller handles them explicitly.

## Consequences

- Business rules are unit tested against the services directly.
- The layering is enforced by the compiler through project references; no separate architecture tests are needed.
- If the rules grow substantially, introducing a domain model is a new decision, recorded in a new ADR.
