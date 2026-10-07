# Shipping Rates & Labels

A .NET 10 service that quotes parcel shipping rates across carriers and creates shipping labels, plus a small
command-line tool for the shipping desk.

- **Quotes** — `POST /api/quotes` asks every eligible carrier (currently Alder Parcel and Corvid Courier) which
  services it can run for a shipment and prices each one from our published tariff: base fee plus a price per
  chargeable kilogram (the greater of actual and volumetric weight) per destination zone, a service-level factor,
  and the carriers' surcharges (fuel, oversize, heavy, remote area, customs, dangerous goods).
- **Labels** — `POST /api/labels` books the shipment with the chosen carrier, renders a 4x6" ZPL label, archives
  it, records it in the label index and e-mails it to the customer. Labels can be downloaded, voided and re-sent.
  Labels and their index entries are deleted after `Labels:RetentionDays` (default 90) by an hourly job.
- **Tracking webhooks** — carriers post tracking events to `POST /api/webhooks/tracking`, authenticated by an
  HMAC signature.
- **CLI** (`shipping-rates`) — imports the carriers' CSV rate cards, waits for rate-card drops, and prints labels on
  CUPS label printers.

## Build and run

Requires the .NET 10 SDK (pinned in `global.json`).

```bash
dotnet build Shipping.Rates.slnx -c Release
Authentication__Authority=https://identity.example.org/realms/shipping \
Webhooks__Secret=<base64 key shared with the carriers> \
Carriers__Alder__ApiKey=<key> Carriers__Corvid__ApiKey=<key> \
  dotnet run --project src/Shipping.Rates.Api
```

The API refuses to start without an HTTPS `Authentication:Authority` and a `Webhooks:Secret`. Carrier API keys and
the webhook secret come from the environment and are never committed. Label e-mails are sent only when both
`Labels:SmtpHost` and `Labels:FromAddress` are configured. Calls to the carriers use the standard resilience
pipeline (timeouts and circuit breaker; retries only for safe methods, so a label is never booked twice). Callers need an access token with the
`rates.read` scope for quotes and `labels.write` for labels; `/health` is public.

The CLI:

```bash
dotnet run --project src/Shipping.Rates.Tools -- import ./rate-cards
dotnet run --project src/Shipping.Rates.Tools -- print ./labels/SR260310000001.zpl zebra-desk
```

Country zones are generated from `data/country-zones.csv` by `python3 tools/generate-zones.py`; do not edit
`CountryZones.g.cs` by hand.

## Testing

```bash
dotnet test Shipping.Rates.slnx -c Release
```

`tests/Shipping.Rates.UnitTests` covers pricing, the carrier adapters (against canned HTTP responses), label
rendering and storage, and the CLI's rate-card parsing. `tests/Shipping.Rates.IntegrationTests` hosts the API in
memory with `WebApplicationFactory`, signs its own test tokens and replaces the carriers with canned responses.

## Architecture

See [docs/architecture.md](docs/architecture.md) and the [ADRs](docs/adr).

- `Shipping.Rates.Core` — domain model, pricing, carrier adapters, label rendering and storage.
- `Shipping.Rates.Api` — minimal-API endpoints, authentication and authorization, hosting.
- `Shipping.Rates.Tools` — the `shipping-rates` command-line tool.

## Contributing

Open a pull request against `main`. CI builds and runs both test suites on every pull request; please add tests for
new pricing rules and carrier behaviour, and keep carrier-specific code inside its adapter.

## Security

See [SECURITY.md](SECURITY.md).
