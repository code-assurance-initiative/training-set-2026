# 1. The schema is owned by versioned knex migrations

Date: 2026-05-12

## Status

Accepted

## Context

The service stores parcels, tracking events and a webhook outbox in PostgreSQL. The API and the worker both read
and write it, and production also contains tables of the legacy dispatch system that the service adopts.

## Decision

Every schema change is a knex migration under `apps/tracking-service/src/db/migrations`, listed explicitly and in
order in `migrations/index.ts`. A migration Job runs them (from the same image) before each rollout; no process
creates or alters tables at start-up. Tests and specifications apply the same migrations to an in-memory database.
A migration that adopts a legacy table checks for its existence instead of assuming a fresh database.

## Consequences

- The schema of every environment is described by the migration history, and a release that needs a new column
  ships the migration that adds it.
- Migrations must be backward compatible with the previous release, which is still running while the Job runs.
- A migration cannot be edited once released; corrections are new migrations.
