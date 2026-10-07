# ADR 0003: JWT bearer tokens verified against a configured public key, with scopes per route

- Status: accepted
- Date: 2026-10-07

## Context

Every operation except the health check changes or reveals warehouse state and must be restricted. Callers already
obtain access tokens from the organisation's identity provider. The service must hold no secret, and it should not
depend on reaching the identity provider at run time.

## Decision

Requests to `/api` carry a bearer access token. The `jose` library verifies it: the ES256 signature against the
issuer's **public** key from `JWT_PUBLIC_KEY`, the `iss` and `aud` claims against configuration, and expiry with
30 seconds of clock tolerance. Only ES256 is accepted, so a token cannot choose a weaker or symmetric algorithm.
Because the key is public and configured, the service needs no secret and makes no network call to verify a token.

Authorisation uses three scopes from the token's `scope` claim: `stock.read`, `stock.write` and
`reservations.write`. Every route under `/api` names the scope it needs with `requireScope`; a request without a
valid token is answered 401, one without the scope 403. `/health` is the only anonymous route.

## Consequences

- Rotating the issuer's signing key means updating `JWT_PUBLIC_KEY` and restarting; the README says so.
- Tests generate their own key pair per run and sign tokens with it; no key material is committed.
- A route added without `requireScope` is still authenticated; the HTTP tests check the required scope of every
  route, so a new route without its row in that table is noticed in review.
