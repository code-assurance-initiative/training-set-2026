# parcel-tracking

Parcel tracking for merchants. Merchants register the parcels they hand to a carrier; a worker polls the carriers'
tracking APIs, records each parcel's events and status, and tells the merchant about every status change with a
signed webhook. Merchants read parcels through a small JSON API (or the published client package), hand recipients
signed, expiring tracking links, and verify webhooks with the published webhooks package.

This is an npm-workspaces monorepo on Node.js 22 and strict TypeScript:

| Workspace                  | What it is                                                                                                                                                                        |
| -------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `apps/tracking-service`    | the service: the API (`src/api`, `src/http`), the worker (`src/worker`), the PostgreSQL schema and its versioned migrations (`src/db`), and cucumber specifications (`features/`) |
| `packages/tracking-client` | `@parcel-tracking/client`, the published client for the merchant API                                                                                                              |
| `packages/webhooks`        | `@parcel-tracking/webhooks`, the published webhook verification and parsing package                                                                                               |

## Build and run

```sh
npm ci
npm run build            # every workspace
npm run typecheck && npm run lint && npm run format:check
```

The service is configured through environment variables (validated at start-up; see `apps/tracking-service/src/config.ts`):

- API: `DATABASE_URL`, `PUBLIC_BASE_URL`, `JWT_PUBLIC_KEY` (PEM, ES256), `JWT_ISSUER`, `JWT_AUDIENCE`,
  `LINK_SIGNING_KEY`, optional `PORT` (8080) and `LOG_LEVEL`.
- Worker: `DATABASE_URL`, `CARRIER_API_URL`, `CARRIER_API_KEY`, `WEBHOOK_SIGNING_SECRET`, optional
  `POLL_INTERVAL_MS`, `POLL_BATCH_SIZE`, `CARRIER_TIMEOUT_MS`, `HEALTH_PORT` (8081).
- Migrations: `DATABASE_URL`.

```sh
npm run migrate --workspace apps/tracking-service       # apply the versioned migrations
npm run start:api --workspace apps/tracking-service
npm run start:worker --workspace apps/tracking-service
```

One container image (`Dockerfile`) runs all three; the Kubernetes manifests in `deploy/k8s` choose the entry point.
Deployment is described in [docs/operations/deployment.md](docs/operations/deployment.md).

## Testing

```sh
npm test                   # vitest: unit and integration tests of every workspace
npm run test:coverage      # the same with v8 coverage and the CI thresholds
npm run test:bdd           # cucumber: the tracking specifications
npm run test:bdd:webhooks  # cucumber: the webhook delivery specifications
```

Integration tests and specifications run against `pg-mem`, an in-memory PostgreSQL emulation, with the real
migrations applied; HTTP tests use supertest against the Express app; carrier and merchant endpoints are faked in
process or served on the loopback interface. Nothing reaches the network.

## Architecture

See [docs/architecture.md](docs/architecture.md) and the decision records in [docs/adr](docs/adr). In short: the API
and the worker share the parcel store (knex over PostgreSQL); the worker writes status changes and a webhook outbox
row in one transaction and delivers the outbox with retries; the published packages depend on nothing in the service.

## Contributing

Open a pull request against `main`. CI must pass: formatting, lint, type-check, build, tests with coverage thresholds,
both cucumber profiles and the package contents check. Report security issues as described in [SECURITY.md](SECURITY.md).

## Licence

MIT, see [LICENSE](LICENSE).
