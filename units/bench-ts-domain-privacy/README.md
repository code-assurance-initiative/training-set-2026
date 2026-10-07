# Club Membership Service

The system of record of a fitness club: **members** and their consents, the **class schedule** and members' bookings
with **reminders** by e-mail and SMS, and **monthly invoicing** with concessions and late fees. It is a Node.js 22 web
API written in strict TypeScript over PostgreSQL, organised as two bounded contexts — Membership and Billing — each
built from vertical feature slices around its own domain model.

The service holds personal data (names, e-mail addresses, phone numbers, dates of birth). What it keeps, why and for how
long is in [docs/privacy/data-inventory.md](docs/privacy/data-inventory.md); members can export their data and have it
erased through the API.

## Build and run

Requires Node.js 22 (pinned in `.nvmrc` and `engines`) and PostgreSQL 16 or later.

```bash
npm ci
npm run build
npm run migrate   # applies the schema migrations to DATABASE_URL
npm start
```

Configuration comes from environment variables and is validated at start-up; the service refuses to start when a value
is missing or malformed, and never echoes a value back.

| Variable                                                               | Default                                               | Meaning                                                            |
| ---------------------------------------------------------------------- | ----------------------------------------------------- | ------------------------------------------------------------------ |
| `DATABASE_URL`                                                         | — (required)                                          | PostgreSQL connection string                                       |
| `JWT_ISSUER`, `JWT_PUBLIC_KEY`                                         | — (required)                                          | HTTPS issuer URL and its ES256 public key (SPKI, PEM)              |
| `JWT_AUDIENCE`                                                         | `club-membership-service`                             | Required `aud` of every token                                      |
| `FIELD_ENCRYPTION_KEY`                                                 | — (required)                                          | Base64 256-bit key for phone numbers and dates of birth (ADR 0004) |
| `PSEUDONYM_KEY`                                                        | — (required)                                          | Base64 key (≥ 256 bits) for member pseudonyms in logs              |
| `EMAIL_API_URL`, `EMAIL_API_TOKEN`, `EMAIL_SENDER`                     | — (required)                                          | Transactional e-mail provider                                      |
| `SMS_API_URL`, `SMS_API_TOKEN`, `SMS_SENDER`                           | — (required)                                          | SMS provider                                                       |
| `REMINDER_LEAD_MINUTES`                                                | `1440`                                                | How long before a class its reminder goes out                      |
| `MEMBER_RETENTION_MONTHS`                                              | `24`                                                  | Months after the paid period ends before a member is erased        |
| `CURRENCY`, `MONTHLY_FEE_MINOR`, `LATE_FEE_MINOR`, `PAYMENT_TERM_DAYS` | `EUR`, `3900`, `500`, `14`                            | Invoicing                                                          |
| `JOB_INTERVAL_SECONDS`                                                 | `60`                                                  | Interval of the background jobs                                    |
| `NODE_ENV`, `HOST`, `PORT`, `TRUST_PROXY`, `LOG_LEVEL`                 | `production`, `127.0.0.1`, `8080`, `loopback`, `info` | Process                                                            |

Keys and tokens come from the deployment's secret store. TLS terminates at the reverse proxy; in production the API
refuses requests that did not arrive over HTTPS.

| Endpoint                                                                                                                                                                      | Scope              |
| ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------ |
| `GET /health/live`, `GET /health/ready`                                                                                                                                       | none (public)      |
| `POST /api/members`, `PUT /api/members/:id/contact-details`, `PUT /api/members/:id/consents/:purpose`, `POST /api/members/:id/cancellation`, `POST /api/classes/:id/bookings` | `members.write`    |
| `GET /api/members/:id/bookings`                                                                                                                                               | `members.read`     |
| `POST /api/classes`                                                                                                                                                           | `classes.write`    |
| `GET /api/members/:id/personal-data`, `POST /api/members/:id/erasure`                                                                                                         | `privacy.requests` |
| `POST /api/billing/invoice-runs`, `POST /api/billing/late-fees`, `POST /api/billing/invoices/:id/payment`                                                                     | `billing.write`    |
| `GET /api/billing/invoices/overdue`                                                                                                                                           | `billing.read`     |

Errors are answered as RFC 9457 problem documents (`application/problem+json`).

## Testing

```bash
npm test               # all tests
npm run test:coverage  # with v8 coverage and the thresholds CI enforces
npm run lint && npm run typecheck && npm run format:check
```

`tests/unit` covers the value objects, aggregates, encryption, configuration, gateways, outbox and jobs.
`tests/integration` drives the real Express application over HTTP with supertest and signed test tokens, against the
real migrations applied to pg-mem (an in-memory PostgreSQL emulation); the e-mail and SMS providers are replaced by a
recording fake. No test touches a network. CI runs formatting, lint, type-check, build and the tests with coverage
thresholds on every push and pull request, and weekly.

## Architecture

See [docs/architecture.md](docs/architecture.md) and the [ADRs](docs/adr):

- `src/membership`, `src/billing` — the bounded contexts: `domain/` (aggregates, value objects, repository ports),
  `db/` (knex stores, unit of work), `features/` (one folder per slice), `routes.ts`.
- `src/shared-kernel` — base classes, ids, `Money`, `Result`.
- `src/platform` — database and migrations, HTTP plumbing, logging, field encryption, pseudonyms, audit log, outbox.

## Security

See [SECURITY.md](SECURITY.md) for how to report a vulnerability.

## Contributing

Open an issue before starting anything larger than a small fix. Pull requests target `main`, keep to the slice and
context boundaries of ADR 0001, and must pass CI. Add or update tests with every behaviour change; a new field of
personal data needs a row in the data inventory first. Note user-visible changes in [CHANGELOG.md](CHANGELOG.md).

## Licence

MIT — see [LICENSE](LICENSE).
