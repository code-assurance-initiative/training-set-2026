-- Audit database: append-only record of export actions. The service role may insert, never update or delete.

CREATE TABLE IF NOT EXISTS audit_entries (
    id                  bigint       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    occurred_at         timestamptz  NOT NULL,
    action              varchar(40)  NOT NULL,
    export_id           uuid         NOT NULL,
    client_application  varchar(100) NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_audit_entries_export_id ON audit_entries (export_id);
