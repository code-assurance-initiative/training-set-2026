# Deployment

Production runs in the `parcel-tracking` namespace of the platform cluster.

## Release

1. Tag `service-vX.Y.Z` on `main`. The release workflow builds the image, pushes it by digest with an SBOM and a
   build-provenance attestation, and creates a GitHub release naming the digest.
2. Run the **Deploy** workflow with that digest. It needs the `production` environment's approval.

## What the deploy workflow does

1. Pins the released digest in `deploy/k8s/api.yaml`, `worker.yaml` and `migrate-job.yaml`.
2. Applies the namespace, the namespace's admission policy (images by digest) and the network policies.
3. Runs the migration Job and waits for it to complete; a failed migration stops the deploy before any pod changes.
4. Applies the Deployments and waits for both rollouts; on failure it rolls both back to the previous revision.

The API rolls one pod at a time with no unavailability (`maxSurge: 1`, `maxUnavailable: 0`, a PodDisruptionBudget of
two). The worker is recreated (one replica, ADR 0002) with a 60 s grace period to finish its round.

## Network

The namespace denies all ingress and egress by default. Allowed: DNS; ingress to the API from the ingress controller;
the API and the worker to the database subnet on 5432; the worker to public HTTPS (carriers and merchant endpoints are
arbitrary public hosts; private, link-local and carrier-grade NAT ranges are excluded); the migration Job to the
database.

## Rolling back

Re-run **Deploy** with the previous release's digest. Migrations are backward compatible with the previous release
(ADR 0001), so a rollback never needs a down-migration.
