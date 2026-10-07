# 2. ECB reference rates behind a cache with a background refresh

Date: 2026-09-03 · Status: accepted

## Context

The rates are published once per working day; requests arrive continuously. The feed is a third-party host that can
be slow or unavailable.

## Decision

Fetch the ECB daily XML with retries (exponential back-off, per-attempt timeout) and keep it in a cache with a
time-to-live of two hours, refreshed hourly in the background. Requests read the cache; only an expired cache makes
a request wait for the feed.

## Consequences

A feed outage shorter than the time-to-live is invisible to callers. The origin behind the cache is a keyed service,
so a fixed table can stand in for the feed (Development, tests).
