# 4. Minimal API endpoints, no MVC controllers

Date: 2026-02-02
Status: Accepted

## Context

The API is a thin HTTP layer over the application use cases: validate the request, call one handler, map the result.
MVC controllers bring a second hosting model (controller discovery, filters, model-binding conventions) that we would
have to learn, configure and test alongside the endpoint routing we already use for health checks.

## Decision

HTTP endpoints are Minimal API route groups. Each area has one static class under `src/ClinicScheduling.Api/Endpoints/`
with a `Map...Endpoints` extension method that the host calls at start-up. Authorization is attached per endpoint or
per group with `RequireAuthorization` and the named scope policies. We do not use MVC controllers: no `ControllerBase`
types, no `AddControllers` and no `MapControllers`.

## Consequences

- One way to write an endpoint, one place to find each route.
- Request validation is explicit in each endpoint (the request records validate themselves) instead of relying on MVC
  model-state filters.
- If we ever need a feature only MVC offers, that is a new decision recorded here first.
