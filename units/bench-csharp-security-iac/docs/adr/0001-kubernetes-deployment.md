# 1. Deploy to the shared Kubernetes cluster

Date: 2026-08-20

## Status

Accepted

## Context

The depot's other services run on the platform team's Kubernetes cluster, which provides an NGINX ingress, a secret
store with the External Secrets operator, sealed-secrets for values that must live in git, and a log sink.

## Decision

Depot Slots ships plain Kubernetes manifests in `deploy/k8s/`, applied by the `deploy.yml` workflow. Images are
referenced by digest. The database is the platform's managed PostgreSQL; its connection string is never committed.

## Consequences

No templating layer: one set of manifests describes production. A second environment would need an overlay tool,
which we will introduce when one is needed.
