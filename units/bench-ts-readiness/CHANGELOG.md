# Changelog

All notable changes to the parcel-tracking service. The published packages keep their own changelogs in
`packages/*/CHANGELOG.md`.

## [service-1.4.0] - 2026-10-02

### Added

- Signed, expiring share links and the public tracking endpoint behind them.
- A log of every webhook delivery attempt, for merchant support.

### Changed

- The carrier client bounds every attempt with a timeout and retries transient failures with backoff.

## [service-1.3.0] - 2026-09-15

### Added

- Merchant delivery statistics for the dashboard.
- Redirecting a parcel to a pickup point.

## [service-1.2.0] - 2026-08-20

### Added

- Webhook outbox with retries; the worker signs every delivery.

## [service-1.1.0] - 2026-07-08

### Added

- Carrier polling worker with its own health endpoints.

## [service-1.0.0] - 2026-06-01

### Added

- Parcel registration and lookup API, versioned migrations, Kubernetes deployment.
