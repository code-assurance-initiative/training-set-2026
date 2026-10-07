# ADR 0001: Three layers with dependencies pointing inward

- Status: accepted
- Date: 2026-10-07

## Context

The service is small, but it has three distinct concerns: speaking HTTP (routing, validation, authentication),
the stock and reservation rules, and keeping state. The rules are what the service exists for and what most tests
target; they should not change when the transport or the store does.

## Decision

`src/` has three folders. `application` holds the services, the domain types as readonly interfaces, and the store
interfaces the services need; it imports nothing from the other two. `http` maps requests to service calls and
`Result` values to responses. `infrastructure` implements the store interfaces and runs background jobs. Wiring
happens in one composition root (`composition.ts`, `server.ts`).

The application layer is a set of transaction scripts over plain types, not a domain model: the rules are few and
fit in short service methods.

The rule is enforced by ESLint (`no-restricted-imports` per folder), so a violation fails CI rather than waiting for
review.

## Consequences

- Services are unit-tested without HTTP or timers; routes are tested over HTTP against the real services.
- Moving to another transport or store touches one layer.
- A richer domain model would be a new decision.
