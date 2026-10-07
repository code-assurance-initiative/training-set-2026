# ADR 0001: A domain model in layers

- Status: accepted
- Date: 2026-10-07

## Context

Rates and labels involve real rules: tariff zones, surcharges per carrier, label batches that close, shipments that
are labelled, archived or voided. Carriers and storage change more often than those rules.

## Decision

`src/` has four layers. `domain` holds the aggregates (`entities`), value objects, domain events and the ports
(interfaces) the application needs; `application` holds the use cases (pricing, labels, accounts); `infrastructure`
implements the ports (carrier adapters, rate-card cache, label archive, mail pickup directory, printing, in-memory
persistence); `api` is the Express surface. Dependencies point inward: `domain` imports nothing from the other
layers, `application` only from `domain`. One composition root (`composition.ts`) wires them.

Shipments are kept in process memory for now; a database is a later decision.

## Consequences

- Pricing and label rules are unit-tested without HTTP or a network; adapters are tested against a stubbed `fetch`.
- The layering is a convention checked in review; it is not enforced by a lint rule yet.
