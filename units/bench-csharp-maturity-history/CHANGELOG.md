# Changelog

All notable changes to this project are documented in this file. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project follows
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.4.0] - 2026-06-26

### Added

- Appointments record how often they were rescheduled; rescheduling is limited to three moves.
- Cancellation for illness is free but counts as a strike.

### Changed

- Time comes from `TimeProvider` everywhere (ADR 0006); the reminder worker's polling is now testable.
- The no-show status is written `no-show` in responses and the portal feed.

### Removed

- The waitlist module and its endpoints. Back-fill offers were rarely accepted in the pilot clinics and the front desk
  fills cancelled slots by phone instead.

## [0.3.0] - 2026-04-30

### Added

- Telehealth (video) appointments and slots, with in-clinic-only opening hours.
- The patient-portal feed and the iCalendar subscription.
- Reduced cancellation fees for video appointments.

### Fixed

- iCalendar lines with Danish characters were folded in the middle of a character.

## [0.2.0] - 2026-03-31

### Added

- SMS reminders through a reminder outbox and a background worker (ADR 0005), in English, Danish and German.
- The waitlist and automatic back-fill of cancelled slots.
- Deposits after two strikes within six months.

### Fixed

- Slots were offered across a practitioner's lunch break.

## [0.1.0] - 2026-02-27

### Added

- Practitioner schedules, slot search with public holidays, booking, cancellation with late-cancellation fees.
- Treatment series from a weekly recurrence rule.
- JWT bearer authentication with scope policies, security headers and health checks.

[Unreleased]: https://github.com/code-assurance-initiative/bench-csharp-maturity-history/compare/v0.4.0...HEAD
[0.4.0]: https://github.com/code-assurance-initiative/bench-csharp-maturity-history/compare/v0.3.0...v0.4.0
[0.3.0]: https://github.com/code-assurance-initiative/bench-csharp-maturity-history/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/code-assurance-initiative/bench-csharp-maturity-history/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/code-assurance-initiative/bench-csharp-maturity-history/releases/tag/v0.1.0
