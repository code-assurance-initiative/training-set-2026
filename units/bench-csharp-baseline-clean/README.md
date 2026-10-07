# Warehouse Stock & Reservations API

A small ASP.NET Core web API that tracks what a warehouse holds and where: **SKUs** (the kinds of item stocked),
**bin locations** (aisle-rack-shelf storage places with a capacity), **stock levels** per SKU and bin, and
**reservations** that hold stock for an order for a limited time. A reservation is either fulfilled (the goods are
picked and leave the bin), released (the hold is cancelled), or it expires and its quantity becomes available again.

The service is a reference implementation: it keeps its data in memory (see
[ADR 0002](docs/adr/0002-in-memory-store.md)), so data is lost on restart and it runs as a single instance. It handles
no personal data and calls no other services.

## Build and run

Requires the .NET 10 SDK (pinned in `global.json`).

```bash
dotnet build -c Release
Authentication__Authority=https://identity.example.org/realms/warehouse \
  dotnet run --project src/Warehouse.Stock.Api
```

The API refuses to start without `Authentication:Authority` (an absolute HTTPS URL of an OpenID Connect issuer) and
`Authentication:Audience` (default `warehouse-stock-api`). Signing keys are read from the issuer's metadata, so no key
or secret is ever configured. Callers need an access token carrying the scopes below in its `scope` claim.

| Endpoint | Scope |
|---|---|
| `GET /health` | none (public) |
| `GET /api/skus`, `GET /api/skus/{code}`, `GET /api/bins`, `GET /api/bins/{code}` | `stock.read` |
| `POST /api/skus`, `POST /api/bins` | `stock.write` |
| `GET /api/stock/{sku}` | `stock.read` |
| `POST /api/stock/{sku}/receipts`, `POST /api/stock/{sku}/counts` | `stock.write` |
| `GET /api/reservations/{id}` | `stock.read` |
| `POST /api/reservations`, `POST /api/reservations/{id}/release`, `POST /api/reservations/{id}/fulfilment` | `reservations.write` |

Reservation timings live under `Reservations` in `appsettings.json` (default hold 15 minutes, maximum 24 hours, expiry
sweep every 30 seconds).

## Testing

```bash
dotnet test Warehouse.Stock.slnx -c Release
```

`tests/Warehouse.Stock.UnitTests` exercises the application services and the in-memory stores directly, with a fake
clock. `tests/Warehouse.Stock.IntegrationTests` hosts the real API in memory with `WebApplicationFactory` and drives
every endpoint over HTTPS with signed test tokens; the token signing key is generated per run and never written down.
CI runs both on every push and pull request. To measure line coverage locally, add
`--collect:"XPlat Code Coverage"` (coverlet); CI does not collect coverage because it does not gate on it.

## Architecture

Three projects in a plain layered arrangement — see [docs/architecture.md](docs/architecture.md) and the
[ADRs](docs/adr):

- `Warehouse.Stock.Api` — controllers, request validation, authentication and authorization, security headers.
- `Warehouse.Stock.Application` — the services that hold the business rules, and the store interfaces they need.
- `Warehouse.Stock.Infrastructure` — the in-memory store implementations and the reservation expiry worker.

## Security

See [SECURITY.md](SECURITY.md) for how to report a vulnerability.

## Contributing

Open an issue before starting anything larger than a small fix, so the approach can be agreed first. Pull requests
target `main`, keep to the existing layering (see [docs/adr](docs/adr)), and must pass CI: a warning-free Release
build and the full test suite. Add or update tests with every behaviour change, and record a decision that changes
the architecture as a new ADR. Note user-visible changes in [CHANGELOG.md](CHANGELOG.md).

## Licence

MIT — see [LICENSE](LICENSE).
