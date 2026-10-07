# Harbour Lane room booking

The room-booking portal of the Harbour Lane Community Centre. Visitors browse the centre's rooms, filter them by
amenity and size, look at photos, a narrated tour and the building map, and check a week calendar of bookings. Signed-in
members book a room, get a booking summary, list and cancel their bookings, download a booking as a calendar file and
choose how they are reminded. Centre staff edit the rooms and the notice shown at the top of every public page.

It is a **Blazor Web App** on .NET 10 with interactive server rendering, plus a small **Razor Pages** staff area. Data
is kept in memory (rooms are seeded on start-up), so it runs as a single instance and loses bookings on restart; see
[ADR 0001](docs/adr/0001-blazor-web-app-with-razor-pages-staff-area.md).

## Build and run

Requires the .NET 10 SDK (pinned in `global.json`).

```bash
dotnet build -c Release
Authentication__Oidc__ClientSecret=<secret from your identity provider> \
  dotnet run --project src/HarbourLane.Bookings.Web
```

Sign-in uses OpenID Connect (authorization code flow with PKCE). The authority and client id are in
`appsettings.json` under `Authentication:Oidc`; the client secret is never committed — supply it through user secrets
or the environment. Members need an `email` claim; staff need the role `facilities-staff`. Room photos and videos are
served from the media host named in the content security policy.

| Area | Who |
|---|---|
| `/`, `/rooms`, `/rooms/{slug}`, `/calendar`, `/terms` | everyone |
| `/book`, `/bookings`, `/settings` | signed-in members |
| `/Admin`, `/Admin/EditRoom/{id}`, `/Admin/Notice` | staff |
| `/health` | everyone (health check) |

## Testing

```bash
dotnet test -c Release
```

- `tests/HarbourLane.Bookings.UnitTests` — xUnit v3 unit tests for validation, booking, the calendar, the calendar
  file and the notice sanitiser; bUnit component tests for the pages and shared components; and a contract test that
  checks every JavaScript function the typed interop wrappers call against the scripts in `wwwroot/js`.
- `tests/HarbourLane.Bookings.IntegrationTests` — the whole application hosted in memory with
  `WebApplicationFactory`: server-rendered pages, security headers, the health endpoint and the staff-area challenge.

## Architecture

- `src/HarbourLane.Bookings.Core` — rooms, bookings, the week calendar, the iCalendar writer, site notice and member
  preferences; in-memory stores behind interfaces.
- `src/HarbourLane.Bookings.Web` — the Blazor components (`Components/`), the Razor Pages staff area (`Pages/`),
  authentication and security headers (`Security/`), typed JavaScript interop wrappers (`Interop/`) and the static
  assets (`wwwroot/`).

See [docs/architecture.md](docs/architecture.md) for the component diagram and [docs/adr](docs/adr) for the decisions.

## Contributing

Open a pull request against `main`. CI builds with warnings as errors, restores in locked mode and runs every test;
CodeQL analyses each pull request. Report security issues as described in [SECURITY.md](SECURITY.md).

## Licence

MIT — see [LICENSE](LICENSE).
