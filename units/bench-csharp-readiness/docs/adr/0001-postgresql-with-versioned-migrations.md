# ADR 0001: PostgreSQL, with the schema evolved by versioned EF Core migrations

- Status: accepted
- Date: 2026-08-03

## Context

Parcels, their tracking history and the notification outbox need transactions (a status change and its notification
are written together) and outlive any one deployment. Schema changes must be reviewable and must run before the code
that needs them.

## Decision

Use PostgreSQL through EF Core (Npgsql). Every schema change is an EF Core migration generated with
`dotnet ef migrations add` and committed. Releases build a migrations bundle; the deploy workflow runs it against the
production database before rolling out new pods. Applications never create or migrate the schema at startup.

## Consequences

- Migrations must be backward compatible with the previous release (expand, then contract in a later release), because
  old pods keep running while the new ones start.
- Local development applies migrations with `dotnet ef database update`.
