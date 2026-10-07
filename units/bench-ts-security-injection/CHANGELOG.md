# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses [Semantic Versioning](https://semver.org/).

## [1.0.0] - 2026-10-07

### Added

- Document search by title with paging and sort orders, term and pattern highlighting, and an HTML search widget.
- Document details, cards rendered with Nunjucks (with ETags), attachments, report templates and thumbnails.
- Exports through LibreOffice with optional download passwords and an export audit log.
- Import previews by URL, metadata imports as XML or JSON, retention schedules, and partner transfer feeds.
- Reports: listings, schedules, previews with computed columns and per-preview layout settings, YAML layouts with
  cell formatters, and column mappings for partners.
- Saved searches with filter trees or filter expressions.
- Password-protected share links with an expiry and a periodic purge of expired links.
- Report subscriptions in MongoDB, delivery through the mail relay with CRM display names, public unsubscribe.
- Per-user preferences.
- Upload page for scanning stations (metadata preview, upload queue, recent recipients, view settings).
- JWT bearer authentication with scopes, security headers, HTTPS enforcement behind a trusted proxy.
