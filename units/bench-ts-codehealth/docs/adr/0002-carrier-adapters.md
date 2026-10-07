# ADR 0002: One adapter per carrier, mail through a pickup directory

- Status: accepted
- Date: 2026-10-07

## Context

Each carrier has its own API, authentication, error format and label format. Customers want their label by e-mail,
and the service should hold no SMTP credentials.

## Decision

Every carrier is an adapter behind the `CarrierGateway` port, registered in `CarrierRegistry` with its aliases. Every
carrier call carries an `AbortSignal` with a timeout. Label e-mails are written as `.eml` files to the pickup
directory of the local mail transfer agent (`PickupDirectoryMailer`), which delivers them.

## Consequences

- A new carrier is a new adapter and a registry entry.
- Mail delivery problems show up in the MTA's queue, not in the service.
