# ADR 0004 — Stay on FluentAssertions 7

- Status: accepted
- Date: 2025-02-24

## Context

FluentAssertions 8.0 (January 2025) moved to a commercial licence (Xceed) for commercial use. Version 7 remains
Apache-2.0 and its maintainers continue to publish fixes on the 7.x line. FluentAssertions is used only by our test
projects.

## Decision

Pin FluentAssertions to the newest 7.x release and take 7.x updates as they appear. Do not move to 8.x without a
licence decision.

## Consequences

- Package update reports will keep showing 8.x as "available"; that is expected and not a reason to upgrade.
- If the 7.x line stops receiving fixes, revisit (alternatives: Shouldly, AwesomeAssertions, xUnit asserts).
