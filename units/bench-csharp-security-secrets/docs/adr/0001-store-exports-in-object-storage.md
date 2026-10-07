# ADR 0001: Store exports in S3-compatible object storage, encrypted by the service

- Status: accepted
- Date: 2026-09-10

## Context

Inventory exports range from a few kilobytes to tens of megabytes and are downloaded a handful of times within a day
of creation. Keeping them in the exports database bloats backups and replication; keeping them on the API's local disk
ties a download to the instance that rendered it.

## Decision

Exports are written to an S3-compatible bucket with path-style addressing, so the same client works against the
production store and a local emulator. The service encrypts each document with AES-256-GCM before upload (the bucket's
server-side encryption is kept on as a second layer) and records the object key, never a public URL, in `export_jobs`.

## Consequences

- Any instance can serve any download.
- Losing the bucket loses exports, which can be re-rendered from inventory; they are not a system of record.
- The encryption key must be available to every instance; rotating it requires key versioning, which a later ADR
  has to decide.
