# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses [Semantic Versioning](https://semver.org/).

## [1.4.0] - 2026-10-07

### Added

- Tracking webhooks with HMAC signatures, and a per-carrier request rate limit.
- `shipping-rates watch` and `follow` commands for rate-card drops.

## [1.3.0] - 2026-09-14

### Added

- Corvid Courier as a second carrier; quotes now compare carriers.
- Label void and e-mail re-send endpoints.

## [1.2.0] - 2026-08-03

### Added

- Dangerous-goods handling, customs declarations on cross-border labels.

### Changed

- Rate cards are downloaded from the carriers and cached instead of being shipped with the service.

## [1.1.0] - 2026-06-22

### Added

- The `shipping-rates` command-line tool: rate-card import and label printing.

## [1.0.0] - 2026-05-11

### Added

- Quotes and labels with Alder Parcel; JWT bearer authentication with scope policies; CI, CodeQL and Dependabot.
