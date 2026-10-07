---
title: Thin controllers
status: accepted
enforcement: test
enforcement_link: tests/FleetOps.ArchitectureTests/ControllerRulesTests.cs
---

# 0004. Thin controllers

## Context

With business rules in controllers, the Worker and the API applied different rules to the same work order.

## Decision

Controllers translate HTTP into Application commands and queries and nothing else. Code under
`src/FleetOps.Api/Controllers/` must not reference `src/FleetOps.Infrastructure/`; controllers reach data only
through the Application layer. Pricing and approval rules belong to the Domain or to a handler.

`Program.cs` is the composition root and is not a controller.

## Consequences

- Every entry point (API, Worker, tests) applies the same rules.
