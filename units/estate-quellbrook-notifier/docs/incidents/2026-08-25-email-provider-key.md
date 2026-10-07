# Incident: e-mail provider API key committed to the repository

- Date: 2026-08-25
- Severity: high (credential exposure), no customer impact found
- Owner: Customer Comms (Lucia Brennan)

## Timeline (CEST)

- 2026-08-18 17:52 — the provider API key used for staging is committed in `src/Quellbrook.Notifier/appsettings.json`
  to make staging send e-mail. The repository is public.
- 2026-08-25 07:40 — the provider's leaked-key alert reaches the Customer Comms mailbox.
- 08:05 — the key is revoked in the provider's console and a new key is created in the secret store.
- 09:12 — the key is removed from the configuration; the worker reads both provider keys from the secret store.
- 11:00 — the provider's activity log for the revoked key is reviewed: only messages to the staging test inboxes
  between 2026-08-18 and 2026-08-25; no other use.

## Cause

Staging had no way to receive the key (the manifest did not pass it). The quickest way to get staging sending was
taken, and the review did not stop it.

## Actions

- Done: key revoked and replaced; the key now comes from the secret store in every environment (ADR 0003).
- Done: CI scans the commits of every push and pull request for secrets.
- Accepted: the revoked key remains in the repository's history. Rewriting the published history would break every
  clone and fork and would not make the key secret again; it no longer authenticates.
