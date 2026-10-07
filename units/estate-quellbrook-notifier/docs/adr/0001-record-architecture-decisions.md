# 1. Record architecture decisions

- Status: Accepted
- Date: 2026-07-27
- Deciders: Lucia Brennan, Sami Oyelaran

## Context

The notifier talks to consignees on Quellbrook's behalf and handles their personal data; its decisions need to be
traceable for the team and for the privacy reviews.

## Decision

We record every decision that shapes the worker's structure, data or operation as an architecture decision record in
`docs/adr`, numbered, with status, context, decision and consequences.

## Consequences

- Positive: the reasons behind the design survive staff changes and can be shown in a review.
- Negative: records take time and must be kept current when a decision changes.
