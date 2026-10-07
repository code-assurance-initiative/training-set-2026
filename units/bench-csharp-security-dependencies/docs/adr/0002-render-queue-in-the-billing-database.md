# ADR 0002 — The render queue is a table in the billing database

- Status: accepted
- Date: 2026-02-09

## Context

The worker must render and e-mail invoices the billing system produces. Customers run the bundle on their own
infrastructure; asking them to operate a message broker for one queue has been the most common installation problem.

## Decision

The billing system inserts rows into `render_jobs` (status `queued`, the invoice as JSON, the recipient). A worker
claims a batch with one `UPDATE … SET claimed_by = <instance id> … LIMIT n` and then reads back only its own rows, so
several worker instances never take the same job. Each job ends `sent` or `failed` with a reason.

## Consequences

- No extra infrastructure: the billing database already exists and is backed up.
- Polling on a cron schedule adds up to one schedule interval of latency, which billing accepts.
- The worker depends on a MySQL client library.
