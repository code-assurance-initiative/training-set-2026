# 1. Feature folders instead of horizontal layers

Date: 2026-09-21

## Status

Accepted

## Context

The service covers a dozen loosely related features (search, exports, imports, reports, shares, subscriptions …)
that different people change at different times. A layered layout (`http/`, `application/`, `infrastructure/`) would
spread each feature over three folders.

## Decision

Each feature gets one folder under `src/` with its routes, its logic and its store. Cross-cutting HTTP concerns
(authentication, scopes, validation, problem responses) live in `src/http`. Stores are classes over a narrow client
interface (`SqlClient`, a knex instance, a MongoDB collection), and routes depend on `Pick<…>` of the classes they use,
so tests substitute fakes without a mocking library. `src/composition.ts` is the only place that creates
connections.

## Consequences

- A feature can be read, changed and tested in one place.
- Shared helpers must stay small; anything feature-specific stays in its feature.
