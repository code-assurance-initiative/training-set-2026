# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses [Semantic Versioning](https://semver.org/).

## [1.4.0] - 2026-10-07

### Added

- Carrier status webhook: Ed25519-signed XML feed updates the status of planned stops.
- Labels download bundles the rasterised labels of a run per vehicle, with an index for the print station.

### Changed

- Run identifiers come from the runtime's `crypto.randomUUID`.
- `jws` is pinned through `overrides` to the release that fixes GHSA-869p-cjfg-cm3x.

## [1.3.0] - 2026-06-15

### Added

- Linehaul quote for a run's parcel count and weight.
- Label rasterisation at the configured printer resolution.

## [1.2.0] - 2026-02-02

### Added

- Geocoding of delivery addresses; stops carry their position when the provider knows it.
- Cut-off per depot: runs close a configured lead time before the first departure.

## [1.0.0] - 2025-09-01

### Added

- Dispatch runs: city matching against the depot service area, routes per vehicle, two-hour delivery windows.
- Terminal bearer tokens (RS256) with depot and scope checks; public health endpoint.
- Run export for the `manifest-export` command-line tool.
