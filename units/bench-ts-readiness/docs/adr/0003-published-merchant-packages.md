# 3. Two published packages for merchants

Date: 2026-07-01

## Status

Accepted

## Context

Merchants integrate in two places: their order system calls the API, and their webhook endpoint receives status
changes. Many webhook receivers are small functions that should not depend on an HTTP client.

## Decision

Publish `@parcel-tracking/client` (the API client) and `@parcel-tracking/webhooks` (signature verification and event
parsing) separately, both ESM, both with npm provenance from a tag-triggered workflow, both versioned with semantic
versioning and a changelog per package. Neither package imports the service; the client leaves timeouts and retries
to the caller.

## Consequences

- Each package has its own release cadence and tag prefix (`client-v*`, `webhooks-v*`).
- The webhook signature format is a public contract: the worker's signer and the package's verifier are tested
  against each other.
- Breaking changes to either package need a major version.
