# ADR 0003: Resilience for upstream HTTP calls

- Status: accepted
- Date: 2026-08-17

## Context

The carrier API is rate limited and has short outages; a hung request must not stall polling, and an outage must not be
hammered.

## Decision

Every upstream HTTP client is a typed client from `IHttpClientFactory` with the standard resilience pipeline
(`Microsoft.Extensions.Http.Resilience`: rate limiter, total timeout, retry with jittered back-off, circuit breaker,
per-attempt timeout), tuned from configuration. The readiness probe's carrier client is the exception: one short
attempt, no retry, so the probe reports the current state. The published client library does not choose a policy;
it returns the `IHttpClientBuilder` so the consuming application does.

## Consequences

- Retries make non-idempotent calls risky; upstream calls that change state must be idempotent on the server side
  (webhook events carry an event id for de-duplication).
- A failing upstream costs latency before it costs errors: a request can take up to the total timeout (30 s by
  default) while attempts time out and back off. The worker absorbs this; API callers see it on the on-demand
  refresh and label endpoints.
- Retries multiply load on an upstream that is already struggling; the circuit breaker and the rate limiter exist to
  cap that, and the retry count stays low (3).
- The policies are configuration that must be tuned per upstream and revisited when an upstream's latency profile
  changes; their values live in `CarrierApi:*` and are reviewed with each change to the carrier contract.
