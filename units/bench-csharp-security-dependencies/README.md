# Invoicing — rendering service, worker and archive exporter

Invoicing turns invoices from a customer's ERP and billing system into documents their own customers receive: a PDF
for people and a signed UBL 2.1 e-invoice for e-invoicing gateways. It is shipped to customers as an installable
bundle (container images plus a command-line tool) and runs inside their network.

> **Benchmark repository.** This code is a unit of a public scanner benchmark. Some dependencies are deliberately
> pinned to versions with published advisories and one tool targets an end-of-life runtime; see
> [`benchmark/README.md`](benchmark/README.md). Nothing here is deployed.

## What is in the box

| Component | Path | What it does |
|---|---|---|
| Contracts | `src/Invoicing.Contracts` | The invoice document model (`netstandard2.0` + `net10.0`, so the ERP connector on .NET Framework 4.8 can use it) |
| Rendering | `src/Invoicing.Rendering` | PDF layout with PdfSharpCore; UBL 2.1 writer and XML-DSig signer |
| API | `src/Invoicing.Api` | `POST /invoices/pdf`, `POST /invoices/ubl`, `POST /erp/invoices`, `GET /health` |
| Worker | `src/Invoicing.Worker` | Drains the render queue in the billing MySQL database on a cron schedule and e-mails each invoice |
| Archive exporter | `tools/Invoicing.ArchiveExporter` | Packs a year of archived invoices into one ZIP with an `index.csv` |

## Build and run

Requirements: the .NET 10 SDK (`global.json`). The archive exporter still targets .NET 6, so running it needs the
.NET 6 runtime; building it does not.

```bash
dotnet build Invoicing.slnx -c Release
dotnet run --project src/Invoicing.Api        # needs Authentication__Authority, Branding__BaseAddress, Signing__CertificatePath
dotnet run --project src/Invoicing.Worker     # needs ConnectionStrings__Billing and Smtp__* settings
dotnet run --project tools/Invoicing.ArchiveExporter -- --source /archive/2025 --output invoices-2025.zip
```

Configuration comes from `appsettings.json` and environment variables. No credential is stored in the repository:
the signing-certificate password, the database connection string and the SMTP password are supplied by the
installer through the environment.

## Testing

```bash
dotnet test Invoicing.slnx -c Release
```

`tests/Invoicing.UnitTests` covers the document model, both renderers and the signature, the ERP payload parser, the
worker's batch processing and schedule (with a fake clock) and the archive exporter. `tests/Invoicing.Api.IntegrationTests`
hosts the API in memory with `WebApplicationFactory`, signs test tokens with a key generated per run, and exercises
every endpoint, its authorization and the security headers. The tests need no network and no database.

## Architecture

Three deployable parts share one rendering library; see [`docs/architecture.md`](docs/architecture.md) and the
decision records in [`docs/adr`](docs/adr). Dependencies are pinned centrally in `Directory.Packages.props` and
locked per project in `packages.lock.json`; CI restores in locked mode. Third-party licences are governed by
[ADR 0003](docs/adr/0003-third-party-licence-policy.md).

## Contributing

Open a pull request against `main`; CI builds, tests and reports the dependency state on every pull request.
Security issues: see [`SECURITY.md`](SECURITY.md).
