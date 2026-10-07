# Changelog

All notable changes to this service are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses semantic versioning.

## [1.3.0] - 2026-10-06

### Changed
- Staging reads the exports database password from `EXPORTS_DB_PASSWORD`.

### Added
- Staging configuration.
- Release and migration scripts, Compose stack for local development, container image.

## [1.2.0] - 2026-09-28

### Added
- Exports are announced to the fulfilment partner and to the notification relay.
- Audit entries for export creation and download.

## [1.1.0] - 2026-09-21

### Added
- Signed manifests (RSA-PSS over the document's SHA-256).
- Download tokens and the `/downloads/{id}` endpoint.

## [1.0.0] - 2026-09-14

### Added
- Inventory exports rendered to CSV, encrypted with AES-256-GCM and stored in object storage.
