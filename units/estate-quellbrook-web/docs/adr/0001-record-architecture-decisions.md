# 1. Record architecture decisions

- Status: Accepted
- Date: 2026-07-27
- Deciders: Teodor Vasko, Mara Ellingsworth, Ines Halvard

## Context

The console is the face of the platform for every operator and depends on the gateway's routes and the platform's
sign-in. Decisions about how it is built and served affect other teams.

## Decision

We record every decision that shapes the console's structure, its use of the platform or its delivery as an
architecture decision record in `docs/adr`, numbered, with status, context, decision and consequences.

## Consequences

- Positive: newcomers can see why the console is built the way it is.
- Negative: records take time and go stale unless superseded decisions are marked.
