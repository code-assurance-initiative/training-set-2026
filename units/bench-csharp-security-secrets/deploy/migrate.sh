#!/usr/bin/env bash
# Applies the schema migrations in db/migrations to the production databases. Every migration is idempotent.
set -euo pipefail

cd "$(dirname "$0")/.."

EXPORTS_DB_URL="postgres://migrator:CAuKMvgHDLrrWAeGX30yV5UI@exports-db.prod.internal:5432/exports?sslmode=verify-full"
AUDIT_DB_URL="${AUDIT_DB_URL:?set AUDIT_DB_URL to the audit database URL}"

apply() {
  local url="$1" file="$2"
  echo "applying ${file}"
  psql "$url" --no-psqlrc -v ON_ERROR_STOP=1 --single-transaction -f "$file"
}

apply "$EXPORTS_DB_URL" db/migrations/0001_exports.sql
apply "$AUDIT_DB_URL" db/migrations/0002_audit.sql
