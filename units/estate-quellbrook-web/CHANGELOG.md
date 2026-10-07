# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses [Semantic Versioning](https://semver.org/).

## [0.3.0] - 2026-09-04

### Added

- Searching the orders page by consignee or city, filtering by status.
- Dispatch board: compact or comfortable cards (remembered in the browser), stops filtered by postal code.
- Release images carry a signed build-provenance attestation, verified before deploying.

### Fixed

- nginx no longer drops the server's security headers in the page and asset locations.

## [0.2.0] - 2026-08-21

### Added

- Dispatch board: the routes of a day with their vehicles, drivers, load and stops; starting a route.
- The order page shows the delivery status and can cancel the order with a reason.

## [0.1.0] - 2026-08-07

### Added

- Orders list and order page against the gateway; layout with a skip link and the main navigation.
- nginx image with security headers, Kubernetes manifests behind the authentication proxy, CI, CodeQL, release and
  deploy workflows.
