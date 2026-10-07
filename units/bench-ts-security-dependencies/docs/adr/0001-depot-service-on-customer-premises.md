# ADR 0001 — One installation per depot, on the operator's server

- Status: accepted
- Date: 2025-06-10

## Context

Depots are run by independent operators under contract. Each has its own server room, its own network and a
dispatch day that must continue when the internet connection is down for an hour: the run is planned the evening
before, labels are printed before the vehicles leave, and the status feed can catch up later.

## Decision

Depot Dispatch is delivered to each operator as an installable bundle (the built service with its production
`node_modules`, and the `manifest-export` tool for the depot PCs) and runs on the operator's server, licensed to the
operator under our proprietary licence. One process serves one depot's terminals; runs live in memory for the
service day. Terminals authenticate with tokens from the depot identity service, verified with its public key.

## Consequences

- Every third-party package in the bundle is redistributed by us to the operator; ADR 0002 governs which licences
  that allows.
- A restart loses the day's runs; the desk re-plans from its parcel list (minutes, not hours).
- Upgrades ship as a new bundle; an operator may run a release for months, so dependency updates have to land in
  the bundle we ship, not only in this repository.
