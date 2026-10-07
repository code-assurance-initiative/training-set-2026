# 3. Outbound HTTP: allowlisted partners, guarded webhooks

Date: 2026-09-28

## Status

Accepted

## Context

The service makes three kinds of outbound request: it pulls the partner records feed, it imports documents that a
user points at by URL, and it calls webhooks that tenants register to hear when a scheduled report is ready.

## Decision

- The partner feed may only reach hosts listed in `Feeds:AllowedHosts`, over HTTPS.
- Webhook callbacks are resolved before connecting and every resolved address must be public
  (`CallbackAddressPolicy`); the check runs in the socket connect callback so a DNS answer cannot change between the
  check and the connection.
- Imports by URL use the named `imports` client with a size cap and a 30-second timeout.
- The partner-feed and CRM clients add the standard resilience handler (retries with back-off, circuit breaker).

## Consequences

- Each outbound feature has its own named `HttpClient`, so limits and handlers are configured per feature.
