# 3. Test strategy: xUnit v3, in-memory integration tests, Gherkin for the money rules

Date: 2026-09-05 · Status: accepted

## Context

Rounding and allocation rules are agreed with finance colleagues who read examples, not code. The API must be tested
with its real middleware, authentication and serialisation.

## Decision

- Unit tests with xUnit v3 on the VSTest runner (so `coverlet.collector` can measure coverage), time on
  `FakeTimeProvider`, HTTP on stub handlers.
- Integration tests through `WebApplicationFactory`, with a test token issuer and a fixed rate table.
- The conversion and allocation rules as Gherkin scenarios (Reqnroll), executed by the same runner.

## Consequences

The scenarios double as documentation for finance. Nothing in the default test run needs the network.
