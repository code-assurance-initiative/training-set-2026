# ADR 0002: Keep state in memory behind store interfaces

- Status: accepted
- Date: 2026-10-06

## Context

This service is a reference implementation used to demonstrate the API and its rules. It runs as a single instance and
must start with no external dependencies. A database would add a migration story, connection management and
credentials without adding anything to what the service demonstrates.

## Decision

State is held in process memory by `InMemoryCatalogStore` and `InMemoryInventoryStore`, behind the
`ICatalogStore` and `IInventoryStore` interfaces owned by the application layer. The interfaces are asynchronous and
take a `CancellationToken`, so a persistent implementation can replace the in-memory one without changing callers.

`IInventoryStore` exposes reads and writes as callbacks executed under a single lock, which makes each callback an
atomic unit of work.

## Consequences

- All data is lost on restart, and the service cannot be scaled out to more than one instance. Both are acceptable
  for its purpose and are stated in the README.
- There is nothing to back up and no schema to migrate.
- A persistent store must provide the same atomicity per callback (for example a serializable transaction).
