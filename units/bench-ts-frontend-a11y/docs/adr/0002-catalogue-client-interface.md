# ADR 0002: One CatalogueClient interface with an HTTP and an in-memory implementation

- Status: accepted
- Date: 2026-09-16

## Context

Front-end work often starts before the matching API endpoint is deployed, and component tests need predictable data.
Mocking `fetch` per test couples tests to URLs and makes them brittle.

## Decision

All server access goes through the `CatalogueClient` interface, provided by React context. `createHttpClient` talks to
`/api`; `createDemoClient` implements the same interface in memory with a small fixed catalogue and is used by the
development server and by the component tests. Only the HTTP client's own tests look at URLs.

## Consequences

- Components never call `fetch` directly.
- The demo client must follow API behaviour that components rely on (paging, renewal limits, 404s); its tests pin that.
