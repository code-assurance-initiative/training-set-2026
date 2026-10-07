---
title: Record architecture decisions
status: accepted
enforcement: not-applicable
---

# 0001. Record architecture decisions

## Context

Decisions about FleetOps' structure were made in meetings and chat threads and then forgotten. New team members
could not tell a deliberate constraint from an accident.

## Decision

We record every architecture decision as a short Markdown file in `docs/adr/`, numbered in order, with the context,
the decision and its consequences. Each record states in front matter how it is enforced (`test`, `analyzer`,
`prose` for code review, or `not-applicable`).

## Consequences

- A decision can be found, linked from a pull request and superseded explicitly.
- This record is a process decision; there is nothing in the code to enforce.
