---
title: Layered architecture
status: accepted
enforcement: test
enforcement_link: tests/FleetOps.ArchitectureTests/LayeringTests.cs
---

# 0002. Layered architecture

## Context

FleetOps started as one ASP.NET Core project. Persistence code, vendor calls and business rules were mixed in
controllers, and the maintenance rules could not be tested without a database.

## Decision

The solution is split into layers whose dependencies point inward: the Domain at the centre, the Application around
it, Infrastructure and the hosts outside.

- Code under `src/FleetOps.Domain/` must not reference `src/FleetOps.Application/`.
- Code under `src/FleetOps.Domain/` must not reference `src/FleetOps.Infrastructure/`; this covers every
  infrastructure adapter project as well (`FleetOps.Infrastructure.*`).
- Code under `src/FleetOps.Application/` must not reference `src/FleetOps.Infrastructure/`.
- Code under `src/FleetOps.Infrastructure/` must not reference `src/FleetOps.Api/`.

The Domain declares the ports it needs (repository interfaces, `IUnitOfWork`); Infrastructure implements them. The
hosts (`FleetOps.Api`, `FleetOps.Worker`) are the composition roots and reference every layer to wire dependency
injection. Wire DTOs live in `FleetOps.Contracts` (ADR 0005).

## Consequences

- Domain rules are unit-tested without a database or network.
- `tests/FleetOps.ArchitectureTests/LayeringTests.cs` checks these rules on every build.
