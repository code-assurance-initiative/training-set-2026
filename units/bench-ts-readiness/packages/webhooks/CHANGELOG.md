# Changelog

All notable changes to `@parcel-tracking/webhooks`. The package follows [Semantic Versioning](https://semver.org/);
while it is 0.x, a minor version may change the API.

## [0.4.1] - 2026-09-08

### Fixed

- A signature header with several `v1` entries (during secret rotation) is accepted when any of them matches.

## [0.4.0] - 2026-08-25

### Added

- `parseWebhookEvent()` and the `WebhookEvent` types.

## [0.3.0] - 2026-07-14

### Changed

- `verifyWebhookSignature()` returns `{ valid, reason }` instead of a boolean.
