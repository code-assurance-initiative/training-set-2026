# ADR 0002: Hand out short-lived signed download tokens instead of object-store URLs

- Status: accepted
- Date: 2026-09-17

## Context

Callers often pass a finished export on to a batch job or a partner process that has no identity-provider
credentials. Pre-signed object-store URLs would expose the encrypted object, not the document, and would put a
credential in a URL that ends up in proxy logs.

## Decision

`POST /exports/{id}/download-token` issues a JWT signed with HMAC-SHA256 that names exactly one export and lives ten
minutes (five in production). The token travels in the `X-Download-Token` header, never in the URL. A dedicated
`DownloadToken` authentication scheme validates issuer, audience, lifetime and signature; the endpoint additionally
checks that the token's `export_id` matches the route.

## Consequences

- A leaked token opens one export for minutes, not the bucket.
- The HMAC key is shared by all instances; rotating it invalidates outstanding tokens, which is acceptable at this
  lifetime.
- Every download is audited and returns the manifest digest and signature so the receiver can verify the document.
