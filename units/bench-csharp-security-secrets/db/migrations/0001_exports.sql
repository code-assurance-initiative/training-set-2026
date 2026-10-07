-- Exports database: the inventory the service reads and the export jobs it writes.

CREATE TABLE IF NOT EXISTS inventory_items (
    warehouse_code  char(5)      NOT NULL,
    sku             varchar(32)  NOT NULL,
    description     varchar(200) NOT NULL,
    quantity        integer      NOT NULL CHECK (quantity >= 0),
    bin_location    varchar(16)  NOT NULL,
    PRIMARY KEY (warehouse_code, sku)
);

CREATE TABLE IF NOT EXISTS export_jobs (
    id               uuid         PRIMARY KEY,
    warehouse_code   char(5)      NOT NULL,
    status           smallint     NOT NULL,
    object_key       varchar(200) NOT NULL,
    manifest_sha256  char(64),
    requested_by     varchar(100) NOT NULL,
    created_at       timestamptz  NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_export_jobs_created_at ON export_jobs (created_at);
