# Document Export Service

The document export service turns a warehouse's current inventory into a downloadable CSV document. A caller
(another internal application, authenticated by the identity provider) requests an export; the service renders it,
signs a manifest of its SHA-256 digest with the service's RSA key, encrypts the document with AES-256-GCM and stores
it in S3-compatible object storage. It then announces the export to the fulfilment partner's API and posts a message
to the team's notification relay. Downloads use a short-lived, HMAC-signed download token so that the document can
be handed to a process that has no identity-provider credentials of its own.

> This repository is also a scanner benchmark: see [`benchmark/README.md`](benchmark/README.md). Every credential
> and key in it is a generated fake that authenticates nothing.

## Build and run

Requirements: the .NET 10 SDK (see `global.json`); Docker for the local stack.

```bash
dotnet build DocumentExport.slnx -c Release
cp .env.example .env            # then fill in the development values
docker compose up --build       # API on http://127.0.0.1:8080, Postgres on 127.0.0.1:5432
```

Configuration is layered: `appsettings.json`, `appsettings.<Environment>.json`, `partner-api.json`, an optional
untracked `appsettings.Local.json` (start from `src/DocumentExport.Api/appsettings.example.json`), then environment
variables and the command line. Schema migrations live in `db/migrations` and are applied by `deploy/migrate.sh`.
The contracts package is published with `deploy/publish.ps1 -Version <x.y.z>`.

## Testing

```bash
dotnet test --solution DocumentExport.slnx -c Release
```

`tests/DocumentExport.UnitTests` covers encryption, download tokens, manifest signing and CSV rendering.
`tests/DocumentExport.IntegrationTests` hosts the API with `WebApplicationFactory`, replacing the database, object
store and outbound HTTP clients with in-memory doubles, and drives the full create → token → download flow.

## Architecture

One ASP.NET Core minimal-API project (`src/DocumentExport.Api`) and a strong-named contracts library
(`src/DocumentExport.Contracts`) that consumers reference. See [`docs/architecture.md`](docs/architecture.md) for the
component diagram and [`docs/adr`](docs/adr) for the decisions behind object storage and download tokens.
Operational notes are in [`docs/operations.md`](docs/operations.md).

| Endpoint | Policy | Purpose |
|---|---|---|
| `POST /exports` | `exports.write` | Render, encrypt, store and announce an export |
| `GET /exports/{id}` | `exports.read` | Export state |
| `POST /exports/{id}/download-token` | `exports.read` | Issue a download token |
| `GET /downloads/{id}` | download token | Stream the decrypted CSV with its manifest headers |
| `GET /health` | anonymous | Liveness |

## License

MIT — see [`LICENSE`](LICENSE).
