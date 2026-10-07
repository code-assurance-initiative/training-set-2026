# 1. Dapper for search queries, EF Core for report definitions

Date: 2026-09-14

## Status

Accepted

## Context

The service has two kinds of data access. Document search runs hand-tuned PostgreSQL queries (trigram `ILIKE`,
`ts_rank`, keyset paging) over a large, read-mostly `documents` table that another system writes. Report definitions
and schedules are a small, write-heavy model that this service owns, with relations and migrations.

## Decision

- Search and document reads use **Dapper** over `NpgsqlDataSource`: the SQL is visible in the code, and the planner
  sees exactly what we wrote.
- Report definitions and schedules use **EF Core** (`Npgsql.EntityFrameworkCore.PostgreSQL`) with a `DbContext` the
  service owns. Raw SQL through EF is allowed for the reporting views EF cannot express.
- Saved searches, which predate both, stay on plain `NpgsqlCommand` until they are migrated.

## Consequences

- Two data-access styles to review. Every query that takes a value from a request must bind it as a parameter
  (`@name` with Dapper and `NpgsqlCommand`, `FromSqlInterpolated`/`FromSql` with EF Core); identifiers such as sort
  columns cannot be parameters and must come from a fixed list in code.
- The search tables are not migrated by this service.
