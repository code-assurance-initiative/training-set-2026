# ADR 0002: Keep state in memory behind synchronous store interfaces

- Status: accepted
- Date: 2026-10-07

## Context

This service is a reference implementation used to demonstrate the API and its rules. It runs as a single instance
and must start with no external dependencies. A database would add a migration story, connection management and
credentials without adding anything to what the service demonstrates.

## Decision

State is held in process memory by `InMemoryCatalogStore` and `InMemoryInventoryStore`, behind the `CatalogStore`
and `InventoryStore` interfaces owned by the application layer.

The interfaces are synchronous. Node.js runs JavaScript on a single thread, so a service method that calls the store
several times without awaiting is atomic with respect to every other request; no lock is needed. Reserved quantities
are derived from the active reservations rather than stored separately, so they cannot drift.

## Consequences

- All data is lost on restart, and the service cannot be scaled out to more than one instance. Both are acceptable
  for its purpose and are stated in the README.
- There is nothing to back up and no schema to migrate.
- A persistent store would make the interfaces asynchronous and must then provide the same atomicity per service
  operation (for example a serializable transaction); that is a new decision.
