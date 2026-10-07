# Changelog

All notable changes to this service are documented here. The format follows Keep a Changelog; versions follow
Semantic Versioning.

## [1.0.0] - 2026-10-07

### Added

- Signed browser uploads to S3-compatible storage, with a content-type allow-list.
- Five-minute download tokens scoped to one object.
- Upload fan-out: Ed25519-signed webhooks, push notifications, optional e-mail; Slack alerts on failure.
- Thumbnail jobs at the transcoding vendor.
- Nightly metered-usage reporting and Slack upload digest.
- Storage plan upgrades through a payment intent confirmed in the browser.
