# Rentals — tool library core

The core of a community **tool library**: members borrow drills, ladders and hedge trimmers for a few days, pay a
deposit while an item is out, and pay late fees and damage charges when it comes back late or broken. The code is a
.NET 10 modular monolith with two bounded contexts that talk only through integration events.

- **Lending** owns members, the equipment catalogue (each equipment type with its physical, serial-numbered units and
  their maintenance records), reservations and loans. It is persisted with EF Core and publishes integration events
  through a transactional outbox.
- **Billing** owns member accounts. An account is event-sourced: charges, deposits, late fees and payments are events
  in an append-only stream, and a projection keeps a balance read model up to date. Billing consumes Lending's events
  and de-duplicates redeliveries with an inbox.

Messaging runs on an in-process bus behind small `IMessageBus` / `IIntegrationEventHandler<T>` / `ICommandHandler<T>`
abstractions, so the whole system runs and is tested without a broker. See [docs/architecture.md](docs/architecture.md)
and the decision records in [docs/adr](docs/adr).

## Build and run

Requirements: the .NET 10 SDK (`global.json` pins the feature band).

```bash
dotnet build Rentals.slnx -c Release
dotnet run --project src/Rentals.Worker
```

The worker migrates the Lending database (a local SQLite file, `lending.db`) and hosts the outbox dispatcher and the
projection runner. Schema changes are EF Core migrations:
`dotnet ef migrations add <Name> --project src/Rentals.Lending.Infrastructure --output-dir Persistence/Migrations`. Commands
reach the handlers through `ICommandDispatcher`; the HTTP front end that sends them is a separate deployable and not
part of this repository. Configuration lives in `src/Rentals.Worker/appsettings.json` (`Outbox`, `Billing`,
`ConnectionStrings:Lending`).

## Testing

```bash
dotnet test Rentals.slnx -c Release
```

- `tests/Rentals.Lending.Tests` — aggregates and value objects, and the Lending handlers against an in-memory SQLite
  database (outbox contents, the dispatcher, reservations).
- `tests/Rentals.Billing.Tests` — the event-sourced account (decisions and replay), the consumers (including
  redelivery through the inbox) and the balance projection, with a stubbed catalogue service.
- `tests/Rentals.IntegrationTests` — both contexts in one process: a loan returned late or damaged reaches the member's
  account through the outbox and the bus.

Time is a `FakeTimeProvider` in every test; nothing touches the network.

## Architecture

| Project | Role |
|---|---|
| `Rentals.SharedKernel` | `Entity`, `AggregateRoot`, `IDomainEvent`, `Money`, repository and unit-of-work ports |
| `Rentals.Messaging` | integration-event, command and query contracts; the in-process bus and command dispatcher |
| `Rentals.Contracts` | the integration events Lending publishes |
| `Rentals.Lending.Domain` / `.Application` / `.Infrastructure` | the Lending context: model, handlers, EF Core persistence and outbox |
| `Rentals.Billing.Domain` / `.Application` / `.Infrastructure` | the Billing context: event-sourced account, consumers and projection, in-memory event store |
| `Rentals.Worker` | the host: outbox dispatcher and projection runner |

## Contributing

Open a pull request against `main`; CI builds and runs every test with locked package versions. Security issues:
see [SECURITY.md](SECURITY.md).

## Licence

MIT — see [LICENSE](LICENSE).
