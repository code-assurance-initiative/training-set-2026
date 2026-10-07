# 1. Money rules in a library, transport in a host

Date: 2026-09-01 · Status: accepted

## Context

Conversion and rounding rules are needed by the API and by till software that cannot call an API for every
receipt (cash rounding, allocation of a bill).

## Decision

All money rules live in `Fx.Conversion.Core`, which has no I/O dependencies. The API and the ECB adapter depend on
it, never the reverse.

## Consequences

The library can be shipped as a package on its own. The API stays thin: controllers translate requests into library
calls.
