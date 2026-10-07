# Depot Dispatch

A small Node.js service, written in TypeScript, that plans a parcel depot's delivery day. A dispatch desk posts the
parcels for tomorrow; the service matches each recipient's city to the depot's service area (forgiving the spellings
shippers type), finds each address on the map, splits the parcels into vehicle routes, promises each recipient a
two-hour delivery window and closes the run at the depot's cut-off. It quotes the trunk route with the linehaul
carrier, downloads the carrier's label PDFs and turns them into images the depot's thermal printers take, and keeps
each parcel's status current from the carrier's signed status feed.

The service is installed on each depot's own server and licensed to the depot operator (see
[ADR 0001](docs/adr/0001-depot-service-on-customer-premises.md)). It keeps a day's runs in memory and is re-planned
after a restart. A small command-line tool, [`tools/manifest-export`](tools/manifest-export), turns an exported run
into the CSV manifest the label printers read; it is its own npm package and runs on the depot PCs.

## Build and run

Requires Node.js 22 (pinned in `.nvmrc` and `engines`).

```bash
npm ci
npm run build
TERMINAL_TOKEN_PUBLIC_KEY="$(cat depot-identity-rs256-public.pem)" \
LINEHAUL_BASE_URL=https://partners.linehaul.example \
LINEHAUL_API_KEY=… GEOCODING_API_KEY=… CARRIER_STATUS_PUBLIC_KEY=… \
  npm start
```

Configuration comes from environment variables and is validated at start-up; the service refuses to start when a
value is missing or malformed, and never echoes a value in the error.

| Variable                                           | Default                            | Meaning                                                              |
| -------------------------------------------------- | ---------------------------------- | -------------------------------------------------------------------- |
| `TERMINAL_TOKEN_PUBLIC_KEY`                        | — (required)                       | The depot identity service's RS256 **public** key (SPKI, PEM)        |
| `TERMINAL_TOKEN_ISSUER`, `TERMINAL_TOKEN_AUDIENCE` | `depot-identity`, `depot-dispatch` | Required `iss` and `aud` of every terminal token                     |
| `LINEHAUL_BASE_URL`, `LINEHAUL_API_KEY`            | — (required)                       | The linehaul carrier's partner API (HTTPS only) and this depot's key |
| `GEOCODING_API_KEY`                                | — (required)                       | Key for the geocoding provider                                       |
| `CARRIER_STATUS_PUBLIC_KEY`                        | — (required)                       | The carrier's Ed25519 key for the status feed, base64 (32 bytes)     |
| `LABEL_PRINTER_DPI`                                | `203`                              | Resolution of the depot's label printers                             |
| `HOST`, `PORT`                                     | `127.0.0.1`, `8080`                | Listen address; TLS terminates at the depot server's reverse proxy   |
| `SHUTDOWN_GRACE_SECONDS`                           | `10`                               | How long a shutdown waits for in-flight requests (label downloads)   |
| `LOG_LEVEL`                                        | `info`                             | pino log level                                                       |

## API

Every `/api` route needs a terminal bearer token (RS256, issued by the depot identity service) with a `depot` claim
and the scope named below; a terminal only ever sees its own depot's runs.

| Method and path                 | Scope            | What it does                                                      |
| ------------------------------- | ---------------- | ----------------------------------------------------------------- |
| `GET /health`                   | none             | Liveness                                                          |
| `POST /api/runs`                | `dispatch:write` | Plan a run: cities matched, addresses located, routes and windows |
| `GET /api/runs/{id}`            | `dispatch:read`  | The run                                                           |
| `GET /api/runs/{id}/export`     | `dispatch:read`  | The run as a JSON download, the input of `manifest-export`        |
| `POST /api/runs/{id}/quote`     | `dispatch:write` | A linehaul quote for the run's parcels and weight                 |
| `GET /api/runs/{id}/labels.zip` | `labels:print`   | Every label of the run, rasterised, a folder per vehicle          |
| `POST /webhooks/carrier-status` | signature        | The carrier's status feed (XML), verified with its Ed25519 key    |

Errors are RFC 9457 problem documents.

## Testing

```bash
npm test                 # unit and HTTP-level tests (vitest)
npm run test:coverage    # the same, with the coverage thresholds CI enforces
npm run lint && npm run typecheck && npm run format:check
(cd tools/manifest-export && npm ci && npm test)
```

The tests run offline: the carrier and the geocoding provider are replaced by in-process fakes or intercepted HTTP,
and every key the tests use is generated for the test run.

## Architecture

An Express 5 application (`src/app.ts`) composed in `src/composition.ts`: HTTP routes in `src/http`, terminal token
verification in `src/auth`, the dispatch planning in `src/dispatch` with its helpers in `src/addresses` and
`src/scheduling`, the carrier integration in `src/carriers`, label handling in `src/labels` and the webhook signature
in `src/webhooks`. See [docs/architecture.md](docs/architecture.md) and the decision records in
[docs/adr](docs/adr).

## Licence

MIT, see [LICENSE](LICENSE). Third-party packages are governed by the licence policy in
[ADR 0002](docs/adr/0002-third-party-licence-policy.md).
