# Changelog

All notable changes to `@parcel-tracking/client`. The package follows [Semantic Versioning](https://semver.org/).

## [2.3.0] - 2026-09-29

### Added

- `deliveryStats()` for the merchant dashboard's delivery figures.

### Changed

- `Parcel.registeredAt` is now `Parcel.createdAt`, the name the API uses.

### Removed

- `listEvents()`: a parcel's tracking events are part of `getParcel()`.

## [2.2.0] - 2026-08-18

### Added

- `redirect()` to send a parcel to a pickup point and hold it there.

## [2.1.0] - 2026-07-02

### Added

- `createShareLink()` for signed, expiring recipient tracking links.

## [2.0.0] - 2026-05-20

### Changed

- ESM only, with an `exports` map; requires Node.js 20.19 or later.
- Every method takes an optional `AbortSignal`; the client no longer retries on its own.
