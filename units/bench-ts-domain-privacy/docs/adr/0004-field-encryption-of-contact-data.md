# ADR 0004: Field-level encryption of phone numbers and dates of birth

- Status: accepted
- Date: 2026-10-07

## Context

The database is encrypted at rest by the hosting platform, which protects against a lost disk but not against a
leaked backup, a read replica opened too widely or a support query. Phone numbers and dates of birth are not needed to
find a member, so they need not be readable to anyone who can read the table. The e-mail address is the log-in name and
the uniqueness key and must be searchable.

## Decision

Phone numbers and dates of birth are encrypted by the application before they are written, wherever they are stored,
with AES-256-GCM (`src/platform/field-cipher.ts`): a fresh 96-bit IV per value, the authentication tag stored with the
ciphertext, and a version prefix so the scheme can be rotated. The 256-bit key comes from the deployment's secret store
through `FIELD_ENCRYPTION_KEY`; it is never in the repository. Names and e-mail addresses rely on the platform's storage
encryption (data inventory).

## Consequences

- Phone numbers and dates of birth cannot be searched or sorted in SQL.
- Rotating the key means re-encrypting the two columns with a migration job that reads with the old key.
