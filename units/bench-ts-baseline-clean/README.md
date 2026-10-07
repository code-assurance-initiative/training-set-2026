# Warehouse Stock & Reservations API

A small Node.js web API, written in TypeScript, that tracks what a warehouse holds and where: **SKUs** (the kinds
of item stocked), **bin locations** (aisle-rack-shelf storage places with a capacity), **stock levels** per SKU and
bin, and **reservations** that hold stock for an order for a limited time. A reservation is either fulfilled (the
goods are picked and leave the bin), released (the hold is cancelled), or it expires and its quantity becomes
available again.

The service is a reference implementation: it keeps its data in memory (see
[ADR 0002](docs/adr/0002-in-memory-store.md)), so data is lost on restart and it runs as a single instance. It handles
no personal data and calls no other services.

## Build and run

Requires Node.js 22 (pinned in `.nvmrc` and `engines`).

```bash
npm ci
npm run build
JWT_ISSUER=https://identity.example.org/realms/warehouse \
JWT_PUBLIC_KEY="$(cat issuer-es256-public.pem)" \
  npm start
```

Configuration comes from environment variables and is validated at start-up; the service refuses to start when a
value is missing or malformed.

| Variable                           | Default               | Meaning                                                                                |
| ---------------------------------- | --------------------- | -------------------------------------------------------------------------------------- |
| `JWT_ISSUER`                       | — (required)          | HTTPS URL of the token issuer; tokens must carry it as `iss`                           |
| `JWT_PUBLIC_KEY`                   | — (required)          | The issuer's ES256 **public** key (SPKI, PEM). It verifies tokens and cannot sign them |
| `JWT_AUDIENCE`                     | `warehouse-stock-api` | Required `aud` of every token                                                          |
| `NODE_ENV`                         | `production`          | `production` refuses plain-HTTP API requests; `development` and `test` do not          |
| `HOST`, `PORT`                     | `127.0.0.1`, `8080`   | Listen address                                                                         |
| `TRUST_PROXY`                      | `loopback`            | Proxies whose `X-Forwarded-*` headers are trusted (Express `trust proxy` syntax)       |
| `LOG_LEVEL`                        | `info`                | pino log level                                                                         |
| `RESERVATION_DEFAULT_HOLD_MINUTES` | `15`                  | Hold time when a request names none                                                    |
| `RESERVATION_MAX_HOLD_MINUTES`     | `1440`                | Longest hold a request may ask for                                                     |
| `RESERVATION_SWEEP_SECONDS`        | `30`                  | Interval of the expiry sweep                                                           |

TLS terminates at the reverse proxy in front of the service. When the issuer rotates its signing key, update
`JWT_PUBLIC_KEY` and restart the service. Callers need an access token carrying the scopes below
in its space-separated `scope` claim.

| Endpoint                                                                                                | Scope                |
| ------------------------------------------------------------------------------------------------------- | -------------------- |
| `GET /health`                                                                                           | none (public)        |
| `GET /api/skus`, `GET /api/skus/:code`, `GET /api/bins`, `GET /api/bins/:code`                          | `stock.read`         |
| `POST /api/skus`, `POST /api/bins`                                                                      | `stock.write`        |
| `GET /api/stock/:sku`                                                                                   | `stock.read`         |
| `POST /api/stock/:sku/receipts`, `POST /api/stock/:sku/counts`                                          | `stock.write`        |
| `GET /api/reservations/:id`                                                                             | `stock.read`         |
| `POST /api/reservations`, `POST /api/reservations/:id/release`, `POST /api/reservations/:id/fulfilment` | `reservations.write` |

Errors are answered as RFC 9457 problem documents (`application/problem+json`).

## Testing

```bash
npm test               # all tests
npm run test:coverage  # with v8 coverage and the thresholds CI enforces
npm run lint && npm run typecheck && npm run format:check
```

`tests/unit` exercises the application services, the in-memory stores, configuration, token verification and the
expiry job directly, with a fake clock. `tests/integration` drives every route of the real Express application over
HTTP with supertest and signed test tokens; the ES256 key pair is generated per run and never written down. CI runs
formatting, lint, type-check, build and the tests with coverage thresholds on every push and pull request, and weekly.

## Architecture

Three layers under `src/`, with dependencies pointing inward — see [docs/architecture.md](docs/architecture.md) and
the [ADRs](docs/adr):

- `src/http` — Express routes, request validation (zod), authentication and scope checks, problem responses.
- `src/application` — the services that hold the business rules, and the store interfaces they need.
- `src/infrastructure` — the in-memory store implementations and the reservation expiry job.

`src/app.ts` assembles the Express application, `src/composition.ts` wires services to stores, and
`src/lifecycle.ts` starts the server from the environment and stops it on `SIGTERM` or `SIGINT`; `src/main.ts` is the
entry point. An ESLint rule keeps the application layer free of HTTP and infrastructure imports.

## Security

See [SECURITY.md](SECURITY.md) for how to report a vulnerability.

## Contributing

Open an issue before starting anything larger than a small fix, so the approach can be agreed first. Pull requests
target `main`, keep to the existing layering (see [docs/adr](docs/adr)), and must pass CI: formatting, lint,
type-check, build and the full test suite with its coverage thresholds. Add or update tests with every behaviour
change, and record a decision that changes the architecture as a new ADR. Note user-visible changes in
[CHANGELOG.md](CHANGELOG.md).

## Licence

MIT — see [LICENSE](LICENSE).
