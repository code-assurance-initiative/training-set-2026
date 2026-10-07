---
title: Dependency-free contracts
status: accepted
enforcement: test
enforcement_link: tests/FleetOps.ArchitectureTests/ContractsTests.cs
---

# 0005. Dependency-free contracts

## Context

The API's JSON and the Worker's messages are consumed by the fleet office front end and by partners. Changing a
domain type must not silently change the wire format.

## Decision

`FleetOps.Contracts` holds the wire DTOs as immutable records. Code under `src/FleetOps.Contracts/` must not
reference `src/FleetOps.Domain/`, nor any other FleetOps project. Contracts change additively; `ContractVersion`
is raised on every change.

## Consequences

- Every project may depend on Contracts; Contracts is the most stable package in the solution.
- Mapping from domain types to contracts lives in the Application layer.
