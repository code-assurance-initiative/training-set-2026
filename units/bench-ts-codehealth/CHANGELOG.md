# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses [Semantic Versioning](https://semver.org/).

## [1.0.0] - 2026-10-07

### Added

- Rate quotes across Alder Parcel and Corvid Courier from downloaded rate cards, with shared and carrier-specific
  surcharges, fuel, insurance and per-piece handling.
- Shipping labels: carrier label or our own 4x6 inch ZPL rendering, label archive with an index, customer e-mail via
  the mail pickup directory, voiding, reprinting, print batches, customs declarations and retention purge.
- Signed carrier tracking webhooks.
- `parcel-rates` command-line tool: rate-card CSV import and label printing over CUPS.
- JWT bearer authentication (ES256, configured public key) with scope checks; security headers; HTTPS enforcement.
- Unit, HTTP-level and tool test suites with v8 coverage thresholds; CI, CodeQL and Dependabot.
