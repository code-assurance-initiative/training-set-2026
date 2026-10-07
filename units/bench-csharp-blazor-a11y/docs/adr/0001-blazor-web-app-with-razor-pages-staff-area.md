# 1. Blazor Web App for the public site, Razor Pages for the staff area

Date: 2026-10-01

## Status

Accepted

## Context

The public site needs interactive filtering, a calendar and a booking form without a separate JavaScript front end.
The staff area is a handful of forms used by two or three people a day.

## Decision

The public site is a Blazor Web App with interactive server rendering, prerendered so pages arrive as HTML. The staff
area is Razor Pages in the same host: plain server-rendered forms with model binding and antiforgery, authorised by
folder. Data stays in memory behind interfaces until the centre chooses a database.

## Consequences

- One deployable, one sign-in, one set of security headers.
- Two markup dialects (`.razor`, `.cshtml`) and two layouts.
- Bookings are lost on restart and the portal runs as a single instance until a persistent store replaces the
  in-memory one.
