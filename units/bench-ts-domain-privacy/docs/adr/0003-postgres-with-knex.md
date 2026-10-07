# ADR 0003: PostgreSQL through knex, with versioned migrations

- Status: accepted
- Date: 2026-10-07

## Context

The service needs transactions across a handful of tables per context, and tests that exercise real SQL without a
database server on every developer machine and CI runner.

## Decision

PostgreSQL is the store. Code reaches it through the knex query builder, in each context's `db/` folder only, behind
the repository ports of the domain model. The schema is created by versioned migrations in
`src/platform/db/migrations`, applied by `npm run migrate` before a new version starts. Tests run the same migrations
against pg-mem, an in-memory PostgreSQL emulation, so no test touches a network.

Each context exposes a unit of work (`MembershipTransactions`, `BillingTransactions`) that hands a slice the
repositories bound to one database transaction.

## Consequences

- No ORM mapping lives in the domain model; the stores map rows to aggregates by hand.
- pg-mem implements a subset of PostgreSQL; SQL that it cannot run is avoided rather than tested elsewhere.
