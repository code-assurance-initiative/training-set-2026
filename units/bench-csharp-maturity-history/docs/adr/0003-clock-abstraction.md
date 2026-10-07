# 3. A clock abstraction for testable time

Date: 2026-01-09
Status: Superseded by [ADR 0006](0006-timeprovider.md)

## Context

Slot search, cancellation fees and reminders all depend on the current time. Code that calls
`DateTimeOffset.UtcNow` directly cannot be tested for "two hours before the appointment" or "after the same-day
cut-off" without waiting for the wall clock.

## Decision

We introduce `IClock` (a single `UtcNow` property) in the domain and a `SystemClock` implementation in
infrastructure. Every class that needs the current time takes an `IClock`; tests pass a fixed clock.

## Consequences

- Time-dependent rules can be tested deterministically.
- We own a small abstraction that the framework does not know about, so framework components (timers, hosted
  services) still read the real clock.
