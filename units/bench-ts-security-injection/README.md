# Archive Search API

The document search, export and report-delivery API behind a records archive. Archive staff use it to search
documents by title, read document cards, download attachments and report templates, export documents to PDF or other
office formats (optionally behind a download password), preview and import metadata (XML or JSON) and retention
schedules, read the transfer feeds partner archives publish, build report previews with computed columns and uploaded
layouts, keep saved searches, share a document behind a password, and mail reports to subscribers. The archive's web
front end is its main client and inserts the HTML fragments this API renders (the quick-search widget and document
cards) into its own pages; `web/` holds the small upload page the scanning stations use.

## Build and run

Requires Node.js 22 (pinned in `.nvmrc` and `engines`). A full run also needs PostgreSQL with the archive schema,
MongoDB, LibreOffice and ImageMagick on the host, the CRM and the HTTP mail relay.

```bash
npm ci
npm run build
JWT_ISSUER=https://identity.example.org/realms/records \
JWT_PUBLIC_KEY="$(cat issuer-es256-public.pem)" \
DATABASE_URL=postgres://archive@db.internal/archive \
MONGODB_URL=mongodb://mongo.internal/archive \
CRM_BASE_URL=https://crm.internal/api MAIL_RELAY_URL=https://relay.internal \
PUBLIC_BASE_URL=https://archive.example.org PSEUDONYM_KEY="$(cat pseudonym.key)" \
  npm start
```

Configuration comes from environment variables and is validated at start-up (`src/config.ts`); the service refuses to
start when a value is missing or malformed and never echoes values back. Secrets (`PSEUDONYM_KEY`, database
credentials) are supplied by the platform and never committed. Other settings: `STORAGE_ROOT` (originals,
attachments, templates, exports and the export audit log live below it), `PARTNER_FEED_HOSTS` (comma-separated),
`SOFFICE_PATH`, `MAGICK_PATH`, `CONVERSION_TIMEOUT_SECONDS`, `HOST`, `PORT`, `TRUST_PROXY`, `LOG_LEVEL`, `NODE_ENV`.

Every `/api` route requires an ES256 bearer token with the route's scope (`documents.read`, `documents.write`,
`exports.write`, `reports.read`, `reports.write`, `subscriptions.admin`). Public: `GET /health`, `POST /unsubscribe`
(token from the mail) and the share-link pages under `/shares/:token`. Errors are RFC 9457 problem documents.

## Testing

```bash
npm test               # all tests
npm run test:coverage  # with v8 coverage and the thresholds CI enforces
npm run lint && npm run typecheck && npm run format:check
```

`tests/unit` exercises the stores against a recording SQL client, the XML and YAML readers, the converters (against
stand-in scripts for LibreOffice and ImageMagick), the mail relay and CRM clients (against a fake `fetch`) and the
report, filter and share logic. `tests/integration` drives every route of the real Express application over HTTP with
supertest, signed test tokens and in-memory fakes for the databases. `web/tests` runs the upload page's modules under
happy-dom. No test touches a network or a database.

## Architecture

One Express 5 service organised by feature (`src/search`, `src/documents`, `src/exports`, `src/imports`,
`src/reports`, `src/saved-searches`, `src/shares`, `src/subscriptions`, `src/preferences`, …), each holding its
routes, its logic and the store it needs. `src/app.ts` assembles the application, `src/composition.ts` connects the
stores and wires the features, `src/lifecycle.ts` starts and stops the server. See
[docs/architecture.md](docs/architecture.md) and the [ADRs](docs/adr).

## Security

See [SECURITY.md](SECURITY.md) for how to report a vulnerability.

## Contributing

Open an issue before starting anything larger than a small fix. Pull requests target `main` and must pass CI
(formatting, lint, type-check, build and the tests with their coverage thresholds). Add tests with every behaviour
change, record architectural decisions as ADRs and note user-visible changes in [CHANGELOG.md](CHANGELOG.md).

## Licence

MIT — see [LICENSE](LICENSE).
