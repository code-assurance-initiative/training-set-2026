# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses [Semantic Versioning](https://semver.org/).

## [1.0.0] - 2026-10-07

### Added

- Public pages: home, rooms with search and amenity filters, room pages with photos, tour video and building map,
  week calendar per room, conditions of hire.
- Booking for signed-in members: booking form with validation, booking summary dialog, a list of the member's bookings
  with cancellation and calendar-file download, reminder and display settings.
- Staff area (Razor Pages under `/Admin`): edit rooms and the site notice.
- OpenID Connect sign-in with member and staff policies; security response headers, HTTPS redirection and HSTS.
- JavaScript interop for the building map, native dialogs, the clipboard and calendar files.
- bUnit component tests, unit tests and in-memory integration tests; CI, CodeQL and Dependabot.
