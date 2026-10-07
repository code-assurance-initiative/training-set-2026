# Releases and deployment

## Release

Pushing a tag `vX.Y.Z` runs `.github/workflows/release.yml`: it builds the API and worker images (pushed to GHCR,
with provenance and SBOM attestations), the EF Core migrations bundle and the client NuGet package, and attaches the
bundle, the package and the image digests to a GitHub release.

## Deploy

`.github/workflows/deploy.yml` is started by hand with the release tag. It runs in the `production` environment,
which requires a reviewer's approval. It then:

1. runs the release's migrations bundle against the production database (migrations are additive: expand now,
   contract in a later release);
2. sets both Deployments to the released image digests;
3. waits for each rollout (`kubectl rollout status`, five minutes); if pods never become ready it runs
   `kubectl rollout undo` and fails the run.

The API rolls with `maxUnavailable: 0` behind readiness probes and a PodDisruptionBudget, so a bad build never takes
serving capacity away. The worker is recreated (one replica).

## Rollback

A rollout that fails its readiness gate is undone automatically. To roll back a release that passed, run the deploy
workflow with the previous tag: because migrations are additive, the previous release runs against the newer schema.
