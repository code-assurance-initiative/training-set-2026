# 5. What may cross the boundary between Lending and Billing

Date: 2026-10-07 · Status: accepted

## Context

ADR 0001 separates Lending and Billing and names the shared kernel. Both contexts need to talk about money, and
Billing needs to name the member and the loan a charge belongs to, so the question of which types may appear in the
other context's public surface keeps coming up in review.

## Decision

- **Shared kernel types cross freely.** `Money` and the entity/aggregate base types in `Rentals.SharedKernel` are
  owned by neither context; either context may expose them in its public types.
- **Integration contracts cross by design.** Types in `Rentals.Contracts` are the published language; a consumer's
  handler takes them as parameters.
- **A context's own types do not cross.** Lending's ids and value objects (`MemberId`, `LoanId`, `EquipmentId`, …)
  stay in Lending; Billing names members and loans by the primitive ids the contracts carry, wrapped in its own types
  (`MemberAccountId`, `LoanReference`).

## Consequences

Billing's public types reference `Rentals.SharedKernel` and `Rentals.Contracts`, never `Rentals.Lending.*`. A Lending
type in a Billing signature is a review finding.
