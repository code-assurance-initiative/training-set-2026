# Fx.Conversion

A small .NET library and HTTP API for **currency conversion and rounding**: ISO 4217 currencies with their minor
units, a `Money` type that refuses to mix currencies, six rounding modes, allocation of an amount into parts without
losing a minor unit, the European Central Bank's daily reference rates (fetched, parsed, cached and refreshed in the
background), cross-rate conversion, and firm quotes with a tiered fee and a 90-second validity.

The library (`Fx.Conversion.Core`) has no I/O and can be used on its own, for example by till software that needs
cash rounding. The API (`Fx.Conversion.Api`) exposes rates, conversions, quotes and allocations to internal callers
holding an OAuth access token with the matching scope.

## Build and run

Requirements: the .NET 10 SDK (`global.json` pins the feature band).

```bash
dotnet build -c Release
dotnet run --project src/Fx.Conversion.Api
```

Configuration (`appsettings.json`): `Authentication:Authority` and `Authentication:Audience` name the token issuer
and this API; `Ecb:FeedUri`, `Ecb:RefreshInterval` and `Ecb:CacheTimeToLive` control the rate feed. In the
Development environment the API serves a fixed rate table (from `tests/Fx.Conversion.TestSupport`) instead of
calling the ECB, so it runs offline.

| Endpoint | Scope | Purpose |
|---|---|---|
| `GET /api/rates` | `fx.rates.read` | The latest reference rates (per euro) |
| `POST /api/conversions` | `fx.convert` | Convert an amount, with a chosen rounding mode |
| `POST /api/allocations` | `fx.convert` | Split an amount by weights |
| `POST /api/quotes`, `GET /api/quotes/{id}` | `fx.quote` | Issue and read back a firm quote |
| `GET /health` | none | Liveness |

## Testing

```bash
dotnet test -c Release --collect:"XPlat Code Coverage"
```

- `tests/Fx.Conversion.UnitTests` — xUnit v3 unit tests of the library and the ECB adapter (time is a
  `FakeTimeProvider`; HTTP runs on stub handlers and a checked-in copy of the feed), plus CsCheck property tests.
- `tests/Fx.Conversion.IntegrationTests` — the real API hosted in memory with `WebApplicationFactory`, tokens signed by
  an in-memory test issuer.
- `tests/Fx.Conversion.Specs` — Gherkin scenarios for conversion and allocation, run by Reqnroll on the xUnit runner.
- `tests/Fx.Conversion.TestSupport` — shared fakes, rate tables and assertions.

CI runs the whole suite on every pull request and uploads the Cobertura coverage reports as a build artifact.

## Architecture

See [docs/architecture.md](docs/architecture.md) and the decision records in [docs/adr](docs/adr). In short:
`Fx.Conversion.Api` → `Fx.Conversion.Rates` (ECB adapter, cache refresh) → `Fx.Conversion.Core` (money, rounding,
allocation, rates, quotes).

## Contributing

Issues and pull requests are welcome. Keep `dotnet build` free of warnings (they are errors), add tests with every
change, and commit the updated `packages.lock.json` files when dependencies change.

## Licence

MIT — see [LICENSE](LICENSE).
