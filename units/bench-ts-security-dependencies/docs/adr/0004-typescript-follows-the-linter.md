# ADR 0004 — The TypeScript compiler moves with the linter

- Status: accepted
- Date: 2026-09-14

## Context

TypeScript 7 has been released. The lint rules this repository relies on come from `typescript-eslint`, whose current
release (8.71) declares support for TypeScript `>=4.8.4 <6.1.0`; with TypeScript 7 the type-aware rules either refuse
to run or run against an unsupported compiler API.

## Decision

`typescript` stays on the newest 6.0 release. It moves to 7 in the same change as the `typescript-eslint` release that
supports it.

## Consequences

- `npm outdated` lists `typescript` as a major behind; that is this decision, not drift.
- The compiler and the type-aware lint rules always agree about the language version.
