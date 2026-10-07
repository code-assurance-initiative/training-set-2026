# FleetOps

FleetOps is a fleet-maintenance service for a company that runs its own vans and trucks. The fleet office registers
vehicles, opens work orders and gets them priced and approved, records inspections (a failed inspection opens a
repair work order on its own), and sees which vehicles are due for a service based on the live odometer reported by
each vehicle's telematics unit. A background worker e-mails the workshop when services fall due, outside the depot's
quiet hours.

It is a .NET 10 solution with an ASP.NET Core API, a worker host, EF Core over SQLite and a Mediator-based
Application layer organised in vertical feature slices.

## Build and run

Requirements: the .NET 10 SDK (see `global.json`).

```sh
dotnet build FleetOps.slnx -c Release
dotnet run --project src/FleetOps.Api
dotnet run --project src/FleetOps.Worker
```

Configuration lives in `appsettings.json` of each host: the identity provider (`Authentication`), the database
connection string (`Database`) and the base addresses of the vendor APIs (`Telematics`, `FuelCards`, `TyreVendor`,
`PartsSupplier`, `Geocoding`). The API requires bearer tokens with the scopes `fleet.read`, `fleet.write` and, to
approve work orders above the workshop limit, `fleet.manage`. `/health` is anonymous.

## Testing

```sh
dotnet test FleetOps.slnx -c Release
```

- `tests/FleetOps.Application.Tests` — domain rules, handlers over in-memory SQLite, infrastructure helpers.
- `tests/FleetOps.Api.IntegrationTests` — the real API hosted in memory (`WebApplicationFactory`) with signed test tokens.
- `tests/FleetOps.ArchitectureTests` — ArchUnitNET rules for the decisions in `docs/adr/`.

## Architecture

See [`docs/architecture.md`](docs/architecture.md) for the containers and the project graph, and
[`docs/adr/`](docs/adr/) for the decisions behind them. Dependencies point inward (Domain, Application,
Infrastructure, hosts); the hosts are the composition roots.

## Benchmark

This repository is also a unit of the scanner benchmark in
[code-assurance-initiative/scanner-benchmark](https://github.com/code-assurance-initiative/scanner-benchmark). It
contains deliberate architecture defects; [`benchmark/README.md`](benchmark/README.md) lists them.

## License

MIT — see [LICENSE](LICENSE).
