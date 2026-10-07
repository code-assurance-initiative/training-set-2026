# 2. Carrier polling and webhook delivery run in a separate worker

Date: 2026-06-20

## Status

Accepted

## Context

Carriers offer no push interface we can rely on, so parcels are polled. Polling and webhook delivery are slow,
outbound and retried; the API must stay fast and scale with merchant traffic.

## Decision

A worker process, built from the same image, runs the carrier poller and the webhook dispatcher as two loops. It runs
as a single replica: polling is idempotent per event, but two pollers would double the carrier load and two
dispatchers would race on the outbox. The worker exposes `/healthz` and `/readyz` on its own port for the kubelet.

## Consequences

- API latency does not depend on carriers or merchants.
- A worker restart delays notifications by at most one poll interval; nothing is lost, because the outbox is in the
  database.
- Scaling out the worker needs leasing (for example `select … for update skip locked`) first.
