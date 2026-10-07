# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses [Semantic Versioning](https://semver.org/).

## [2.4.0] — 2026-10-07

### Added
- Signed UBL 2.1 e-invoices (`POST /invoices/ubl`), XML-DSig RSA-SHA256 with the signing certificate in `KeyInfo`.
- Tenant logos on PDF invoices, fetched from the branding service with retries.

### Changed
- The worker's queue schedule is a cron expression (`Worker:Schedule`) instead of a fixed interval.

## [2.3.0] — 2026-06-15

### Added
- The worker e-mails rendered invoices to the buyer's billing address over STARTTLS.
- `ARCHIVE COPY` stamp on every PDF the archive exporter packs.

## [2.2.0] — 2026-03-02

### Added
- ERP webhook (`POST /erp/invoices`) accepting the connector's native payload.

## [2.0.0] — 2025-11-20

### Changed
- PDF rendering moved to PdfSharpCore (ADR 0001); the API and worker moved to .NET 10.
