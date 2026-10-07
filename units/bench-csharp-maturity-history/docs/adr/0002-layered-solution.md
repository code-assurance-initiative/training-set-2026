# 2. A layered solution with a dependency-free domain

Date: 2026-01-06
Status: Accepted

## Context

The scheduling rules (opening hours, slots, cancellations) are the part of the system that
changes with the business and must be testable without a web server, a database or a clock. Hosting, persistence and
messaging change for different reasons.

## Decision

The solution has four production projects:

- `ClinicScheduling.Domain` - entities, value objects and scheduling rules. It references no other project and no
  package.
- `ClinicScheduling.Application` - use cases (booking an appointment, finding slots) and the ports they need, such
  as repositories. It references only the domain.
- `ClinicScheduling.Infrastructure` - implementations of the ports and everything else that talks to the outside
  world. It references the application.
- `ClinicScheduling.Api` - the ASP.NET Core host. It is the composition root and the only project that references
  everything.

## Consequences

- Domain rules are unit tested directly, with fakes for the holiday calendar and the clock.
- The compiler enforces the direction of the references; a domain type cannot reach infrastructure by accident.
- Some mapping code is needed at the edges (requests to commands, entities to responses).
