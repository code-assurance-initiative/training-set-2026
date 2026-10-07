# 3. Outbound HTTP requests

Date: 2026-09-28

## Status

Accepted

## Context

The service makes four kinds of outbound request: it reads partner transfer feeds, previews documents a user wants to
import by URL, looks up recipients in the CRM, and sends mail through the HTTP mail relay.

## Decision

- Partner feeds may only reach hosts listed in `PARTNER_FEED_HOSTS`, over HTTPS; redirects are refused.
- Import previews send a `HEAD` request with a five-second timeout and return only the status, content type, size
  and modification date, never the body.
- CRM and mail-relay calls go to configured HTTPS base URLs with timeouts; message bodies travel in request bodies.

## Consequences

- Adding a partner is a configuration change.
- A slow remote host costs a request at most its timeout.
