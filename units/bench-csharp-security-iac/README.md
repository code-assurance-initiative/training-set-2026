# Depot Slots

Depot Slots books the loading docks of a distribution depot. Carriers (or the planners who work with them) reserve a
dock for a time window through a small HTTP API; the service refuses overlapping windows on the same dock, keeps the
bookings in PostgreSQL, and a separate reminder worker posts "carrier X arrives at dock Y" to the yard team's chat
channel shortly before each slot starts. It runs on Kubernetes behind an NGINX ingress, with a log forwarder on every
node, and is built and released by GitHub Actions.

## Build and run

Requirements: the .NET 10 SDK (pinned in `global.json`) and, for the full stack, Docker.

```bash
dotnet build Depot.Slots.slnx -c Release
dotnet run --project src/Depot.Slots.Api          # Development: in-memory store, local identity provider
```

The local stack runs PostgreSQL, the API and the reminder worker together:

```bash
cp .env.template .env      # choose a database password, add a chat bot token for a test workspace
docker compose up --build
```

## Testing

```bash
dotnet test Depot.Slots.slnx -c Release
```

Unit tests cover the booking rules, the in-memory store, the reminder pass and the chat client (against a stub HTTP
handler). Integration tests host the real API in memory with `WebApplicationFactory`, issue tokens from an in-process
test issuer and exercise every endpoint, the authorization policies and the security headers.

## API

| Method | Path | Scope |
|---|---|---|
| `GET` | `/api/docks/{dockCode}/bookings?date=YYYY-MM-DD` | `slots.read` |
| `POST` | `/api/bookings` | `slots.write` |
| `DELETE` | `/api/bookings/{id}` | `slots.write` |
| `GET` | `/health/live`, `/health/ready` | anonymous |

## Architecture

Four projects under `src/`: `Depot.Slots.Core` (booking rules, reminder pass, store interface),
`Depot.Slots.Infrastructure` (PostgreSQL store and schema), `Depot.Slots.Api` (minimal API, JWT bearer
authentication) and `Depot.Slots.Reminders` (background worker with health endpoints). Deployment manifests are in
`deploy/k8s/`; container images are built from the Dockerfile beside each runnable project. See
[docs/architecture.md](docs/architecture.md) and the decision records in [docs/adr](docs/adr).
