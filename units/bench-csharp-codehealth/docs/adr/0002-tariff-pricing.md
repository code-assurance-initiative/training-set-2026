# ADR 0002: Price from our tariff, not from the carrier's net rate

- Status: accepted
- Date: 2026-08-01

## Context

Carriers return net rates that change without notice and differ in what they include. Customers need prices that
are stable within a rate-card period and comparable across carriers.

## Decision

The customer price is computed from the carrier's published rate card (base fee, price per chargeable kilogram per
zone) with our service-level factors and the carrier's surcharges plus our markup. The carrier's rate response is
used for which services are available and their transit times. Rate cards are downloaded and cached; a quote fails
with 503 when no current card is available rather than guessing.

## Consequences

Prices are explainable line by line. When a carrier changes its tariff, we import the new card (`shipping-rates
import`) before the effective date.
