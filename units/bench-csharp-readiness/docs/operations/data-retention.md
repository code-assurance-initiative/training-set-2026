# Data retention

| Data | Kept for | Deleted by |
|---|---|---|
| Delivered webhook notifications (outbox rows) | 30 days after delivery | `RetentionSweeper` (worker) |
| Delivered or returned parcels, with their tracking history, pickup point and redirect note | 180 days after their last update | `RetentionSweeper` (worker) |
| Parcels still in transit | while in transit | — |

The sweeper runs every six hours (`Retention:*` in the worker's configuration). Deleted rows remain in database
backups until those expire (35 days, see [backup-and-restore.md](backup-and-restore.md)).

Redirect notes are free text a merchant's customer may have written; they are kept no longer than the parcel itself.
