# ClinicScheduling

ClinicScheduling is the appointment-scheduling API behind a chain of physiotherapy clinics. Practitioners publish
their weekly opening hours; the front desk and the patient portal search for open slots, book, move and cancel
appointments; treatment series are booked in one go; patients get SMS reminders; and the patient portal keeps a feed
and a calendar subscription of each patient's appointments.

## Features

- **Slot search** per practitioner: opening hours and breaks, public holidays (Denmark, Germany, Norway, Sweden),
  buffers between appointments, minimum notice, a same-day cut-off and a daily cap. Video (telehealth) slots for
  practitioners who offer them.
- **Booking, rescheduling and cancellation** with the clinic's cancellation policy: free until 24 hours before, a fee
  for late cancellations and no-shows, and a deposit after two strikes within six months.
- **Treatment series**: a weekly recurrence (`WEEKLY;INTERVAL=1;COUNT=8;BYDAY=MO,TH`) booked as one request; sessions
  that fall on a public holiday move to the next free weekday.
- **Reminders** by SMS two days before and on the day of the appointment, in the clinic's language, never during the
  night.
- **Waitlist back-fill**: when an appointment is cancelled, the first matching patient on the practitioner's waitlist
  is offered the freed slot automatically.
- **Patient portal** feed (JSON) and calendar subscription (iCalendar).

## Getting started

Prerequisites: the .NET 10 SDK.

```bash
dotnet build ClinicScheduling.slnx
dotnet run --project src/ClinicScheduling.Api
```

The API validates bearer tokens from the identity provider configured under `Authentication` (an `https` authority
and the audience `clinic-scheduling-api`). In development the authority is a realm of a local identity provider
(`appsettings.Development.json`). Every endpoint except `/health` needs a token with the matching scope:
`appointments.read`, `appointments.write`, `schedules.write`, `reports.read` or `portal.read`.

## Configuration

| Section | Setting | Default | Meaning |
|---|---|---|---|
| `Authentication` | `Authority`, `Audience` | - | Where tokens come from and whom they must be for. |
| `Reminders` | `DayBeforeLead`, `SameDayLead` | 2 days, 2 hours | How long before the appointment each reminder goes out. |
| `Reminders` | `QuietHoursStart`, `QuietHoursEnd` | 21:00, 08:00 | No SMS in this window; a reminder that would fall in it is sent at 20:30 the evening before. |
| `ReminderDispatch` | `PollInterval`, `BatchSize` | 30 s, 50 | How often the worker looks for due reminders, and how many it sends per poll. |
| `ReminderDispatch` | `ClinicPhone` | - | The number patients are asked to call to cancel or move an appointment. |

The patient-portal feed takes the patient's time zone as a query parameter (`?timeZone=Europe/Copenhagen`); times in
the feed are written with that zone's offset.

## Testing

```bash
dotnet test ClinicScheduling.slnx
```

`tests/ClinicScheduling.UnitTests` covers the domain rules (slot search, cancellation policy, recurrence), the
reminder planner and composer, the iCalendar writer, the portal feed and the booking handlers.
`tests/ClinicScheduling.IntegrationTests` hosts the API in memory with `WebApplicationFactory`, signs its own test
tokens and drives the endpoints over HTTPS.

## Architecture

A layered solution (see [`docs/architecture.md`](docs/architecture.md) and the decision records in
[`docs/adr/`](docs/adr/)):

| Project | Role |
|---|---|
| `ClinicScheduling.Domain` | Appointments, slot search, cancellation policy, recurrence. No dependencies. |
| `ClinicScheduling.Application` | Use cases (book, cancel, reschedule, find slots, book a series) and their ports. |
| `ClinicScheduling.Infrastructure` | In-memory stores, the public-holiday table, reminders, the patient-portal feed and the calendar writer. |
| `ClinicScheduling.Waitlist` | The waitlist and the back-fill of cancelled slots. |
| `ClinicScheduling.Api` | The ASP.NET Core host: endpoints, authentication, security headers. |

The public-holiday table (`src/ClinicScheduling.Infrastructure/Holidays/PublicHolidays.g.cs`) is generated: edit
`tools/holidays/holidays.csv` and run `tools/holidays/generate-holidays.sh`.

## Waitlist

Patients who could not get a suitable time can join a practitioner's waitlist with
`POST /api/waitlist` (patient, practitioner, earliest and latest acceptable date).
`GET /api/waitlist/{practitionerId}` lists the queue in order. When an appointment is cancelled, the
`ClinicScheduling.Waitlist` module offers the freed slot to the first patient whose window matches; the offer is held
for 30 minutes before it moves on to the next patient.

## API overview

| Method and path | Scope | Purpose |
|---|---|---|
| `PUT /api/practitioners/{id}/schedule` | `schedules.write` | Set a practitioner's weekly hours, skills and daily cap. |
| `GET /api/practitioners/{id}/slots?from&to&durationMinutes` | `appointments.read` | Open slots. |
| `POST /api/appointments` | `appointments.write` | Book. |
| `GET /api/appointments/{id}` | `appointments.read` | Read one appointment. |
| `POST /api/appointments/{id}/reschedule` | `appointments.write` | Move to another open slot (at most three times). |
| `POST /api/appointments/{id}/cancellation` | `appointments.write` | Cancel; returns the fee, if any. |
| `POST /api/appointments/{id}/no-show` | `appointments.write` | Record a no-show. |
| `POST /api/series` | `appointments.write` | Book a treatment series. |
| `GET /api/patients/{id}/feed` | `portal.read` | The patient-portal feed. |
| `GET /api/patients/{id}/calendar.ics` | `portal.read` | The patient's calendar subscription. |
| `GET /api/reports/no-shows?from&to` | `reports.read` | No-shows and late cancellations per practitioner. |

## Persistence

Appointments, schedules, strikes and planned reminders are held in process memory and are lost on restart. A database
is planned; until then the service runs as a single instance.

## Contributing

Work happens on short-lived branches with a pull request to `main`. CI builds, checks for vulnerable packages and runs
every test; a pull request is merged when CI is green and one other team member has approved it. Decisions that change
the structure of the code get an ADR in `docs/adr/` in the same pull request.

## License

MIT - see [LICENSE](LICENSE).
