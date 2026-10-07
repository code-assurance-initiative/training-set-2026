# 6. Use TimeProvider instead of our own clock

Date: 2026-05-11
Status: Accepted (supersedes [ADR 0003](0003-clock-abstraction.md))

## Context

ADR 0003 introduced `IClock`. Since then .NET ships `TimeProvider`, which the framework itself understands: timers
(`PeriodicTimer`), hosted services and `Task.Delay` can all be driven by it, and `FakeTimeProvider` lets tests advance
time for those too. Our `IClock` could not do that, so the reminder worker's polling still ran on the real clock in
tests.

## Decision

Every class that needs the current time takes a `TimeProvider`. The application registers `TimeProvider.System`; tests
use `FakeTimeProvider`. `IClock` and `SystemClock` are removed. ADR 0003 is superseded.

## Consequences

- One time abstraction for our code and the framework's.
- The migration touches every time-dependent class once; afterwards there is nothing of our own to maintain.
