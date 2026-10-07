# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses [Semantic Versioning](https://semver.org/).

## [1.0.0] - 2026-10-07

### Added

- SKU and bin location catalogue with paged listings.
- Stock receipts and physical counts per SKU and bin, bounded by bin capacity.
- Reservations with a configurable hold time: create, read, release, fulfil; lapsed holds expire automatically.
- JWT bearer authentication (ES256, configured public key) with scope checks on every API route; public health
  endpoint.
- Security response headers (helmet), HTTPS enforcement behind a trusted proxy, size-limited JSON bodies and zod
  validation of every input.
- Unit and HTTP-level test suites with v8 coverage thresholds; CI, CodeQL and Dependabot.
