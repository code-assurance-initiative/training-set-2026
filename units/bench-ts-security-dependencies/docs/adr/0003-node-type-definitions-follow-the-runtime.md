# ADR 0003 — Node.js type definitions follow the runtime

- Status: accepted
- Date: 2025-09-01

## Context

`@types/node` publishes one major per Node.js release line, and its newest major describes APIs that older runtimes do
not have. The service runs on Node.js 22 (`.nvmrc`, `engines`).

## Decision

`@types/node` stays on the major of the runtime line the service runs on (22), at the newest release of that major.
It moves to a new major together with the runtime, in the same change as `.nvmrc` and `engines`.

## Consequences

- Type-checking rejects runtime APIs the service cannot call.
- `npm outdated` lists `@types/node` as behind the newest major; that is this decision, not drift.
