# Quellbrook Notifier

The notifier of Quellbrook Freight's operations platform tells consignees about their deliveries. It is a background
worker: it consumes the platform's events from RabbitMQ, keeps the consignee's name and contact details that the
order service announces with each order, and sends plain-text e-mail through a transactional e-mail provider and SMS
through an SMS gateway.

**Owner:** the Customer Comms team (`#team-comms`). **System:** Quellbrook operations platform (see
`catalog-info.yaml`).

## What it sends

| Event consumed | Notification | Channel |
|---|---|---|
| `orders.order-placed.v1` | "Your delivery … is booked" | e-mail (when the order has an address) |
| `dispatch.consignment-out-for-delivery.v1` | "… arrives today" | e-mail and SMS (when the order has a phone number) |
| `dispatch.consignment-delivered.v1` | "… has been delivered" | e-mail |

Every message is processed once per message id (inbox), and every notification is sent at most once per order, kind
and channel (the notification log is checked before sending) — see ADR 0002.

## Personal data and secrets

The notifier holds consignees' names and contact details; what it keeps, why and for how long is in
[docs/privacy.md](docs/privacy.md) — contact details are deleted 30 days after the delivery. Credentials (database,
broker, both providers) come only from the secret store (ADR 0003); configuration files hold empty values, and the
worker refuses to start without them.

## Build and run

Requires the .NET 10 SDK (`global.json`), a PostgreSQL 16 database, a RabbitMQ broker and an e-mail provider key.

```sh
dotnet tool restore
dotnet build Quellbrook.Notifier.slnx
export ConnectionStrings__Notifier="Host=localhost;Database=notifier;Username=notifier"
dotnet ef database update --project src/Quellbrook.Notifier
dotnet user-secrets set EmailProvider:ApiKey "<your sandbox key>" --project src/Quellbrook.Notifier
dotnet run --project src/Quellbrook.Notifier
```

## Configuration

| Setting | Meaning |
|---|---|
| `ConnectionStrings__Notifier` | PostgreSQL connection string (a Kubernetes Secret in production) |
| `RabbitMq__Uri`, `RabbitMq__UserName`, `RabbitMq__Password`, `RabbitMq__Exchange` | the broker; credentials from a Secret |
| `EmailProvider__ApiKey`, `EmailProvider__FromAddress`, `EmailProvider__BaseAddress` | the e-mail provider; the key from a Secret or user secrets |
| `SmsProvider__ApiKey`, `SmsProvider__Sender`, `SmsProvider__BaseAddress` | the SMS gateway; the key from a Secret or user secrets |
| `Retention__RecipientAfterDelivery`, `Retention__NotificationLog`, … | retention periods (docs/privacy.md) |
| `Heartbeat__Path`, `Heartbeat__Interval` | the file the Kubernetes probes read |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | where traces, metrics and logs are exported, when set |

## Testing

```sh
dotnet test Quellbrook.Notifier.slnx
```

`tests/Quellbrook.Notifier.UnitTests` covers the inbox and the notification log over in-memory SQLite, the consumer's
acknowledgement and queue setup, the e-mail and SMS senders against stub HTTP handlers, the templates and masking, the
retention sweeper and the heartbeat. No test touches the network or a real provider.

## Architecture

See [docs/architecture.md](docs/architecture.md) and the decision records in [docs/adr](docs/adr).

## Deployment

A published GitHub release builds the image and the migrations bundle; the manual `deploy.yml` workflow migrates the
database and rolls the worker out to the `quellbrook-notifier` namespace (`deploy/k8s/`).

## Contributing

Open a pull request against `main`. CI must be green and CodeQL must report no new alerts. Never put a credential in a
file of this repository. Record user-visible changes in `CHANGELOG.md`.

## Licence

MIT; see [LICENSE](LICENSE).
