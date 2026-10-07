# Parcel rates service

Shipping rates and labels for two parcel carriers, Alder Parcel and Corvid Courier, over a small JSON HTTP API, plus
the `parcel-rates` command-line tool for rate-card import and label printing. Node.js 22, TypeScript, Express 5.

The service quotes a shipment across both carriers (cheapest and fastest marked), creates the shipping label with the
cheapest carrier or the one you name, renders a 4x6 inch ZPL label when the carrier does not, archives it, and e-mails
it to the customer. Carriers push tracking events to a signed webhook.

## Build and run

Requirements: Node.js 22 (see `.nvmrc`) and npm.

```bash
npm ci
npm run build
JWT_ISSUER=https://identity.example/realms/shipping JWT_PUBLIC_KEY="$(cat issuer.pub.pem)" \
ALDER_API_KEY=… CORVID_ACCOUNT=… CORVID_TOKEN=… WEBHOOK_SECRET=… npm start
```

Configuration is read from the environment and validated at start-up (`src/config.ts`): `PORT` (8080), `HOST`
(127.0.0.1), `LOG_LEVEL`, `TRUST_PROXY`, `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_PUBLIC_KEY`, the carrier base URLs and
credentials, `RATE_CARD_URL`, `LABEL_ARCHIVE_DIR`, `LABEL_FROM_ADDRESS`, `LABEL_RETENTION_DAYS`, `MAIL_PICKUP_DIR`,
`WEBHOOK_SECRET` and the daily collection cut-offs `CUTOFF_ALDER` / `CUTOFF_CORVID` (UTC).

| Endpoint                     | Scope          | Purpose                 |
| ---------------------------- | -------------- | ----------------------- |
| `GET /health`                | none           | liveness                |
| `POST /api/quotes`           | `rates.read`   | quote a shipment        |
| `POST /api/labels`           | `labels.write` | create a label          |
| `GET /api/labels/:id`        | `labels.write` | the label's ZPL         |
| `DELETE /api/labels/:id`     | `labels.write` | void a label            |
| `GET /api/statistics/labels` | `labels.write` | label counters          |
| `POST /webhooks/tracking`    | HMAC signature | carrier tracking events |

The CLI: `parcel-rates import <output.json> <folder>...` merges rate-card CSV exports into a tariff file;
`parcel-rates print <label-id> [--printer <name>]` prints an archived label on a CUPS queue.

## Testing

```bash
npm run lint && npm run typecheck && npm test
npm run test:coverage   # with the coverage thresholds CI enforces
```

Unit tests cover the domain, pricing, labels and adapters (against a stubbed `fetch`); HTTP-level tests drive the
Express app with supertest and in-memory carriers; tool tests run the CLI against temporary folders.

## Architecture

Four layers — `domain`, `application`, `infrastructure`, `api` — wired in `src/composition.ts`. See
[docs/architecture.md](docs/architecture.md) and the decision records in [docs/adr](docs/adr).

## License

MIT — see [LICENSE](LICENSE).
