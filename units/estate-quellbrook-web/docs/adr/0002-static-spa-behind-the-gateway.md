# 2. A static single-page application behind the gateway and the sign-in proxy

- Status: Accepted
- Date: 2026-07-28
- Deciders: Teodor Vasko, Mara Ellingsworth

## Context

Operators need a fast console that works with the platform's sign-in. Handling OAuth tokens in browser code means
storing them somewhere scripts can read, and every XSS becomes a token theft.

## Decision

The console is a React application built by Vite into static files and served by nginx on the same host as the
gateway (`/` and `/api`). The ingress's authentication proxy signs operators in and keeps the session in an
HttpOnly cookie; the browser sends it with every same-origin request, and the gateway receives the operator's token
from the proxy. The console calls only `/api` on its own origin with `credentials: 'same-origin'` and stores no
token. nginx sends a strict Content-Security-Policy (`'self'` only).

## Consequences

- Positive: no token in browser storage; one origin, so no CORS in production; the static image is small and needs
  no egress.
- Negative: an expired session shows as 401 from the gateway and the console can only ask for a reload; the console
  cannot be run against the gateway from another origin without the development proxy.
