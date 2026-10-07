# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses [Semantic Versioning](https://semver.org/).

## [1.0.0] - 2026-10-06

### Added

- SKU and bin location catalogue with paged listings.
- Stock receipts and physical counts per SKU and bin, bounded by bin capacity.
- Reservations with a configurable hold time: create, read, release, fulfil; lapsed holds expire automatically.
- JWT bearer authentication with scope-based authorization policies; public health endpoint.
- Security response headers, HTTPS redirection and HSTS.
- Unit and integration test suites; CI, CodeQL and Dependabot.
