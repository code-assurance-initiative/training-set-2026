# ADR 0002: A separate worker process for polling and notifications, on Kubernetes

- Status: accepted
- Date: 2026-08-03

## Context

Polling carriers and delivering webhooks are periodic, long-running jobs whose load does not follow API traffic. Run
inside the API they would run once per API replica.

## Decision

Run them in `ParcelTracking.Worker`, a separate generic host deployed as one Kubernetes replica (recreated, not rolled,
so two pollers never overlap). Both processes are deployed to Kubernetes only; images carry no Docker HEALTHCHECK
because Kubernetes ignores it, and probes are declared in the manifests instead.

## Consequences

- The worker exposes no HTTP endpoint; its liveness is a heartbeat file written by the health-check publisher.
- Polling throughput is bounded by one replica; batching (`Polling:BatchSize`) is the scaling knob.
