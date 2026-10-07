# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses [Semantic Versioning](https://semver.org/).

## [1.0.0] - 2026-10-07

### Added

- Membership: registration (minimum age 16), contact-detail changes, consent per purpose, cancellation, erasure and
  export of a member's personal data, and a retention job that erases members whose paid period ended more than the
  retention period ago.
- Classes: scheduling, booking with capacity and standing checks, upcoming bookings per member, and reminders by e-mail
  and SMS ahead of each booked class.
- Billing: an account per member opened from the registration message, monthly invoices with student and senior
  concessions, late-payment fees, overdue listing and payments; accounts anonymised when a member is erased.
- Field-level encryption of phone numbers and dates of birth, an audit log of personal-data changes, pseudonymised
  logging, and a transactional outbox between the contexts.
- PostgreSQL schema migrations; tests against an in-memory PostgreSQL emulation; CI, CodeQL and Dependabot.
