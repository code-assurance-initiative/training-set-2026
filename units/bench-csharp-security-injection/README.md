# ReportDesk

ReportDesk is the report builder and document search API behind a records archive. It lets archive staff search
documents by title, keep report definitions and schedules, export reports to PDF, DOCX or ODT, download attachments,
import documents (by URL, or as bundles exported by the desktop client), read and preview document metadata, look up
owners and teams in the directory, share a document behind a password, and mail scheduled reports to subscribers.
The archive's web front end is its only client; it inserts the HTML fragments this API renders (quick-search results,
document cards, metadata previews) into its own pages.

## Build and run

Requirements: the .NET 10 SDK (see `global.json`), and for a full run a PostgreSQL database with the archive schema,
LibreOffice and ImageMagick on the host, an LDAPS directory and an SMTP relay.

```bash
dotnet build ReportDesk.slnx -c Release
dotnet run --project src/ReportDesk.Api
```

Configuration lives in `src/ReportDesk.Api/appsettings.json`. Secrets (the database password, the pseudonymisation
key `Delivery:PseudonymKey`) are supplied by the platform through environment variables, never committed. The service
refuses to start when required options are missing.

## Testing

```bash
dotnet test ReportDesk.slnx -c Release
```

- `tests/ReportDesk.UnitTests` — path containment, address policies, the feed allowlist, LDAP escaping, XML readers,
  renderers, serializers and the bundle format.
- `tests/ReportDesk.IntegrationTests` — the whole API in memory through `WebApplicationFactory`, with signed test
  tokens and in-memory fakes for every store, the directory, the mail relay and the CRM. No test touches a network.

## Architecture

One ASP.NET Core project, organised by feature (`Search`, `Documents`, `Reports`, `SavedSearches`, `Conversion`,
`Attachments`, `Templates`, `Importing`, `Feeds`, `Webhooks`, `Metadata`, `Previews`, `People`, `Delivery`, `Shares`),
with thin controllers under `Controllers`. Search and document reads use Dapper; report definitions use EF Core
(ADR 0001). Conversions run LibreOffice and ImageMagick out of process (ADR 0002). Outbound requests use one named
`HttpClient` per feature (ADR 0003). See [`docs/architecture.md`](docs/architecture.md).

Every endpoint except the health check and the share landing form requires a bearer token with the scope its policy
names (`documents.read`, `documents.write`, `reports.read`, `reports.write`, `reportdesk.admin`).

## Contributing

Open an issue before a larger change. Pull requests need a green CI run (build, tests, CodeQL). Report security
issues privately as described in [`SECURITY.md`](SECURITY.md).

## Licence

MIT — see [`LICENSE`](LICENSE).
