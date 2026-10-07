# Backup and restore

## What holds state

Only the PostgreSQL database. The API and the worker are stateless; their pods can be replaced at any time.

## Backups

The database is a managed PostgreSQL instance with:

- daily automated snapshots, kept for 35 days;
- continuous WAL archiving, giving point-in-time recovery to any second in the retention window;
- a cross-region replica of the snapshots.

## Objectives

- RPO (data we accept to lose): 5 minutes.
- RTO (time to restore service): 1 hour.

## Restore

1. Create a new instance from the point in time just before the incident (provider console or CLI).
2. Update the `parcel-tracking-db` Kubernetes secret with the new connection string.
3. Restart both Deployments (`kubectl -n parcel-tracking rollout restart deployment`).
4. Run the current release's migrations bundle; it is a no-op when the schema is already current.

Notifications written between the restore point and the incident are lost with the data; carriers resend scans on the
next poll, so tracking history recovers by itself.

## Drills

A restore into a scratch instance is rehearsed every quarter; the time taken is recorded against the RTO.
