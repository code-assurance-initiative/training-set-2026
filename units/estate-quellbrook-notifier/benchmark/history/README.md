# Scripted history

25 commits by fictional authors, rebuilt and verified by `build-history.sh` (see its header). Commits after them are real benchmark maintenance.

## Sprint 1 (2026-07-27 – 2026-08-07, release `v0.1.0`)

| Commit | Date | Author | Subject | Note |
|---|---|---|---|---|
| `5cafdbe` | 2026-07-27 | Lucia Brennan | chore: worker skeleton with central package management and lock files |  |
| `997009a` | 2026-07-28 | Lucia Brennan | docs: ADR 0001 and ADR 0002 (notify at most once per order, kind and channel) |  |
| `f3ddbf8` | 2026-07-29 | Sami Oyelaran | feat: recipients, processed messages and the notification log in PostgreSQL |  |
| `949b9a8` | 2026-07-30 | Sami Oyelaran | feat: consume orders.order-placed.v1 through an inbox |  |
| `ca18859` | 2026-07-31 | Lucia Brennan | feat: booking confirmations by e-mail through the provider |  |
| `b2b2339` | 2026-08-03 | Sami Oyelaran | feat: worker host, heartbeat probes and telemetry |  |
| `070efff` | 2026-08-04 | Lucia Brennan | build: container image and Kubernetes manifests |  |
| `82ba30b` | 2026-08-05 | Sami Oyelaran | ci: build, test, CodeQL, release and deploy workflows |  |
| `80300b6` | 2026-08-06 | Lucia Brennan | docs: README, architecture with C4 diagrams and catalog entry |  |
| `6d4297e` | 2026-08-07 | Lucia Brennan | release 0.1.0 | tag `v0.1.0` |

## Sprint 2 (2026-08-10 – 2026-08-21, release `v0.2.0`)

| Commit | Date | Author | Subject | Note |
|---|---|---|---|---|
| `26c4847` | 2026-08-11 | Sami Oyelaran | feat: SMS channel through the gateway |  |
| `814da09` | 2026-08-12 | Lucia Brennan | feat: out-for-delivery and delivered notifications from dispatch events |  |
| `a033c7c` | 2026-08-18 | Sami Oyelaran | chore(email): send from the staging environment | THE SECURITY FINDING: the e-mail provider's API key is committed in appsettings.json (line 15) |
| `c9bbeb7` | 2026-08-19 | Sami Oyelaran | chore(email): log the recipient of accepted e-mails to chase staging bounces | the recipient's e-mail address goes into the Information log (NTF-002, still there) |
| `db1c0c5` | 2026-08-20 | Lucia Brennan | test: wait for the consumer to start before stopping it |  |
| `5d1ed75` | 2026-08-21 | Lucia Brennan | docs: SMS and the delivery-day notifications |  |
| `a65e004` | 2026-08-21 | Lucia Brennan | release 0.2.0 | tag `v0.2.0` |

## Sprint 3 (2026-08-24 – 2026-09-04, release `v0.3.0`)

| Commit | Date | Author | Subject | Note |
|---|---|---|---|---|
| `4fba718` | 2026-08-25 | Lucia Brennan | fix(security): remove the e-mail provider key from configuration (key revoked and replaced) | THE SECURITY FIX: key revoked and replaced, removed from configuration, both keys from the secret store |
| `b7edcf9` | 2026-08-25 | Lucia Brennan | ci: scan the commits of every push and pull request for secrets | security fix: secret scan of every push and pull request in CI |
| `8e8277e` | 2026-08-26 | Lucia Brennan | docs: ADR 0003 credentials only from the secret store; incident note | security fix: ADR 0003 and the incident note |
| `5b72f00` | 2026-08-27 | Sami Oyelaran | feat: delete contact details 30 days after delivery, the notification log after 90 days |  |
| `f29d6c4` | 2026-09-01 | Lucia Brennan | contracts: pin the consumed order and dispatch schemas |  |
| `36cbbb8` | 2026-09-02 | Sami Oyelaran | docs: personal data, secrets and retention in the README and architecture |  |
| `d616696` | 2026-09-03 | Sami Oyelaran | test: the worker's service registration and its refusal to start without a provider key |  |
| `4d48e7b` | 2026-09-04 | Lucia Brennan | release 0.3.0 | tag `v0.3.0` |
