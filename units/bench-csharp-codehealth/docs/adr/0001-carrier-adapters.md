# ADR 0001: One adapter per carrier behind ICarrierAdapter

- Status: accepted
- Date: 2026-05-04

## Context

Every carrier has its own REST API, product codes, wire formats and error conventions, and they change on their own
schedules. Quotes and labels must not depend on any one of them.

## Decision

Each carrier gets one adapter class implementing `ICarrierAdapter` (rates, label, void, tracking, pickup), using a
typed `HttpClient` configured from `Carriers:<Name>`. The adapter owns the carrier's wire types (for example
`AlderAddress`), even where they look like our own contracts today: they change for different reasons. Product
codes live in `ServiceCodeMap`. Capabilities a carrier lacks are declared in `CarrierCapabilities` and refused with
`NotSupportedException`.

## Consequences

Adding a carrier means one adapter, one capability declaration and its product codes. Partner adapters can be
loaded by type name, behind an experimental flag, until the certification suite covers them.
