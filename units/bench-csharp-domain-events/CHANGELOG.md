# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses [Semantic Versioning](https://semver.org/).

## [1.1.0] - 2026-10-07

### Added

- Equipment storage locations (branch and shelf) and relocation (`RelocateEquipmentCommand`).
- Billing's event store keeps account events in their serialised form (type name, schema version, JSON).

### Changed

- `LateFeeCharged` records the daily fee (schema version 2); stored version-1 events are upcast when read.
- The loan of a held deposit is named `LoanId` in `DepositHeld`.

## [1.0.0] - 2026-10-07

### Added

- Lending context: members, equipment catalogue with units and maintenance records, reservations and loans on EF Core
  (SQLite), with a transactional outbox and an outbox dispatcher.
- Billing context: event-sourced member accounts (charges, deposits, late fees, payments), an in-memory event store
  with an inbox, and the account balance projection.
- In-process message bus, command dispatcher and the worker host, which applies the Lending migrations at start-up.
