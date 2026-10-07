# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses [semantic versioning](https://semver.org/).

## [1.1.0] — 2026-10-07

### Added
- Firm quotes (`POST /api/quotes`, `GET /api/quotes/{id}`) with a tiered fee and a 90-second validity.
- Allocation endpoint (`POST /api/allocations`).
- Cash rounding for currencies whose smallest coin is larger than the minor unit (CHF, DKK, SEK, NOK, …).

### Changed
- Request amounts are validated with culture-invariant limits.

## [1.0.0] — 2026-09-14

### Added
- ISO 4217 currency catalog, `Money`, rounding modes and allocation.
- ECB daily reference rates: parser, HTTP source with retries, cache and background refresh.
- Conversion and rates endpoints with JWT scopes and security headers.
