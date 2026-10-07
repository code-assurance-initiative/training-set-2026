# ADR 0003: JWT bearer tokens with scope-based policies

- Status: accepted
- Date: 2026-10-06

## Context

Callers are other systems (order management, handheld picking terminals) that obtain access tokens from the
organisation's OpenID Connect issuer. The service must not manage credentials of its own.

## Decision

Authenticate with the ASP.NET Core JWT bearer handler, configured with only the issuer's authority URL and the
expected audience. Signing keys are discovered from the issuer's metadata over HTTPS; no key or secret is configured
anywhere. Configuration is validated at startup, and the service refuses to start without an HTTPS authority.

Authorize with three named policies, each satisfied by one OAuth scope in the token's space-delimited `scope` claim:
`stock.read`, `stock.write` and `reservations.write`. Every controller requires an authenticated caller and every
action names its policy; a fallback policy requires authentication for anything else. Only the health endpoint is
anonymous.

## Consequences

- Rotating signing keys is the issuer's concern and needs no change here.
- Tests issue their own tokens with a key generated per run and inject the matching issuer metadata, so the real
  validation path is exercised without a network call.
